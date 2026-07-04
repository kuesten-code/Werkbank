namespace Kuestencode.Werkbank.Acta.Domain.Dtos;

public class StundensatzDto
{
    public int RolleId { get; set; }
    public string RolleName { get; set; } = "";
    public decimal? Stundensatz { get; set; }
}

public class AbrechnungsPosition
{
    public int RolleId { get; set; }
    public string RolleName { get; set; } = "";
    public decimal Stunden { get; set; }
    public decimal Stundensatz { get; set; }
    public decimal Betrag => Stunden * Stundensatz;
}

public class MitarbeiterKostenPosition
{
    public Guid MitarbeiterId { get; set; }
    public string MitarbeiterName { get; set; } = "";
    public decimal Stunden { get; set; }
    public decimal Kostensatz { get; set; }
    public decimal Betrag => Stunden * Kostensatz;
}

public class BerechneterAufwandDto
{
    public string Belegnummer { get; set; } = "";
    public string Lieferant { get; set; } = "";
    public decimal Netto { get; set; }
    public decimal Brutto { get; set; }
}

public class ProjektAbrechnung
{
    public List<AbrechnungsPosition> Positionen { get; set; } = new();
    public List<AbrechnungsPosition> BerechnetePositionen { get; set; } = new();
    public List<BerechneterAufwandDto> BerechneteAufwaende { get; set; } = new();
    public List<MitarbeiterKostenPosition> Arbeitskosten { get; set; } = new();
    public decimal MaterialNetto { get; set; }
    public decimal MaterialBrutto { get; set; }
    public decimal MaterialBerechnedNetto { get; set; }
    public decimal MaterialBerechnedBrutto { get; set; }
    public decimal GesamtNetto => Positionen.Sum(p => p.Betrag) + MaterialNetto;
    public decimal ArbeitszeitkostenNetto => Arbeitskosten.Sum(p => p.Betrag);
}
