using System.Net;
using Kuestencode.Werkbank.Host.Models.Feedback;
using Kuestencode.Werkbank.Host.Services.Email;

namespace Kuestencode.Werkbank.Host.Services.Feedback.Hub;

public class FeedbackMailNotifier : IFeedbackNotifier
{
    private readonly IFeedbackSettingsService _settingsService;
    private readonly IInternalEmailSender _emailSender;
    private readonly ILogger<FeedbackMailNotifier> _logger;

    public FeedbackMailNotifier(IFeedbackSettingsService settingsService, IInternalEmailSender emailSender, ILogger<FeedbackMailNotifier> logger)
    {
        _settingsService = settingsService;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task NotifyNewReportAsync(FeedbackReport report, string instanceName)
    {
        try
        {
            var settings = await _settingsService.GetSettingsAsync();
            if (!settings.NotifyOnNewReport || string.IsNullOrWhiteSpace(settings.NotificationEmail))
                return;

            var type = FeedbackLabels.Of(report.Type);
            var module = report.Module ?? "–";
            var subject = $"[Feedback-Hub] #{report.Id} {type} von {instanceName}: {report.Title}";

            var html = $"""
                <p>Neue Meldung im Feedback-Hub.</p>
                <table cellpadding="4">
                    <tr><td><strong>Meldung</strong></td><td>#{report.Id}</td></tr>
                    <tr><td><strong>Kunde</strong></td><td>{Encode(instanceName)}</td></tr>
                    <tr><td><strong>Typ</strong></td><td>{type}</td></tr>
                    <tr><td><strong>Modul</strong></td><td>{Encode(module)}</td></tr>
                    <tr><td><strong>Titel</strong></td><td>{Encode(report.Title)}</td></tr>
                    <tr><td><strong>Gemeldet von</strong></td><td>{Encode(report.ReporterName)}</td></tr>
                </table>
                <p style="white-space: pre-wrap;">{Encode(report.Actual)}</p>
                """;

            var text = $"""
                Neue Meldung im Feedback-Hub.

                Meldung:      #{report.Id}
                Kunde:        {instanceName}
                Typ:          {type}
                Modul:        {module}
                Titel:        {report.Title}
                Gemeldet von: {report.ReporterName}

                {report.Actual}
                """;

            await _emailSender.SendInternalEmailAsync(settings.NotificationEmail, subject, html, text);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Benachrichtigung zu Feedback-Meldung {ReportId} konnte nicht gesendet werden", report.Id);
        }
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
