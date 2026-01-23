namespace Kuestencode.Shared.Contracts.Host;

public record CompanyDto
{
    public int Id { get; init; }
    public string OwnerFullName { get; init; } = string.Empty;
    public string? BusinessName { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string TaxNumber { get; init; } = string.Empty;
    public string? VatId { get; init; }
    public bool IsKleinunternehmer { get; init; }
    public string BankName { get; init; } = string.Empty;
    public string BankAccount { get; init; } = string.Empty;
    public string? Bic { get; init; }
    public string? AccountHolder { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Website { get; init; }
    public int DefaultPaymentTermDays { get; init; }
    public string? InvoiceNumberPrefix { get; init; }
    public string? FooterText { get; init; }
    public byte[]? LogoData { get; init; }
    public string? LogoContentType { get; init; }
    public string? EndpointId { get; init; }
    public string? EndpointSchemeId { get; init; }

    // SMTP Settings
    public string? SmtpHost { get; init; }
    public int? SmtpPort { get; init; }
    public bool SmtpUseSsl { get; init; }
    public string? SmtpUsername { get; init; }
    public string? SmtpPassword { get; init; }
    public string? EmailSenderEmail { get; init; }
    public string? EmailSenderName { get; init; }
    public string? EmailSignature { get; init; }

    // Email Design
    public string EmailLayout { get; init; } = "Klar";
    public string EmailPrimaryColor { get; init; } = "#0F2A3D";
    public string EmailAccentColor { get; init; } = "#3FA796";
    public string? EmailGreeting { get; init; }
    public string? EmailClosing { get; init; }

    // PDF Design
    public string PdfLayout { get; init; } = "Klar";
    public string PdfPrimaryColor { get; init; } = "#1f3a5f";
    public string PdfAccentColor { get; init; } = "#3FA796";
    public string? PdfHeaderText { get; init; }
    public string? PdfFooterText { get; init; }
    public string? PdfPaymentNotice { get; init; }
}

public record CreateCompanyRequest
{
    public string OwnerFullName { get; init; } = string.Empty;
    public string? BusinessName { get; init; }
    public string Address { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = "Deutschland";
    public string TaxNumber { get; init; } = string.Empty;
    public string? VatId { get; init; }
    public bool IsKleinunternehmer { get; init; } = true;
    public string BankName { get; init; } = string.Empty;
    public string BankAccount { get; init; } = string.Empty;
    public string? Bic { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public int DefaultPaymentTermDays { get; init; } = 14;
}

public record UpdateCompanyRequest
{
    public string OwnerFullName { get; init; } = string.Empty;
    public string? BusinessName { get; init; }
    public string Address { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string TaxNumber { get; init; } = string.Empty;
    public string? VatId { get; init; }
    public bool IsKleinunternehmer { get; init; }
    public string BankName { get; init; } = string.Empty;
    public string BankAccount { get; init; } = string.Empty;
    public string? Bic { get; init; }
    public string? AccountHolder { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Website { get; init; }
    public int DefaultPaymentTermDays { get; init; }
    public string? InvoiceNumberPrefix { get; init; }
    public string? FooterText { get; init; }
    public string? EndpointId { get; init; }
    public string? EndpointSchemeId { get; init; }

    // SMTP Settings
    public string? SmtpHost { get; init; }
    public int? SmtpPort { get; init; }
    public bool? SmtpUseSsl { get; init; }
    public string? SmtpUsername { get; init; }
    public string? SmtpPassword { get; init; }
    public string? EmailSenderEmail { get; init; }
    public string? EmailSenderName { get; init; }
    public string? EmailSignature { get; init; }

    // Email Design
    public string? EmailLayout { get; init; }
    public string? EmailPrimaryColor { get; init; }
    public string? EmailAccentColor { get; init; }
    public string? EmailGreeting { get; init; }
    public string? EmailClosing { get; init; }

    // PDF Design
    public string? PdfLayout { get; init; }
    public string? PdfPrimaryColor { get; init; }
    public string? PdfAccentColor { get; init; }
    public string? PdfHeaderText { get; init; }
    public string? PdfFooterText { get; init; }
    public string? PdfPaymentNotice { get; init; }
}
