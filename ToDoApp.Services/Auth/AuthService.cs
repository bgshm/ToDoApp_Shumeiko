using Microsoft.Extensions.Options;
using ToDoApp.Interfaces.Dtos;
using ToDoApp.Interfaces.Entities;
using ToDoApp.Interfaces.Exceptions;
using ToDoApp.Interfaces.Repositories;
using ToDoApp.Interfaces.Services;
using ToDoApp.Services.Mapping;
using ToDoApp.Services.Options;

namespace ToDoApp.Services.Auth;

public class AuthService : IAuthService
{
    private const string GoogleProvider = "Google";
    private const string DevProvider = "Dev";

    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokens;
    private readonly IGoogleTokenValidator _googleValidator;
    private readonly ICategoryService _categories;
    private readonly GoogleAuthOptions _options;

    public AuthService(
        IUserRepository users,
        IUnitOfWork unitOfWork,
        ITokenService tokens,
        IGoogleTokenValidator googleValidator,
        ICategoryService categories,
        IOptions<GoogleAuthOptions> options)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _tokens = tokens;
        _googleValidator = googleValidator;
        _categories = categories;
        _options = options.Value;
    }

    public AuthConfigDto GetConfig() => new()
    {
        GoogleClientId = string.IsNullOrWhiteSpace(_options.ClientId) ? null : _options.ClientId,
        DevLoginEnabled = _options.AllowDevLogin
    };

    public async Task<AuthResponse> LoginWithGoogleAsync(string credential, CancellationToken cancellationToken = default)
    {
        var info = await _googleValidator.ValidateAsync(credential, cancellationToken);

        return await SignInAsync(
            GoogleProvider,
            info.Subject,
            info.Email,
            info.DisplayName,
            info.PictureUrl,
            cancellationToken);
    }

    public Task<AuthResponse> DevLoginAsync(DevLoginRequest request, CancellationToken cancellationToken = default)
    {
        if (!_options.AllowDevLogin)
        {
            throw new AuthenticationFailedException("Development sign-in is disabled.");
        }

        var email = string.IsNullOrWhiteSpace(request.Email)
            ? "demo@todoapp.local"
            : request.Email.Trim().ToLowerInvariant();

        var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? email.Split('@')[0]
            : request.DisplayName.Trim();

        return SignInAsync(DevProvider, email, email, displayName, null, cancellationToken);
    }

    public async Task<UserDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken)
                   ?? throw NotFoundException.For("User", userId);

        return user.ToDto();
    }

    private async Task<AuthResponse> SignInAsync(
        string provider,
        string providerKey,
        string email,
        string displayName,
        string? pictureUrl,
        CancellationToken cancellationToken)
    {
        var user = await _users.GetByProviderAsync(provider, providerKey, cancellationToken);
        var isNewAccount = user is null;

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Provider = provider,
                ProviderKey = providerKey,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _users.AddAsync(user, cancellationToken);
        }

        user.Email = email;
        user.DisplayName = displayName;
        user.PictureUrl = pictureUrl;
        user.LastLoginAt = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (isNewAccount)
        {
            await _categories.SeedDefaultsAsync(user.Id, cancellationToken);
        }

        var (accessToken, expiresAt) = _tokens.CreateAccessToken(user);

        return new AuthResponse
        {
            AccessToken = accessToken,
            ExpiresAtUtc = expiresAt,
            User = user.ToDto()
        };
    }
}
