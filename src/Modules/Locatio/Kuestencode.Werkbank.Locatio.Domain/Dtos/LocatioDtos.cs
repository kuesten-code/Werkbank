namespace Kuestencode.Werkbank.Locatio.Domain.Dtos;

public class GebaeudeKachelDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Adresse { get; set; } = string.Empty;
    public string? AbrechnerUrl { get; set; }

    public int EinheitenGesamt { get; set; }
    public int EinheitenEigentum { get; set; }
    public int EinheitenVermietet { get; set; }
    public int EinheitenLeer { get; set; }

    public DateOnly? VerkaufsDatum { get; set; }
    public bool IstVeraeussert => VerkaufsDatum.HasValue;

    /// <summary>Summe der vereinbarten Monatsmieten (netto inkl. NK-Vorauszahlung) vermieteter eigener Einheiten.</summary>
    public decimal SollmieteMonat { get; set; }
    public decimal EinnahmenJahr { get; set; }
    public decimal WerbungskostenJahr { get; set; }
    public decimal UeberschussJahr => EinnahmenJahr - WerbungskostenJahr;
}

public record VeraeusserungDaten(DateOnly Datum, decimal? Preis, decimal? Kosten);

public record DateiUpload(string Dateiname, string ContentType, byte[] Inhalt)
{
    public const long MaxGroesse = 20 * 1024 * 1024;

    private static readonly string[] ErlaubteTypen = ["application/pdf", "image/jpeg", "image/png"];

    public Entities.Dokument ZuDokument()
    {
        if (!ErlaubteTypen.Contains(ContentType, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Nur PDF, JPEG und PNG sind erlaubt.");
        if (Inhalt.LongLength > MaxGroesse)
            throw new InvalidOperationException("Die Datei darf maximal 20 MB groß sein.");

        return new Entities.Dokument
        {
            Id = Guid.NewGuid(),
            Dateiname = Path.GetFileName(Dateiname),
            ContentType = ContentType,
            Groesse = Inhalt.LongLength,
            Inhalt = Inhalt
        };
    }
}

public class AfaVorschlagDto
{
    public Guid MieteinheitId { get; set; }
    public Guid GebaeudeId { get; set; }
    public string Gebaeude { get; set; } = string.Empty;
    public string Einheit { get; set; } = string.Empty;
    public decimal AfaBasis { get; set; }
    public decimal AfaSatz { get; set; }

    /// <summary>AfA-Basis ist der Flächenanteil am Gebäude statt eigener Anschaffungsdaten.</summary>
    public bool AusGebaeude { get; set; }

    public int Monate { get; set; }
    public decimal BereitsAngesetzt { get; set; }
    public decimal Betrag { get; set; }
    public DateOnly Buchungsdatum { get; set; }
    public bool BereitsGebucht { get; set; }
}

/// <summary>
/// Orientierungswert für den Steuerberater, keine steuerliche Berechnung.
/// </summary>
public class VeraeusserungsgewinnDto
{
    public string Objekt { get; set; } = string.Empty;
    public DateOnly? AnschaffungsDatum { get; set; }
    public DateOnly? VerkaufsDatum { get; set; }
    public int? HaltedauerMonate { get; set; }
    public decimal Veraeusserungspreis { get; set; }
    public decimal Veraeusserungskosten { get; set; }
    public decimal Anschaffungskosten { get; set; }
    public decimal KumulierteAfa { get; set; }

    public decimal Restbuchwert => Anschaffungskosten - KumulierteAfa;
    public decimal Gewinn => Veraeusserungspreis - Veraeusserungskosten - Restbuchwert;

    /// <summary>Innerhalb von zehn Jahren ist der Verkauf ein privates Veräußerungsgeschäft (§ 23 EStG).</summary>
    public bool UnterZehnJahren => HaltedauerMonate is < 120;
}
