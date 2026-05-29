using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Kuestencode.Werkbank.Host.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IPasswordResetService _passwordResetService;
    private readonly IAuthService _authService;
    private readonly ITotpService _totpService;
    private readonly IPasswordService _passwordService;
    private readonly HostDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(
        IPasswordResetService passwordResetService,
        IAuthService authService,
        ITotpService totpService,
        IPasswordService passwordService,
        HostDbContext context,
        IConfiguration configuration)
    {
        _passwordResetService = passwordResetService;
        _authService = authService;
        _totpService = totpService;
        _passwordService = passwordService;
        _context = context;
        _configuration = configuration;
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

        if (result.RequiresMfa)
            return Ok(new { requiresMfa = true, mfaToken = result.MfaToken });

        SetAuthCookie(result.Token!, request.RememberMe);

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

    [HttpPost("login/mfa")]
    public async Task<IActionResult> LoginMfa([FromBody] MfaLoginRequest request)
    {
        var principal = ValidateMfaToken(request.MfaToken);
        if (principal == null)
            return Unauthorized(new { error = "Ungültiger oder abgelaufener MFA-Token." });

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var id))
            return Unauthorized(new { error = "Ungültiger Token." });

        var member = await _context.TeamMembers.FirstOrDefaultAsync(m => m.Id == id && m.IsActive);
        if (member == null || !member.MfaEnabled)
            return Unauthorized(new { error = "Ungültiger Token." });

        var code = request.Code.Trim();
        bool valid;

        if (request.IsRecoveryCode)
        {
            valid = _totpService.VerifyAndConsumeRecoveryCode(member, code);
            if (valid)
                await _context.SaveChangesAsync();
        }
        else
        {
            valid = string.IsNullOrEmpty(member.TotpSecret)
                ? false
                : _totpService.VerifyCode(member.TotpSecret, code);
        }

        if (!valid)
            return Unauthorized(new { error = "Ungültiger Code." });

        var token = _authService.GenerateToken(member);
        SetAuthCookie(token, false);

        return Ok(new
        {
            token,
            user = new
            {
                id = member.Id,
                displayName = member.DisplayName,
                role = (int)member.Role
            }
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("werkbank_auth_cookie", new CookieOptions { Path = "/" });
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

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

    [HttpGet("totp/setup")]
    public IActionResult TotpSetup()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email) ?? userId ?? "user";

        var secret = _totpService.GenerateSecret();
        var issuer = _configuration["Jwt:Issuer"] ?? "KuestencodeWerkbank";
        var qrSvg = _totpService.GetQrCodeSvg(secret, email, issuer);

        return Ok(new { secret, qrSvg });
    }

    [HttpPost("totp/enable")]
    public async Task<IActionResult> TotpEnable([FromBody] TotpEnableRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var id))
            return Unauthorized();

        var member = await _context.TeamMembers.FirstOrDefaultAsync(m => m.Id == id && m.IsActive);
        if (member == null)
            return Unauthorized();

        if (!_totpService.VerifyCode(request.Secret, request.Code))
            return BadRequest(new { error = "Ungültiger Code. Bitte erneut versuchen." });

        var recoveryCodes = _totpService.GenerateRecoveryCodes();
        var hashedCodes = recoveryCodes
            .Select(c => _totpService.HashRecoveryCode(c))
            .ToList();

        member.TotpSecret = request.Secret;
        member.MfaEnabled = true;
        member.MfaRecoveryCodes = JsonSerializer.Serialize(hashedCodes);
        await _context.SaveChangesAsync();

        return Ok(new { recoveryCodes });
    }

    [HttpPost("totp/disable")]
    public async Task<IActionResult> TotpDisable([FromBody] TotpDisableRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var id))
            return Unauthorized();

        var member = await _context.TeamMembers.FirstOrDefaultAsync(m => m.Id == id && m.IsActive);
        if (member == null)
            return Unauthorized();

        if (string.IsNullOrEmpty(member.PasswordHash) ||
            !_passwordService.VerifyPassword(member.PasswordHash, request.Password))
            return BadRequest(new { error = "Falsches Passwort." });

        member.TotpSecret = null;
        member.MfaEnabled = false;
        member.MfaRecoveryCodes = null;
        await _context.SaveChangesAsync();

        return Ok(new { message = "MFA deaktiviert." });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _passwordResetService.RequestResetAsync(request.Email);
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

    private void SetAuthCookie(string token, bool rememberMe)
    {
        Response.Cookies.Append("werkbank_auth_cookie", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = rememberMe
                ? DateTimeOffset.UtcNow.AddDays(30)
                : DateTimeOffset.UtcNow.AddDays(7)
        });
    }

    private ClaimsPrincipal? ValidateMfaToken(string token)
    {
        try
        {
            var secret = GetJwtSecret();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var issuer = _configuration["Jwt:Issuer"] ?? "KuestencodeWerkbank";

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = issuer,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            }, out _);

            if (principal.FindFirstValue("mfa_pending") != "true")
                return null;

            return principal;
        }
        catch
        {
            return null;
        }
    }

    private string GetJwtSecret()
    {
        var secret = _configuration["Jwt:Secret"];
        if (!string.IsNullOrWhiteSpace(secret) && secret.Length >= 32)
            return secret;

        var filePath = Path.Combine(AppContext.BaseDirectory, "data", "jwt-secret.txt");
        if (System.IO.File.Exists(filePath))
        {
            var stored = System.IO.File.ReadAllText(filePath).Trim();
            if (stored.Length >= 32)
                return stored;
        }

        throw new InvalidOperationException("JWT-Secret nicht verfügbar.");
    }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}

public class MfaLoginRequest
{
    public string MfaToken { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsRecoveryCode { get; set; }
}

public class TotpEnableRequest
{
    public string Secret { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class TotpDisableRequest
{
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
