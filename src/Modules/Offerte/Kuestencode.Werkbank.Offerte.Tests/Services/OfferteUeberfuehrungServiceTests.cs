using FluentAssertions;
using Kuestencode.Werkbank.Offerte.Data.Repositories;
using Kuestencode.Werkbank.Offerte.Domain.Entities;
using Kuestencode.Werkbank.Offerte.Domain.Enums;
using Kuestencode.Werkbank.Offerte.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Kuestencode.Werkbank.Offerte.Tests.Services;

public class OfferteUeberfuehrungServiceTests
{
    private readonly Mock<IAngebotRepository> _repo = new();

    private OfferteUeberfuehrungService CreateService() =>
        new(_repo.Object, NullLogger<OfferteUeberfuehrungService>.Instance);

    private static Angebot MakeAngebot(AngebotStatus status = AngebotStatus.Angenommen) =>
        new()
        {
            Id = Guid.NewGuid(),
            Angebotsnummer = "ANG-2026-00001",
            KundeId = 7,
            Status = status,
            Erstelldatum = DateTime.UtcNow,
            GueltigBis = DateTime.UtcNow.AddDays(30),
            Referenz = "Projekt X",
            Positionen = new List<Angebotsposition>
            {
                new() { Position = 1, Text = "Frontend", Menge = 10, Einzelpreis = 150, Steuersatz = 19 },
                new() { Position = 2, Text = "Backend", Menge = 5, Einzelpreis = 200, Steuersatz = 19, Rabatt = 5 }
            }
        };

    [Fact]
    public async Task InRechnungUeberfuehren_AngenommeneAngebot_GibtDtoZurueck()
    {
        var angebot = MakeAngebot(AngebotStatus.Angenommen);
        _repo.Setup(r => r.GetByIdAsync(angebot.Id)).ReturnsAsync(angebot);

        var dto = await CreateService().InRechnungUeberfuehrenAsync(angebot.Id);

        dto.Should().NotBeNull();
        dto.KundeId.Should().Be(7);
        dto.Referenz.Should().Be($"Angebot {angebot.Angebotsnummer}");
    }

    [Fact]
    public async Task InRechnungUeberfuehren_KopiertAllePositionen()
    {
        var angebot = MakeAngebot(AngebotStatus.Angenommen);
        _repo.Setup(r => r.GetByIdAsync(angebot.Id)).ReturnsAsync(angebot);

        var dto = await CreateService().InRechnungUeberfuehrenAsync(angebot.Id);

        dto.Positionen.Should().HaveCount(2);
        dto.Positionen[0].Text.Should().Be("Frontend");
        dto.Positionen[0].Menge.Should().Be(10);
        dto.Positionen[0].Einzelpreis.Should().Be(150);
        dto.Positionen[1].Rabatt.Should().Be(5);
    }

    [Fact]
    public async Task InRechnungUeberfuehren_NichtAngenommenes_WirftException()
    {
        var angebot = MakeAngebot(AngebotStatus.Versendet);
        _repo.Setup(r => r.GetByIdAsync(angebot.Id)).ReturnsAsync(angebot);

        var act = () => CreateService().InRechnungUeberfuehrenAsync(angebot.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*angenommen*");
    }

    [Fact]
    public async Task InRechnungUeberfuehren_EntwurfAngebot_WirftException()
    {
        var angebot = MakeAngebot(AngebotStatus.Entwurf);
        _repo.Setup(r => r.GetByIdAsync(angebot.Id)).ReturnsAsync(angebot);

        var act = () => CreateService().InRechnungUeberfuehrenAsync(angebot.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task InRechnungUeberfuehren_NichtGefunden_WirftException()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Angebot?)null);

        var act = () => CreateService().InRechnungUeberfuehrenAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*nicht gefunden*");
    }
}
