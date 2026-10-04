namespace Kuestencode.Werkbank.Host.Services.Email;

/// <summary>
/// Versand interner Hinweis-Mails (z. B. an den Betreiber) über den konfigurierten SMTP-Server —
/// ohne das Kunden-Layout aus Firmendaten, Anrede, Grußformel und Signatur, das
/// <see cref="Kuestencode.Core.Interfaces.IEmailEngine"/> um jeden Inhalt legt.
/// </summary>
public interface IInternalEmailSender
{
    /// <returns>false, wenn SMTP nicht konfiguriert ist oder der Versand scheitert.</returns>
    Task<bool> SendInternalEmailAsync(string recipientEmail, string subject, string contentHtml, string contentText);
}
