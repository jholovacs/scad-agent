using Microsoft.AspNetCore.SignalR;
using ScadAgent.Api.Hubs;
using ScadAgent.Application.DTOs;
using ScadAgent.Application.Interfaces;

namespace ScadAgent.Api.Services;

public class SignalRAgentNotifier : IAgentNotifier
{
    private readonly IHubContext<AgentHub> _hub;

    public SignalRAgentNotifier(IHubContext<AgentHub> hub)
    {
        _hub = hub;
    }

    public Task NotifyIterationStartedAsync(AgentProgressDto progress, CancellationToken cancellationToken = default) =>
        _hub.Clients.Group(progress.SessionId.ToString()).SendAsync("IterationStarted", progress, cancellationToken);

    public Task NotifyIterationProgressAsync(AgentProgressDto progress, CancellationToken cancellationToken = default) =>
        _hub.Clients.Group(progress.SessionId.ToString()).SendAsync("IterationProgress", progress, cancellationToken);

    public Task NotifyIterationCompletedAsync(AgentProgressDto progress, CancellationToken cancellationToken = default) =>
        _hub.Clients.Group(progress.SessionId.ToString()).SendAsync("IterationCompleted", progress, cancellationToken);

    public Task NotifyIterationFailedAsync(AgentProgressDto progress, CancellationToken cancellationToken = default) =>
        _hub.Clients.Group(progress.SessionId.ToString()).SendAsync("IterationFailed", progress, cancellationToken);
}
