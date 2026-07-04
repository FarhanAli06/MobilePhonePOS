using Microsoft.AspNetCore.Mvc;
using Empire.Web.Authorization;
using Empire.Web.DTOs.Auth;
using Empire.Web.Services.Auth;
using Empire.Web.Services.Session;
using Empire.Web.DTOs.User;

namespace Empire.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthApiService _authService;
    private readonly ISessionHelper _sessionHelper;

    public AuthController(IAuthApiService authService, ISessionHelper sessionHelper)
    {
        _authService = authService;
        _sessionHelper = sessionHelper;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var response = await _authService.LoginAsync(request);
        if (response == null)
            return Unauthorized(new { message = "Invalid username or password" });

        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponseDto>> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        var response = await _authService.RefreshTokenAsync(request);
        if (response == null)
            return Unauthorized(new { message = "Invalid refresh token" });

        return Ok(response);
    }

    [HttpPost("logout")]
    [SessionAuthorize]
    public async Task<ActionResult> Logout()
    {
        await _authService.LogoutAsync();
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpGet("me")]
    [SessionAuthorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var user = await _authService.GetCurrentUserAsync();
        if (user == null)
            return Unauthorized();

        return Ok(user);
    }

    /// <summary>
    /// Returns auth context from server-side session — no JWT required from browser JS.
    /// Called by auth-context.js on every page load.
    /// </summary>
    [HttpGet("context")]
    public IActionResult GetContext()
    {
        if (!_sessionHelper.IsAuthenticated())
            return Unauthorized(new { isAuthenticated = false });

        return Ok(new
        {
            success = true,
            data = new
            {
                userId = _sessionHelper.GetCurrentUserId(),
                userName = _sessionHelper.GetCurrentUserName(),
                shopId = _sessionHelper.GetCurrentShopId(),
                shopName = _sessionHelper.GetCurrentShopName(),
                role = _sessionHelper.GetCurrentUserRole(),
                isAuthenticated = true
            }
        });
    }
}
