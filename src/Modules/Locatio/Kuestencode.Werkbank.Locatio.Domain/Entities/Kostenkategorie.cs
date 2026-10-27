using Kuestencode.Werkbank.Locatio.Domain.Enums;

namespace Kuestencode.Werkbank.Locatio.Domain.Entities;

public class Kostenkategorie
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public KategorieTyp Typ { get; set; }

    /// <summary>Zielkonto in Saldo. Leer = Saldo gruppiert nach dem Kategorienamen.</summary>
    public string? KontoNummer { get; set; }

    public int Sortierung { get; set; }

    /// <summary>Systemkategorien bebucht Locatio selbst (Miete, NK-Abrechnung); sie können nicht gelöscht werden.</summary>
    public bool IstSystem { get; set; }
}
