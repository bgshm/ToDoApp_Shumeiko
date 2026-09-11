using ToDoApp.Interfaces.Dtos;

namespace ToDoApp.Interfaces.Services;

public interface IAuthService
{
    AuthConfigDto GetConfig();

    Task<AuthResponse> LoginWithGoogleAsync(string credential, CancellationToken cancellationToken = default);

    Task<AuthResponse> DevLoginAsync(DevLoginRequest request, CancellationToken cancellationToken = default);

    Task<UserDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
}
