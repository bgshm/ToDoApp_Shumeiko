using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToDoApp.Interfaces.Dtos;
using ToDoApp.Interfaces.Exceptions;
using ToDoApp.Interfaces.Services;
using ToDoApp.Services.Options;

namespace ToDoApp.Services.Auth;

public class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly GoogleAuthOptions _options;
    private readonly ILogger<GoogleTokenValidator> _logger;

    public GoogleTokenValidator(IOptions<GoogleAuthOptions> options, ILogger<GoogleTokenValidator> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GoogleUserInfo> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId))
        {
            throw new AuthenticationFailedException(
                "Google sign-in is not configured on the server (Authentication:Google:ClientId is missing).");
        }

        cancellationToken.ThrowIfCancellationRequested();

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _options.ClientId }
                });
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogDebug(ex, "Rejected a Google ID token.");
            throw new AuthenticationFailedException("The Google sign-in token could not be verified.");
        }

        if (payload.EmailVerified != true)
        {
            throw new AuthenticationFailedException("The Google account does not have a verified email address.");
        }

        return new GoogleUserInfo
        {
            Subject = payload.Subject,
            Email = payload.Email,
            DisplayName = string.IsNullOrWhiteSpace(payload.Name) ? payload.Email : payload.Name,
            PictureUrl = payload.Picture
        };
    }
}
