using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ToDoApp.Server.Hubs;

[Authorize]
public class BoardHub : Hub
{
    public const string Route = "/hubs/board";

    public static string GroupFor(Guid userId) => $"user:{userId}";

    public override async Task OnConnectedAsync()
    {
        if (TryGetUserId(out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(userId));
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (TryGetUserId(out var userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupFor(userId));
        }

        await base.OnDisconnectedAsync(exception);
    }

    private bool TryGetUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var subject = Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return subject is not null && Guid.TryParse(subject, out userId);
    }
}
