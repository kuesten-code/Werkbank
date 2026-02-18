using Kuestencode.Werkbank.Host.Models;

namespace Kuestencode.Werkbank.Host.Services;

public interface IMobileTokenService
{
    /// <summary>
    /// Generiert einen eindeutigen 10-stelligen Token (lowercase alphanumeric)
    /// </summary>
    string GenerateToken();

    /// <summary>
    /// Erstellt einen mobilen Zugang für einen Mitarbeiter
    /// </summary>
    Task<string> CreateMobileAccessAsync(Guid teamMemberId);

    /// <summary>
    /// Validiert einen Token und gibt den zugehörigen Mitarbeiter zurück
    /// </summary>
    Task<TeamMember?> ValidateTokenAsync(string token);

    /// <summary>
    /// Setzt die PIN für einen mobilen Zugang
    /// </summary>
    Task SetPinAsync(string token, string pin);

    /// <summary>
    /// Verifiziert eine PIN und gibt das Ergebnis zurück
    /// </summary>
    Task<MobilePinResult> VerifyPinAsync(string token, string pin);

    /// <summary>
    /// Setzt den mobilen Zugang zurück (neuer Token, PIN gelöscht)
    /// </summary>
    Task ResetMobileAccessAsync(Guid teamMemberId);

    /// <summary>
    /// Entsperrt einen gesperrten mobilen Zugang
    /// </summary>
    Task UnlockMobileAccessAsync(Guid teamMemberId);
}

public class MobilePinResult
{
    public bool Success { get; set; }
    public bool Locked { get; set; }
    public int RemainingAttempts { get; set; }
    public TeamMember? User { get; set; }
    public string? JwtToken { get; set; }
}
