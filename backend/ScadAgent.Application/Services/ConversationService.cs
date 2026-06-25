using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScadAgent.Application.Diagnostics;
using ScadAgent.Application.Interfaces;
using ScadAgent.Application.Options;
using ScadAgent.Domain.Enums;

namespace ScadAgent.Application.Services;

public class ConversationService : IConversationService
{
    private readonly ISessionService _sessions;
    private readonly IContextManager _contextManager;
    private readonly IConversationContextPreparer _contextPreparer;
    private readonly IOllamaService _ollama;
    private readonly OllamaOptions _ollamaOptions;
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(
        ISessionService sessions,
        IContextManager contextManager,
        IConversationContextPreparer contextPreparer,
        IOllamaService ollama,
        IOptions<OllamaOptions> ollamaOptions,
        ILogger<ConversationService> logger)
    {
        _sessions = sessions;
        _contextManager = contextManager;
        _contextPreparer = contextPreparer;
        _ollama = ollama;
        _ollamaOptions = ollamaOptions.Value;
        _logger = logger;
    }

    public async Task ReplyAsync(Guid sessionId, string userContent, CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetSessionEntityAsync(sessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Session {sessionId} not found.");

        var currentScad = session.Iterations
            .Where(i => i.Status == IterationStatus.Succeeded)
            .OrderByDescending(i => i.Version)
            .FirstOrDefault()?.ScadContent;

        var preparedHistory = await _contextPreparer.PrepareAsync(sessionId, cancellationToken);

        var context = new ConversationContext(
            currentScad,
            userContent.Trim(),
            preparedHistory.Summary,
            preparedHistory.Messages);

        string answer;
        try
        {
            answer = await _ollama.ChatAsync(_contextManager.BuildConversationMessages(context), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama conversation failed for session {SessionId}", sessionId);
            var diagnostic = ex is OllamaRequestException ollamaEx
                ? AgentDiagnosticReport.FormatOllamaFailure(
                    "Conversation",
                    sessionId,
                    Guid.Empty,
                    new OllamaOptionsSnapshot(_ollamaOptions.BaseUrl, _ollamaOptions.Model),
                    ollamaEx)
                : AgentDiagnosticReport.Format(
                    "Conversation",
                    sessionId,
                    Guid.Empty,
                    _ollamaOptions.Model,
                    _ollamaOptions.BaseUrl,
                    ex);

            await _sessions.AddMessageAsync(
                sessionId,
                MessageRole.Assistant,
                diagnostic,
                cancellationToken: cancellationToken);
            return;
        }

        var trimmed = answer.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            trimmed = "I don't have an answer right now. Please try rephrasing your question.";

        await _sessions.AddMessageAsync(
            sessionId,
            MessageRole.Assistant,
            trimmed,
            cancellationToken: cancellationToken);
    }
}
