namespace ToDoApp.Interfaces.Entities;

public class TaskItem
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TaskState State { get; set; }

    public int Position { get; set; }

    public Guid? CategoryId { get; set; }

    public Category? Category { get; set; }

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public DateTimeOffset? DueDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
