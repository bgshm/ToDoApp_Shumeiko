using ToDoApp.Interfaces.Entities;

namespace ToDoApp.Interfaces.Dtos;

public class BoardQuery
{
    private int _pageSize = TaskQuery.DefaultPageSize;
    private int _toDoPage = 1;
    private int _doingPage = 1;
    private int _donePage = 1;

    public string? Search { get; set; }

    public Guid? CategoryId { get; set; }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => TaskQuery.DefaultPageSize,
            > TaskQuery.MaxPageSize => TaskQuery.MaxPageSize,
            _ => value
        };
    }

    public int ToDoPage
    {
        get => _toDoPage;
        set => _toDoPage = value < 1 ? 1 : value;
    }

    public int DoingPage
    {
        get => _doingPage;
        set => _doingPage = value < 1 ? 1 : value;
    }

    public int DonePage
    {
        get => _donePage;
        set => _donePage = value < 1 ? 1 : value;
    }

    public int PageFor(TaskState state) => state switch
    {
        TaskState.ToDo => ToDoPage,
        TaskState.Doing => DoingPage,
        _ => DonePage
    };

    public TaskQuery ToTaskQuery(TaskState state) => new()
    {
        Search = Search,
        CategoryId = CategoryId,
        State = state,
        Page = PageFor(state),
        PageSize = PageSize
    };
}
