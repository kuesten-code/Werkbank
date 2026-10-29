using Kuestencode.Werkbank.Locatio.Domain.Enums;

namespace Kuestencode.Werkbank.Locatio.Domain.Entities;

/// <summary>
/// Abgelegtes Übergabeprotokoll bei Ein- oder Auszug. Das unterschriebene Protokoll liegt als Dokument bei.
/// </summary>
public class Uebergabeprotokoll
{
    public Guid Id { get; set; }
    public Guid MieteinheitId { get; set; }
    public Mieteinheit? Mieteinheit { get; set; }

    public UebergabeArt Art { get; set; }
    public DateOnly Datum { get; set; }

    /// <summary>Mieter zum Zeitpunkt der Übergabe; die Einheit kann später einen anderen Mieter haben.</summary>
    public string? Mieter { get; set; }

    public string? Notiz { get; set; }

    public Guid DokumentId { get; set; }
    public Dokument? Dokument { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
