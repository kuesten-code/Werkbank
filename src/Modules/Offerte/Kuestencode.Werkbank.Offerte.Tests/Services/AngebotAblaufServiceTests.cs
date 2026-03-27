using FluentAssertions;
using Kuestencode.Werkbank.Offerte.Data.Repositories;
using Kuestencode.Werkbank.Offerte.Domain.Entities;
using Kuestencode.Werkbank.Offerte.Domain.Enums;
using Kuestencode.Werkbank.Offerte.Domain.Services;
using Kuestencode.Werkbank.Offerte.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Kuestencode.Werkbank.Offerte.Tests.Services;

public class AngebotAblaufServiceTests
{
    private readonly Mock<IAngebotRepository> _repo = new();
    private readonly AngebotStatusService _statusService = new();

    private AngebotAblaufService CreateService() =>
        new(_repo.Object, _statusService, NullLogger<AngebotAblaufService>.Instance);

    private static Angebot MakeAbgelaufenes(string nummer = "ANG-2026-00001") =>
        new()
        {
            Id = Guid.NewGuid(),
            Angebotsnummer = nummer,
            KundeId = 1,
            Status = AngebotStatus.Versendet,
            Erstelldatum = DateTime.UtcNow.AddDays(-30),
            GueltigBis = DateTime.UtcNow.AddDays(-1),
            Positionen = new List<Angebotsposition>()
        };

    [Fact]
    public async Task PruefeUndAktualisiere_KeineAbgelaufenen_GibtNullZurueck()
    {
        _repo.Setup(r => r.GetAbgelaufeneAsync()).ReturnsAsync(new List<Angebot>());

        var count = await CreateService().PruefeUndAktualiereAbgelaufeneAsync();

        count.Should().Be(0);
        _repo.Verify(r => r.UpdateAsync(It.IsAny<Angebot>()), Times.Never);
    }

    [Fact]
    public async Task PruefeUndAktualisiere_EinAbgelaufenes_MarktIhAlsAbgelaufen()
    {
        var angebot = MakeAbgelaufenes();
        _repo.Setup(r => r.GetAbgelaufeneAsync()).ReturnsAsync(new List<Angebot> { angebot });
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Angebot>())).Returns(Task.CompletedTask);

        var count = await CreateService().PruefeUndAktualiereAbgelaufeneAsync();

        count.Should().Be(1);
        angebot.Status.Should().Be(AngebotStatus.Abgelaufen);
        angebot.AbgelaufenAm.Should().NotBeNull();
        _repo.Verify(r => r.UpdateAsync(angebot), Times.Once);
    }

    [Fact]
    public async Task PruefeUndAktualisiere_MehrereAbgelaufene_AktualisiertAlle()
    {
        var a1 = MakeAbgelaufenes("ANG-2026-00001");
        var a2 = MakeAbgelaufenes("ANG-2026-00002");
        _repo.Setup(r => r.GetAbgelaufeneAsync()).ReturnsAsync(new List<Angebot> { a1, a2 });
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Angebot>())).Returns(Task.CompletedTask);

        var count = await CreateService().PruefeUndAktualiereAbgelaufeneAsync();

        count.Should().Be(2);
        _repo.Verify(r => r.UpdateAsync(It.IsAny<Angebot>()), Times.Exactly(2));
    }

    [Fact]
    public async Task PruefeUndAktualisiere_UpdateFehlerBeiEinem_FaehrtFortUndGibtTeilzahlZurueck()
    {
        var a1 = MakeAbgelaufenes("ANG-2026-00001");
        var a2 = MakeAbgelaufenes("ANG-2026-00002");
        _repo.Setup(r => r.GetAbgelaufeneAsync()).ReturnsAsync(new List<Angebot> { a1, a2 });
        _repo.Setup(r => r.UpdateAsync(a1)).ThrowsAsync(new Exception("DB-Fehler"));
        _repo.Setup(r => r.UpdateAsync(a2)).Returns(Task.CompletedTask);

        var count = await CreateService().PruefeUndAktualiereAbgelaufeneAsync();

        // a1 failed, a2 succeeded → count = 1
        count.Should().Be(1);
    }
}
