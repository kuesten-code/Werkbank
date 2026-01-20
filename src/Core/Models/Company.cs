using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Kuestencode.Core.Enums;
using Kuestencode.Core.Validation;

namespace Kuestencode.Core.Models;

/// <summary>
/// Represents company/business information.
/// This is the central entity for company data across all modules.
/// </summary>
public class Company : BaseEntity
{
    // === Grunddaten ===

    [Required(ErrorMessage = "Vollständiger Name ist erforderlich")]
    [MaxLength(200)]
    [FullName]
    public string OwnerFullName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? BusinessName { get; set; }

    [Required(ErrorMessage = "Adresse ist erforderlich")]
    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "PLZ ist erforderlich")]
    [GermanPostalCode]
    [MaxLength(10)]
    public string PostalCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Stadt ist erforderlich")]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Land ist erforderlich")]
    [MaxLength(100)]
    public string Country { get; set; } = "Deutschland";

    // === Steuer & Recht ===

    [Required(ErrorMessage = "Steuernummer ist erforderlich")]
    [MaxLength(50)]
    public string TaxNumber { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? VatId { get; set; }

    public bool IsKleinunternehmer { get; set; } = true;

    // === Bankverbindung ===

    [Required(ErrorMessage = "Bankname ist erforderlich")]
    [MaxLength(100)]
    public string BankName { get; set; } = string.Empty;

    [Required(ErrorMessage = "IBAN ist erforderlich")]
    [Iban]
    [MaxLength(50)]
    public string BankAccount { get; set; } = string.Empty;

    [MaxLength(11)]
    public string? Bic { get; set; }

    [MaxLength(200)]
    public string? AccountHolder { get; set; }

    // === Kontakt ===

    [Required(ErrorMessage = "Email ist erforderlich")]
    [EmailAddress(ErrorMessage = "Ungültige Email-Adresse")]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(200)]
    [Url(ErrorMessage = "Ungültige URL")]
    public string? Website { get; set; }

    // === Rechnungseinstellungen ===

    public int DefaultPaymentTermDays { get; set; } = 14;

    [MaxLength(10)]
    public string? InvoiceNumberPrefix { get; set; }

    [MaxLength(1000)]
    public string? FooterText { get; set; }

    // === Logo ===

    public byte[]? LogoData { get; set; }

    [MaxLength(100)]
    public string? LogoContentType { get; set; }

    // === XRechnung/PEPPOL ===

    public string? EndpointId { get; set; }
    public string? EndpointSchemeId { get; set; }

    // === SMTP Email Settings ===

    [MaxLength(200)]
    public string? SmtpHost { get; set; }

    public int? SmtpPort { get; set; }

    public bool SmtpUseSsl { get; set; } = true;

    [MaxLength(200)]
    public string? SmtpUsername { get; set; }

    [MaxLength(500)]
    public string? SmtpPassword { get; set; }

    [EmailAddress(ErrorMessage = "Ungültige Email-Adresse")]
    [MaxLength(200)]
    public string? EmailSenderEmail { get; set; }

    [MaxLength(200)]
    public string? EmailSenderName { get; set; }

    [MaxLength(2000)]
    public string? EmailSignature { get; set; }

    // === Email Design Settings ===

    public EmailLayout EmailLayout { get; set; } = EmailLayout.Klar;

    [MaxLength(7)]
    public string EmailPrimaryColor { get; set; } = "#0F2A3D";

    [MaxLength(7)]
    public string EmailAccentColor { get; set; } = "#3FA796";

    [MaxLength(500)]
    public string? EmailGreeting { get; set; }

    [MaxLength(500)]
    public string? EmailClosing { get; set; }

    // === PDF Design Settings ===

    public PdfLayout PdfLayout { get; set; } = PdfLayout.Klar;

    [MaxLength(7)]
    public string PdfPrimaryColor { get; set; } = "#1f3a5f";

    [MaxLength(7)]
    public string PdfAccentColor { get; set; } = "#3FA796";

    [MaxLength(500)]
    public string? PdfHeaderText { get; set; }

    [MaxLength(1000)]
    public string? PdfFooterText { get; set; }

    [MaxLength(500)]
    public string? PdfPaymentNotice { get; set; }

    // === Computed Properties ===

    /// <summary>
    /// Returns the display name (BusinessName if set, otherwise OwnerFullName).
    /// </summary>
    [NotMapped]
    public string DisplayName => !string.IsNullOrWhiteSpace(BusinessName) ? BusinessName : OwnerFullName;

    // === Methods ===

    /// <summary>
    /// Returns the full formatted address.
    /// </summary>
    public string GetFormattedAddress()
    {
        return $"{Address}\n{PostalCode} {City}\n{Country}";
    }

    /// <summary>
    /// Checks if SMTP email is properly configured.
    /// </summary>
    public bool IsEmailConfigured()
    {
        return !string.IsNullOrWhiteSpace(EmailSenderEmail) &&
               !string.IsNullOrWhiteSpace(SmtpHost) &&
               SmtpPort.HasValue &&
               !string.IsNullOrWhiteSpace(SmtpUsername) &&
               !string.IsNullOrWhiteSpace(SmtpPassword);
    }

    /// <summary>
    /// Checks if essential company data is filled.
    /// </summary>
    public bool HasRequiredData()
    {
        return !string.IsNullOrWhiteSpace(OwnerFullName) &&
               !string.IsNullOrWhiteSpace(Address) &&
               !string.IsNullOrWhiteSpace(PostalCode) &&
               !string.IsNullOrWhiteSpace(City) &&
               !string.IsNullOrWhiteSpace(TaxNumber) &&
               !string.IsNullOrWhiteSpace(Email) &&
               !string.IsNullOrWhiteSpace(BankName) &&
               !string.IsNullOrWhiteSpace(BankAccount);
    }
}
