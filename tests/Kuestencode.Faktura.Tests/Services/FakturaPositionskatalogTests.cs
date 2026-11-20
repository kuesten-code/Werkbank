using FluentAssertions;
using Kuestencode.Faktura.Data;
using Kuestencode.Faktura.Data.Repositories;
using Kuestencode.Faktura.Models;
using Kuestencode.Faktura.Services.Positionskatalog;
using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Host;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Kuestencode.Faktura.Tests.Services;

public class FakturaPositionskatalogTests
{
    private static readonly DateTime Rechnungsdatum = new(2026, 5, 4, 0, 0, 0, DateTimeKind.Utc);

    private static Invoice Rechnung(InvoiceType typ = InvoiceType.Invoice, string nummer = "R-1") => new()
    {
        InvoiceNumber = nummer,
        CustomerId = 1,
        Type = typ,
        InvoiceDate = Rechnungsdatum,
        Items =
        [
            new InvoiceItem { Description = "Bad", IsHeader = true },
            new InvoiceItem { Description = "Silikonfuge erneuern", Unit = "lfm", Quantity = 4, UnitPrice = 12.5m }
        ]
    };

    [Fact]
    public void AusRechnung_UebernimmtTextEinheitPreisUndRechnungsdatum_OhneUeberschriften()
    {
        var positionen = FakturaPositionskatalog.AusRechnung(Rechnung());

        positionen.Should().ContainSingle().Which.Should().BeEquivalentTo(new PositionsvorlageMerkenDto
        {
            Text = "Silikonfuge erneuern",
            Einheit = "lfm",
            Einzelpreis = 12.5m,
            Dokumentdatum = Rechnungsdatum
        });
    }

    [Fact]
    public void AusRechnung_Gutschrift_LiefertNichts()
    {
        FakturaPositionskatalog.AusRechnung(Rechnung(InvoiceType.CreditNote)).Should().BeEmpty();
    }

    [Fact]
    public void Uebernehmen_SetztEinheitUndPreis_TextBleibt()
    {
        var item = new InvoiceItem { Description = "Anfahrt", Quantity = 2 };

        FakturaPositionskatalog.Uebernehmen(item, new PositionsvorlageDto { Text = "Anfahrt", Einheit = "km", Einzelpreis = 0.5m });

        item.Unit.Should().Be("km");
        item.UnitPrice.Should().Be(0.5m);
        item.Quantity.Should().Be(2);
    }

    [Fact]
    public async Task Quelle_LiestNurRechnungenAusDemRepository()
    {
        var context = new FakturaDbContext(new DbContextOptionsBuilder<FakturaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Invoices.AddRange(Rechnung(nummer: "R-1"), Rechnung(InvoiceType.CreditNote, "G-1"));
        await context.SaveChangesAsync();
        var quelle = new FakturaPositionskatalogQuelle(new InvoiceRepository(context, Mock.Of<IHostApiClient>()));

        var positionen = await quelle.GetPositionenAsync();

        positionen.Should().ContainSingle().Which.Text.Should().Be("Silikonfuge erneuern");
    }
}
