using FluentAssertions;
using Kuestencode.Werkbank.Saldo.Data.Repositories;
using Kuestencode.Werkbank.Saldo.Domain.Dtos;
using Kuestencode.Werkbank.Saldo.Domain.Entities;
using Kuestencode.Werkbank.Saldo.Domain.Enums;
using Kuestencode.Werkbank.Saldo.Services;
using Moq;
using Xunit;

namespace Kuestencode.Werkbank.Saldo.Tests.Services;

public class KontoServiceTests
{
    private readonly Mock<IKontoRepository> _kontoRepo = new();
    private readonly Mock<IKategorieKontoMappingRepository> _mappingRepo = new();

    private KontoService CreateService() => new(_kontoRepo.Object, _mappingRepo.Object);

    // ─── GetKontenAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetKonten_GibtAlleKontenZurueck()
    {
        _kontoRepo.Setup(r => r.GetAllAsync(null))
            .ReturnsAsync(new List<Konto>
            {
                new() { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", KontoNummer = "8400", KontoBezeichnung = "Erlöse 19%", KontoTyp = KontoTyp.Einnahme, UstSatz = 19, IsActive = true },
                new() { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", KontoNummer = "4930", KontoBezeichnung = "Bürobedarf", KontoTyp = KontoTyp.Ausgabe, IsActive = true }
            });

        var service = CreateService();
        var result = await service.GetKontenAsync();

        result.Should().HaveCount(2);
        result[0].KontoNummer.Should().Be("8400");
        result[0].KontoTyp.Should().Be("Einnahme");
        result[0].UstSatz.Should().Be(19);
        result[0].IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetKonten_FiltertNachKontenrahmen()
    {
        _kontoRepo.Setup(r => r.GetAllAsync("SKR04"))
            .ReturnsAsync(new List<Konto>
            {
                new() { KontoNummer = "4400", KontoBezeichnung = "Erlöse 19%", KontoTyp = KontoTyp.Einnahme, Kontenrahmen = "SKR04" }
            });

        var service = CreateService();
        var result = await service.GetKontenAsync("SKR04");

        result.Should().HaveCount(1);
        result[0].KontoNummer.Should().Be("4400");
        _kontoRepo.Verify(r => r.GetAllAsync("SKR04"), Times.Once);
    }

    [Fact]
    public async Task GetKonten_GibtLeereListeBeiLeeremRepository()
    {
        _kontoRepo.Setup(r => r.GetAllAsync(null)).ReturnsAsync(new List<Konto>());

        var service = CreateService();
        var result = await service.GetKontenAsync();

        result.Should().BeEmpty();
    }

    // ─── GetMappingsAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetMappings_GibtAlleMappingsZurueck()
    {
        _mappingRepo.Setup(r => r.GetAllAsync(null))
            .ReturnsAsync(new List<KategorieKontoMapping>
            {
                new()
                {
                    Id = Guid.NewGuid(), Kontenrahmen = "SKR03",
                    ReceiptaKategorie = "Büromaterial", KontoNummer = "4930",
                    IsCustom = false,
                    Konto = new Konto { KontoBezeichnung = "Bürobedarf" }
                }
            });

        var service = CreateService();
        var result = await service.GetMappingsAsync();

        result.Should().HaveCount(1);
        result[0].ReceiptaKategorie.Should().Be("Büromaterial");
        result[0].KontoNummer.Should().Be("4930");
        result[0].KontoBezeichnung.Should().Be("Bürobedarf");
        result[0].IsCustom.Should().BeFalse();
    }

    [Fact]
    public async Task GetMappings_FiltertNachKontenrahmen()
    {
        _mappingRepo.Setup(r => r.GetAllAsync("SKR04"))
            .ReturnsAsync(new List<KategorieKontoMapping>
            {
                new() { ReceiptaKategorie = "Reise", KontoNummer = "6320", Kontenrahmen = "SKR04" }
            });

        var service = CreateService();
        var result = await service.GetMappingsAsync("SKR04");

        result.Should().HaveCount(1);
        _mappingRepo.Verify(r => r.GetAllAsync("SKR04"), Times.Once);
    }

    [Fact]
    public async Task GetMappings_KontoBezeichnungLeerWennKeinKontoNavigationProperty()
    {
        _mappingRepo.Setup(r => r.GetAllAsync(null))
            .ReturnsAsync(new List<KategorieKontoMapping>
            {
                new()
                {
                    ReceiptaKategorie = "Sonstiges", KontoNummer = "4980",
                    Konto = null // kein Navigation Property geladen
                }
            });

        var service = CreateService();
        var result = await service.GetMappingsAsync();

        result[0].KontoBezeichnung.Should().BeEmpty();
    }

    // ─── UpdateMappingAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdateMapping_GibtNullZurueckWennMappingNichtGefunden()
    {
        var id = Guid.NewGuid();
        _mappingRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((KategorieKontoMapping?)null);

        var service = CreateService();
        var result = await service.UpdateMappingAsync(id, new UpdateKategorieKontoMappingDto { KontoNummer = "4930" });

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateMapping_WirftExceptionWennKontoNichtImKontenrahmen()
    {
        var id = Guid.NewGuid();
        _mappingRepo.Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(new KategorieKontoMapping
            {
                Id = id, Kontenrahmen = "SKR03", ReceiptaKategorie = "Büromaterial", KontoNummer = "4930"
            });
        _kontoRepo.Setup(r => r.ExistsAsync("SKR03", "9999")).ReturnsAsync(false);

        var service = CreateService();
        var act = () => service.UpdateMappingAsync(id, new UpdateKategorieKontoMappingDto { KontoNummer = "9999" });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*9999*");
    }

    [Fact]
    public async Task UpdateMapping_AktualisiertKontoNummerUndSetztIsCustom()
    {
        var id = Guid.NewGuid();
        var mapping = new KategorieKontoMapping
        {
            Id = id, Kontenrahmen = "SKR03", ReceiptaKategorie = "Büromaterial", KontoNummer = "4930", IsCustom = false
        };
        _mappingRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(mapping);
        _kontoRepo.Setup(r => r.ExistsAsync("SKR03", "4960")).ReturnsAsync(true);
        _mappingRepo.Setup(r => r.UpdateAsync(It.IsAny<KategorieKontoMapping>()))
            .ReturnsAsync((KategorieKontoMapping m) => m);
        _mappingRepo.Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(new KategorieKontoMapping
            {
                Id = id, Kontenrahmen = "SKR03", ReceiptaKategorie = "Büromaterial",
                KontoNummer = "4960", IsCustom = true,
                Konto = new Konto { KontoBezeichnung = "Sonstiger Bürobedarf" }
            });

        var service = CreateService();
        var result = await service.UpdateMappingAsync(id, new UpdateKategorieKontoMappingDto { KontoNummer = "4960" });

        result.Should().NotBeNull();
        result!.KontoNummer.Should().Be("4960");
        result.IsCustom.Should().BeTrue();
        _mappingRepo.Verify(r => r.UpdateAsync(It.Is<KategorieKontoMapping>(m =>
            m.KontoNummer == "4960" && m.IsCustom == true)), Times.Once);
    }
}
