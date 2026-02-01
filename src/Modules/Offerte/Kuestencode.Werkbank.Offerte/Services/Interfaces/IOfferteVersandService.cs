using Kuestencode.Werkbank.Offerte.Domain.Entities;

namespace Kuestencode.Werkbank.Offerte.Services;

/// <summary>
/// Service zum Versenden von Angeboten per E-Mail.
/// </summary>
public interface IOfferteVersandService
{
    /// <summary>
    /// Versendet ein Angebot per E-Mail.
    /// Erzeugt PDF, sendet E-Mail und setzt Status auf Versendet.
    /// </summary>
    /// <param name="angebotId">ID des Angebots.</param>
    /// <param name="empfaengerEmail">E-Mail-Adresse des Empfängers (optional, sonst Kunde.Email).</param>
    /// <param name="betreff">Betreff der E-Mail (optional, sonst Standard-Template).</param>
    /// <param name="nachricht">Nachrichtentext (optional, sonst Standard-Template).</param>
    /// <param name="ccEmails">CC-Empfänger (optional, kommagetrennt).</param>
    /// <param name="bccEmails">BCC-Empfänger (optional, kommagetrennt).</param>
    /// <param name="includeClosing">Abschiedsformel einfügen (Standard: true).</param>
    /// <returns>Erfolg/Misserfolg.</returns>
    Task<bool> VersendeAsync(
        Guid angebotId,
        string? empfaengerEmail = null,
        string? betreff = null,
        string? nachricht = null,
        string? ccEmails = null,
        string? bccEmails = null,
        bool includeClosing = true);
}
