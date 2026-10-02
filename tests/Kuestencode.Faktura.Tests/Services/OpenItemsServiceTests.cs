using FluentAssertions;
using Kuestencode.Core.Models;
using Kuestencode.Faktura.Data.Repositories;
using Kuestencode.Faktura.Models;
using Kuestencode.Faktura.Services;
using Moq;
using Xunit;

namespace Kuestencode.Faktura.Tests.Services;

public class OpenItemsServiceTests
{
    private readonly Mock<IInvoiceRepository> _repo = new();
    private readonly OpenItemsService _service;

    public OpenItemsServiceTests()
    {
        _service = new OpenItemsService(_repo.Object);
    }

    [Fact]
    public async Task GetOpenItems_BildetRechnungAufOffenenPostenAb()
    {
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 3, 31);
        var invoice = new Invoice
        {
            InvoiceNumber = "R-2026-0007",
            InvoiceDate = new DateTime(2026, 2, 10),
            DueDate = new DateTime(2026, 2, 24),
            RevisedDueDate = new DateTime(2026, 3, 20),
            Customer = new Customer { Name = "Hafen GmbH" },
            Items = { new InvoiceItem { Quantity = 1, UnitPrice = 1000m, VatRate = 19m } },
            Payments = { new InvoicePayment { Amount = 400m } }
        };
        _repo.Setup(r => r.GetOpenInvoicesByInvoiceDateRangeAsync(from, to)).ReturnsAsync([invoice]);

        var result = await _service.GetOpenItemsAsync(from, to);

        result.Should().ContainSingle().Which.Should().Be(new OpenItem(
            "R-2026-0007", new DateTime(2026, 2, 10), "Hafen GmbH", new DateTime(2026, 3, 20), 1190m, 400m, 790m));
    }

    [Fact]
    public async Task GetOpenItems_OhneKunde_SetztLeerenNamen()
    {
        _repo.Setup(r => r.GetOpenInvoicesByInvoiceDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync([new Invoice { InvoiceNumber = "R-1" }]);

        var result = await _service.GetOpenItemsAsync(DateTime.Today, DateTime.Today);

        result.Single().CustomerName.Should().BeEmpty();
    }
}
