namespace Kuestencode.Werkbank.Contracta.Domain.Entities;

public class Abrechnungshistorie
{
    public Guid Id { get; set; }
    public Guid WartungsvertragId { get; set; }
    public DateTime Abrechnungsdatum { get; set; }
    public int? RechnungId { get; set; }
    public string? Rechnungsnummer { get; set; }
    public decimal Betrag { get; set; }
}
