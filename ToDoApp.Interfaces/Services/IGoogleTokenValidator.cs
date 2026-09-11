using ToDoApp.Interfaces.Dtos;

namespace ToDoApp.Interfaces.Services;

public interface IGoogleTokenValidator
{
    Task<GoogleUserInfo> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}
