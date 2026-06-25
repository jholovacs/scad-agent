namespace ScadAgent.Application.Interfaces;

public interface ISessionMessageService
{
    Task HandleAsync(Guid sessionId, string content, CancellationToken cancellationToken = default);
}
