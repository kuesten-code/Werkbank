using System.Security.Claims;
using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Services;
using Microsoft.AspNetCore.Mvc;

namespace Kuestencode.Werkbank.Host.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IPasswordResetService _passwordResetService;
    private readonly IAuthService _authService;

    public AuthController(IPasswordResetService passwordResetService, IAuthService authService)
    {
        _passwordResetService = passwordResetService;
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request.Email, request.Password);

        if (!result.Success)
        {
            return Unauthorized(new
            {
                error = result.Error,
                remainingAttempts = result.RemainingAttempts,
                lockedForMinutes = result.LockedForMinutes
            });
        }

        return Ok(new
        {
            token = result.Token,
            user = new
            {
                id = result.User!.Id,
                displayName = result.User.DisplayName,
                role = (int)result.User.Role
            }
        });
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Auth deaktiviert: Fake-Admin
        if (userId == Guid.Empty.ToString())
        {
            return Ok(new
            {
                id = Guid.Empty,
                displayName = "Admin",
                role = (int)UserRole.Admin,
                authEnabled = false
            });
        }

        var member = await _authService.GetCurrentUserAsync(userId);
        if (member == null)
            return Unauthorized();

        return Ok(new
        {
            id = member.Id,
            displayName = member.DisplayName,
            role = (int)member.Role,
            authEnabled = true
        });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _passwordResetService.RequestResetAsync(request.Email);
        // Immer OK zurückgeben um E-Mail-Enumeration zu verhindern
        return Ok(new { message = "Falls ein Account mit dieser E-Mail existiert, wurde ein Reset-Link gesendet." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            await _passwordResetService.ResetPasswordAsync(request.Token, request.Password);
            return Ok(new { message = "Passwort wurde zurückgesetzt." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("reset/{token}/valid")]
    public async Task<IActionResult> ValidateResetToken(string token)
    {
        var member = await _passwordResetService.ValidateResetTokenAsync(token);
        if (member == null)
            return Ok(new { valid = false });

        return Ok(new { valid = true, displayName = member.DisplayName });
    }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
