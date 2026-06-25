namespace ScadAgent.Application.Interfaces;

public interface IDesignAgentService
{
    Task<Guid> RunIterationAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
