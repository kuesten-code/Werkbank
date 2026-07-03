using Kuestencode.Core.Interfaces;

namespace Kuestencode.Faktura.Services.Email;

/// <summary>
/// Eine versandfertige Email — reine Inhaltsdaten, kein MimeKit-Objekt.
/// Wird per <see cref="IEmailEngine"/> an den zentralen Host-Email-Service übergeben.
/// </summary>
public class EmailMessage
{
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public string? ContentText { get; set; }
    public string? CcEmails { get; set; }
    public string? BccEmails { get; set; }
    public string? Greeting { get; set; }
    public List<EmailAttachment> Attachments { get; set; } = [];
}
