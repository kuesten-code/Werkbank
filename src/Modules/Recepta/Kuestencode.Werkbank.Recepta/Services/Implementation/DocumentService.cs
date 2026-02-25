using System.Globalization;
using Kuestencode.Werkbank.Recepta.Controllers.Dtos;
using Kuestencode.Werkbank.Recepta.Data.Repositories;
using Kuestencode.Werkbank.Recepta.Domain.Dtos;
using Kuestencode.Werkbank.Recepta.Domain.Entities;
using Kuestencode.Werkbank.Recepta.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Kuestencode.Werkbank.Recepta.Services;

/// <summary>
/// Service-Implementierung für Belegverwaltung.
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IOcrService _ocrService;
    private readonly IOcrPatternService _patternService;
    private readonly IXRechnungService _xRechnungService;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        IDocumentRepository documentRepository,
        ISupplierRepository supplierRepository,
        IOcrService ocrService,
        IOcrPatternService patternService,
        IXRechnungService xRechnungService,
        ILogger<DocumentService> logger)
    {
        _documentRepository = documentRepository;
        _supplierRepository = supplierRepository;
        _ocrService = ocrService;
        _patternService = patternService;
        _xRechnungService = xRechnungService;
        _logger = logger;
    }

    public async Task<IEnumerable<DocumentDto>> GetAllAsync(DocumentFilterDto filter)
    {
        var documents = await _documentRepository.GetAllAsync(
            filter.Status, filter.Category, filter.SupplierId, filter.ProjectId, filter.HasBeenAttached);

        IEnumerable<Document> result = documents;

        if (filter.From.HasValue)
        {
            result = result.Where(d => d.InvoiceDate >= filter.From.Value);
        }

        if (filter.To.HasValue)
        {
            result = result.Where(d => d.InvoiceDate <= filter.To.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.ToLower();
            result = result.Where(d =>
                d.DocumentNumber.ToLower().Contains(term) ||
                d.InvoiceNumber.ToLower().Contains(term) ||
                (d.Supplier?.Name.ToLower().Contains(term) ?? false) ||
                (d.Notes?.ToLower().Contains(term) ?? false));
        }

        return result.Select(MapToDto);
    }

    public async Task<DocumentDto?> GetByIdAsync(Guid id)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        return document != null ? MapToDto(document) : null;
    }

    public async Task<DocumentDto> CreateAsync(CreateDocumentDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DocumentNumber))
        {
            throw new InvalidOperationException("Belegnummer ist erforderlich.");
        }

        if (await _documentRepository.ExistsNumberAsync(dto.DocumentNumber))
        {
            throw new InvalidOperationException($"Belegnummer '{dto.DocumentNumber}' existiert bereits.");
        }

        if (string.IsNullOrWhiteSpace(dto.InvoiceNumber))
        {
            throw new InvalidOperationException("Rechnungsnummer ist erforderlich.");
        }

        var supplier = await _supplierRepository.GetByIdAsync(dto.SupplierId);
        if (supplier == null)
        {
            throw new InvalidOperationException("Lieferant muss ausgewählt werden.");
        }

        var document = new Document
        {
            Id = Guid.NewGuid(),
            DocumentNumber = dto.DocumentNumber,
            SupplierId = dto.SupplierId,
            InvoiceNumber = dto.InvoiceNumber,
            InvoiceDate = dto.InvoiceDate,
            DueDate = dto.DueDate,
            AmountNet = dto.AmountNet,
            TaxRate = dto.TaxRate,
            AmountTax = dto.AmountTax,
            AmountGross = dto.AmountGross,
            Category = dto.Category,
            Status = DocumentStatus.Draft,
            ProjectId = dto.ProjectId,
            OcrRawText = dto.OcrRawText,
            Notes = dto.Notes
        };

        await _documentRepository.AddAsync(document);

        // Reload mit Supplier-Navigation
        var created = await _documentRepository.GetByIdAsync(document.Id);
        return MapToDto(created!);
    }

    public async Task<ScanResultDto> CreateFromScanAsync(Stream file, string fileName)
    {
        // XRechnung/ZUGFeRD zuerst prüfen (100% Trefferquote)
        if (_xRechnungService.CanProcess(file, fileName))
        {
            file.Position = 0;
            return await CreateFromXRechnungAsync(file, fileName);
        }

        // Fallback auf OCR
        file.Position = 0;
        return await CreateFromOcrAsync(file, fileName);
    }

    private async Task<ScanResultDto> CreateFromXRechnungAsync(Stream file, string fileName)
    {
        var xData = await _xRechnungService.ParseAsync(file, fileName);

        var result = new ScanResultDto
        {
            IsXRechnung = true,
            XRechnungData = xData,
            InvoiceNumber = xData.InvoiceNumber,
            InvoiceDate = xData.InvoiceDate,
            DueDate = xData.DueDate,
            AmountNet = xData.AmountNet,
            AmountTax = xData.AmountTax,
            AmountGross = xData.AmountGross,
            TaxRate = xData.TaxRate,
            Iban = xData.SupplierIban
        };

        // Lieferant matchen: TaxId → IBAN → Name (sequentiell)
        Supplier? matchedSupplier = null;

        if (!string.IsNullOrWhiteSpace(xData.SupplierTaxId))
        {
            matchedSupplier = await _supplierRepository.FindByTaxIdAsync(xData.SupplierTaxId);
            if (matchedSupplier != null)
                _logger.LogInformation("XRechnung: Lieferant über USt-ID '{TaxId}' erkannt: {Name}", xData.SupplierTaxId, matchedSupplier.Name);
        }

        if (matchedSupplier == null && !string.IsNullOrWhiteSpace(xData.SupplierIban))
        {
            matchedSupplier = await _supplierRepository.FindByIbanAsync(xData.SupplierIban);
            if (matchedSupplier != null)
                _logger.LogInformation("XRechnung: Lieferant über IBAN erkannt: {Name}", matchedSupplier.Name);
        }

        if (matchedSupplier == null && !string.IsNullOrWhiteSpace(xData.SupplierName))
        {
            matchedSupplier = await _supplierRepository.FindByNameAsync(xData.SupplierName);
            if (matchedSupplier != null)
                _logger.LogInformation("XRechnung: Lieferant über Name erkannt: {Name}", matchedSupplier.Name);
        }

        if (matchedSupplier != null)
        {
            result.SuggestedSupplierId = matchedSupplier.Id;
            result.SuggestedSupplierName = matchedSupplier.Name;
        }

        _logger.LogInformation(
            "XRechnung/ZUGFeRD erkannt: Rechnung {InvoiceNumber}, Lieferant: {SupplierName}",
            xData.InvoiceNumber ?? "—", result.SuggestedSupplierName ?? "nicht erkannt");

        return result;
    }

    private async Task<ScanResultDto> CreateFromOcrAsync(Stream file, string fileName)
    {
        var ocrText = await _ocrService.ExtractTextAsync(file, fileName);

        var result = new ScanResultDto
        {
            OcrRawText = ocrText
        };

        if (string.IsNullOrWhiteSpace(ocrText))
        {
            return result;
        }

        // Lieferant im OCR-Text suchen
        var suppliers = await _supplierRepository.GetAllAsync();
        foreach (var supplier in suppliers)
        {
            if (ocrText.Contains(supplier.Name, StringComparison.OrdinalIgnoreCase))
            {
                result.SuggestedSupplierId = supplier.Id;
                result.SuggestedSupplierName = supplier.Name;
                break;
            }
        }

        // Felder extrahieren (mit Lieferanten-Patterns falls vorhanden)
        var fields = await _patternService.ExtractFieldsAsync(result.SuggestedSupplierId, ocrText);
        result.ExtractedFields = fields;

        // Extrahierte Felder in typisierte Properties übertragen
        if (fields.TryGetValue("InvoiceNumber", out var invoiceNumber))
        {
            result.InvoiceNumber = invoiceNumber;
        }

        if (fields.TryGetValue("InvoiceDate", out var invoiceDateStr) &&
            DateOnly.TryParse(invoiceDateStr, CultureInfo.InvariantCulture, out var invoiceDate))
        {
            result.InvoiceDate = invoiceDate;
        }

        if (fields.TryGetValue("AmountNet", out var amountNetStr) &&
            decimal.TryParse(amountNetStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amountNet))
        {
            result.AmountNet = amountNet;
        }

        if (fields.TryGetValue("AmountGross", out var amountGrossStr) &&
            decimal.TryParse(amountGrossStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amountGross))
        {
            result.AmountGross = amountGross;
        }

        if (fields.TryGetValue("TaxRate", out var taxRateStr) &&
            decimal.TryParse(taxRateStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var taxRate))
        {
            result.TaxRate = taxRate;
        }

        if (fields.TryGetValue("IBAN", out var iban))
        {
            result.Iban = iban;
        }

        _logger.LogInformation(
            "OCR-Scan abgeschlossen: {FieldCount} Felder extrahiert, Lieferant: {SupplierName}",
            fields.Count, result.SuggestedSupplierName ?? "nicht erkannt");

        return result;
    }

    public async Task<DocumentDto> UpdateAsync(Guid id, UpdateDocumentDto dto)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document == null)
        {
            throw new InvalidOperationException($"Beleg mit ID {id} nicht gefunden.");
        }

        if (!document.IsEditable)
        {
            throw new InvalidOperationException(
                $"Beleg kann nicht bearbeitet werden. Status: {document.Status}");
        }

        if (string.IsNullOrWhiteSpace(dto.InvoiceNumber))
        {
            throw new InvalidOperationException("Rechnungsnummer ist erforderlich.");
        }

        var supplier = await _supplierRepository.GetByIdAsync(dto.SupplierId);
        if (supplier == null)
        {
            throw new InvalidOperationException("Lieferant muss ausgewählt werden.");
        }

        document.SupplierId = dto.SupplierId;
        document.InvoiceNumber = dto.InvoiceNumber;
        document.InvoiceDate = dto.InvoiceDate;
        document.DueDate = dto.DueDate;
        document.AmountNet = dto.AmountNet;
        document.TaxRate = dto.TaxRate;
        document.AmountTax = dto.AmountTax;
        document.AmountGross = dto.AmountGross;
        document.Category = dto.Category;
        document.ProjectId = dto.ProjectId;
        document.Notes = dto.Notes;

        await _documentRepository.UpdateAsync(document);
        return MapToDto(document);
    }

    public async Task ChangeStatusAsync(Guid id, DocumentStatus newStatus)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document == null)
        {
            throw new InvalidOperationException($"Beleg mit ID {id} nicht gefunden.");
        }

        var validTransition = (document.Status, newStatus) switch
        {
            (DocumentStatus.Draft, DocumentStatus.Booked) => true,
            (DocumentStatus.Booked, DocumentStatus.Paid) => true,
            (DocumentStatus.Booked, DocumentStatus.Draft) => true,
            _ => false
        };

        if (!validTransition)
        {
            throw new InvalidOperationException(
                $"Ungültiger Statusübergang von '{document.Status}' nach '{newStatus}'.");
        }

        document.Status = newStatus;
        await _documentRepository.UpdateAsync(document);
    }

    public async Task LearnPatternsAsync(Guid id)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document == null)
        {
            throw new InvalidOperationException($"Beleg mit ID {id} nicht gefunden.");
        }

        if (string.IsNullOrWhiteSpace(document.OcrRawText))
        {
            _logger.LogDebug("Kein OCR-Text für Beleg {DocumentId} vorhanden, Lernvorgang übersprungen", id);
            return;
        }

        var ocrText = document.OcrRawText;
        var supplierId = document.SupplierId;

        // Alle verfügbaren Felder lernen
        if (!string.IsNullOrWhiteSpace(document.InvoiceNumber))
        {
            await _patternService.LearnPatternAsync(supplierId, "InvoiceNumber", ocrText, document.InvoiceNumber);
        }

        await _patternService.LearnPatternAsync(supplierId, "InvoiceDate", ocrText,
            document.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        if (document.AmountNet != 0)
        {
            await _patternService.LearnPatternAsync(supplierId, "AmountNet", ocrText,
                document.AmountNet.ToString(CultureInfo.InvariantCulture));
        }

        if (document.AmountGross != 0)
        {
            await _patternService.LearnPatternAsync(supplierId, "AmountGross", ocrText,
                document.AmountGross.ToString(CultureInfo.InvariantCulture));
        }

        if (document.TaxRate != 0)
        {
            await _patternService.LearnPatternAsync(supplierId, "TaxRate", ocrText,
                document.TaxRate.ToString(CultureInfo.InvariantCulture));
        }

        _logger.LogInformation("Patterns für Beleg {DocumentId}, Lieferant {SupplierId} gelernt", id, supplierId);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _documentRepository.DeleteAsync(id);
    }

    public async Task<string> GenerateDocumentNumberAsync()
    {
        return await _documentRepository.GenerateDocumentNumberAsync();
    }

    public async Task UpdateOcrTextAsync(Guid id, string ocrText)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document == null)
        {
            throw new InvalidOperationException($"Beleg mit ID {id} nicht gefunden.");
        }

        document.OcrRawText = ocrText;
        await _documentRepository.UpdateAsync(document);
    }

    public async Task MarkAsAttachedAsync(IEnumerable<Guid> documentIds)
    {
        await _documentRepository.MarkAsAttachedAsync(documentIds);
    }

    private static DocumentDto MapToDto(Document document)
    {
        return new DocumentDto
        {
            Id = document.Id,
            DocumentNumber = document.DocumentNumber,
            SupplierId = document.SupplierId,
            SupplierName = document.Supplier?.Name ?? string.Empty,
            InvoiceNumber = document.InvoiceNumber,
            InvoiceDate = document.InvoiceDate,
            DueDate = document.DueDate,
            AmountNet = document.AmountNet,
            TaxRate = document.TaxRate,
            AmountTax = document.AmountTax,
            AmountGross = document.AmountGross,
            Category = document.Category.ToString(),
            Status = document.Status.ToString(),
            ProjectId = document.ProjectId,
            HasBeenAttached = document.HasBeenAttached,
            Notes = document.Notes,
            OcrRawText = document.OcrRawText,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt,
            IsOverdue = document.IsOverdue,
            FileCount = document.Files.Count
        };
    }
}
