using FluentAssertions;
using Kuestencode.Werkbank.Offerte.Data.Repositories;
using Kuestencode.Werkbank.Offerte.Domain.Entities;
using Kuestencode.Werkbank.Offerte.Domain.Enums;
using Kuestencode.Werkbank.Offerte.Domain.Interfaces;
using Kuestencode.Werkbank.Offerte.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Kuestencode.Werkbank.Offerte.Tests.Services;

public class OfferteKopierServiceTests
{
    private readonly Mock<IAngebotRepository> _repo = new();
    private readonly Mock<IAngebotsnummernService> _nummernService = new();

    private OfferteKopierService CreateService() =>
        new(_repo.Object, _nummernService.Object, NullLogger<OfferteKopierService>.Instance);

    private static Angebot MakeAngebot() =>
        new()
        {
            Id = Guid.NewGuid(),
            Angebotsnummer = "ANG-2026-00001",
            KundeId = 5,
            Status = AngebotStatus.Versendet,
            Erstelldatum = DateTime.UtcNow.AddDays(-10),
            GueltigBis = DateTime.UtcNow.AddDays(-5),
            Referenz = "Ref-123",
            Bemerkungen = "Bemerkung",
            Einleitung = "Sehr geehrte...",
            Schlusstext = "Mit freundlichen Grüßen",
            Positionen = new List<Angebotsposition>
            {
                new() { Id = Guid.NewGuid(), Position = 1, Text = "Pos 1", Menge = 2, Einzelpreis = 100, Steuersatz = 19 },
                new() { Id = Guid.NewGuid(), Position = 2, Text = "Pos 2", Menge = 1, Einzelpreis = 50, Steuersatz = 7, Rabatt = 10 }
            }
        };

    [Fact]
    public async Task Kopiere_ErstelltNeuesAngebotImEntwurfStatus()
    {
        var original = MakeAngebot();
        _repo.Setup(r => r.GetByIdAsync(original.Id)).ReturnsAsync(original);
        _nummernService.Setup(n => n.NaechsteNummerAsync()).ReturnsAsync("ANG-2026-00002");
        _repo.Setup(r => r.AddAsync(It.IsAny<Angebot>())).Returns(Task.CompletedTask);

        var kopie = await CreateService().KopiereAsync(original.Id);

        kopie.Status.Should().Be(AngebotStatus.Entwurf);
        kopie.Angebotsnummer.Should().Be("ANG-2026-00002");
        kopie.Id.Should().NotBe(original.Id);
    }

    [Fact]
    public async Task Kopiere_KopiertKundendatenUndTexte()
    {
        var original = MakeAngebot();
        _repo.Setup(r => r.GetByIdAsync(original.Id)).ReturnsAsync(original);
        _nummernService.Setup(n => n.NaechsteNummerAsync()).ReturnsAsync("ANG-2026-00002");
        _repo.Setup(r => r.AddAsync(It.IsAny<Angebot>())).Returns(Task.CompletedTask);

        var kopie = await CreateService().KopiereAsync(original.Id);

        kopie.KundeId.Should().Be(original.KundeId);
        kopie.Referenz.Should().Be(original.Referenz);
        kopie.Bemerkungen.Should().Be(original.Bemerkungen);
        kopie.Einleitung.Should().Be(original.Einleitung);
        kopie.Schlusstext.Should().Be(original.Schlusstext);
    }

    [Fact]
    public async Task Kopiere_KopiertAllePositionenMitNeuenGuids()
    {
        var original = MakeAngebot();
        _repo.Setup(r => r.GetByIdAsync(original.Id)).ReturnsAsync(original);
        _nummernService.Setup(n => n.NaechsteNummerAsync()).ReturnsAsync("ANG-2026-00002");
        _repo.Setup(r => r.AddAsync(It.IsAny<Angebot>())).Returns(Task.CompletedTask);

        var kopie = await CreateService().KopiereAsync(original.Id);

        kopie.Positionen.Should().HaveCount(2);
        kopie.Positionen[0].Text.Should().Be("Pos 1");
        kopie.Positionen[1].Rabatt.Should().Be(10);
        // New GUIDs
        kopie.Positionen[0].Id.Should().NotBe(original.Positionen[0].Id);
        kopie.Positionen[1].Id.Should().NotBe(original.Positionen[1].Id);
    }

    [Fact]
    public async Task Kopiere_SetztGueltigkeitAufHeutePlusGueltigkeitstage()
    {
        var original = MakeAngebot();
        _repo.Setup(r => r.GetByIdAsync(original.Id)).ReturnsAsync(original);
        _nummernService.Setup(n => n.NaechsteNummerAsync()).ReturnsAsync("ANG-2026-00002");
        _repo.Setup(r => r.AddAsync(It.IsAny<Angebot>())).Returns(Task.CompletedTask);

        var vorher = DateTime.UtcNow;
        var kopie = await CreateService().KopiereAsync(original.Id, gueltigkeitsTage: 30);
        var nachher = DateTime.UtcNow;

        kopie.GueltigBis.Should().BeOnOrAfter(vorher.AddDays(30));
        kopie.GueltigBis.Should().BeOnOrBefore(nachher.AddDays(30));
    }

    [Fact]
    public async Task Kopiere_NichtGefunden_WirftException()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Angebot?)null);

        var act = () => CreateService().KopiereAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*nicht gefunden*");
    }

    [Fact]
    public async Task Kopiere_SpeichertKopieInRepository()
    {
        var original = MakeAngebot();
        _repo.Setup(r => r.GetByIdAsync(original.Id)).ReturnsAsync(original);
        _nummernService.Setup(n => n.NaechsteNummerAsync()).ReturnsAsync("ANG-2026-00002");
        _repo.Setup(r => r.AddAsync(It.IsAny<Angebot>())).Returns(Task.CompletedTask);

        await CreateService().KopiereAsync(original.Id);

        _repo.Verify(r => r.AddAsync(It.Is<Angebot>(a => a.Angebotsnummer == "ANG-2026-00002")), Times.Once);
    }
}
