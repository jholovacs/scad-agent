using ScadAgent.Application.DTOs;

namespace ScadAgent.Application.Interfaces;

public interface IAgentNotifier
{
    Task NotifyIterationStartedAsync(AgentProgressDto progress, CancellationToken cancellationToken = default);
    Task NotifyIterationProgressAsync(AgentProgressDto progress, CancellationToken cancellationToken = default);
    Task NotifyIterationCompletedAsync(AgentProgressDto progress, CancellationToken cancellationToken = default);
    Task NotifyIterationFailedAsync(AgentProgressDto progress, CancellationToken cancellationToken = default);
}
