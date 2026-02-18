using Kuestencode.Werkbank.Host.Services;
using Microsoft.AspNetCore.Mvc;

namespace Kuestencode.Werkbank.Host.Controllers;

[ApiController]
[Route("api/mobile")]
public class MobileController : ControllerBase
{
    private readonly IMobileTokenService _mobileTokenService;
    private readonly ILogger<MobileController> _logger;

    public MobileController(
        IMobileTokenService mobileTokenService,
        ILogger<MobileController> logger)
    {
        _mobileTokenService = mobileTokenService;
        _logger = logger;
    }

    /// <summary>
    /// Prüft den Status eines Mobile-Tokens
    /// </summary>
    [HttpGet("{token}/status")]
    public async Task<IActionResult> GetStatus(string token)
    {
        var member = await _mobileTokenService.ValidateTokenAsync(token);

        if (member == null)
        {
            return Ok(new
            {
                valid = false,
                pinSet = false,
                locked = false,
                userName = (string?)null
            });
        }

        return Ok(new
        {
            valid = true,
            pinSet = member.MobilePinSet,
            locked = member.MobileTokenLocked,
            userName = member.DisplayName
        });
    }

    /// <summary>
    /// Setzt die PIN für einen Mobile-Token
    /// </summary>
    [HttpPost("{token}/pin/set")]
    public async Task<IActionResult> SetPin(string token, [FromBody] SetPinRequest request)
    {
        try
        {
            await _mobileTokenService.SetPinAsync(token, request.Pin);
            return Ok(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Verifiziert die PIN und gibt einen JWT-Token zurück
    /// </summary>
    [HttpPost("{token}/pin/verify")]
    public async Task<IActionResult> VerifyPin(string token, [FromBody] VerifyPinRequest request)
    {
        var result = await _mobileTokenService.VerifyPinAsync(token, request.Pin);

        if (result.Success)
        {
            return Ok(new
            {
                success = true,
                jwtToken = result.JwtToken,
                user = new
                {
                    id = result.User!.Id,
                    displayName = result.User.DisplayName,
                    role = (int)result.User.Role
                }
            });
        }

        if (result.Locked)
        {
            return Ok(new
            {
                success = false,
                locked = true,
                remainingAttempts = 0
            });
        }

        return Ok(new
        {
            success = false,
            locked = false,
            remainingAttempts = result.RemainingAttempts
        });
    }
}

public class SetPinRequest
{
    public string Pin { get; set; } = string.Empty;
}

public class VerifyPinRequest
{
    public string Pin { get; set; } = string.Empty;
}
