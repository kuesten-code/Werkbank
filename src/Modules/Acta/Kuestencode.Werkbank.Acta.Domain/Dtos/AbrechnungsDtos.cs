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

public class ProjektAbrechnung
{
    public List<AbrechnungsPosition> Positionen { get; set; } = new();
    public List<AbrechnungsPosition> BerechnetePositionen { get; set; } = new();
    public decimal MaterialNetto { get; set; }
    public decimal MaterialBrutto { get; set; }
    public decimal GesamtNetto => Positionen.Sum(p => p.Betrag) + MaterialNetto;
}
