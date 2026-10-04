using FluentAssertions;
using Kuestencode.Faktura.Data;
using Kuestencode.Faktura.Data.Repositories;
using Kuestencode.Faktura.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kuestencode.Faktura.Tests.Data;

public class InvoicePaymentRepositoryTests
{
    private readonly FakturaDbContext _context;
    private readonly InvoicePaymentRepository _repository;

    public InvoicePaymentRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<FakturaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new FakturaDbContext(options);
        _repository = new InvoicePaymentRepository(_context);
    }

    private static DateTime Utc(int month, int day) => new(2026, month, day, 0, 0, 0, DateTimeKind.Utc);

    private async Task SeedAsync()
    {
        var first = new Invoice
        {
            Id = 1, InvoiceNumber = "R-0001", InvoiceDate = Utc(1, 1), CustomerId = 1, Status = InvoiceStatus.Sent,
            Items = [new InvoiceItem { Description = "A", Quantity = 1, UnitPrice = 1000, VatRate = 19 }],
            Payments =
            [
                new InvoicePayment { Amount = 100, PaymentDate = Utc(3, 1) },
                new InvoicePayment { Amount = 200, PaymentDate = Utc(3, 31) },
                new InvoicePayment { Amount = 300, PaymentDate = Utc(4, 1) }
            ]
        };
        var second = new Invoice
        {
            Id = 2, InvoiceNumber = "R-0002", InvoiceDate = Utc(1, 1), CustomerId = 1, Status = InvoiceStatus.Sent,
            Payments = [new InvoicePayment { Amount = 50, PaymentDate = Utc(3, 15) }]
        };
        _context.Invoices.AddRange(first, second);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    [Fact]
    public async Task GetByInvoiceId_NurZahlungenDieserRechnungNeuesteZuerst()
    {
        await SeedAsync();

        var result = await _repository.GetByInvoiceIdAsync(1);

        result.Select(p => p.Amount).Should().Equal(300m, 200m, 100m);
    }

    [Fact]
    public async Task GetByPaymentDateRange_GrenzenInklusiveSortiertUndMitRechnungspositionen()
    {
        await SeedAsync();

        var result = (await _repository.GetByPaymentDateRangeAsync(Utc(3, 1), Utc(3, 31))).ToList();

        result.Select(p => p.Amount).Should().Equal(100m, 50m, 200m);
        result.First().Invoice.Items.Should().ContainSingle();
    }
}
