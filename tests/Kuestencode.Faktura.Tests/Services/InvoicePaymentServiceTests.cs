using FluentAssertions;
using Kuestencode.Faktura.Data;
using Kuestencode.Faktura.Data.Repositories;
using Kuestencode.Faktura.Models;
using Kuestencode.Faktura.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kuestencode.Faktura.Tests.Services;

public class InvoicePaymentServiceTests
{
    private readonly FakturaDbContext _context;
    private readonly InvoicePaymentService _service;

    public InvoicePaymentServiceTests()
    {
        var options = new DbContextOptionsBuilder<FakturaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new FakturaDbContext(options);
        _service = new InvoicePaymentService(
            new InvoicePaymentRepository(_context), _context, NullLogger<InvoicePaymentService>.Instance);
    }

    private async Task<Invoice> SeedInvoiceAsync(InvoiceStatus status, DateTime? dueDate)
    {
        var invoice = new Invoice
        {
            Id = 1,
            InvoiceNumber = "R-2026-0001",
            InvoiceDate = DateTime.UtcNow.AddDays(-30),
            CustomerId = 1,
            Status = status,
            DueDate = dueDate,
            Items = { new InvoiceItem { Quantity = 1, UnitPrice = 100m, VatRate = 19m } }
        };
        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();
        return invoice;
    }

    [Fact]
    public async Task ZahlungErfassen_TeilzahlungNachFaelligkeit_SetztOverdue()
    {
        await SeedInvoiceAsync(InvoiceStatus.Overdue, DateTime.UtcNow.AddDays(-1));

        await _service.ZahlungErfassenAsync(1, 50m, DateTime.Today, null);

        (await _context.Invoices.FindAsync(1))!.Status.Should().Be(InvoiceStatus.Overdue);
    }

    [Fact]
    public async Task ZahlungszielSetzen_SpeichertNeuesZielOhneFaelligkeitsdatumZuAendern()
    {
        var originalDueDate = DateTime.UtcNow.AddDays(-1);
        await SeedInvoiceAsync(InvoiceStatus.Overdue, originalDueDate);
        await _service.ZahlungErfassenAsync(1, 50m, DateTime.Today, null);

        var newTarget = DateTime.Today.AddDays(14);
        await _service.ZahlungszielSetzenAsync(1, newTarget);

        var invoice = (await _context.Invoices.FindAsync(1))!;
        invoice.DueDate.Should().Be(originalDueDate);
        invoice.RevisedDueDate.Should().Be(DateTime.SpecifyKind(newTarget, DateTimeKind.Utc));
        invoice.RevisedDueDate!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task ZahlungszielSetzen_InZukunft_SetztUeberfaelligeTeilzahlungAufPartiallyPaid()
    {
        await SeedInvoiceAsync(InvoiceStatus.Overdue, DateTime.UtcNow.AddDays(-1));
        await _service.ZahlungErfassenAsync(1, 50m, DateTime.Today, null);

        await _service.ZahlungszielSetzenAsync(1, DateTime.Today.AddDays(14));

        (await _context.Invoices.FindAsync(1))!.Status.Should().Be(InvoiceStatus.PartiallyPaid);
    }

    [Fact]
    public async Task ZahlungszielSetzen_UnbekannteRechnung_WirftException()
    {
        var act = () => _service.ZahlungszielSetzenAsync(999, DateTime.Today);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
