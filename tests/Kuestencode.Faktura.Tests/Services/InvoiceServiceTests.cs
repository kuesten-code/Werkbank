using FluentAssertions;
using Kuestencode.Faktura.Data.Repositories;
using Kuestencode.Faktura.Models;
using Kuestencode.Faktura.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Kuestencode.Faktura.Tests.Services;

public class InvoiceServiceTests
{
    private readonly Mock<IInvoiceRepository> _repo = new();
    private readonly InvoiceService _service;

    public InvoiceServiceTests()
    {
        _service = new InvoiceService(_repo.Object, NullLogger<InvoiceService>.Instance);
    }

    private static Invoice MakeInvoice(int id = 1, InvoiceStatus status = InvoiceStatus.Draft) =>
        new()
        {
            Id = id,
            InvoiceNumber = $"R-2026-{id:D4}",
            CustomerId = 1,
            Status = status,
            Items = new List<InvoiceItem>()
        };

    private static InvoiceItem MakeItem(decimal qty, decimal price, decimal vat = 19m) =>
        new() { Quantity = qty, UnitPrice = price, VatRate = vat };

    // ─── GetAllAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_DelegiertAnRepository()
    {
        var invoices = new List<Invoice> { MakeInvoice(1), MakeInvoice(2) };
        _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(invoices);

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(2);
    }

    // ─── GetByStatusAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetByStatus_DelegiertAnRepository()
    {
        _repo.Setup(r => r.GetByStatusAsync(InvoiceStatus.Sent))
            .ReturnsAsync(new List<Invoice> { MakeInvoice(1, InvoiceStatus.Sent) });

        var result = await _service.GetByStatusAsync(InvoiceStatus.Sent);

        result.Should().HaveCount(1);
        _repo.Verify(r => r.GetByStatusAsync(InvoiceStatus.Sent), Times.Once);
    }

    // ─── GetPaidByDateRangeAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetPaidByDateRange_DelegiertAnRepository()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        _repo.Setup(r => r.GetPaidByDateRangeAsync(from, to))
            .ReturnsAsync(new List<Invoice> { MakeInvoice(1, InvoiceStatus.Paid) });

        var result = await _service.GetPaidByDateRangeAsync(from, to);

