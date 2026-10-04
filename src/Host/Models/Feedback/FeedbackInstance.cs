using Kuestencode.Core.Models;

namespace Kuestencode.Werkbank.Host.Models.Feedback;

/// <summary>Eine beim Hub registrierte Kunden-Werkbank (Rolle Hub).</summary>
public class FeedbackInstance : BaseEntity
{
    public string Name { get; set; } = "";

    /// <summary>SHA-256 des API-Keys; der Klartext wird nur einmal beim Erzeugen angezeigt.</summary>
    public string ApiKeyHash { get; set; } = "";

    public bool IsActive { get; set; } = true;
}
