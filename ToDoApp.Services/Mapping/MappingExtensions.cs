using ToDoApp.Interfaces.Dtos;
using ToDoApp.Interfaces.Entities;

namespace ToDoApp.Services.Mapping;

public static class MappingExtensions
{
    public static TaskDto ToDto(this TaskItem task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Description = task.Description,
        State = task.State,
        Position = task.Position,
        CategoryId = task.CategoryId,
        CategoryName = task.Category?.Name,
        CategoryColor = task.Category?.Color,
        DueDate = task.DueDate,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt
    };

    public static IReadOnlyList<TaskDto> ToDtos(this IEnumerable<TaskItem> tasks) =>
        tasks.Select(ToDto).ToList();

    public static CategoryDto ToDto(this Category category, int taskCount = 0) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Color = category.Color,
        TaskCount = taskCount
    };

    public static UserDto ToDto(this User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        DisplayName = user.DisplayName,
        PictureUrl = user.PictureUrl
    };
}
