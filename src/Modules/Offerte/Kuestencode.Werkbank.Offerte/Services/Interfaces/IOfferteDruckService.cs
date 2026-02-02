namespace Kuestencode.Werkbank.Offerte.Services;

/// <summary>
/// Service zur Druckvorbereitung von Angeboten.
/// </summary>
public interface IOfferteDruckService
{
    /// <summary>
    /// Bereitet ein Angebot für den Druck vor (erzeugt PDF).
    /// </summary>
    /// <param name="angebotId">ID des Angebots.</param>
    /// <returns>PDF als Byte-Array.</returns>
    Task<byte[]> DruckvorbereitungAsync(Guid angebotId);

    /// <summary>
    /// Markiert ein Angebot als gedruckt.
    /// </summary>
    Task MarkiereAlsGedrucktAsync(Guid angebotId);
}
