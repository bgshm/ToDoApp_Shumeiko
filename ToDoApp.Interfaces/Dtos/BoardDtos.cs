using System.ComponentModel.DataAnnotations;
using ToDoApp.Interfaces.Entities;

namespace ToDoApp.Interfaces.Dtos;

public class MoveTaskRequest
{
    public TaskState TargetState { get; set; }

    [Range(0, int.MaxValue)]
    public int TargetIndex { get; set; }
}

public class BoardDto
{
    private static PagedResult<TaskDto> Empty =>
        PagedResult<TaskDto>.Create(Array.Empty<TaskDto>(), 1, TaskQuery.DefaultPageSize, 0);

    public PagedResult<TaskDto> ToDo { get; init; } = Empty;

    public PagedResult<TaskDto> Doing { get; init; } = Empty;

    public PagedResult<TaskDto> Done { get; init; } = Empty;
}
