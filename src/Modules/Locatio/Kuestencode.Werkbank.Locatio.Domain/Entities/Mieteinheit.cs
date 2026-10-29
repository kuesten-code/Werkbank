using Kuestencode.Werkbank.Locatio.Domain.Enums;

namespace Kuestencode.Werkbank.Locatio.Domain.Entities;

public class Mieteinheit : Eigentumsobjekt
{
    public Guid Id { get; set; }
    public Guid GebaeudeId { get; set; }
    public Gebaeude? Gebaeude { get; set; }

    public string Bezeichnung { get; set; } = string.Empty;
    public EinheitArt Art { get; set; }
    public EinheitStatus Status { get; set; } = EinheitStatus.Leer;

    /// <summary>Wohn- bzw. Nutzfläche in m².</summary>
    public decimal? Flaeche { get; set; }

    /// <summary>Monatliche Bruttokaltmiete; bei Gewerbe mit USt-Option der Nettobetrag.</summary>
    public decimal Kaltmiete { get; set; }

    /// <summary>Monatliche Nebenkosten-Vorauszahlung; bei Gewerbe mit USt-Option netto.</summary>
    public decimal NebenkostenVorauszahlung { get; set; }

    /// <summary>Freitext (z. B. "125/1000"); bleibt leer, solange die Teilungserklärung fehlt.</summary>
    public string? Miteigentumsanteil { get; set; }

    public string? Mieter { get; set; }
    public DateOnly? VertragsBeginn { get; set; }
    public DateOnly? VertragsEnde { get; set; }

    /// <summary>Option zur Umsatzsteuer nach § 9 UStG, nur bei Gewerbe wirksam.</summary>
    public bool UstOption { get; set; }
    public decimal UstSatz { get; set; } = 19m;

    public Guid? GrundrissId { get; set; }
    public Dokument? Grundriss { get; set; }

    public string? Notizen { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public bool IstEigentum => Status is not (EinheitStatus.Fremdeigentum or EinheitStatus.Veraeussert);

    public bool IstVermietet => Status is EinheitStatus.Vermietet or EinheitStatus.Gekuendigt;

    public bool IstUstPflichtig => Art == EinheitArt.Gewerbe && UstOption;

    public decimal WirksamerUstSatz => IstUstPflichtig ? UstSatz : 0m;

    public decimal MonatsmieteNetto => Kaltmiete + NebenkostenVorauszahlung;

    public decimal MonatsmieteUst => Math.Round(MonatsmieteNetto * WirksamerUstSatz / 100m, 2);

    public decimal MonatsmieteBrutto => MonatsmieteNetto + MonatsmieteUst;

    /// <summary>
    /// Erster Tag, ab dem keine Buchungen mehr erlaubt sind: der eigene Verkauf oder der des ganzen Gebäudes.
    /// </summary>
    public DateOnly? GesperrtAb => VerkaufsDatum ?? Gebaeude?.VerkaufsDatum;
}
