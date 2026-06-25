using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScadAgent.Application.Interfaces;
using ScadAgent.Application.Options;
using ScadAgent.Domain.ValueObjects;

namespace ScadAgent.Infrastructure.OpenScad;

public class OpenScadRemoteClient : IOpenScadService
{
    private readonly HttpClient _http;
    private readonly AgentOptions _options;
    private readonly ILogger<OpenScadRemoteClient> _logger;

    public OpenScadRemoteClient(
        HttpClient http,
        IOptions<AgentOptions> options,
        ILogger<OpenScadRemoteClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsAvailable()
    {
        try
        {
            var response = _http.GetAsync($"{RemoteBaseUrl}/api/health").GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
                return false;

            var health = response.Content.ReadFromJsonAsync<RemoteHealthResponse>().GetAwaiter().GetResult();
            return health?.OpenScadAvailable == true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Remote OpenSCAD health check failed");
            return false;
        }
    }

    public async Task<RenderResult> RenderAsync(
        string scadContent,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(outputDirectory);
        var stlPath = Path.Combine(outputDirectory, "model.stl");
        var previewPath = Path.Combine(outputDirectory, "preview.png");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        using var response = await _http.PostAsJsonAsync(
            $"{RemoteBaseUrl}/api/render",
            new RemoteRenderRequest(scadContent),
            cancellationToken);
        stopwatch.Stop();

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return RenderResult.Failed(
                $"Remote OpenSCAD returned {(int)response.StatusCode}: {body}",
                stopwatch.Elapsed);
        }

        var payload = await response.Content.ReadFromJsonAsync<RemoteRenderResponse>(cancellationToken);
        if (payload is null)
            return RenderResult.Failed("Remote OpenSCAD returned an empty response.", stopwatch.Elapsed);

        if (!payload.Success || string.IsNullOrWhiteSpace(payload.StlBase64))
            return RenderResult.Failed(payload.Error ?? "Remote OpenSCAD render failed.", stopwatch.Elapsed);

        await File.WriteAllBytesAsync(stlPath, Convert.FromBase64String(payload.StlBase64), cancellationToken);

        string? savedPreviewPath = null;
        if (!string.IsNullOrWhiteSpace(payload.PreviewBase64))
        {
            await File.WriteAllBytesAsync(previewPath, Convert.FromBase64String(payload.PreviewBase64), cancellationToken);
            savedPreviewPath = previewPath;
        }

        return RenderResult.Succeeded(stlPath, savedPreviewPath, stopwatch.Elapsed);
    }

    private string RemoteBaseUrl => (_options.OpenScadRemoteUrl ?? string.Empty).TrimEnd('/');

    private sealed record RemoteRenderRequest(string ScadContent);

    private sealed record RemoteRenderResponse(
        bool Success,
        string? StlBase64,
        string? PreviewBase64,
        string? Error);

    private sealed record RemoteHealthResponse(
        [property: JsonPropertyName("openScadAvailable")] bool OpenScadAvailable);
}
