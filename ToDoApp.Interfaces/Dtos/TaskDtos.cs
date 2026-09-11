using System.ComponentModel.DataAnnotations;
using ToDoApp.Interfaces.Entities;

namespace ToDoApp.Interfaces.Dtos;

public class TaskDto
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public TaskState State { get; init; }

    public int Position { get; init; }

    public Guid? CategoryId { get; init; }

    public string? CategoryName { get; init; }

    public string? CategoryColor { get; init; }

    public DateTimeOffset? DueDate { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}

public class CreateTaskRequest
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "A title is required.")]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public TaskState State { get; set; } = TaskState.ToDo;

    public Guid? CategoryId { get; set; }

    public DateTimeOffset? DueDate { get; set; }
}

public class UpdateTaskRequest
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "A title is required.")]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public TaskState State { get; set; }

    public Guid? CategoryId { get; set; }

    public DateTimeOffset? DueDate { get; set; }
}
