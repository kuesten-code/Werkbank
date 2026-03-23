using FluentAssertions;
using Kuestencode.Werkbank.Saldo.Data.Repositories;
using Kuestencode.Werkbank.Saldo.Domain.Dtos;
using Kuestencode.Werkbank.Saldo.Domain.Entities;
using Kuestencode.Werkbank.Saldo.Services;
using Moq;
using Xunit;

namespace Kuestencode.Werkbank.Saldo.Tests.Services;

public class SaldoSettingsServiceTests
{
    private readonly Mock<ISaldoSettingsRepository> _repo = new();

    private SaldoSettingsService CreateService() => new(_repo.Object);

    // ─── GetSettingsAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetSettings_GibtNullZurueckWennNichtVorhanden()
    {
        _repo.Setup(r => r.GetAsync()).ReturnsAsync((SaldoSettings?)null);

        var service = CreateService();
        var result = await service.GetSettingsAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSettings_MapptEntityKorrektAufDto()
    {
        var entity = new SaldoSettings
        {
            Id = Guid.NewGuid(),
            Kontenrahmen = "SKR04",
            BeraterNummer = "98765",
            MandantenNummer = "11111",
            WirtschaftsjahrBeginn = 4
        };
        _repo.Setup(r => r.GetAsync()).ReturnsAsync(entity);

        var service = CreateService();
        var result = await service.GetSettingsAsync();

        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
        result.Kontenrahmen.Should().Be("SKR04");
        result.BeraterNummer.Should().Be("98765");
        result.MandantenNummer.Should().Be("11111");
        result.WirtschaftsjahrBeginn.Should().Be(4);
    }

    // ─── UpdateSettingsAsync (Create) ─────────────────────────────────────────

    [Fact]
    public async Task UpdateSettings_ErstelltNeueSettingsWennNichtVorhanden()
    {
        _repo.Setup(r => r.GetAsync()).ReturnsAsync((SaldoSettings?)null);
        _repo.Setup(r => r.CreateAsync(It.IsAny<SaldoSettings>()))
            .ReturnsAsync((SaldoSettings s) => s);

        var dto = new UpdateSaldoSettingsDto
        {
            Kontenrahmen = "SKR03",
            BeraterNummer = "12345",
            MandantenNummer = "67890",
            WirtschaftsjahrBeginn = 1
        };

        var service = CreateService();
        var result = await service.UpdateSettingsAsync(dto);

        _repo.Verify(r => r.CreateAsync(It.Is<SaldoSettings>(s =>
            s.Kontenrahmen == "SKR03" &&
            s.BeraterNummer == "12345" &&
            s.MandantenNummer == "67890" &&
            s.WirtschaftsjahrBeginn == 1
        )), Times.Once);
        result.Kontenrahmen.Should().Be("SKR03");
    }

    [Fact]
    public async Task UpdateSettings_AktualisiertVorhandeneSettings()
    {
        var existing = new SaldoSettings
        {
            Id = Guid.NewGuid(),
            Kontenrahmen = "SKR03",
            BeraterNummer = "old",
            MandantenNummer = "old",
            WirtschaftsjahrBeginn = 1
        };
        _repo.Setup(r => r.GetAsync()).ReturnsAsync(existing);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<SaldoSettings>()))
            .ReturnsAsync((SaldoSettings s) => s);

        var dto = new UpdateSaldoSettingsDto
        {
            Kontenrahmen = "SKR04",
            BeraterNummer = "99999",
            MandantenNummer = "88888",
            WirtschaftsjahrBeginn = 7
        };

        var service = CreateService();
        var result = await service.UpdateSettingsAsync(dto);

        _repo.Verify(r => r.UpdateAsync(It.Is<SaldoSettings>(s =>
            s.Kontenrahmen == "SKR04" &&
            s.BeraterNummer == "99999" &&
            s.MandantenNummer == "88888" &&
            s.WirtschaftsjahrBeginn == 7
        )), Times.Once);
        _repo.Verify(r => r.CreateAsync(It.IsAny<SaldoSettings>()), Times.Never);

        result.Kontenrahmen.Should().Be("SKR04");
        result.BeraterNummer.Should().Be("99999");
    }

    [Fact]
    public async Task UpdateSettings_GibtAktualisiertesDtoZurueck()
    {
        _repo.Setup(r => r.GetAsync()).ReturnsAsync((SaldoSettings?)null);
        var created = new SaldoSettings
        {
            Id = Guid.NewGuid(),
            Kontenrahmen = "SKR03",
            BeraterNummer = "12345",
            MandantenNummer = null,
            WirtschaftsjahrBeginn = 1
        };
        _repo.Setup(r => r.CreateAsync(It.IsAny<SaldoSettings>())).ReturnsAsync(created);

        var service = CreateService();
        var result = await service.UpdateSettingsAsync(new UpdateSaldoSettingsDto { Kontenrahmen = "SKR03", BeraterNummer = "12345" });

        result.Id.Should().Be(created.Id);
        result.BeraterNummer.Should().Be("12345");
    }

    [Fact]
    public async Task UpdateSettings_KeineCreateUndUpdateBeiVorhandenenSettings()
    {
        var existing = new SaldoSettings { Id = Guid.NewGuid(), Kontenrahmen = "SKR03" };
        _repo.Setup(r => r.GetAsync()).ReturnsAsync(existing);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<SaldoSettings>())).ReturnsAsync(existing);

        var service = CreateService();
        await service.UpdateSettingsAsync(new UpdateSaldoSettingsDto { Kontenrahmen = "SKR03" });

        _repo.Verify(r => r.CreateAsync(It.IsAny<SaldoSettings>()), Times.Never);
        _repo.Verify(r => r.UpdateAsync(It.IsAny<SaldoSettings>()), Times.Once);
    }
}
