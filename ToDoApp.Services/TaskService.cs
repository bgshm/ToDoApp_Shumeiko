using ToDoApp.Interfaces.Dtos;
using ToDoApp.Interfaces.Entities;
using ToDoApp.Interfaces.Exceptions;
using ToDoApp.Interfaces.Repositories;
using ToDoApp.Interfaces.Services;
using ToDoApp.Services.Mapping;

namespace ToDoApp.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _tasks;
    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBoardNotifier _notifier;

    public TaskService(
        ITaskRepository tasks,
        ICategoryRepository categories,
        IUnitOfWork unitOfWork,
        IBoardNotifier notifier)
    {
        _tasks = tasks;
        _categories = categories;
        _unitOfWork = unitOfWork;
        _notifier = notifier;
    }

    public async Task<PagedResult<TaskDto>> GetPagedAsync(
        Guid userId,
        TaskQuery query,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _tasks.GetPagedAsync(userId, query, cancellationToken);
        return PagedResult<TaskDto>.Create(items.ToDtos(), query.Page, query.PageSize, total);
    }

    public async Task<BoardDto> GetBoardAsync(
        Guid userId,
        BoardQuery query,
        CancellationToken cancellationToken = default)
    {
        var toDo = await GetPagedAsync(userId, query.ToTaskQuery(TaskState.ToDo), cancellationToken);
        var doing = await GetPagedAsync(userId, query.ToTaskQuery(TaskState.Doing), cancellationToken);
        var done = await GetPagedAsync(userId, query.ToTaskQuery(TaskState.Done), cancellationToken);

        return new BoardDto { ToDo = toDo, Doing = doing, Done = done };
    }

    public async Task<TaskDto> GetByIdAsync(Guid userId, Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await _tasks.GetForUserAsync(taskId, userId, cancellationToken)
                   ?? throw NotFoundException.For("Task", taskId);

        return task.ToDto();
    }

    public async Task<TaskDto> CreateAsync(
        Guid userId,
        CreateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureCategoryBelongsToUserAsync(userId, request.CategoryId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title.Trim(),
            Description = Normalise(request.Description),
            State = request.State,
            CategoryId = request.CategoryId,
            DueDate = request.DueDate,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var column = await _tasks.GetColumnForUpdateAsync(userId, task.State, ct);
            await _tasks.AddAsync(task, ct);

            column.Insert(0, task);
            Renumber(column);

            await _unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);

        await _notifier.NotifyAsync(userId, BoardChange.Task(BoardChangeKind.TaskCreated, task.Id), cancellationToken);

        return await GetByIdAsync(userId, task.Id, cancellationToken);
    }

    public async Task<TaskDto> UpdateAsync(
        Guid userId,
        Guid taskId,
        UpdateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var task = await _tasks.GetForUserAsync(taskId, userId, cancellationToken)
                   ?? throw NotFoundException.For("Task", taskId);

        await EnsureCategoryBelongsToUserAsync(userId, request.CategoryId, cancellationToken);

        var previousState = task.State;

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            task.Title = request.Title.Trim();
            task.Description = Normalise(request.Description);
            task.CategoryId = request.CategoryId;
            task.DueDate = request.DueDate;
            task.UpdatedAt = DateTimeOffset.UtcNow;

            if (request.State != previousState)
            {
                await RelocateAsync(userId, task, previousState, request.State, targetIndex: 0, ct);
            }
            else
            {
                _tasks.Update(task);
            }

            await _unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);

        await _notifier.NotifyAsync(userId, BoardChange.Task(BoardChangeKind.TaskUpdated, task.Id), cancellationToken);

        return await GetByIdAsync(userId, task.Id, cancellationToken);
    }

    public async Task DeleteAsync(Guid userId, Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await _tasks.GetForUserAsync(taskId, userId, cancellationToken)
                   ?? throw NotFoundException.For("Task", taskId);

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var column = await _tasks.GetColumnForUpdateAsync(userId, task.State, ct);

            _tasks.Remove(task);

            column.RemoveAll(t => t.Id == task.Id);
            Renumber(column);

            await _unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);

        await _notifier.NotifyAsync(userId, BoardChange.Task(BoardChangeKind.TaskDeleted, taskId), cancellationToken);
    }

    public async Task<TaskDto> MoveAsync(
        Guid userId,
        Guid taskId,
        MoveTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var task = await _tasks.GetForUserAsync(taskId, userId, cancellationToken)
                   ?? throw NotFoundException.For("Task", taskId);

        var sourceState = task.State;

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await RelocateAsync(userId, task, sourceState, request.TargetState, request.TargetIndex, ct);
            task.UpdatedAt = DateTimeOffset.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);

        await _notifier.NotifyAsync(userId, BoardChange.Task(BoardChangeKind.TaskMoved, task.Id), cancellationToken);

        return await GetByIdAsync(userId, task.Id, cancellationToken);
    }

    private async Task RelocateAsync(
        Guid userId,
        TaskItem task,
        TaskState sourceState,
        TaskState targetState,
        int targetIndex,
        CancellationToken cancellationToken)
    {
        var source = await _tasks.GetColumnForUpdateAsync(userId, sourceState, cancellationToken);
        source.RemoveAll(t => t.Id == task.Id);

        if (sourceState == targetState)
        {
            source.Insert(Math.Clamp(targetIndex, 0, source.Count), task);
            Renumber(source);
            return;
        }

        Renumber(source);

        var target = await _tasks.GetColumnForUpdateAsync(userId, targetState, cancellationToken);
        task.State = targetState;
        target.Insert(Math.Clamp(targetIndex, 0, target.Count), task);
        Renumber(target);
    }

    private static void Renumber(IList<TaskItem> column)
    {
        for (var index = 0; index < column.Count; index++)
        {
            column[index].Position = index;
        }
    }

    private async Task EnsureCategoryBelongsToUserAsync(
        Guid userId,
        Guid? categoryId,
        CancellationToken cancellationToken)
    {
        if (!categoryId.HasValue)
        {
            return;
        }

        if (await _categories.GetForUserAsync(categoryId.Value, userId, cancellationToken) is null)
        {
            throw new BusinessRuleException("The selected category does not exist.");
        }
    }

    private static string? Normalise(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
