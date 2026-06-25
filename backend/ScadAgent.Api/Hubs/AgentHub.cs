using Microsoft.AspNetCore.SignalR;

namespace ScadAgent.Api.Hubs;

public class AgentHub : Hub
{
    public async Task JoinSession(string sessionId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);

    public async Task LeaveSession(string sessionId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
}
