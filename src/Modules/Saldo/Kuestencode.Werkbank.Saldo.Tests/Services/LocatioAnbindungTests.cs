using System.Net;
using System.Text;
using FluentAssertions;
using Kuestencode.Core.Interfaces;
using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Faktura;
using Kuestencode.Shared.Contracts.Locatio;
using Kuestencode.Shared.Contracts.Recepta;
using Kuestencode.Werkbank.Saldo.Data.Repositories;
using Kuestencode.Werkbank.Saldo.Domain.Dtos;
using Kuestencode.Werkbank.Saldo.Domain.Entities;
using Kuestencode.Werkbank.Saldo.Domain.Enums;
using Kuestencode.Werkbank.Saldo.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Kuestencode.Werkbank.Saldo.Tests.Services;

/// <summary>
/// Saldo-Erweiterung für Locatio: Buchungen als dritte Quelle und Begriffe der Ermittlungsart (V+V).
/// </summary>
public class LocatioAnbindungTests
{
    private static readonly DateOnly Von = new(2026, 1, 1);
    private static readonly DateOnly Bis = new(2026, 12, 31);

    private static readonly List<Konto> Konten =
    [
        new() { KontoNummer = "2750", KontoBezeichnung = "Grundstückserträge", KontoTyp = KontoTyp.Einnahme },
        new() { KontoNummer = "8120", KontoBezeichnung = "Steuerfreie Umsätze", KontoTyp = KontoTyp.Einnahme, UstSatz = 0 },
        new() { KontoNummer = "2120", KontoBezeichnung = "Zinsaufwendungen", KontoTyp = KontoTyp.Ausgabe }
    ];

    private static LocatioBuchungDto Buchung(bool einnahme, decimal netto, string? konto, decimal ust = 0, decimal satz = 0) => new()
    {
        Id = Guid.NewGuid(),
        Referenz = einnahme ? "Miete Whg 1 03/2026" : "Zins Sparkasse",
        Zahlungsdatum = new DateOnly(2026, 3, 3),
        IstEinnahme = einnahme,
        Beschreibung = "Test",
        Netto = netto,
        Ust = ust,
        Brutto = netto + ust,
        UstSatz = satz,
        Kategorie = einnahme ? "Mieteinnahmen" : "Darlehenszinsen",
        KontoNummer = konto
    };

    [Fact]
    public void AusLocatio_UebernimmtKontoUndBezeichnungAusKontenstamm()
    {
        var buchung = StandardKonten.AusLocatio(Buchung(false, 312.50m, "2120"), Konten, "SKR03");

        buchung.Quelle.Should().Be("Locatio");
        buchung.QuelleId.Should().Be("Zins Sparkasse");
        buchung.Typ.Should().Be(BuchungsTyp.Ausgabe);
        buchung.KontoNummer.Should().Be("2120");
        buchung.KontoBezeichnung.Should().Be("Zinsaufwendungen");
        buchung.Netto.Should().Be(312.50m);
    }

    [Theory]
    [InlineData(true, "SKR03", "8120")]
    [InlineData(false, "SKR03", "4980")]
    [InlineData(false, "SKR04", "4900")]
    public void AusLocatio_OhneKonto_NutztFallback(bool einnahme, string kontenrahmen, string erwartet)
    {
        var buchung = StandardKonten.AusLocatio(Buchung(einnahme, 100m, null), Konten, kontenrahmen);

        buchung.KontoNummer.Should().Be(erwartet);
    }

    [Fact]
    public void AusLocatio_EinnahmeOhnePassendesErloeskonto_Nutzt8400UndKategorieAlsBezeichnung()
    {
        var buchung = StandardKonten.AusLocatio(Buchung(true, 1000m, null, 190m, 19m), [], "SKR03");

        buchung.KontoNummer.Should().Be("8400");
        buchung.KontoBezeichnung.Should().Be("Mieteinnahmen");
        buchung.Ust.Should().Be(190m);
    }

    [Fact]
    public async Task EinnahmenUndAusgaben_TrennenLocatioBuchungenNachTyp()
    {
        var locatio = new Mock<ILocatioDataService>();
        locatio.Setup(l => l.GetBuchungenAsync(Von, Bis))
            .ReturnsAsync(new List<LocatioBuchungDto> { Buchung(true, 1000m, "2750"), Buchung(false, 312.50m, "2120") });
        var kontoRepo = new Mock<IKontoRepository>();
        kontoRepo.Setup(r => r.GetByKontenrahmenAsync("SKR03")).ReturnsAsync(Konten);
        var settingsRepo = new Mock<ISaldoSettingsRepository>();
        settingsRepo.Setup(r => r.GetAsync()).ReturnsAsync(new SaldoSettings { Kontenrahmen = "SKR03" });
        var faktura = new Mock<IFakturaApiClient>();
        faktura.Setup(f => f.GetEuerPaymentsAsync(Von, Bis)).ReturnsAsync(new List<InvoiceEuerPaymentDto>());
        var recepta = new Mock<IReceptaDataService>();
        recepta.Setup(r => r.GetPaymentsAsync(Von, Bis)).ReturnsAsync(new List<ReceptaPaymentDto>());
        var mappingRepo = new Mock<IKategorieKontoMappingRepository>();
        mappingRepo.Setup(r => r.GetAllAsync("SKR03")).ReturnsAsync(new List<KategorieKontoMapping>());

        var einnahmen = await new EinnahmenService(faktura.Object, locatio.Object, kontoRepo.Object, settingsRepo.Object,
            NullLogger<EinnahmenService>.Instance).GetEinnahmenAsync(Von, Bis);
        var ausgaben = await new AusgabenService(recepta.Object, locatio.Object, mappingRepo.Object,
            new Mock<IKontoMappingOverrideRepository>().Object, kontoRepo.Object, settingsRepo.Object,
            NullLogger<AusgabenService>.Instance).GetAusgabenAsync(Von, Bis);

        einnahmen.Should().ContainSingle().Which.KontoNummer.Should().Be("2750");
        ausgaben.Should().ContainSingle().Which.Netto.Should().Be(312.50m);
    }

