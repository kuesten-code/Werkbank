using FluentAssertions;
using Kuestencode.Core.Interfaces;
using Kuestencode.Shared.Contracts.Feedback;
using Kuestencode.Werkbank.Host.Models.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback.Hub;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Kuestencode.Host.Tests.Services.Feedback;

public class FeedbackMailNotifierTests
{
    private readonly Mock<IFeedbackSettingsService> _settingsService = new();
    private readonly Mock<IEmailEngine> _emailEngine = new();
    private readonly FeedbackMailNotifier _notifier;
    private readonly FeedbackReport _report = new() { Id = 12, Type = FeedbackType.Bug, Module = "Faktura", Title = "<b>Fehler</b>" };

    public FeedbackMailNotifierTests()
    {
        _notifier = new FeedbackMailNotifier(_settingsService.Object, _emailEngine.Object, NullLogger<FeedbackMailNotifier>.Instance);
    }

    private void SetupSettings(bool notify, string? email) =>
        _settingsService.Setup(s => s.GetSettingsAsync())
            .ReturnsAsync(new FeedbackSettings { NotifyOnNewReport = notify, NotificationEmail = email });

    private void VerifyMailSent(Times times) =>
        _emailEngine.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<IEnumerable<EmailAttachment>?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<bool>()), times);

    [Fact]
    public async Task Aktiviert_SendetHinweisMitEscaptemTitel()
    {
        SetupSettings(true, "ich@example.com");
        string? html = null;
        _emailEngine.Setup(e => e.SendEmailAsync("ich@example.com", It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<IEnumerable<EmailAttachment>?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<bool>()))
            .Callback<string, string, string, string?, IEnumerable<EmailAttachment>?, string?, string?, string?, bool>(
                (_, _, content, _, _, _, _, _, _) => html = content)
            .ReturnsAsync(true);

        await _notifier.NotifyNewReportAsync(_report, "Kunde A");

        html.Should().Contain("&lt;b&gt;Fehler&lt;/b&gt;").And.Contain("Kunde A");
    }

    [Theory]
    [InlineData(false, "ich@example.com")]
    [InlineData(true, null)]
    [InlineData(true, " ")]
    public async Task Deaktiviert_OderOhneEmpfaenger_SendetNichts(bool notify, string? email)
    {
        SetupSettings(notify, email);

        await _notifier.NotifyNewReportAsync(_report, "Kunde A");

        VerifyMailSent(Times.Never());
    }

    [Fact]
    public async Task MailFehler_WirdGeschlucktUndNurGeloggt()
    {
        SetupSettings(true, "ich@example.com");
        _emailEngine.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<IEnumerable<EmailAttachment>?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<bool>()))
            .ThrowsAsync(new InvalidOperationException("SMTP nicht konfiguriert"));

        var act = () => _notifier.NotifyNewReportAsync(_report, "Kunde A");

        await act.Should().NotThrowAsync();
    }
}
