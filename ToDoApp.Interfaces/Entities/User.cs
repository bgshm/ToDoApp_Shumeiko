namespace ToDoApp.Interfaces.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = string.Empty;

    public string ProviderKey { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? PictureUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset LastLoginAt { get; set; }

    public ICollection<Category> Categories { get; set; } = new List<Category>();

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
