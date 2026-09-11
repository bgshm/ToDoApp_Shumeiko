using ToDoApp.Interfaces.Dtos;

namespace ToDoApp.Interfaces.Services;

public interface IBoardNotifier
{
    Task NotifyAsync(Guid userId, BoardChange change, CancellationToken cancellationToken = default);
}
