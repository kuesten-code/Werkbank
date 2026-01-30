namespace Kuestencode.Werkbank.Offerte.Domain.Interfaces;

/// <summary>
/// Service zur Generierung eindeutiger Angebotsnummern.
/// Die Implementierung erfolgt in der Data-Schicht.
/// </summary>
public interface IAngebotsnummernService
{
    /// <summary>
    /// Generiert die nächste verfügbare Angebotsnummer.
    /// </summary>
    /// <returns>Die nächste Angebotsnummer (z.B. "ANG-2024-0001").</returns>
    Task<string> NaechsteNummerAsync();

    /// <summary>
    /// Prüft, ob eine Angebotsnummer bereits existiert.
    /// </summary>
    Task<bool> ExistiertAsync(string angebotsnummer);
}
