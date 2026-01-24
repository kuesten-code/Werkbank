using Kuestencode.Core.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Kuestencode.Faktura.Services.Email;

/// <summary>
/// Wrapper for MailKit SMTP client providing testability and abstraction
/// </summary>
public class SmtpClientWrapper : ISmtpClient
{
    private readonly PasswordEncryptionService _passwordEncryption;
    private readonly ILogger<SmtpClientWrapper> _logger;

    public SmtpClientWrapper(
        PasswordEncryptionService passwordEncryption,
        ILogger<SmtpClientWrapper> logger)
    {
        _passwordEncryption = passwordEncryption;
        _logger = logger;
    }

    public async Task SendAsync(MimeMessage message, Company company)
    {
        ValidateCompanySettings(company);

        using var client = new SmtpClient();
        client.Timeout = 15000;
        var decryptedPassword = _passwordEncryption.Decrypt(company.SmtpPassword!);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        try
        {
            _logger.LogInformation(
                "SMTP connect to {Host}:{Port} (UseSsl={UseSsl})",
                company.SmtpHost,
                company.SmtpPort,
                company.SmtpUseSsl);

            await client.ConnectAsync(
                company.SmtpHost,
                company.SmtpPort!.Value,
                company.SmtpUseSsl ? SecureSocketOptions.Auto : SecureSocketOptions.None,
                cts.Token);

            if (!string.IsNullOrWhiteSpace(company.SmtpUsername) && !string.IsNullOrWhiteSpace(decryptedPassword))
            {
                await client.AuthenticateAsync(company.SmtpUsername, decryptedPassword, cts.Token);
            }

            await client.SendAsync(message, cts.Token);
            await client.DisconnectAsync(true, cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException("SMTP-Versand hat zu lange gedauert. Bitte SMTP-Host/Port/SSL pruefen.");
        }
    }

    public async Task<(bool success, string? errorMessage)> TestConnectionAsync(Company company)
    {
        try
        {
            if (!IsEmailConfigured(company))
            {
                return (false, "E-Mail-Einstellungen sind unvollständig. Bitte füllen Sie alle erforderlichen Felder aus.");
            }

            using var client = new SmtpClient();
            client.Timeout = 10000;
            var decryptedPassword = _passwordEncryption.Decrypt(company.SmtpPassword!);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            try
            {
                await client.ConnectAsync(
                    company.SmtpHost,
                    company.SmtpPort!.Value,
                    company.SmtpUseSsl ? SecureSocketOptions.Auto : SecureSocketOptions.None,
                    cts.Token);
            }
            catch (Exception)
            {
                return (false, "Verbindung zum Server fehlgeschlagen. Bitte prüfen Sie den SMTP-Server und Port.");
            }

            if (!string.IsNullOrWhiteSpace(company.SmtpUsername) && !string.IsNullOrWhiteSpace(decryptedPassword))
            {
                try
                {
                    await client.AuthenticateAsync(company.SmtpUsername, decryptedPassword, cts.Token);
                }
                catch (Exception)
                {
                    await client.DisconnectAsync(true, cts.Token);
                    return (false, "Anmeldung fehlgeschlagen. Bitte prüfen Sie Benutzername und Passwort.");
                }
            }

            await client.DisconnectAsync(true, cts.Token);

            _logger.LogInformation("SMTP E-Mail-Konfigurationstest erfolgreich");
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E-Mail-Konfigurationstest fehlgeschlagen");
            return (false, $"Unerwarteter Fehler: {ex.Message}");
        }
    }

    private void ValidateCompanySettings(Company company)
    {
        if (!IsEmailConfigured(company))
        {
            throw new InvalidOperationException("E-Mail-Versand ist nicht konfiguriert. Bitte konfigurieren Sie die E-Mail-Einstellungen.");
        }
    }

    private bool IsEmailConfigured(Company company)
    {
        return !string.IsNullOrWhiteSpace(company.EmailSenderEmail) &&
               !string.IsNullOrWhiteSpace(company.SmtpHost) &&
               company.SmtpPort.HasValue &&
               !string.IsNullOrWhiteSpace(company.SmtpUsername) &&
               !string.IsNullOrWhiteSpace(company.SmtpPassword);
    }
}
