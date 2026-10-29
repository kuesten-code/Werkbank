using System.Text;
using FluentAssertions;
using Kuestencode.Werkbank.Locatio.Domain.Dtos;
using Kuestencode.Werkbank.Locatio.Domain.Entities;
using Kuestencode.Werkbank.Locatio.Domain.Enums;
using Kuestencode.Werkbank.Locatio.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using Xunit;

namespace Kuestencode.Werkbank.Locatio.Tests.Services;

public class AfaUndGewinnTests
{
    private readonly TestDaten _daten = new();
    private readonly AfaService _afa;
    private readonly VeraeusserungsgewinnService _gewinn;

    public AfaUndGewinnTests()
    {
        _afa = new AfaService(_daten.Factory);
        _gewinn = new VeraeusserungsgewinnService(_daten.Factory);
    }

    private Mieteinheit EinheitMitAnschaffung(EinheitStatus status = EinheitStatus.Vermietet)
    {
        var einheit = TestDaten.Einheit("Whg 1", status);
        einheit.AnschaffungsDatum = new DateOnly(2020, 1, 1);
        einheit.AnschaffungskostenGebaeude = 150_000m;
        einheit.AnschaffungskostenGrundUndBoden = 50_000m;
        return einheit;
    }

    [Fact]
    public async Task BucheAfa_LegtKostenpostenAn_ZweitesMalWirftException()
    {
        var einheit = EinheitMitAnschaffung();
        _daten.Gebaeude(einheiten: [einheit]);

        await _afa.BucheAsync(einheit.Id, 2026);

        await using (var db = _daten.Factory.CreateDbContext())
        {
            var posten = await db.Kostenposten.SingleAsync();
            posten.Betrag.Should().Be(3_000m);
            posten.KategorieId.Should().Be(_daten.Afa.Id);
            posten.Zahlungsdatum.Should().Be(new DateOnly(2026, 12, 31));
        }

        var vorschlag = (await _afa.GetVorschlaegeAsync(2026)).Single();
        vorschlag.BereitsGebucht.Should().BeTrue();
        var nochmal = () => _afa.BucheAsync(einheit.Id, 2026);
        await nochmal.Should().ThrowAsync<InvalidOperationException>().WithMessage("*bereits gebucht*");
    }

    [Fact]
    public async Task AfaVorschlag_BeruecksichtigtVorjahre()
    {
        var einheit = EinheitMitAnschaffung();
        _daten.Gebaeude(einheiten: [einheit]);
        await _afa.BucheAsync(einheit.Id, 2025);

        var vorschlag = (await _afa.GetVorschlaegeAsync(2026)).Single();

        vorschlag.BereitsAngesetzt.Should().Be(3_000m);
        vorschlag.BereitsGebucht.Should().BeFalse();
        vorschlag.Monate.Should().Be(12);
    }

    [Fact]
    public async Task AfaVorschlag_FremdeigentumUndOhneAnschaffung_Ausgeschlossen()
    {
        var ohneAnschaffung = TestDaten.Einheit("Whg 2");
        _daten.Gebaeude(einheiten: [EinheitMitAnschaffung(EinheitStatus.Fremdeigentum), ohneAnschaffung]);

        (await _afa.GetVorschlaegeAsync(2026)).Should().BeEmpty();
        var act = () => _afa.BucheAsync(ohneAnschaffung.Id, 2026);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Veraeusserungsgewinn_Einheit_ZiehtAngesetzteAfaAb()
    {
        var einheit = EinheitMitAnschaffung();
        _daten.Gebaeude(einheiten: [einheit]);
        await _afa.BucheAsync(einheit.Id, 2025);
        await new VeraeusserungService(_daten.Factory)
            .VeraeussereEinheitAsync(einheit.Id, new VeraeusserungDaten(new DateOnly(2026, 7, 1), 260_000m, 8_000m));

        var dto = (await _gewinn.FuerEinheitAsync(einheit.Id))!;

        dto.Anschaffungskosten.Should().Be(200_000m);
        dto.KumulierteAfa.Should().Be(3_000m);
        dto.Restbuchwert.Should().Be(197_000m);
        dto.Gewinn.Should().Be(55_000m);
        dto.HaltedauerMonate.Should().Be(78);
        dto.UnterZehnJahren.Should().BeTrue();
        dto.Objekt.Should().Contain("Whg 1");
    }

    [Fact]
    public async Task Veraeusserungsgewinn_GebaeudeOhneEigeneAnschaffung_SummiertEinheiten()
    {
        var einheit = EinheitMitAnschaffung();
        var fremd = TestDaten.Einheit("Whg 5", EinheitStatus.Fremdeigentum);
        var gebaeude = _daten.Gebaeude(einheiten: [einheit, fremd]);
        await new VeraeusserungService(_daten.Factory)
            .VeraeussereGebaeudeAsync(gebaeude.Id, new VeraeusserungDaten(new DateOnly(2031, 3, 1), 300_000m, null));

        var dto = (await _gewinn.FuerGebaeudeAsync(gebaeude.Id))!;

        dto.Anschaffungskosten.Should().Be(200_000m);
        dto.AnschaffungsDatum.Should().Be(new DateOnly(2020, 1, 1));
        dto.HaltedauerMonate.Should().Be(134);
        dto.UnterZehnJahren.Should().BeFalse();
    }

    [Fact]
    public async Task Veraeusserungsgewinn_Unbekannt_Null()
    {
        (await _gewinn.FuerEinheitAsync(Guid.NewGuid())).Should().BeNull();
        (await _gewinn.FuerGebaeudeAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Theory]
    [InlineData(2020, 5, 15, 2030, 5, 14, 119)]
    [InlineData(2020, 5, 15, 2030, 5, 15, 120)]
    public void Haltedauer_ZaehltVolleMonate(int ja, int jm, int jt, int vj, int vm, int vt, int erwartet)
    {
        VeraeusserungsgewinnService.Haltedauer(new DateOnly(ja, jm, jt), new DateOnly(vj, vm, vt)).Should().Be(erwartet);
        VeraeusserungsgewinnService.Haltedauer(null, new DateOnly(vj, vm, vt)).Should().BeNull();
    }

    [Fact]
    public void Export_CsvUndPdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var dto = new VeraeusserungsgewinnDto
        {
            Objekt = "Haus; am Deich",
            AnschaffungsDatum = new DateOnly(2020, 1, 1),
            VerkaufsDatum = new DateOnly(2026, 7, 1),
            HaltedauerMonate = 78,
            Veraeusserungspreis = 260_000m,
            Anschaffungskosten = 200_000m
        };

        var csv = Encoding.UTF8.GetString(VeraeusserungsgewinnExport.ErzeugeCsv(dto));
        csv.Should().Contain("Objekt;Haus, am Deich");
        csv.Should().Contain("260.000,00 €");
        csv.Should().Contain("§ 23 EStG");
        csv.Should().Contain("Orientierungswert");

        var pdf = VeraeusserungsgewinnExport.ErzeugePdf(dto, "Muster eGbR");
        Encoding.ASCII.GetString(pdf, 0, 4).Should().Be("%PDF");

        VeraeusserungsgewinnExport.HaltedauerText(null).Should().Contain("unbekannt");
    }
}
