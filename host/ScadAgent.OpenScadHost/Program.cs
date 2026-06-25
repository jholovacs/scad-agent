using System.Diagnostics;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

var executable = builder.Configuration["OPENSCAD_EXECUTABLE"]
    ?? builder.Configuration["OpenScadExecutablePath"]
    ?? Environment.GetEnvironmentVariable("OPENSCAD_EXECUTABLE")
    ?? "openscad";

var timeoutSeconds = int.TryParse(builder.Configuration["OpenScadTimeoutSeconds"], out var parsedTimeout)
    ? parsedTimeout
    : 60;

app.MapGet("/api/health", () => Results.Ok(new HealthResponse(IsOpenScadAvailable(executable))));

app.MapPost("/api/render", async (RenderRequest request, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.ScadContent))
        return Results.BadRequest(new RenderResponse(false, null, null, "SCAD content is required."));

    var workDir = Path.Combine(Path.GetTempPath(), "scad-agent-host", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(workDir);

    try
    {
        var scadPath = Path.Combine(workDir, "model.scad");
        var stlPath = Path.Combine(workDir, "model.stl");
        var previewPath = Path.Combine(workDir, "preview.png");

        await File.WriteAllTextAsync(scadPath, request.ScadContent, cancellationToken);

        var stlResult = await RunOpenScadAsync(executable, scadPath, stlPath, timeoutSeconds, cancellationToken);
        if (!stlResult.Success)
            return Results.Ok(new RenderResponse(false, null, null, stlResult.Error));

        var previewResult = await RunOpenScadAsync(executable, scadPath, previewPath, timeoutSeconds, cancellationToken);
        var previewBase64 = previewResult.Success && File.Exists(previewPath)
            ? Convert.ToBase64String(await File.ReadAllBytesAsync(previewPath, cancellationToken))
            : null;

        var stlBase64 = Convert.ToBase64String(await File.ReadAllBytesAsync(stlPath, cancellationToken));
        return Results.Ok(new RenderResponse(true, stlBase64, previewBase64, null));
    }
    finally
    {
        try
        {
            if (Directory.Exists(workDir))
                Directory.Delete(workDir, recursive: true);
        }
        catch
        {
            // ignore cleanup failures
        }
    }
});

var port = builder.Configuration["PORT"] ?? "9333";
app.Run($"http://0.0.0.0:{port}");

static bool IsOpenScadAvailable(string executable)
{
    try
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = executable,
            Arguments = "--version",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        return process?.WaitForExit(5000) == true && process.ExitCode == 0;
    }
    catch
    {
        return false;
    }
}

static async Task<(bool Success, string? Error)> RunOpenScadAsync(
    string executable,
    string scadPath,
    string outputPath,
    int timeoutSeconds,
    CancellationToken cancellationToken)
{
    var startInfo = new ProcessStartInfo
    {
        FileName = executable,
        Arguments = $"-o \"{outputPath}\" \"{scadPath}\"",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using var process = new Process { StartInfo = startInfo };

    try
    {
        if (!process.Start())
            return (false, "Failed to start OpenSCAD process.");

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);

        var completed = await Task.WhenAny(
            Task.WhenAll(stdoutTask, stderrTask, process.WaitForExitAsync(cancellationToken)),
            Task.Delay(timeout, cancellationToken));

        if (completed is not Task t || !t.IsCompletedSuccessfully || !process.HasExited)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
            return (false, $"OpenSCAD timed out after {timeoutSeconds}s.");
        }

        var stderr = await stderrTask;
        if (process.ExitCode != 0 || !File.Exists(outputPath))
            return (false, string.IsNullOrWhiteSpace(stderr) ? "OpenSCAD failed." : stderr);

        return (true, null);
    }
    catch (Exception ex)
    {
        return (false, ex.Message);
    }
}

record RenderRequest(string ScadContent);
record RenderResponse(bool Success, string? StlBase64, string? PreviewBase64, string? Error);
record HealthResponse(bool OpenScadAvailable);
