namespace ToDoApp.Interfaces.Services;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid UserId { get; }

    string? ConnectionId { get; }
}
