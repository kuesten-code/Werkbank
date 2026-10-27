namespace Kuestencode.Werkbank.Locatio.Domain.Entities;

/// <summary>
/// Eine Rate, getrennt nach Zins und Tilgung. Nur der Zinsanteil ist Werbungskosten.
/// </summary>
public class Darlehenszahlung
{
    public Guid Id { get; set; }
    public Guid DarlehenId { get; set; }
    public Darlehen? Darlehen { get; set; }

    public DateOnly Zahlungsdatum { get; set; }
    public decimal Zinsanteil { get; set; }
    public decimal Tilgungsanteil { get; set; }
    public string? Notiz { get; set; }

    public DateTime CreatedAt { get; set; }
}
