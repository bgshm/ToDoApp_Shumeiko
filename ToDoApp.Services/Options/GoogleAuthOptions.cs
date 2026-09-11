namespace ToDoApp.Services.Options;

public class GoogleAuthOptions
{
    public const string SectionName = "Authentication:Google";

    public string? ClientId { get; set; }

    public bool AllowDevLogin { get; set; }
}
