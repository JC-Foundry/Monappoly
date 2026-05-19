using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using UltimateMonopoly.Areas.Social.Services;

namespace UltimateMonopoly.Areas.Social.Hubs;

[Authorize]
public class PresenceHub : Hub
{
    private readonly PresenceService _presence;

    public PresenceHub(PresenceService presence)
    {
        _presence = presence;
    }

    public override Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
            _presence.AddConnection(userId, Context.ConnectionId);

        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
            _presence.RemoveConnection(userId, Context.ConnectionId);

        return base.OnDisconnectedAsync(exception);
    }

    public Task Ping()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
            _presence.MarkActive(userId);

        return Task.CompletedTask;
    }
}