using Microsoft.AspNetCore.Mvc;
using Kuestencode.Faktura.Models;
using Kuestencode.Faktura.Services;
using Kuestencode.Shared.Contracts.Faktura;
using Kuestencode.Shared.ApiClients;

namespace Kuestencode.Faktura.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly IPdfGeneratorService _pdfGeneratorService;
    private readonly IEmailService _emailService;
    private readonly IHostApiClient _hostApiClient;
    private readonly ILogger<InvoiceController> _logger;

    public InvoiceController(
        IInvoiceService invoiceService,
        IPdfGeneratorService pdfGeneratorService,
        IEmailService emailService,
        IHostApiClient hostApiClient,
        ILogger<InvoiceController> logger)
    {
        _invoiceService = invoiceService;
        _pdfGeneratorService = pdfGeneratorService;
        _emailService = emailService;
        _hostApiClient = hostApiClient;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<InvoiceDto>>> GetAll([FromQuery] string? status = null, [FromQuery] int? customerId = null)
    {
        try
        {
            List<Invoice> invoices;

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<InvoiceStatus>(status, out var invoiceStatus))
            {
                invoices = await _invoiceService.GetByStatusAsync(invoiceStatus);
            }
            else
            {
                invoices = await _invoiceService.GetAllAsync();
            }

            // Filter by customer if specified
            if (customerId.HasValue)
            {
                invoices = invoices.Where(i => i.CustomerId == customerId.Value).ToList();
            }

            var dtos = new List<InvoiceDto>();
            foreach (var invoice in invoices)
            {
                var dto = await MapToDtoAsync(invoice);
                dtos.Add(dto);
            }

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoices");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InvoiceDto>> GetById(int id)
    {
        try
        {
            var invoice = await _invoiceService.GetByIdAsync(id);
            if (invoice == null) return NotFound();

            var dto = await MapToDtoAsync(invoice);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice {InvoiceId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceDto>> Create([FromBody] CreateInvoiceRequest request)
    {
        try
        {
            var invoice = new Invoice
            {
                InvoiceDate = request.InvoiceDate,
                ServicePeriodStart = request.ServicePeriodStart,
                ServicePeriodEnd = request.ServicePeriodEnd,
                DueDate = request.DueDate,
                CustomerId = request.CustomerId,
                Notes = request.Notes,
                DiscountType = Enum.Parse<DiscountType>(request.DiscountType),
                DiscountValue = request.DiscountValue,
                Status = InvoiceStatus.Draft,
                Items = request.Items.Select(item => new InvoiceItem
                {
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    VatRate = item.VatRate
                }).ToList()
            };

            var created = await _invoiceService.CreateAsync(invoice);
            var dto = await MapToDtoAsync(created);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invoice");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateInvoiceRequest request)
    {
        try
        {
            var invoice = await _invoiceService.GetByIdAsync(id);
            if (invoice == null) return NotFound();

            invoice.InvoiceDate = request.InvoiceDate;
            invoice.ServicePeriodStart = request.ServicePeriodStart;
            invoice.ServicePeriodEnd = request.ServicePeriodEnd;
            invoice.DueDate = request.DueDate;
            invoice.CustomerId = request.CustomerId;
            invoice.Notes = request.Notes;
            invoice.DiscountType = Enum.Parse<DiscountType>(request.DiscountType);
            invoice.DiscountValue = request.DiscountValue;

            // Update items
            invoice.Items.Clear();
            invoice.Items = request.Items.Select(item => new InvoiceItem
            {
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                VatRate = item.VatRate
            }).ToList();

            await _invoiceService.UpdateAsync(invoice);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating invoice {InvoiceId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _invoiceService.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting invoice {InvoiceId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{id}/send")]
    public async Task<IActionResult> SendInvoice(int id, [FromBody] SendInvoiceRequest? request = null)
    {
        try
        {
            var invoice = await _invoiceService.GetByIdAsync(id);
            if (invoice == null) return NotFound();

            // Use customer email if not provided
            string recipientEmail = request?.RecipientEmail ?? invoice.Customer?.Email ?? string.Empty;
            if (string.IsNullOrEmpty(recipientEmail))
            {
                return BadRequest("Recipient email is required");
            }

            await _emailService.SendInvoiceEmailAsync(
                id,
                recipientEmail,
                request?.CustomMessage,
                request?.Format ?? EmailAttachmentFormat.NormalPdf,
                request?.CcEmails,
                request?.BccEmails);

            return Ok(new { message = "Invoice email sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending invoice {InvoiceId}", id);
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("{id}/pdf")]
    public IActionResult GeneratePdf(int id)
    {
        try
        {
            var pdfBytes = _pdfGeneratorService.GenerateInvoicePdf(id);
            var invoice = _invoiceService.GetByIdAsync(id).Result;
            return File(pdfBytes, "application/pdf", $"Invoice-{invoice?.InvoiceNumber ?? id.ToString()}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for invoice {InvoiceId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{id}/mark-paid")]
    public async Task<IActionResult> MarkAsPaid(int id, [FromBody] MarkAsPaidRequest request)
    {
        try
        {
            var invoice = await _invoiceService.GetByIdAsync(id);
            if (invoice == null) return NotFound();

            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidDate = request.PaidDate;
            await _invoiceService.UpdateAsync(invoice);

            return Ok(new { message = "Invoice marked as paid" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking invoice {InvoiceId} as paid", id);
            return StatusCode(500, "Internal server error");
        }
    }

    private async Task<InvoiceDto> MapToDtoAsync(Invoice invoice)
    {
        // Load customer name from Host API
        string? customerName = null;
        if (invoice.CustomerId > 0)
        {
            var customer = await _hostApiClient.GetCustomerAsync(invoice.CustomerId);
            customerName = customer?.Name;
        }

        return new InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            ServicePeriodStart = invoice.ServicePeriodStart,
            ServicePeriodEnd = invoice.ServicePeriodEnd,
            DueDate = invoice.DueDate,
            CustomerId = invoice.CustomerId,
            CustomerName = customerName,
            Notes = invoice.Notes,
            Status = invoice.Status.ToString(),
            PaidDate = invoice.PaidDate,
            CreatedAt = invoice.CreatedAt,
            UpdatedAt = invoice.UpdatedAt,
            EmailSentAt = invoice.EmailSentAt,
            EmailSentTo = invoice.EmailSentTo,
            EmailSendCount = invoice.EmailSendCount,
            EmailCcRecipients = invoice.EmailCcRecipients,
            EmailBccRecipients = invoice.EmailBccRecipients,
            PrintedAt = invoice.PrintedAt,
            PrintCount = invoice.PrintCount,
            DiscountType = invoice.DiscountType.ToString(),
            DiscountValue = invoice.DiscountValue,
            TotalNet = invoice.TotalNet,
            DiscountAmount = invoice.DiscountAmount,
            TotalNetAfterDiscount = invoice.TotalNetAfterDiscount,
            TotalVat = invoice.TotalVat,
            TotalGross = invoice.TotalGross,
            TotalDownPayments = invoice.TotalDownPayments,
            AmountDue = invoice.AmountDue,
            Items = invoice.Items.Select(item => new InvoiceItemDto
            {
                Id = item.Id,
                InvoiceId = item.InvoiceId,
                Position = item.Position,
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                VatRate = item.VatRate,
                TotalNet = item.TotalNet,
                TotalVat = item.TotalVat,
                TotalGross = item.TotalGross
            }).ToList(),
            DownPayments = invoice.DownPayments.Select(dp => new DownPaymentDto
            {
                Id = dp.Id,
                InvoiceId = dp.InvoiceId,
                Description = dp.Description,
                Amount = dp.Amount,
                PaymentDate = dp.PaymentDate,
                CreatedAt = dp.CreatedAt
            }).ToList()
        };
    }
}

public record MarkAsPaidRequest(DateTime PaidDate);
public record SendInvoiceRequest(
    string? RecipientEmail = null,
    string? CustomMessage = null,
    EmailAttachmentFormat Format = EmailAttachmentFormat.NormalPdf,
    string? CcEmails = null,
    string? BccEmails = null);
