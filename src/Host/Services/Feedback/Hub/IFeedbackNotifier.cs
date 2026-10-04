using Kuestencode.Werkbank.Host.Models.Feedback;

namespace Kuestencode.Werkbank.Host.Services.Feedback.Hub;

public interface IFeedbackNotifier
{
    /// <summary>
    /// Reiner Hinweis per Mail — Quelle der Wahrheit bleibt der Hub. Fehler werden nur
    /// geloggt und dürfen das Anlegen der Meldung nie scheitern lassen.
    /// </summary>
    Task NotifyNewReportAsync(FeedbackReport report, string instanceName);
}
