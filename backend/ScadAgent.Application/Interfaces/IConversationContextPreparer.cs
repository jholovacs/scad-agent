namespace ScadAgent.Application.Interfaces;

public interface IConversationContextPreparer
{
    Task<PreparedConversationHistory> PrepareAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);
}

public record PreparedConversationHistory(
    string? Summary,
    IReadOnlyList<ConversationMessageContext> Messages);
