namespace Kuestencode.Werkbank.Offerte.Services;

/// <summary>
/// Service zur Prüfung und Aktualisierung abgelaufener Angebote.
/// </summary>
public interface IAngebotAblaufService
{
    /// <summary>
    /// Prüft alle versendeten Angebote und markiert abgelaufene als Abgelaufen.
    /// </summary>
    /// <returns>Anzahl der als abgelaufen markierten Angebote.</returns>
    Task<int> PruefeUndAktualiereAbgelaufeneAsync();
}
