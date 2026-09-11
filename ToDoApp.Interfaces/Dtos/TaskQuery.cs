using ToDoApp.Interfaces.Entities;

namespace ToDoApp.Interfaces.Dtos;

public class TaskQuery
{
    public const int MaxPageSize = 100;
    public const int DefaultPageSize = 8;

    private int _page = 1;
    private int _pageSize = DefaultPageSize;

    public string? Search { get; set; }

    public Guid? CategoryId { get; set; }

    public TaskState? State { get; set; }

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }
}