        result.Should().HaveCount(1);
    }

    // ─── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_MitDetails_RuftGetWithDetailsAuf()
    {
        var inv = MakeInvoice();
        _repo.Setup(r => r.GetWithDetailsAsync(1)).ReturnsAsync(inv);

        var result = await _service.GetByIdAsync(1, includeCustomer: true);

        result.Should().Be(inv);
        _repo.Verify(r => r.GetWithDetailsAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetById_OhneDetails_RuftGetByIdAuf()
    {
        var inv = MakeInvoice();
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(inv);

        var result = await _service.GetByIdAsync(1, includeCustomer: false, includeItems: false);

        result.Should().Be(inv);
        _repo.Verify(r => r.GetByIdAsync(1), Times.Once);
    }

    // ─── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_GueltigeRechnung_ErstelltRechnung()
    {
        var inv = MakeInvoice();
        inv.Items.Add(MakeItem(1, 100m));
        inv.Items.Add(MakeItem(2, 50m));
        _repo.Setup(r => r.InvoiceNumberExistsAsync(inv.InvoiceNumber)).ReturnsAsync(false);
        _repo.Setup(r => r.AddAsync(It.IsAny<Invoice>())).ReturnsAsync(inv);

        var result = await _service.CreateAsync(inv);

        result.Should().Be(inv);
        inv.Items[0].Position.Should().Be(1);
        inv.Items[1].Position.Should().Be(2);
        _repo.Verify(r => r.AddAsync(inv), Times.Once);
    }

    [Fact]
    public async Task Create_OhneRechnungsnummer_GenersiertNummer()
    {
        var inv = new Invoice { InvoiceNumber = "", CustomerId = 1, Items = new() };
        _repo.Setup(r => r.GenerateInvoiceNumberAsync()).ReturnsAsync("R-2026-0042");
        _repo.Setup(r => r.InvoiceNumberExistsAsync("R-2026-0042")).ReturnsAsync(false);
        _repo.Setup(r => r.AddAsync(It.IsAny<Invoice>())).ReturnsAsync(inv);

        await _service.CreateAsync(inv);

        inv.InvoiceNumber.Should().Be("R-2026-0042");
        _repo.Verify(r => r.GenerateInvoiceNumberAsync(), Times.Once);
    }

    [Fact]
    public async Task Create_DoppelteRechnungsnummer_WirftException()
    {
        var inv = MakeInvoice();
        _repo.Setup(r => r.InvoiceNumberExistsAsync(inv.InvoiceNumber)).ReturnsAsync(true);

        var act = () => _service.CreateAsync(inv);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{inv.InvoiceNumber}*");
    }

    // ─── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_VorhandeneRechnung_AktualisiertPositionen()
    {
        var inv = MakeInvoice();
        inv.Items.Add(MakeItem(1, 100m));
        _repo.Setup(r => r.GetByIdAsync(inv.Id)).ReturnsAsync(inv);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Invoice>())).Returns(Task.CompletedTask);

        await _service.UpdateAsync(inv);

        inv.Items[0].Position.Should().Be(1);
        _repo.Verify(r => r.UpdateAsync(inv), Times.Once);
    }

    [Fact]
    public async Task Update_NichtVorhandeneRechnung_WirftException()
    {
        var inv = MakeInvoice(99);
        _repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Invoice?)null);

        var act = () => _service.UpdateAsync(inv);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*99*");
    }

    // ─── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_VorhandeneRechnung_LoeschtRechnung()
    {
        var inv = MakeInvoice();
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(inv);
        _repo.Setup(r => r.DeleteAsync(inv)).Returns(Task.CompletedTask);

        await _service.DeleteAsync(1);

        _repo.Verify(r => r.DeleteAsync(inv), Times.Once);
    }

    [Fact]
    public async Task Delete_NichtVorhandeneRechnung_WirftException()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Invoice?)null);

        var act = () => _service.DeleteAsync(99);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ─── GenerateInvoiceNumberAsync ───────────────────────────────────────────

    [Fact]
    public async Task GenerateInvoiceNumber_DelegiertAnRepository()
    {
        _repo.Setup(r => r.GenerateInvoiceNumberAsync()).ReturnsAsync("R-2026-0001");

        var result = await _service.GenerateInvoiceNumberAsync();

        result.Should().Be("R-2026-0001");
    }

    // ─── MarkAsPaidAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task MarkAsPaid_SentRechnung_WirdBezahlt()
    {
        var inv = MakeInvoice(1, InvoiceStatus.Sent);
        var paidDate = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(inv);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Invoice>())).Returns(Task.CompletedTask);

        await _service.MarkAsPaidAsync(1, paidDate);

        inv.Status.Should().Be(InvoiceStatus.Paid);
        inv.PaidDate.Should().Be(paidDate);
        _repo.Verify(r => r.UpdateAsync(inv), Times.Once);
    }

    [Fact]
    public async Task MarkAsPaid_LocalDateTime_WirdNachUtcKonvertiert()
    {
        var inv = MakeInvoice(1, InvoiceStatus.Sent);
        var localDate = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Local);
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(inv);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Invoice>())).Returns(Task.CompletedTask);

        await _service.MarkAsPaidAsync(1, localDate);

        inv.PaidDate!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task MarkAsPaid_NichtVorhandeneRechnung_WirftException()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Invoice?)null);

        var act = () => _service.MarkAsPaidAsync(99, DateTime.UtcNow);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ─── MarkAsPrintedAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task MarkAsPrinted_DraftRechnung_WirdSentUndDruckzaehlerErhoehen()
    {
        var inv = MakeInvoice(1, InvoiceStatus.Draft);
        inv.PrintCount = 0;
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(inv);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Invoice>())).Returns(Task.CompletedTask);

        await _service.MarkAsPrintedAsync(1);

        inv.Status.Should().Be(InvoiceStatus.Sent);
        inv.PrintCount.Should().Be(1);
        inv.PrintedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkAsPrinted_SentRechnung_BleibtSentUndErhoehtzaehler()
    {
        var inv = MakeInvoice(1, InvoiceStatus.Sent);
        inv.PrintCount = 2;
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(inv);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Invoice>())).Returns(Task.CompletedTask);

        await _service.MarkAsPrintedAsync(1);

        inv.Status.Should().Be(InvoiceStatus.Sent);
        inv.PrintCount.Should().Be(3);
    }

    [Fact]
    public async Task MarkAsPrinted_NichtVorhandeneRechnung_WirftException()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Invoice?)null);

        var act = () => _service.MarkAsPrintedAsync(99);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ─── CalculateTotalNetAsync ───────────────────────────────────────────────

    [Fact]
    public async Task CalculateTotalNet_SummiertAllePositionen()
    {
        var items = new List<InvoiceItem>
        {
            MakeItem(2, 100m),
            MakeItem(1, 50m)
        };

        var result = await _service.CalculateTotalNetAsync(items);

        result.Should().Be(250m);
    }

    [Fact]
    public async Task CalculateTotalNet_LeereListe_GibtNullZurueck()
    {
        var result = await _service.CalculateTotalNetAsync(new List<InvoiceItem>());
        result.Should().Be(0m);
    }

    // ─── CalculateTotalGrossAsync ─────────────────────────────────────────────

    [Fact]
    public async Task CalculateTotalGross_NichtKleinunternehmer_InklusiveMwSt()
    {
        var items = new List<InvoiceItem> { MakeItem(1, 100m, 19m) };

        var result = await _service.CalculateTotalGrossAsync(items, isKleinunternehmer: false);

        result.Should().Be(119m);
    }

    [Fact]
    public async Task CalculateTotalGross_Kleinunternehmer_OhneMwSt()
    {
        var items = new List<InvoiceItem> { MakeItem(1, 100m, 19m) };

        var result = await _service.CalculateTotalGrossAsync(items, isKleinunternehmer: true);

        result.Should().Be(100m);
    }

    [Fact]
    public async Task CalculateTotalGross_LeereListe_GibtNullZurueck()
    {
        var result = await _service.CalculateTotalGrossAsync(new List<InvoiceItem>(), false);
        result.Should().Be(0m);
    }
}
