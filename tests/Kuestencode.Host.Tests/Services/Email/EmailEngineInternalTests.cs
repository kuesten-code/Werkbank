using FluentAssertions;
using Kuestencode.Core.Interfaces;
using Kuestencode.Core.Models;
using Kuestencode.Werkbank.Host.Services;
using Kuestencode.Werkbank.Host.Services.Email;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Kuestencode.Host.Tests.Services.Email;

public class EmailEngineInternalTests
{
    private readonly Mock<ICompanyService> _companyService = new();
    private readonly EmailEngine _engine;

    public EmailEngineInternalTests()
    {
        _engine = new EmailEngine(_companyService.Object, Array.Empty<IEmailTemplateProvider>(),
            NullLogger<EmailEngine>.Instance, new PasswordEncryptionService(new EphemeralDataProtectionProvider()));
    }

    [Fact]
    public async Task SendInternalEmailAsync_OhneSmtpKonfiguration_LiefertFalse()
    {
        _companyService.Setup(c => c.GetCompanyAsync()).ReturnsAsync(new Company());

        (await _engine.SendInternalEmailAsync("ich@example.com", "Betreff", "<p>x</p>", "x")).Should().BeFalse();
    }

    [Fact]
    public async Task SendInternalEmailAsync_SmtpNichtErreichbar_LiefertFalseStattZuWerfen()
    {
        _companyService.Setup(c => c.GetCompanyAsync()).ReturnsAsync(new Company
        {
            EmailSenderEmail = "hub@example.com",
            SmtpHost = "127.0.0.1",
            SmtpPort = 1,
            SmtpUsername = "user",
            SmtpPassword = "pass"
        });

        (await _engine.SendInternalEmailAsync("ich@example.com", "Betreff", "<p>x</p>", "x")).Should().BeFalse();
    }
}
