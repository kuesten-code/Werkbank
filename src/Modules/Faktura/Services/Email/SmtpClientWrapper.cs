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
        var decryptedPassword = _passwordEncryption.Decrypt(company.SmtpPassword!);

        await client.ConnectAsync(
            company.SmtpHost,
            company.SmtpPort!.Value,
            company.SmtpUseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

        if (!string.IsNullOrWhiteSpace(company.SmtpUsername) && !string.IsNullOrWhiteSpace(decryptedPassword))
        {
            await client.AuthenticateAsync(company.SmtpUsername, decryptedPassword);
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
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
            var decryptedPassword = _passwordEncryption.Decrypt(company.SmtpPassword!);

            try
            {
                await client.ConnectAsync(
                    company.SmtpHost,
                    company.SmtpPort!.Value,
                    company.SmtpUseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
            }
            catch (Exception)
            {
                return (false, "Verbindung zum Server fehlgeschlagen. Bitte prüfen Sie den SMTP-Server und Port.");
            }

            if (!string.IsNullOrWhiteSpace(company.SmtpUsername) && !string.IsNullOrWhiteSpace(decryptedPassword))
            {
                try
                {
                    await client.AuthenticateAsync(company.SmtpUsername, decryptedPassword);
                }
                catch (Exception)
                {
                    await client.DisconnectAsync(true);
                    return (false, "Anmeldung fehlgeschlagen. Bitte prüfen Sie Benutzername und Passwort.");
                }
            }

            await client.DisconnectAsync(true);

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
