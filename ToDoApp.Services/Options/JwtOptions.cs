namespace ToDoApp.Services.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SigningKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "ToDoApp";

    public string Audience { get; set; } = "ToDoApp.Client";

    public int AccessTokenLifetimeMinutes { get; set; } = 480;
}
