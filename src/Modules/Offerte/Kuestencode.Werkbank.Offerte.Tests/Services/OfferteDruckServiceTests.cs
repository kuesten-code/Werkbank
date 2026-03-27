using FluentAssertions;
using Kuestencode.Werkbank.Offerte.Data.Repositories;
using Kuestencode.Werkbank.Offerte.Domain.Entities;
using Kuestencode.Werkbank.Offerte.Domain.Enums;
using Kuestencode.Werkbank.Offerte.Domain.Services;
using Kuestencode.Werkbank.Offerte.Services;
using Kuestencode.Werkbank.Offerte.Services.Pdf;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Kuestencode.Werkbank.Offerte.Tests.Services;

public class OfferteDruckServiceTests
{
    private readonly Mock<IAngebotRepository> _repo = new();
    private readonly Mock<IOffertePdfService> _pdfService = new();
    private readonly AngebotStatusService _statusService = new();

    private OfferteDruckService CreateService() =>
        new(_repo.Object, _pdfService.Object, _statusService, NullLogger<OfferteDruckService>.Instance);

    private static Angebot MakeAngebot(AngebotStatus status = AngebotStatus.Entwurf) =>
        new()
        {
            Id = Guid.NewGuid(),
            Angebotsnummer = "ANG-2026-00001",
            KundeId = 1,
            Status = status,
            Erstelldatum = DateTime.UtcNow,
            GueltigBis = DateTime.UtcNow.AddDays(30),
            DruckAnzahl = 0,
            Positionen = new List<Angebotsposition>()
        };

    // ─── DruckvorbereitungAsync ───────────────────────────────────────────────

    [Fact]
    public async Task Druckvorbereitung_DelegiertAnPdfService()
    {
        var id = Guid.NewGuid();
        var pdfBytes = new byte[] { 1, 2, 3 };
        _pdfService.Setup(p => p.ErstelleAsync(id)).ReturnsAsync(pdfBytes);

        var result = await CreateService().DruckvorbereitungAsync(id);

        result.Should().BeSameAs(pdfBytes);
        _pdfService.Verify(p => p.ErstelleAsync(id), Times.Once);
    }

    // ─── MarkiereAlsGedrucktAsync ─────────────────────────────────────────────

    [Fact]
    public async Task MarkiereAlsGedruckt_EntwurfAngebot_WirdVersendetUndDruckzaehlerErhoehen()
    {
        var angebot = MakeAngebot(AngebotStatus.Entwurf);
        _repo.Setup(r => r.GetByIdAsync(angebot.Id)).ReturnsAsync(angebot);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Angebot>())).Returns(Task.CompletedTask);

        await CreateService().MarkiereAlsGedrucktAsync(angebot.Id);

        angebot.Status.Should().Be(AngebotStatus.Versendet);
        angebot.DruckAnzahl.Should().Be(1);
        angebot.GedrucktAm.Should().NotBeNull();
        _repo.Verify(r => r.UpdateAsync(angebot), Times.Once);
    }

    [Fact]
    public async Task MarkiereAlsGedruckt_VersendetAngebot_BleibtVersendetUndErhoehtzaehler()
    {
        var angebot = MakeAngebot(AngebotStatus.Versendet);
        angebot.DruckAnzahl = 2;
        _repo.Setup(r => r.GetByIdAsync(angebot.Id)).ReturnsAsync(angebot);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Angebot>())).Returns(Task.CompletedTask);

        await CreateService().MarkiereAlsGedrucktAsync(angebot.Id);

        angebot.Status.Should().Be(AngebotStatus.Versendet);
        angebot.DruckAnzahl.Should().Be(3);
    }

    [Fact]
    public async Task MarkiereAlsGedruckt_NichtGefunden_WirftException()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Angebot?)null);

        var act = () => CreateService().MarkiereAlsGedrucktAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
