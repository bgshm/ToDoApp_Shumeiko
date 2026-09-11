using ToDoApp.Interfaces.Dtos;

namespace ToDoApp.Interfaces.Services;

public interface ITaskService
{
    Task<PagedResult<TaskDto>> GetPagedAsync(Guid userId, TaskQuery query, CancellationToken cancellationToken = default);

    Task<BoardDto> GetBoardAsync(Guid userId, BoardQuery query, CancellationToken cancellationToken = default);

    Task<TaskDto> GetByIdAsync(Guid userId, Guid taskId, CancellationToken cancellationToken = default);

    Task<TaskDto> CreateAsync(Guid userId, CreateTaskRequest request, CancellationToken cancellationToken = default);

    Task<TaskDto> UpdateAsync(Guid userId, Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid userId, Guid taskId, CancellationToken cancellationToken = default);

    Task<TaskDto> MoveAsync(Guid userId, Guid taskId, MoveTaskRequest request, CancellationToken cancellationToken = default);
}
