using FluentAssertions;
using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Shared.Contracts.Locatio;
using Kuestencode.Werkbank.Locatio.Controllers;
using Kuestencode.Werkbank.Locatio.Domain.Dtos;
using Kuestencode.Werkbank.Locatio.Domain.Entities;
using Kuestencode.Werkbank.Locatio.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using QuestPDF.Infrastructure;
using Xunit;

namespace Kuestencode.Werkbank.Locatio.Tests;

public class ControllerTests
{
    private readonly TestDaten _daten = new();
    private readonly Mock<IHostApiClient> _host = new();

    private DateienController DateienController() => new(
        _daten.Factory, new VeraeusserungsgewinnService(_daten.Factory), _host.Object, NullLogger<DateienController>.Instance);

    [Fact]
    public async Task SaldoBuchungen_LiefertBuchungenDesZeitraums()
    {
        var gebaeude = _daten.Gebaeude();
        _daten.Speichere(new Kostenposten
        {
            Id = Guid.NewGuid(), GebaeudeId = gebaeude.Id, KategorieId = _daten.Nebenkosten.Id, Betrag = 42m,
            Zahlungsdatum = new DateOnly(2026, 2, 1), Beschreibung = "Wasser"
        });
        var controller = new SaldoBuchungenController(new SaldoBuchungService(_daten.Factory));

        var ergebnis = await controller.Get(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        var buchungen = ergebnis.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeAssignableTo<List<LocatioBuchungDto>>().Subject;
        buchungen.Should().ContainSingle().Which.Netto.Should().Be(42m);
    }

    [Fact]
    public async Task SaldoBuchungen_VonNachBis_BadRequest()
    {
        var controller = new SaldoBuchungenController(new SaldoBuchungService(_daten.Factory));

        var ergebnis = await controller.Get(new DateOnly(2026, 12, 31), new DateOnly(2026, 1, 1));

        ergebnis.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Dokument_LiefertDateiOderNotFound()
    {
        var dokument = new DateiUpload("grundriss.pdf", "application/pdf", [1, 2]).ZuDokument();
        _daten.Speichere(dokument);

        var datei = await DateienController().Dokument(dokument.Id);
        var fehlt = await DateienController().Dokument(Guid.NewGuid());

        var inhalt = datei.Should().BeOfType<FileContentResult>().Subject;
        inhalt.FileDownloadName.Should().Be("grundriss.pdf");
        inhalt.ContentType.Should().Be("application/pdf");
        fehlt.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Veraeusserungsgewinn_ExportiertPdfUndCsv()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var einheit = TestDaten.Einheit("Whg 1");
        _daten.Gebaeude(einheiten: [einheit]);
        _host.Setup(h => h.GetCompanyAsync()).ReturnsAsync(new CompanyDto { BusinessName = "", OwnerFullName = "Muster eGbR" });

        var pdf = await DateienController().Veraeusserungsgewinn("einheit", einheit.Id, "pdf");
        var csv = await DateienController().Veraeusserungsgewinn("einheit", einheit.Id, "csv");

        pdf.Should().BeOfType<FileContentResult>().Which.ContentType.Should().Be("application/pdf");
        csv.Should().BeOfType<FileContentResult>().Which.FileDownloadName.Should().Be("Veraeusserungsgewinn_Haus_am_Deich___Whg_1.csv");
    }

    [Fact]
    public async Task Veraeusserungsgewinn_FirmendatenNichtErreichbar_ErzeugtTrotzdemPdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var gebaeude = _daten.Gebaeude();
        _host.Setup(h => h.GetCompanyAsync()).ThrowsAsync(new HttpRequestException());

        var pdf = await DateienController().Veraeusserungsgewinn("gebaeude", gebaeude.Id, "pdf");

        pdf.Should().BeOfType<FileContentResult>();
    }

    [Theory]
    [InlineData("einheit", "xlsx")]
    [InlineData("unbekannt", "pdf")]
    public async Task Veraeusserungsgewinn_UnbekannteArtOderFormat_NotFound(string art, string format)
    {
        var einheit = TestDaten.Einheit("Whg 1");
        _daten.Gebaeude(einheiten: [einheit]);

        var ergebnis = await DateienController().Veraeusserungsgewinn(art, einheit.Id, format);

        ergebnis.Should().BeOfType<NotFoundResult>();
    }
}
