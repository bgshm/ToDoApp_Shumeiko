using ToDoApp.Interfaces.Dtos;
using ToDoApp.Interfaces.Entities;

namespace ToDoApp.Interfaces.Repositories;

public interface ITaskRepository : IRepository<TaskItem>
{
    Task<TaskItem?> GetForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<TaskItem> Items, int TotalCount)> GetPagedAsync(
        Guid userId,
        TaskQuery query,
        CancellationToken cancellationToken = default);

    Task<List<TaskItem>> GetColumnForUpdateAsync(
        Guid userId,
        TaskState state,
        CancellationToken cancellationToken = default);

    Task<int> CountInColumnAsync(Guid userId, TaskState state, CancellationToken cancellationToken = default);

    Task<int> ClearCategoryAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken = default);
}
