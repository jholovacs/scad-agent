using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScadAgent.Application.Interfaces;
using ScadAgent.Application.Options;

namespace ScadAgent.Infrastructure.Ollama;

public class OllamaHttpClient : IOllamaService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _options;
    private readonly ILogger<OllamaHttpClient> _logger;

    public OllamaHttpClient(HttpClient httpClient, IOptions<OllamaOptions> options, ILogger<OllamaHttpClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public async Task<string> ChatAsync(IReadOnlyList<OllamaMessage> messages, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            model = _options.Model,
            stream = false,
            messages = messages.Select(m => new { role = m.Role, content = m.Content })
        };

        string body;
        HttpStatusCode statusCode;

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("api/chat", payload, cancellationToken);
            statusCode = response.StatusCode;
            body = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Ollama chat timed out after {Timeout}s (model={Model}, url={Url})",
                _options.TimeoutSeconds, _options.Model, _options.BaseUrl);
            throw new OllamaRequestException(
                HttpStatusCode.RequestTimeout,
                string.Empty,
                _options.Model,
                _options.BaseUrl,
                $"Ollama request timed out after {_options.TimeoutSeconds}s.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Ollama chat network error (model={Model}, url={Url})", _options.Model, _options.BaseUrl);
            throw new OllamaRequestException(
                HttpStatusCode.ServiceUnavailable,
                ex.Message,
                _options.Model,
                _options.BaseUrl,
                $"Could not reach Ollama at {_options.BaseUrl}: {ex.Message}");
        }

        if (!responseIsSuccess(statusCode))
        {
            _logger.LogWarning(
                "Ollama chat failed: status={Status} model={Model} url={Url} body={Body}",
                (int)statusCode, _options.Model, _options.BaseUrl, body);
            throw new OllamaRequestException(
                statusCode,
                body,
                _options.Model,
                _options.BaseUrl,
                $"Ollama returned HTTP {(int)statusCode} ({statusCode}).");
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var content))
            {
                var text = content.GetString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogWarning("Ollama returned empty content. body={Body}", body);
                    throw new OllamaRequestException(
                        HttpStatusCode.OK,
                        body,
                        _options.Model,
                        _options.BaseUrl,
                        "Ollama returned an empty message content.");
                }

                _logger.LogDebug("Ollama chat succeeded (model={Model}, chars={Length})", _options.Model, text.Length);
                return text;
            }

            _logger.LogWarning("Unexpected Ollama response format. body={Body}", body);
            throw new OllamaRequestException(
                statusCode,
                body,
                _options.Model,
                _options.BaseUrl,
                "Unexpected Ollama response format (missing message.content).");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse Ollama JSON. body={Body}", body);
            throw new OllamaRequestException(
                statusCode,
                body,
                _options.Model,
                _options.BaseUrl,
                $"Failed to parse Ollama JSON response: {ex.Message}");
        }
    }

    public async Task<bool> IsReachableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync("", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Ollama health check failed");
            return false;
        }
    }

    private static bool responseIsSuccess(HttpStatusCode statusCode) =>
        (int)statusCode is >= 200 and <= 299;
}
