using FluentAssertions;
using Kuestencode.Shared.Contracts.Feedback;
using Kuestencode.Werkbank.Host.Models.Feedback;
using Kuestencode.Werkbank.Host.Services.Email;
using Kuestencode.Werkbank.Host.Services.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback.Hub;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Kuestencode.Host.Tests.Services.Feedback;

public class FeedbackMailNotifierTests
{
    private readonly Mock<IFeedbackSettingsService> _settingsService = new();
    private readonly Mock<IInternalEmailSender> _emailSender = new();
    private readonly FeedbackMailNotifier _notifier;
    private readonly FeedbackReport _report = new()
    {
        Id = 12, Type = FeedbackType.Bug, Module = "Faktura", Title = "<b>Fehler</b>", Actual = "Absturz beim Speichern", ReporterName = "Erika"
    };

    public FeedbackMailNotifierTests()
    {
        _notifier = new FeedbackMailNotifier(_settingsService.Object, _emailSender.Object, NullLogger<FeedbackMailNotifier>.Instance);
    }

    private void SetupSettings(bool notify, string? email) =>
        _settingsService.Setup(s => s.GetSettingsAsync())
            .ReturnsAsync(new FeedbackSettings { NotifyOnNewReport = notify, NotificationEmail = email });

    [Fact]
    public async Task Aktiviert_SendetInterneMailMitEckdatenUndEscaptemTitel()
    {
        SetupSettings(true, "ich@example.com");
        string? subject = null, html = null, text = null;
        _emailSender.Setup(e => e.SendInternalEmailAsync("ich@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string, string>((_, s, h, t) => (subject, html, text) = (s, h, t))
            .ReturnsAsync(true);

        await _notifier.NotifyNewReportAsync(_report, "Kunde A");

        subject.Should().Be("[Feedback-Hub] #12 Bug von Kunde A: <b>Fehler</b>");
        html.Should().Contain("&lt;b&gt;Fehler&lt;/b&gt;").And.Contain("Kunde A").And.Contain("Faktura").And.Contain("Erika");
        text.Should().Contain("Absturz beim Speichern");
    }

    [Fact]
    public async Task Mail_EnthaeltKeineKundenAnredeOderGrussformel()
    {
        SetupSettings(true, "ich@example.com");
        string? html = null;
        _emailSender.Setup(e => e.SendInternalEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string, string>((_, _, h, _) => html = h)
            .ReturnsAsync(true);

        await _notifier.NotifyNewReportAsync(_report, "Kunde A");

        html.Should().NotContain("Sehr geehrte").And.NotContain("Mit freundlichen Grüßen");
    }

    [Theory]
    [InlineData(false, "ich@example.com")]
    [InlineData(true, null)]
    [InlineData(true, " ")]
    public async Task Deaktiviert_OderOhneEmpfaenger_SendetNichts(bool notify, string? email)
    {
        SetupSettings(notify, email);

        await _notifier.NotifyNewReportAsync(_report, "Kunde A");

        _emailSender.Verify(e => e.SendInternalEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task MailFehler_WirdGeschlucktUndNurGeloggt()
    {
        SetupSettings(true, "ich@example.com");
        _emailSender.Setup(e => e.SendInternalEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("SMTP kaputt"));

        var act = () => _notifier.NotifyNewReportAsync(_report, "Kunde A");

        await act.Should().NotThrowAsync();
    }
}
