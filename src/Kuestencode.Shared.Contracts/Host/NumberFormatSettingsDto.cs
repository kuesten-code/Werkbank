namespace Kuestencode.Shared.Contracts.Host;

public record NumberFormatSettingsDto
{
    public string InvoiceFormat { get; init; } = "YYYY-XXXX";
    public string QuoteFormat { get; init; } = "ANG-YYYY-XXXXX";
    public string ProjectFormat { get; init; } = "P-YYYY-XXXX";
    public string IncomingInvoiceFormat { get; init; } = "ER-YYYY-XXXX";
}

public record UpdateNumberFormatSettingsRequest
{
    public string InvoiceFormat { get; init; } = string.Empty;
    public string QuoteFormat { get; init; } = string.Empty;
    public string ProjectFormat { get; init; } = string.Empty;
    public string IncomingInvoiceFormat { get; init; } = string.Empty;
}
