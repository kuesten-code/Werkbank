namespace Kuestencode.Shared.Contracts.Host;

/// <summary>
/// Request zum Versand einer Email über den zentralen Host-Email-Service.
/// ContentHtml/ContentText sind reine Inhalts-Fragmente — Host wickelt sie in das
/// einheitliche Firmen-Layout (Farben, Header, Anrede, Grußformel, Signatur, Footer) ein.
/// </summary>
public record SendEmailRequest
{
    public string RecipientEmail { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string ContentHtml { get; init; } = string.Empty;
    public string? ContentText { get; init; }
    public string? CcEmails { get; init; }
    public string? BccEmails { get; init; }
    public string? Greeting { get; init; }
    public bool IncludeClosing { get; init; } = true;
    public List<EmailAttachmentDto> Attachments { get; init; } = [];
}

public record EmailAttachmentDto
{
    public string FileName { get; init; } = string.Empty;
    public byte[] Content { get; init; } = [];
    public string ContentType { get; init; } = "application/octet-stream";
}

public record SendEmailResultDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
