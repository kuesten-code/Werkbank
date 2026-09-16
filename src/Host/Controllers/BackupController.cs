using Microsoft.AspNetCore.Mvc;
using Kuestencode.Werkbank.Host.Auth;
using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Services.Backup;

namespace Kuestencode.Werkbank.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
[RequireRole(UserRole.Admin)]
public class BackupController : ControllerBase
{
    private readonly IBackupService _backupService;

    public BackupController(IBackupService backupService)
    {
        _backupService = backupService;
    }

    /// <summary>
    /// Manueller Download einer Backup-Datei über die UI (siehe Startseiten-Benachrichtigung
    /// und Restore-Panel unter /settings/backup).
    /// </summary>
    [HttpGet("download")]
    public async Task<IActionResult> Download([FromQuery] int targetId, [FromQuery] string fileName)
    {
        try
        {
            var stream = await _backupService.OpenBackupFileForDownloadAsync(targetId, fileName);
            return File(stream, "application/gzip", fileName);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Download fehlgeschlagen: {ex.Message}");
        }
    }
}
