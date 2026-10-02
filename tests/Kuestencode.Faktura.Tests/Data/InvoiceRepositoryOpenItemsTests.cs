using FluentAssertions;
using Kuestencode.Faktura.Data;
using Kuestencode.Faktura.Data.Repositories;
using Kuestencode.Faktura.Models;
using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Host;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Kuestencode.Faktura.Tests.Data;

public class InvoiceRepositoryOpenItemsTests
{
    private readonly FakturaDbContext _context;
    private readonly InvoiceRepository _repository;

    public InvoiceRepositoryOpenItemsTests()
    {
        var options = new DbContextOptionsBuilder<FakturaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new FakturaDbContext(options);

        var hostApiClient = new Mock<IHostApiClient>();
        hostApiClient.Setup(c => c.GetAllCustomersAsync())
            .ReturnsAsync([new CustomerDto { Id = 1, Name = "Hafen GmbH" }]);

        _repository = new InvoiceRepository(_context, hostApiClient.Object);
    }

    private static Invoice MakeInvoice(int id, DateTime invoiceDate, InvoiceStatus status = InvoiceStatus.Sent,
        InvoiceType type = InvoiceType.Invoice) =>
        new()
        {
            Id = id,
            InvoiceNumber = $"R-{id:D4}",
            InvoiceDate = DateTime.SpecifyKind(invoiceDate, DateTimeKind.Utc),
            CustomerId = 1,
            Status = status,
            Type = type
        };

    [Fact]
    public async Task GetOpenInvoicesByInvoiceDateRange_FiltertNachZeitraumStatusUndTyp()
    {
        _context.Invoices.AddRange(
            MakeInvoice(1, new DateTime(2026, 3, 1)),
            MakeInvoice(2, new DateTime(2026, 3, 31), InvoiceStatus.Overdue),
            MakeInvoice(3, new DateTime(2026, 3, 15), InvoiceStatus.PartiallyPaid),
            MakeInvoice(4, new DateTime(2026, 2, 28)),
            MakeInvoice(5, new DateTime(2026, 4, 1)),
            MakeInvoice(6, new DateTime(2026, 3, 10), InvoiceStatus.Paid),
            MakeInvoice(7, new DateTime(2026, 3, 10), InvoiceStatus.Draft),
            MakeInvoice(8, new DateTime(2026, 3, 10), InvoiceStatus.Cancelled),
            MakeInvoice(9, new DateTime(2026, 3, 10), type: InvoiceType.CreditNote));
        await _context.SaveChangesAsync();

        var result = await _repository.GetOpenInvoicesByInvoiceDateRangeAsync(
            new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));

        result.Select(i => i.Id).Should().Equal(1, 3, 2);
    }

    [Fact]
    public async Task GetOpenInvoicesByInvoiceDateRange_LaedtKunden()
    {
        _context.Invoices.Add(MakeInvoice(1, new DateTime(2026, 3, 1)));
        await _context.SaveChangesAsync();

        var result = await _repository.GetOpenInvoicesByInvoiceDateRangeAsync(
            new DateTime(2026, 3, 1), new DateTime(2026, 3, 1));

        result.Single().Customer!.Name.Should().Be("Hafen GmbH");
    }
}
