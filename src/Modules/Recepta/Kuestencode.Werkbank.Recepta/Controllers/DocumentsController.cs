using Kuestencode.Werkbank.Recepta.Controllers.Dtos;
using Kuestencode.Werkbank.Recepta.Domain.Dtos;
using Kuestencode.Werkbank.Recepta.Domain.Enums;
using Kuestencode.Werkbank.Recepta.Services;
using Kuestencode.Shared.Contracts.Recepta;
using Microsoft.AspNetCore.Mvc;

namespace Kuestencode.Werkbank.Recepta.Controllers;

[ApiController]
[Route("api/recepta/documents")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IOcrService _ocrService;
    private readonly IOcrPatternService _patternService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentService documentService,
        IOcrService ocrService,
        IOcrPatternService patternService,
        ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _ocrService = ocrService;
        _patternService = patternService;
        _logger = logger;
    }

    /// <summary>
    /// Lädt alle Belege mit optionalem Filter.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] string? category = null,
        [FromQuery] Guid? supplierId = null,
        [FromQuery] Guid? projectId = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] string? search = null)
    {
        var filter = new DocumentFilterDto
        {
            SupplierId = supplierId,
            ProjectId = projectId,
            From = from,
            To = to,
            Search = search
        };

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<DocumentStatus>(status, true, out var parsedStatus))
        {
            filter.Status = parsedStatus;
        }

        if (!string.IsNullOrEmpty(category) && Enum.TryParse<DocumentCategory>(category, true, out var parsedCategory))
        {
            filter.Category = parsedCategory;
        }

        var documents = await _documentService.GetAllAsync(filter);
        return Ok(documents);
    }

    /// <summary>
    /// Lädt einen Beleg anhand der ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentDto>> GetById(Guid id)
    {
        var document = await _documentService.GetByIdAsync(id);
        if (document == null)
        {
            return NotFound();
        }

        return Ok(document);
    }

    /// <summary>
    /// Erstellt einen neuen Beleg.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<DocumentDto>> Create([FromBody] CreateDocumentRequest request)
    {
        try
        {
            if (!Enum.TryParse<DocumentCategory>(request.Category, true, out var category))
            {
                category = DocumentCategory.Other;
            }

            var dto = new CreateDocumentDto
            {
                DocumentNumber = request.DocumentNumber,
                SupplierId = request.SupplierId,
                InvoiceNumber = request.InvoiceNumber,
                InvoiceDate = request.InvoiceDate,
                DueDate = request.DueDate,
                AmountNet = request.AmountNet,
                TaxRate = request.TaxRate,
                AmountTax = request.AmountTax,
                AmountGross = request.AmountGross,
                Category = category,
                ProjectId = request.ProjectId,
                OcrRawText = request.OcrRawText,
                Notes = request.Notes
            };

            var document = await _documentService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = document.Id }, document);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Erstellt einen Beleg-Vorschlag aus einem Scan (OCR + Pattern-Extraktion).
    /// </summary>
    [HttpPost("scan")]
    public async Task<ActionResult<ScanResultDto>> CreateFromScan(IFormFile file)
    {
        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _documentService.CreateFromScanAsync(stream, file.FileName);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Scan von '{FileName}'", file.FileName);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Aktualisiert einen Beleg.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DocumentDto>> Update(Guid id, [FromBody] UpdateDocumentRequest request)
    {
        try
        {
            if (!Enum.TryParse<DocumentCategory>(request.Category, true, out var category))
            {
                category = DocumentCategory.Other;
            }

            var dto = new UpdateDocumentDto
            {
                SupplierId = request.SupplierId,
                InvoiceNumber = request.InvoiceNumber,
                InvoiceDate = request.InvoiceDate,
                DueDate = request.DueDate,
                AmountNet = request.AmountNet,
                TaxRate = request.TaxRate,
                AmountTax = request.AmountTax,
                AmountGross = request.AmountGross,
                Category = category,
                ProjectId = request.ProjectId,
                Notes = request.Notes
            };

            var document = await _documentService.UpdateAsync(id, dto);
            return Ok(document);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("nicht gefunden"))
            {
                return NotFound(new { error = ex.Message });
            }
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Ändert den Status eines Belegs.
    /// </summary>
    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeDocumentStatusRequest request)
    {
        try
        {
            if (!Enum.TryParse<DocumentStatus>(request.NewStatus, true, out var newStatus))
            {
                return BadRequest(new { error = $"Ungültiger Status: {request.NewStatus}" });
            }

            await _documentService.ChangeStatusAsync(id, newStatus);
            return Ok(new { message = $"Status geändert auf '{newStatus}'." });
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("nicht gefunden"))
            {
                return NotFound(new { error = ex.Message });
            }
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Triggert den Lerneffekt für einen Beleg.
    /// </summary>
    [HttpPost("{id:guid}/learn")]
    public async Task<IActionResult> LearnPatterns(Guid id)
    {
        try
        {
            await _documentService.LearnPatternsAsync(id);
            return Ok(new { message = "Patterns gelernt." });
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("nicht gefunden"))
            {
                return NotFound(new { error = ex.Message });
            }
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Löscht einen Beleg (nur bei Status Draft).
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _documentService.DeleteAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("nicht gefunden"))
            {
                return NotFound(new { error = ex.Message });
            }
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Generiert die nächste verfügbare Belegnummer.
    /// </summary>
    [HttpGet("next-number")]
    public async Task<ActionResult<string>> GetNextNumber()
    {
        var number = await _documentService.GenerateDocumentNumberAsync();
        return Ok(new { number });
    }

    /// <summary>
    /// Führt OCR auf einer hochgeladenen Datei aus (ohne sie an einen Beleg zu binden).
    /// </summary>
    [HttpPost("ocr")]
    public async Task<ActionResult> ExtractText(IFormFile file)
    {
        try
        {
            await using var stream = file.OpenReadStream();
            var text = await _ocrService.ExtractTextAsync(stream, file.FileName);
            return Ok(new { text, fileName = file.FileName, charCount = text.Length });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR-Fehler bei Datei '{FileName}'", file.FileName);
            return BadRequest(new { error = $"OCR-Fehler: {ex.Message}" });
        }
    }

    /// <summary>
    /// Lernt ein OCR-Pattern aus einem Benutzerwert.
    /// </summary>
    [HttpPost("ocr/learn")]
    public async Task<IActionResult> LearnPattern([FromBody] LearnPatternRequest request)
    {
        try
        {
            await _patternService.LearnPatternAsync(
                request.SupplierId, request.FieldName, request.OcrText, request.UserValue);
            return Ok(new { message = "Pattern gelernt." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Lernen des Patterns für Feld '{FieldName}'", request.FieldName);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Extrahiert Felder aus einem OCR-Text anhand gelernter und generischer Patterns.
    /// </summary>
    [HttpPost("ocr/extract")]
    public async Task<ActionResult<Dictionary<string, string>>> ExtractFields([FromBody] ExtractFieldsRequest request)
    {
        try
        {
            var fields = await _patternService.ExtractFieldsAsync(request.SupplierId, request.OcrText);
            return Ok(fields);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler bei der Feld-Extraktion");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Lädt alle Belege eines Projekts (für Cross-Modul-Integration mit Acta).
    /// </summary>
    [HttpGet("project/{projectId:guid}")]
    public async Task<ActionResult<List<ReceptaDocumentDto>>> GetByProject(Guid projectId)
    {
        var filter = new DocumentFilterDto { ProjectId = projectId };
        var documents = await _documentService.GetAllAsync(filter);

        var result = documents.Select(d => new ReceptaDocumentDto
        {
            Id = d.Id,
            DocumentNumber = d.DocumentNumber,
            SupplierName = d.SupplierName,
            InvoiceNumber = d.InvoiceNumber,
            InvoiceDate = d.InvoiceDate,
            AmountNet = d.AmountNet,
            AmountGross = d.AmountGross,
            Category = d.Category,
            Status = d.Status
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// Liefert die Kostenübersicht eines Projekts (Aufwand extern für Acta).
    /// </summary>
    [HttpGet("project/{projectId:guid}/expenses")]
    public async Task<ActionResult<ProjectExpensesResponseDto>> GetProjectExpenses(Guid projectId)
    {
        var filter = new DocumentFilterDto { ProjectId = projectId };
        var documents = await _documentService.GetAllAsync(filter);
        var docList = documents.ToList();

        var response = new ProjectExpensesResponseDto
        {
            ProjectId = projectId,
            TotalNet = docList.Sum(d => d.AmountNet),
            TotalGross = docList.Sum(d => d.AmountGross),
            DocumentCount = docList.Count,
            Documents = docList.Select(d => new ReceptaDocumentDto
            {
                Id = d.Id,
                DocumentNumber = d.DocumentNumber,
                SupplierName = d.SupplierName,
                InvoiceNumber = d.InvoiceNumber,
                InvoiceDate = d.InvoiceDate,
                AmountNet = d.AmountNet,
                AmountGross = d.AmountGross,
                Category = d.Category,
                Status = d.Status
            }).ToList()
        };

        return Ok(response);
    }
}
