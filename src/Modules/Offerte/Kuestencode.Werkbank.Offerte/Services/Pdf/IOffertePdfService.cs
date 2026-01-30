using Kuestencode.Core.Models;
using Kuestencode.Werkbank.Offerte.Domain.Entities;

namespace Kuestencode.Werkbank.Offerte.Services.Pdf;

/// <summary>
/// Service zur Erzeugung von Angebots-PDFs.
/// </summary>
public interface IOffertePdfService
{
    /// <summary>
    /// Erzeugt ein PDF für das angegebene Angebot.
    /// </summary>
    /// <param name="angebot">Das Angebot mit Positionen.</param>
    /// <param name="kunde">Die Kundendaten.</param>
    /// <param name="firma">Die Firmenstammdaten.</param>
    /// <returns>PDF als Byte-Array.</returns>
    byte[] Erstelle(Angebot angebot, Customer kunde, Company firma);

    /// <summary>
    /// Erzeugt ein PDF für ein Angebot anhand der ID.
    /// Lädt Kunde und Firma automatisch.
    /// </summary>
    Task<byte[]> ErstelleAsync(Guid angebotId);
}
