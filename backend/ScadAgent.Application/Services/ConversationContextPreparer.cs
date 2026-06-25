using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScadAgent.Application.Interfaces;
using ScadAgent.Application.Options;
using ScadAgent.Domain.Entities;
using ScadAgent.Domain.Enums;

namespace ScadAgent.Application.Services;

public class ConversationContextPreparer : IConversationContextPreparer
{
    private readonly ISessionService _sessions;
    private readonly IOllamaService _ollama;
    private readonly AgentOptions _options;
    private readonly ILogger<ConversationContextPreparer> _logger;

    public ConversationContextPreparer(
        ISessionService sessions,
        IOllamaService ollama,
        IOptions<AgentOptions> options,
        ILogger<ConversationContextPreparer> logger)
    {
        _sessions = sessions;
        _ollama = ollama;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PreparedConversationHistory> PrepareAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetSessionEntityAsync(sessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Session {sessionId} not found.");

        var ordered = session.Messages
            .OrderBy(m => m.CreatedAt)
            .ToList();

        var summary = session.ContextSummary;
        var watermarkId = session.ContextSummarizedThroughMessageId;
        var verbatim = SliceMessagesAfterWatermark(ordered, watermarkId);
        var estimatedSize = EstimateSize(summary, verbatim);

        while (estimatedSize > _options.ContextCompressionThresholdChars
               && verbatim.Count > _options.ContextKeepRecentMessages)
        {
            var toCompress = verbatim
                .Take(verbatim.Count - _options.ContextKeepRecentMessages)
                .ToList();

            _logger.LogInformation(
                "Compressing {Count} messages into session memory for {SessionId}",
                toCompress.Count,
                sessionId);

            summary = await SummarizeAsync(summary, toCompress, cancellationToken);
            watermarkId = toCompress[^1].Id;
            session.ContextSummary = summary;
            session.ContextSummarizedThroughMessageId = watermarkId;
            session.UpdatedAt = DateTimeOffset.UtcNow;
            await _sessions.UpdateSessionAsync(session, cancellationToken);

            verbatim = SliceMessagesAfterWatermark(ordered, watermarkId);
            estimatedSize = EstimateSize(summary, verbatim);
        }

        return new PreparedConversationHistory(
            summary,
            verbatim.Select(MapMessage).ToList());
    }

    private async Task<string> SummarizeAsync(
        string? existingSummary,
        IReadOnlyList<ConversationMessage> messages,
        CancellationToken cancellationToken)
    {
        var transcript = FormatTranscript(messages);
        var prompt = new StringBuilder();
        prompt.AppendLine("Merge the existing session memory with the new transcript excerpt.");
        prompt.AppendLine("Preserve user goals, dimensions/parameters, design decisions, iteration outcomes, failures, and important Q&A.");
        prompt.AppendLine("Write concise durable memory the assistant can rely on later. Output only the updated memory.");
        prompt.AppendLine();
        prompt.AppendLine("--- Existing memory ---");
        prompt.AppendLine(string.IsNullOrWhiteSpace(existingSummary) ? "(none)" : existingSummary.Trim());
        prompt.AppendLine();
        prompt.AppendLine("--- New transcript ---");
        prompt.AppendLine(transcript);

        var summary = await _ollama.ChatAsync(
        [
            new OllamaMessage(
                "system",
                "You compress OpenSCAD design-session chat history into durable project memory."),
            new OllamaMessage("user", prompt.ToString())
        ],
        cancellationToken);

        var trimmed = summary.Trim();
        return string.IsNullOrWhiteSpace(trimmed)
            ? existingSummary ?? transcript
            : trimmed;
    }

    private static List<ConversationMessage> SliceMessagesAfterWatermark(
        IReadOnlyList<ConversationMessage> ordered,
        Guid? watermarkId)
    {
        if (watermarkId is null)
            return ordered.ToList();

        var watermarkIndex = ordered.ToList().FindIndex(m => m.Id == watermarkId);
        if (watermarkIndex < 0)
            return ordered.ToList();

        return ordered.Skip(watermarkIndex + 1).ToList();
    }

    private static int EstimateSize(string? summary, IReadOnlyList<ConversationMessage> messages) =>
        (summary?.Length ?? 0) + messages.Sum(m => m.Content.Length);

    private static string FormatTranscript(IReadOnlyList<ConversationMessage> messages) =>
        string.Join(
            "\n\n",
            messages.Select(m =>
            {
                var label = m.Role switch
                {
                    MessageRole.User when m.Intent == MessageIntent.Ask => "question",
                    MessageRole.User => "user",
                    MessageRole.Assistant => "assistant",
                    _ => "system"
                };
                return $"[{label}] {m.Content}";
            }));

    private static ConversationMessageContext MapMessage(ConversationMessage message) =>
        new(
            message.Role switch
            {
                MessageRole.User => "user",
                MessageRole.Assistant => "assistant",
                _ => "system"
            },
            message.Content);
}
