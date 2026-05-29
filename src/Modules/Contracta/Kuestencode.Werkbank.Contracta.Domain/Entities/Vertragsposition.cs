namespace Kuestencode.Werkbank.Contracta.Domain.Entities;

public class Vertragsposition
{
    public Guid Id { get; set; }
    public Guid WartungsvertragId { get; set; }
    public int Position { get; set; }
    public string Text { get; set; } = string.Empty;
    public decimal Menge { get; set; }
    public decimal Einzelpreis { get; set; }
    public decimal Steuersatz { get; set; }

    public decimal Positionssumme => Menge * Einzelpreis;
}
