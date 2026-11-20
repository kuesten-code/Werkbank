using FluentAssertions;
using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Werkbank.Offerte.Data;
using Kuestencode.Werkbank.Offerte.Data.Repositories;
using Kuestencode.Werkbank.Offerte.Domain.Entities;
using Kuestencode.Werkbank.Offerte.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Kuestencode.Werkbank.Offerte.Tests.Services;

public class AngebotPositionskatalogServiceTests
{
    private static readonly DateTime Erstelldatum = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Angebot Angebot(string nummer = "AN-0001")
    {
        var id = Guid.NewGuid();
        return new Angebot
        {
            Id = id,
            Angebotsnummer = nummer,
            KundeId = 1,
            Erstelldatum = Erstelldatum,
            GueltigBis = Erstelldatum.AddDays(30),
            Positionen =
            [
                new Angebotsposition { Id = Guid.NewGuid(), AngebotId = id, Position = 1, Text = "Bad", IsHeader = true },
                new Angebotsposition
                {
                    Id = Guid.NewGuid(), AngebotId = id, Position = 2, Text = "Fliesen verlegen 30x60",
                    Einheit = "m²", Menge = 12, Einzelpreis = 48m, Steuersatz = 19m, Rabatt = 10m
                }
            ]
        };
    }

    [Fact]
    public void AusAngebot_UebernimmtTextEinheitPreisUndErstelldatum_OhneUeberschriften()
    {
        var positionen = AngebotPositionskatalogService.AusAngebot(Angebot());

        positionen.Should().ContainSingle().Which.Should().BeEquivalentTo(new PositionsvorlageMerkenDto
        {
            Text = "Fliesen verlegen 30x60",
            Einheit = "m²",
            Einzelpreis = 48m,
            Dokumentdatum = Erstelldatum
        });
    }

    [Fact]
    public async Task Merken_ReichtPositionenAnLernerWeiter()
    {
        var lerner = new Mock<IPositionskatalogLerner>();
        var service = new AngebotPositionskatalogService(lerner.Object);

        await service.MerkenAsync(Angebot());

        lerner.Verify(l => l.MerkenAsync(It.Is<IReadOnlyCollection<PositionsvorlageMerkenDto>>(
            p => p.Single().Einzelpreis == 48m)), Times.Once);
    }

    [Fact]
    public void Uebernehmen_SetztEinheitUndPreis_RabattUndSteuerBleiben()
    {
        var position = Angebot().Positionen.Single(p => !p.IsHeader);

        AngebotPositionskatalogService.Uebernehmen(position, new PositionsvorlageDto { Einheit = "Pauschal", Einzelpreis = 900m });

        position.Einheit.Should().Be("Pauschal");
        position.Einzelpreis.Should().Be(900m);
        position.Rabatt.Should().Be(10m);
        position.Steuersatz.Should().Be(19m);
    }

    [Fact]
    public async Task Quelle_LiefertPositionenAllerAngebote()
    {
        await using var context = new OfferteDbContext(new DbContextOptionsBuilder<OfferteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Angebote.AddRange(Angebot("AN-0001"), Angebot("AN-0002"));
        await context.SaveChangesAsync();
        var quelle = new OffertePositionskatalogQuelle(new AngebotRepository(context));

        var positionen = await quelle.GetPositionenAsync();

        positionen.Should().HaveCount(2).And.OnlyContain(p => p.Text == "Fliesen verlegen 30x60");
    }
}
