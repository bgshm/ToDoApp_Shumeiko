using System.IdentityModel.Tokens.Jwt;
using ToDoApp.Interfaces.Services;

namespace ToDoApp.Server.Infrastructure;

public class CurrentUser : ICurrentUser
{   
    public const string ConnectionIdHeader = "X-Connection-Id";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    public bool IsAuthenticated => TryGetUserId(out _);

    public Guid UserId => TryGetUserId(out var userId)
        ? userId
        : throw new InvalidOperationException("The current request is not authenticated.");

    public string? ConnectionId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.Request.Headers[ConnectionIdHeader].FirstOrDefault();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }

    private bool TryGetUserId(out Guid userId)
    {
        userId = Guid.Empty;

        var subject = _httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        return subject is not null && Guid.TryParse(subject, out userId);
    }
}
