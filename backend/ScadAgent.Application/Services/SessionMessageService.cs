using Microsoft.Extensions.Logging;
using ScadAgent.Application.Interfaces;
using ScadAgent.Domain.Enums;

namespace ScadAgent.Application.Services;

public class SessionMessageService : ISessionMessageService
{
    private readonly ISessionService _sessions;
    private readonly IMessageIntentClassifier _classifier;
    private readonly IConversationService _conversation;
    private readonly IDesignAgentService _agent;
    private readonly ILogger<SessionMessageService> _logger;

    public SessionMessageService(
        ISessionService sessions,
        IMessageIntentClassifier classifier,
        IConversationService conversation,
        IDesignAgentService agent,
        ILogger<SessionMessageService> logger)
    {
        _sessions = sessions;
        _classifier = classifier;
        _conversation = conversation;
        _agent = agent;
        _logger = logger;
    }

    public async Task HandleAsync(Guid sessionId, string content, CancellationToken cancellationToken = default)
    {
        var trimmed = content.Trim();
        var session = await _sessions.GetSessionEntityAsync(sessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Session {sessionId} not found.");

        var hasDesign = session.Iterations.Any(i => i.Status == IterationStatus.Succeeded);
        var intent = await _classifier.ClassifyAsync(trimmed, hasDesign, cancellationToken);

        _logger.LogInformation(
            "Classified session {SessionId} message as {Intent}",
            sessionId,
            intent);

        await _sessions.AddUserMessageAsync(sessionId, trimmed, intent, cancellationToken);

        if (intent == MessageIntent.Ask)
            await _conversation.ReplyAsync(sessionId, trimmed, cancellationToken);
        else
            await _agent.RunIterationAsync(sessionId, cancellationToken);
    }
}
