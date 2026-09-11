using ToDoApp.Interfaces.Entities;

namespace ToDoApp.Interfaces.Services;

public interface ITokenService
{
    (string AccessToken, DateTimeOffset ExpiresAtUtc) CreateAccessToken(User user);
}
