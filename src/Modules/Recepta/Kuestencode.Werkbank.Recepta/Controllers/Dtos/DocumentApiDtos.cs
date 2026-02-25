namespace Kuestencode.Werkbank.Recepta.Controllers.Dtos;

/// <summary>
/// API-Response DTO für einen Beleg.
/// </summary>
public class DocumentDto
{
    public Guid Id { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public decimal AmountNet { get; set; }
    public decimal TaxRate { get; set; }
    public decimal AmountTax { get; set; }
    public decimal AmountGross { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public bool HasBeenAttached { get; set; }
    public string? Notes { get; set; }
    public string? OcrRawText { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsOverdue { get; set; }
    public int FileCount { get; set; }
}

/// <summary>
/// API-Request DTO zum Erstellen eines Belegs.
/// </summary>
public class CreateDocumentRequest
{
    public string DocumentNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public decimal AmountNet { get; set; }
    public decimal TaxRate { get; set; }
    public decimal AmountTax { get; set; }
    public decimal AmountGross { get; set; }
    public string Category { get; set; } = "Other";
    public Guid? ProjectId { get; set; }
    public string? OcrRawText { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// API-Request DTO zum Aktualisieren eines Belegs.
/// </summary>
public class UpdateDocumentRequest
{
    public Guid SupplierId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public decimal AmountNet { get; set; }
    public decimal TaxRate { get; set; }
    public decimal AmountTax { get; set; }
    public decimal AmountGross { get; set; }
    public string Category { get; set; } = "Other";
    public Guid? ProjectId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// API-Request DTO für Statusänderung.
/// </summary>
public class ChangeDocumentStatusRequest
{
    public string NewStatus { get; set; } = string.Empty;
}

public class MarkDocumentsAttachedRequest
{
    public List<Guid> DocumentIds { get; set; } = new();
}

/// <summary>
/// API-Request DTO zum Lernen eines OCR-Patterns.
/// </summary>
public class LearnPatternRequest
{
    public Guid SupplierId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string OcrText { get; set; } = string.Empty;
    public string UserValue { get; set; } = string.Empty;
}

/// <summary>
/// API-Request DTO zur Feld-Extraktion aus OCR-Text.
/// </summary>
public class ExtractFieldsRequest
{
    public Guid? SupplierId { get; set; }
    public string OcrText { get; set; } = string.Empty;
}
