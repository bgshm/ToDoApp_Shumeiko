using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToDoApp.Interfaces.Dtos;
using ToDoApp.Interfaces.Services;

namespace ToDoApp.Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUser _currentUser;
    private readonly IWebHostEnvironment _environment;

    public AuthController(IAuthService authService, ICurrentUser currentUser, IWebHostEnvironment environment)
    {
        _authService = authService;
        _currentUser = currentUser;
        _environment = environment;
    }

    [HttpGet("config")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthConfigDto), StatusCodes.Status200OK)]
    public ActionResult<AuthConfigDto> GetConfig()
    {
        var config = _authService.GetConfig();

        return Ok(new AuthConfigDto
        {
            GoogleClientId = config.GoogleClientId,
            DevLoginEnabled = config.DevLoginEnabled && _environment.IsDevelopment()
        });
    }

    [HttpPost("google")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> LoginWithGoogle(
        [FromBody] GoogleLoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.LoginWithGoogleAsync(request.Credential, cancellationToken);
        return Ok(response);
    }

    [HttpPost("dev-login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuthResponse>> DevLogin(
        [FromBody] DevLoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var response = await _authService.DevLoginAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> GetProfile(CancellationToken cancellationToken)
    {
        var profile = await _authService.GetProfileAsync(_currentUser.UserId, cancellationToken);
        return Ok(profile);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout() => NoContent();
}
