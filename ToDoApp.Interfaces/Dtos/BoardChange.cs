namespace ToDoApp.Interfaces.Dtos;

public enum BoardChangeKind
{
    TaskCreated,
    TaskUpdated,
    TaskMoved,
    TaskDeleted,
    CategoryCreated,
    CategoryUpdated,
    CategoryDeleted
}

public class BoardChange
{
    public BoardChangeKind Kind { get; init; }

    public Guid? TaskId { get; init; }

    public Guid? CategoryId { get; init; }

    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    public static BoardChange Task(BoardChangeKind kind, Guid taskId) => new() { Kind = kind, TaskId = taskId };

    public static BoardChange Category(BoardChangeKind kind, Guid categoryId) => new() { Kind = kind, CategoryId = categoryId };
}