    [Fact]
    public async Task LocatioDataService_OhneUrl_LiefertNichts()
    {
        var service = new LocatioDataService(new HttpClient(), NullLogger<LocatioDataService>.Instance);

        (await service.GetBuchungenAsync(Von, Bis)).Should().BeEmpty();
    }

    [Fact]
    public async Task LocatioDataService_RuftEndpunktAufUndDeserialisiert()
    {
        var handler = new StubHandler(HttpStatusCode.OK,
            "[{\"id\":\"6f1c1b1e-0000-4000-8000-000000000001\",\"referenz\":\"Miete\",\"zahlungsdatum\":\"2026-03-03\",\"istEinnahme\":true,\"netto\":1000}]");
        var service = new LocatioDataService(new HttpClient(handler) { BaseAddress = new Uri("http://locatio:8087") },
            NullLogger<LocatioDataService>.Instance);

        var buchungen = await service.GetBuchungenAsync(Von, Bis);

        buchungen.Should().ContainSingle().Which.Netto.Should().Be(1000m);
        handler.LetzteAnfrage!.PathAndQuery.Should().Be("/api/locatio/saldo-buchungen?from=2026-01-01&to=2026-12-31");
    }

    [Fact]
    public async Task LocatioDataService_Fehler_LiefertLeereListe()
    {
        var service = new LocatioDataService(
            new HttpClient(new StubHandler(HttpStatusCode.InternalServerError, "")) { BaseAddress = new Uri("http://locatio:8087") },
            NullLogger<LocatioDataService>.Instance);

        (await service.GetBuchungenAsync(Von, Bis)).Should().BeEmpty();
    }

    [Theory]
    [InlineData(Ermittlungsart.Euer, "Betriebsausgaben")]
    [InlineData(Ermittlungsart.VermietungUndVerpachtung, "Werbungskosten")]
    public async Task EuerSummary_GruppeUndBegriffeFolgenDerErmittlungsart(Ermittlungsart art, string ausgabenBegriff)
    {
        var settingsRepo = new Mock<ISaldoSettingsRepository>();
        settingsRepo.Setup(r => r.GetAsync()).ReturnsAsync(new SaldoSettings { Kontenrahmen = "SKR03", Ermittlungsart = art });
        var kontoRepo = new Mock<IKontoRepository>();
        kontoRepo.Setup(r => r.GetByKontenrahmenAsync("SKR03")).ReturnsAsync(Konten);
        var einnahmen = new Mock<IEinnahmenService>();
        einnahmen.Setup(s => s.GetEinnahmenAsync(Von, Bis)).ReturnsAsync(new List<BuchungDto>());
        var ausgaben = new Mock<IAusgabenService>();
        ausgaben.Setup(s => s.GetAusgabenAsync(Von, Bis)).ReturnsAsync(
            new List<BuchungDto> { new() { Netto = 100m, Brutto = 100m, Typ = BuchungsTyp.Ausgabe, KontoNummer = "2120" } });

        var summary = await new EuerService(einnahmen.Object, ausgaben.Object, kontoRepo.Object, settingsRepo.Object,
            new Mock<ICompanyService>().Object).GetEuerSummaryAsync(new EuerFilterDto { Von = Von, Bis = Bis });

        summary.Ermittlungsart.Should().Be(art);
        summary.Ausgaben.Single().Gruppe.Should().Be(ausgabenBegriff);
        summary.Begriffe.Ausgaben.Should().Be(ausgabenBegriff);
    }

    [Fact]
    public void Begriffe_VermietungUndVerpachtung()
    {
        var begriffe = ErmittlungsBegriffe.Fuer(Ermittlungsart.VermietungUndVerpachtung);

        begriffe.Kurz.Should().Be("V+V");
        begriffe.Ergebnis(10m).Should().Be("Überschuss");
        begriffe.Ergebnis(-10m).Should().Be("Werbungskostenüberschuss");
        ErmittlungsBegriffe.Fuer(Ermittlungsart.Euer).Ergebnis(-1m).Should().Be("Verlust");
    }

    [Fact]
    public async Task Settings_SpeichernUndLadenDieErmittlungsart()
    {
        var gespeichert = new SaldoSettings { Kontenrahmen = "SKR03" };
        var repo = new Mock<ISaldoSettingsRepository>();
        repo.Setup(r => r.GetAsync()).ReturnsAsync(gespeichert);
        repo.Setup(r => r.UpdateAsync(It.IsAny<SaldoSettings>())).ReturnsAsync((SaldoSettings s) => s);

        var dto = await new SaldoSettingsService(repo.Object).UpdateSettingsAsync(new UpdateSaldoSettingsDto
        {
            Kontenrahmen = "SKR03",
            Ermittlungsart = Ermittlungsart.VermietungUndVerpachtung
        });

        gespeichert.Ermittlungsart.Should().Be(Ermittlungsart.VermietungUndVerpachtung);
        dto.Ermittlungsart.Should().Be(Ermittlungsart.VermietungUndVerpachtung);
    }

    private sealed class StubHandler(HttpStatusCode status, string inhalt) : HttpMessageHandler
    {
        public Uri? LetzteAnfrage { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LetzteAnfrage = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(inhalt, Encoding.UTF8, "application/json")
            });
        }
    }
}
