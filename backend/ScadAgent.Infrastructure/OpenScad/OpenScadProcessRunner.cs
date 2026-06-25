using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScadAgent.Application.Interfaces;
using ScadAgent.Application.Options;
using ScadAgent.Domain.ValueObjects;

namespace ScadAgent.Infrastructure.OpenScad;

public class OpenScadProcessRunner : IOpenScadService
{
    private readonly AgentOptions _options;
    private readonly ILogger<OpenScadProcessRunner> _logger;

    public OpenScadProcessRunner(IOptions<AgentOptions> options, ILogger<OpenScadProcessRunner> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    private string ExecutablePath => string.IsNullOrWhiteSpace(_options.OpenScadExecutablePath)
        ? "openscad"
        : _options.OpenScadExecutablePath;

    private static bool LooksLikeWindowsPath(string path) =>
        path.Contains('\\') || (path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':');

    private string? ValidateExecutablePath()
    {
        if (!OperatingSystem.IsLinux() || !LooksLikeWindowsPath(ExecutablePath))
            return null;

        return
            "OpenSCAD is configured with a Windows executable path, but the API is running in Linux Docker. " +
            "Either use the OpenSCAD installed in the container (set Agent__OpenScadExecutablePath=openscad), " +
            "or run the host OpenSCAD service (make openscad-host) and set OPENSCAD_REMOTE_URL " +
            "to http://host.docker.internal:9333.";
    }

    public bool IsAvailable()
    {
        var validationError = ValidateExecutablePath();
        if (validationError is not null)
            return false;

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = ExecutablePath,
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

    public async Task<RenderResult> RenderAsync(string scadContent, string outputDirectory, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(outputDirectory);
        var scadPath = Path.Combine(outputDirectory, "model.scad");
        var stlPath = Path.Combine(outputDirectory, "model.stl");
        var previewPath = Path.Combine(outputDirectory, "preview.png");

        await File.WriteAllTextAsync(scadPath, scadContent, cancellationToken);

        var validationError = ValidateExecutablePath();
        if (validationError is not null)
            return RenderResult.Failed(validationError, TimeSpan.Zero);

        var stopwatch = Stopwatch.StartNew();
        var stlResult = await RunOpenScadAsync(scadPath, stlPath, cancellationToken);
        stopwatch.Stop();

        if (!stlResult.Success)
            return RenderResult.Failed(stlResult.StdErr ?? "OpenSCAD failed.", stopwatch.Elapsed, stlResult.Issues);

        stopwatch.Restart();
        var previewResult = await RunOpenScadAsync(scadPath, previewPath, cancellationToken, exportFormat: "png");
        stopwatch.Stop();

        if (!previewResult.Success)
        {
            _logger.LogWarning("Preview render failed, continuing with STL only: {Error}", previewResult.StdErr);
            return RenderResult.Succeeded(stlPath, null, stopwatch.Elapsed, previewResult.StdOut);
        }

        return RenderResult.Succeeded(stlPath, previewPath, stopwatch.Elapsed);
    }

    private async Task<RenderResult> RunOpenScadAsync(
        string scadPath,
        string outputPath,
        CancellationToken cancellationToken,
        string exportFormat = "stl")
    {
        var arguments = $"-o \"{outputPath}\" \"{scadPath}\"";
        var startInfo = new ProcessStartInfo
        {
            FileName = ExecutablePath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
                return RenderResult.Failed("Failed to start OpenSCAD process.", TimeSpan.Zero);

            using var registration = cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill(entireProcessTree: true);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to kill OpenSCAD process");
                }
            });

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            var timeout = TimeSpan.FromSeconds(_options.OpenScadTimeoutSeconds);

            var completed = await Task.WhenAny(
                Task.WhenAll(stdoutTask, stderrTask, process.WaitForExitAsync(cancellationToken)),
                Task.Delay(timeout, cancellationToken));

            if (completed is not Task t || !t.IsCompletedSuccessfully || !process.HasExited)
            {
                try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
                return RenderResult.Failed($"OpenSCAD timed out after {_options.OpenScadTimeoutSeconds}s.", timeout);
            }

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (process.ExitCode != 0 || !File.Exists(outputPath))
                return RenderResult.Failed(stderr, TimeSpan.Zero, RenderResult.ParseIssues(stderr));

            return RenderResult.Succeeded(outputPath, null, TimeSpan.Zero, stdout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenSCAD render error");
            return RenderResult.Failed(ex.Message, TimeSpan.Zero);
        }
    }
}
