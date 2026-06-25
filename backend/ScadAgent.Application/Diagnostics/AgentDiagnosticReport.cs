using System.Text;
using ScadAgent.Application.Interfaces;

namespace ScadAgent.Application.Diagnostics;

public static class AgentDiagnosticReport
{
    public static string Format(
        string phase,
        Guid sessionId,
        Guid iterationId,
        string model,
        string ollamaBaseUrl,
        Exception exception,
        string? responseBody = null,
        int? httpStatusCode = null,
        string? llmResponsePreview = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== SCAD Agent Diagnostic Report ===");
        sb.AppendLine($"Time (UTC): {DateTimeOffset.UtcNow:O}");
        sb.AppendLine($"Phase: {phase}");
        sb.AppendLine($"Session: {sessionId}");
        sb.AppendLine($"Iteration: {iterationId}");
        sb.AppendLine($"Ollama URL: {ollamaBaseUrl}");
        sb.AppendLine($"Model: {model}");
        if (httpStatusCode.HasValue)
            sb.AppendLine($"HTTP status: {httpStatusCode.Value}");
        sb.AppendLine();
        sb.AppendLine("--- Error ---");
        sb.AppendLine(exception.Message);
        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            sb.AppendLine();
            sb.AppendLine("--- Ollama response body ---");
            sb.AppendLine(Truncate(responseBody, 8000));
        }
        if (!string.IsNullOrWhiteSpace(llmResponsePreview))
        {
            sb.AppendLine();
            sb.AppendLine("--- LLM response preview ---");
            sb.AppendLine(Truncate(llmResponsePreview, 4000));
        }
        if (exception.InnerException is not null)
        {
            sb.AppendLine();
            sb.AppendLine("--- Inner exception ---");
            sb.AppendLine(exception.InnerException.Message);
        }
        sb.AppendLine();
        sb.AppendLine("=== End report ===");
        return sb.ToString();
    }

    public static string FormatOllamaFailure(
        string phase,
        Guid sessionId,
        Guid iterationId,
        OllamaOptionsSnapshot options,
        OllamaRequestException exception)
    {
        return Format(
            phase,
            sessionId,
            iterationId,
            options.Model,
            options.BaseUrl,
            exception,
            exception.ResponseBody,
            (int)exception.StatusCode);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "\n... (truncated)";
}

public record OllamaOptionsSnapshot(string BaseUrl, string Model);
