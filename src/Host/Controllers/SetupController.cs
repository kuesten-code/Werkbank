using Kuestencode.Werkbank.Host.Services;
using Microsoft.AspNetCore.Mvc;

namespace Kuestencode.Werkbank.Host.Controllers;

[ApiController]
[Route("api/setup")]
public class SetupController : ControllerBase
{
    private readonly ISetupService _setupService;
    private readonly ILogger<SetupController> _logger;

    public SetupController(ISetupService setupService, ILogger<SetupController> logger)
    {
        _setupService = setupService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/setup/required
    /// Checks if initial setup is required
    /// </summary>
    [HttpGet("required")]
    public async Task<ActionResult<bool>> IsSetupRequired()
    {
        try
        {
            var required = await _setupService.IsSetupRequiredAsync();
            return Ok(required);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if setup is required");
            return StatusCode(500, new { error = "Error checking setup status" });
        }
    }

    /// <summary>
    /// POST /api/setup/complete
    /// Completes the initial setup
    /// </summary>
    [HttpPost("complete")]
    public async Task<ActionResult> CompleteSetup([FromBody] SetupData setupData)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(setupData.AdminName))
            {
                return BadRequest(new { error = "Admin-Name ist erforderlich" });
            }

            if (string.IsNullOrWhiteSpace(setupData.AdminEmail))
            {
                return BadRequest(new { error = "Admin-Email ist erforderlich" });
            }

            if (string.IsNullOrWhiteSpace(setupData.AdminPassword))
            {
                return BadRequest(new { error = "Admin-Passwort ist erforderlich" });
            }

            if (setupData.AdminPassword.Length < 8)
            {
                return BadRequest(new { error = "Passwort muss mindestens 8 Zeichen lang sein" });
            }

            // Check if setup already completed
            if (await _setupService.IsSetupCompletedAsync())
            {
                return BadRequest(new { error = "Setup wurde bereits abgeschlossen" });
            }

            // Complete setup
            await _setupService.CompleteSetupAsync(setupData);

            _logger.LogInformation("Setup completed successfully");

            return Ok(new { success = true, authEnabled = setupData.AuthEnabled });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid setup data");
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Setup already completed");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing setup");
            return StatusCode(500, new { error = "Fehler beim Abschließen des Setups" });
        }
    }
}
