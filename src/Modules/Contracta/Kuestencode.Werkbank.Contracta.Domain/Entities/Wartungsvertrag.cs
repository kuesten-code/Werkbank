using Kuestencode.Werkbank.Contracta.Domain.Enums;

namespace Kuestencode.Werkbank.Contracta.Domain.Entities;

public class Wartungsvertrag
{
    public Guid Id { get; set; }
    public string Vertragsnummer { get; set; } = string.Empty;
    public string Bezeichnung { get; set; } = string.Empty;
    public int KundeId { get; set; }

    public DateTime Startdatum { get; set; }
    public DateTime? Enddatum { get; set; }

    public Abrechnungsintervall Intervall { get; set; }
    public int? CustomIntervallTage { get; set; }

    public DateTime? LetzteAbrechnung { get; set; }
    public DateTime? NaechsteAbrechnung { get; set; }

    public VertragStatus Status { get; set; }

    public List<Vertragsposition> Positionen { get; set; } = new();
    public List<Abrechnungshistorie> Historien { get; set; } = new();

    public string? Notizen { get; set; }
    public DateTime ErstelltAm { get; set; }
    public DateTime? GeaendertAm { get; set; }
}
