using System.Net;
using Kuestencode.Core.Interfaces;
using Kuestencode.Werkbank.Host.Models.Feedback;

namespace Kuestencode.Werkbank.Host.Services.Feedback.Hub;

public class FeedbackMailNotifier : IFeedbackNotifier
{
    private readonly IFeedbackSettingsService _settingsService;
    private readonly IEmailEngine _emailEngine;
    private readonly ILogger<FeedbackMailNotifier> _logger;

    public FeedbackMailNotifier(IFeedbackSettingsService settingsService, IEmailEngine emailEngine, ILogger<FeedbackMailNotifier> logger)
    {
        _settingsService = settingsService;
        _emailEngine = emailEngine;
        _logger = logger;
    }

    public async Task NotifyNewReportAsync(FeedbackReport report, string instanceName)
    {
        try
        {
            var settings = await _settingsService.GetSettingsAsync();
            if (!settings.NotifyOnNewReport || string.IsNullOrWhiteSpace(settings.NotificationEmail))
                return;

            var subject = $"Neue Meldung #{report.Id} ({report.Type}) von {instanceName}: {report.Title}";
            var html = $"""
                <p>Im Feedback-Hub ist eine neue Meldung eingegangen.</p>
                <p><strong>Kunde:</strong> {Encode(instanceName)}<br/>
                <strong>Typ:</strong> {report.Type}<br/>
                <strong>Modul:</strong> {Encode(report.Module ?? "–")}<br/>
                <strong>Titel:</strong> {Encode(report.Title)}</p>
                <p>Details im Hub unter „Feedback-Hub“, Meldung #{report.Id}.</p>
                """;

            await _emailEngine.SendEmailAsync(settings.NotificationEmail, subject, html, includeClosing: false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Benachrichtigung zu Feedback-Meldung {ReportId} konnte nicht gesendet werden", report.Id);
        }
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
