using Microsoft.AspNetCore.SignalR;
using ToDoApp.Interfaces.Dtos;
using ToDoApp.Interfaces.Services;
using ToDoApp.Server.Hubs;

namespace ToDoApp.Server.Infrastructure;

public class SignalRBoardNotifier : IBoardNotifier
{
    public const string ClientMethod = "boardChanged";

    private readonly IHubContext<BoardHub> _hub;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<SignalRBoardNotifier> _logger;

    public SignalRBoardNotifier(
        IHubContext<BoardHub> hub,
        ICurrentUser currentUser,
        ILogger<SignalRBoardNotifier> logger)
    {
        _hub = hub;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task NotifyAsync(Guid userId, BoardChange change, CancellationToken cancellationToken = default)
    {
        var group = BoardHub.GroupFor(userId);
        var originating = _currentUser.ConnectionId;

        try
        {
            var clients = string.IsNullOrEmpty(originating)
                ? _hub.Clients.Group(group)
                : _hub.Clients.GroupExcept(group, originating);

            await clients.SendAsync(ClientMethod, change, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not broadcast {Kind} to {Group}.", change.Kind, group);
        }
    }
}
