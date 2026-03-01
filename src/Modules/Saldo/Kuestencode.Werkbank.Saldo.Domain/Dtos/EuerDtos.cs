namespace Kuestencode.Werkbank.Saldo.Domain.Dtos;

/// <summary>
/// Vollständige EÜR-Zusammenfassung für einen Zeitraum.
/// </summary>
public class EuerSummaryDto
{
    public DateOnly Von { get; set; }
    public DateOnly Bis { get; set; }

    // Einnahmen (aus Faktura)
    public decimal EinnahmenNetto { get; set; }
    public decimal EinnahmenMwst { get; set; }
    public decimal EinnahmenBrutto { get; set; }

    // Ausgaben (aus Recepta)
    public decimal AusgabenNetto { get; set; }
    public decimal AusgabenMwst { get; set; }
    public decimal AusgabenBrutto { get; set; }

    /// <summary>
    /// EÜR-Überschuss = Einnahmen Netto - Ausgaben Netto.
    /// </summary>
    public decimal Ueberschuss => EinnahmenNetto - AusgabenNetto;

    public List<EuerPositionDto> Einnahmen { get; set; } = new();
    public List<EuerPositionDto> Ausgaben { get; set; } = new();
}

/// <summary>
/// Eine aggregierte Position in der EÜR (pro Konto).
/// </summary>
public class EuerPositionDto
{
    public string KontoNummer { get; set; } = string.Empty;
    public string KontoBezeichnung { get; set; } = string.Empty;
    public string Gruppe { get; set; } = string.Empty;
    public decimal BetragNetto { get; set; }
    public decimal MwstBetrag { get; set; }
    public decimal BetragBrutto { get; set; }
    public int AnzahlBelege { get; set; }
}

/// <summary>
/// Filter für EÜR-Abfragen.
/// </summary>
public class EuerFilterDto
{
    public DateOnly Von { get; set; }
    public DateOnly Bis { get; set; }
}

/// <summary>
/// DTO für Konten-Anzeige.
/// </summary>
public class KontoDto
{
    public Guid Id { get; set; }
    public string Kontenrahmen { get; set; } = string.Empty;
    public string KontoNummer { get; set; } = string.Empty;
    public string KontoBezeichnung { get; set; } = string.Empty;
    public string KontoTyp { get; set; } = string.Empty;
    public decimal? UstSatz { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO für Kategorie-Konto-Mapping.
/// </summary>
public class KategorieKontoMappingDto
{
    public Guid Id { get; set; }
    public string Kontenrahmen { get; set; } = string.Empty;
    public string ReceiptaKategorie { get; set; } = string.Empty;
    public string KontoNummer { get; set; } = string.Empty;
    public string KontoBezeichnung { get; set; } = string.Empty;
    public bool IsCustom { get; set; }
}

/// <summary>
/// Update-Dto für ein Kategorie-Mapping.
/// </summary>
public class UpdateKategorieKontoMappingDto
{
    public string KontoNummer { get; set; } = string.Empty;
}

/// <summary>
/// DTO für Saldo-Einstellungen.
/// </summary>
public class SaldoSettingsDto
{
    public Guid Id { get; set; }
    public string Kontenrahmen { get; set; } = "SKR03";
    public string? BeraterNummer { get; set; }
    public string? MandantenNummer { get; set; }
    public int WirtschaftsjahrBeginn { get; set; } = 1;
}

/// <summary>
/// Update-Dto für Saldo-Einstellungen.
/// </summary>
public class UpdateSaldoSettingsDto
{
    public string Kontenrahmen { get; set; } = "SKR03";
    public string? BeraterNummer { get; set; }
    public string? MandantenNummer { get; set; }
    public int WirtschaftsjahrBeginn { get; set; } = 1;
}

/// <summary>
/// Protokolleintrag für einen Export.
/// </summary>
public class ExportLogDto
{
    public Guid Id { get; set; }
    public string ExportTyp { get; set; } = string.Empty;
    public DateOnly ZeitraumVon { get; set; }
    public DateOnly ZeitraumBis { get; set; }
    public int AnzahlBuchungen { get; set; }
    public string DateiName { get; set; } = string.Empty;
    public long DateiGroesse { get; set; }
    public DateTime ExportedAt { get; set; }
}
