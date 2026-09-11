using System.ComponentModel.DataAnnotations;

namespace ToDoApp.Interfaces.Dtos;

public class UserDto
{
    public Guid Id { get; init; }

    public string Email { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string? PictureUrl { get; init; }
}

public class AuthResponse
{
    public string AccessToken { get; init; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; init; }

    public UserDto User { get; init; } = new();
}

public class GoogleLoginRequest
{
    [Required(AllowEmptyStrings = false)]
    public string Credential { get; set; } = string.Empty;
}

public class DevLoginRequest
{
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(80)]
    public string? DisplayName { get; set; }
}

public class AuthConfigDto
{
    public string? GoogleClientId { get; init; }

    public bool GoogleEnabled => !string.IsNullOrWhiteSpace(GoogleClientId);

    public bool DevLoginEnabled { get; init; }
}

public class GoogleUserInfo
{
    public string Subject { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string? PictureUrl { get; init; }
}
