namespace ScadAgent.Application.Interfaces;

public interface IConversationService
{
    Task ReplyAsync(Guid sessionId, string userContent, CancellationToken cancellationToken = default);
}
