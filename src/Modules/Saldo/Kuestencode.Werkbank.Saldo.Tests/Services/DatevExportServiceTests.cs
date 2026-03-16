using FluentAssertions;
using Kuestencode.Shared.ApiClients;
using Kuestencode.Werkbank.Saldo.Data.Repositories;
using Kuestencode.Werkbank.Saldo.Domain.Dtos;
using Kuestencode.Werkbank.Saldo.Domain.Entities;
using Kuestencode.Werkbank.Saldo.Domain.Enums;
using Kuestencode.Werkbank.Saldo.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text;
using Xunit;

namespace Kuestencode.Werkbank.Saldo.Tests.Services;

public class DatevExportServiceTests
{
    private readonly Mock<ISaldoAggregationService> _saldoService = new();
    private readonly Mock<ISaldoSettingsRepository> _settingsRepo = new();
    private readonly Mock<IKontoMappingService> _kontoMappingService = new();
    private readonly Mock<IFakturaApiClient> _fakturaClient = new();
    private readonly Mock<IReceptaApiClient> _receptaClient = new();
    private readonly Mock<IReceptaDataService> _receptaDataService = new();
    private readonly Mock<IExportLogRepository> _exportLogRepo = new();

    private static readonly DateOnly Von = new(2026, 1, 1);
    private static readonly DateOnly Bis = new(2026, 12, 31);

    private DatevExportService CreateService() =>
        new(_saldoService.Object, _settingsRepo.Object, _kontoMappingService.Object,
            _fakturaClient.Object, _receptaClient.Object, _receptaDataService.Object,
            _exportLogRepo.Object, NullLogger<DatevExportService>.Instance);

    private void SetupBasicMocks()
    {
        _settingsRepo.Setup(r => r.GetAsync())
            .ReturnsAsync(new SaldoSettings
            {
                Kontenrahmen = "SKR03",
                BeraterNummer = "12345",
                MandantenNummer = "67890"
            });
        _kontoMappingService.Setup(s => s.GetBankKontoAsync()).ReturnsAsync("1200");
        _exportLogRepo.Setup(r => r.AddAsync(It.IsAny<ExportLog>())).Returns(Task.CompletedTask);
    }

    // ─── Encoding ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExportBuchungsstapel_IstWindows1252Encoded()
    {
        SetupBasicMocks();
        _saldoService.Setup(s => s.GetAlleBuchungenAsync(Von, Bis))
            .ReturnsAsync(new List<BuchungDto>());

        var service = CreateService();
        var result = await service.ExportBuchungsstapelAsync(Von, Bis);

        // Erster Byte-Check: muss mit EXTF in Windows-1252 beginnen
        var windows1252 = Encoding.GetEncoding(1252);
        var text = windows1252.GetString(result);
        text.Should().StartWith("\"EXTF\"");
    }

    [Fact]
    public async Task ExportBuchungsstapel_EnthaltDatevHeader()
    {
        SetupBasicMocks();
        _saldoService.Setup(s => s.GetAlleBuchungenAsync(Von, Bis))
            .ReturnsAsync(new List<BuchungDto>());

        var service = CreateService();
        var result = await service.ExportBuchungsstapelAsync(Von, Bis);

        var text = Encoding.GetEncoding(1252).GetString(result);
        var zeilen = text.Split('\n');

        // Zeile 1: DATEV-Header
        zeilen[0].Should().StartWith("\"EXTF\";700;21;\"Buchungsstapel\"");

        // Zeile 2: Spaltenüberschriften
        zeilen[1].Should().Contain("Umsatz");
        zeilen[1].Should().Contain("Konto");
        zeilen[1].Should().Contain("BU-Schlüssel");
    }

    // ─── Buchungszeilen ───────────────────────────────────────────────────────

    [Fact]
    public async Task ExportBuchungsstapel_EinnahmeHatSollKennzeichen()
    {
        SetupBasicMocks();
        _saldoService.Setup(s => s.GetAlleBuchungenAsync(Von, Bis))
            .ReturnsAsync(new List<BuchungDto>
            {
                new()
                {
                    Typ = BuchungsTyp.Einnahme,
                    Brutto = 1190m,
                    UstSatz = 19,
                    ZahlungsDatum = new DateOnly(2026, 3, 15),
                    QuelleId = "RE-2026-001",
                    Beschreibung = "Kunde XY",
                    KontoNummer = "8400"
                }
            });

        var service = CreateService();
        var result = await service.ExportBuchungsstapelAsync(Von, Bis);

        var text = Encoding.GetEncoding(1252).GetString(result);
        var buchungszeile = text.Split('\n')[2]; // Zeile 3

        buchungszeile.Should().Contain("\"S\"");    // Soll für Einnahme
        buchungszeile.Should().Contain("1190,00");  // Betrag mit Komma
        buchungszeile.Should().Contain("1200");     // Bankkonto
        buchungszeile.Should().Contain("8400");     // Erlöskonto
    }

    [Fact]
    public async Task ExportBuchungsstapel_AusgabeHatHabenKennzeichen()
    {
        SetupBasicMocks();
        _saldoService.Setup(s => s.GetAlleBuchungenAsync(Von, Bis))
            .ReturnsAsync(new List<BuchungDto>
            {
                new()
                {
                    Typ = BuchungsTyp.Ausgabe,
                    Brutto = 357m,
                    UstSatz = 19,
                    ZahlungsDatum = new DateOnly(2026, 4, 1),
                    QuelleId = "ER-001",
                    Beschreibung = "Lieferant AG",
                    KontoNummer = "4980"
                }
            });

        var service = CreateService();
        var result = await service.ExportBuchungsstapelAsync(Von, Bis);

        var text = Encoding.GetEncoding(1252).GetString(result);
        var buchungszeile = text.Split('\n')[2];

        buchungszeile.Should().Contain("\"H\""); // Haben für Ausgabe
        buchungszeile.Should().Contain("357,00");
        buchungszeile.Should().Contain("4980");
    }

    // ─── BU-Schlüssel ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(BuchungsTyp.Einnahme, 19, "3")]
    [InlineData(BuchungsTyp.Einnahme, 7,  "2")]
    [InlineData(BuchungsTyp.Einnahme, 0,  "0")]
    [InlineData(BuchungsTyp.Ausgabe,  19, "9")]
    [InlineData(BuchungsTyp.Ausgabe,  7,  "8")]
    [InlineData(BuchungsTyp.Ausgabe,  0,  "0")]
    public async Task ExportBuchungsstapel_BuSchluesselKorrektNachSteuersatz(
        BuchungsTyp typ, decimal ustSatz, string erwarteteBu)
    {
        SetupBasicMocks();
        _saldoService.Setup(s => s.GetAlleBuchungenAsync(Von, Bis))
            .ReturnsAsync(new List<BuchungDto>
            {
                new()
                {
                    Typ = typ,
                    Brutto = 100m,
                    UstSatz = ustSatz,
                    ZahlungsDatum = Von,
                    KontoNummer = "8400"
                }
            });

        var service = CreateService();
        var result = await service.ExportBuchungsstapelAsync(Von, Bis);

        var text = Encoding.GetEncoding(1252).GetString(result);
        var buchungszeile = text.Split('\n')[2];

        // BU-Schlüssel ist das 6. Feld (0-basiert: Index 5)
        var felder = buchungszeile.Split(';');
        felder[5].Should().Be(erwarteteBu);
    }

    // ─── Belegdatum-Format ────────────────────────────────────────────────────

    [Fact]
    public async Task ExportBuchungsstapel_BelegdatumImDatevFormat()
    {
        SetupBasicMocks();
        _saldoService.Setup(s => s.GetAlleBuchungenAsync(Von, Bis))
            .ReturnsAsync(new List<BuchungDto>
            {
                new()
                {
                    Typ = BuchungsTyp.Einnahme,
                    Brutto = 100m,
                    ZahlungsDatum = new DateOnly(2026, 3, 5), // 05.03.2026
                    KontoNummer = "8400"
                }
            });

        var service = CreateService();
        var result = await service.ExportBuchungsstapelAsync(Von, Bis);

        var text = Encoding.GetEncoding(1252).GetString(result);
        var buchungszeile = text.Split('\n')[2];

        // Belegdatum: TTMMJJJJ
        buchungszeile.Should().Contain("\"05032026\"");
    }

    // ─── Sonderzeichen-Bereinigung ────────────────────────────────────────────

    [Fact]
    public async Task ExportBuchungsstapel_SemikolonInBeschreibungWirdErsetzt()
    {
        SetupBasicMocks();
        _saldoService.Setup(s => s.GetAlleBuchungenAsync(Von, Bis))
            .ReturnsAsync(new List<BuchungDto>
            {
                new()
                {
                    Typ = BuchungsTyp.Einnahme,
                    Brutto = 100m,
                    ZahlungsDatum = Von,
                    Beschreibung = "Kunde; Test",
                    KontoNummer = "8400"
                }
            });

        var service = CreateService();
        var result = await service.ExportBuchungsstapelAsync(Von, Bis);

        var text = Encoding.GetEncoding(1252).GetString(result);

        // Semikolon in Beschreibung muss durch Komma ersetzt worden sein
        text.Should().Contain("Kunde, Test");
        text.Should().NotContain("Kunde; Test");
    }

    [Fact]
    public async Task ExportBuchungsstapel_BeschreibungWirdAuf60ZeichenGekuerzt()
    {
        SetupBasicMocks();
        var langeBeschreibung = new string('A', 100); // 100 Zeichen
        _saldoService.Setup(s => s.GetAlleBuchungenAsync(Von, Bis))
            .ReturnsAsync(new List<BuchungDto>
            {
                new()
                {
                    Typ = BuchungsTyp.Einnahme,
                    Brutto = 100m,
                    ZahlungsDatum = Von,
                    Beschreibung = langeBeschreibung,
                    KontoNummer = "8400"
                }
            });

        var service = CreateService();
        var result = await service.ExportBuchungsstapelAsync(Von, Bis);

        var text = Encoding.GetEncoding(1252).GetString(result);
        var buchungszeile = text.Split('\n')[2];

        // Buchungstext (letztes Feld) darf max. 60 Zeichen enthalten
        var felder = buchungszeile.TrimEnd('\r').Split(';');
        var buchungstext = felder[^1].Trim('"');
        buchungstext.Should().HaveLength(60);
    }

    // ─── Export-Historie ─────────────────────────────────────────────────────

    [Fact]
    public async Task ExportBuchungsstapel_LoggtExport()
    {
        SetupBasicMocks();
        _saldoService.Setup(s => s.GetAlleBuchungenAsync(Von, Bis))
            .ReturnsAsync(new List<BuchungDto>());

        var service = CreateService();
        await service.ExportBuchungsstapelAsync(Von, Bis);

        _exportLogRepo.Verify(r => r.AddAsync(It.Is<ExportLog>(log =>
            log.ExportTyp == ExportTyp.DatevBuchungsstapel &&
            log.ZeitraumVon == Von &&
            log.ZeitraumBis == Bis
        )), Times.Once);
    }

    // ─── Dezimaltrennzeichen ──────────────────────────────────────────────────

    [Fact]
    public async Task ExportBuchungsstapel_NutztKommaAlsDezmialtrennzeichen()
    {
        SetupBasicMocks();
        _saldoService.Setup(s => s.GetAlleBuchungenAsync(Von, Bis))
            .ReturnsAsync(new List<BuchungDto>
            {
                new()
                {
                    Typ = BuchungsTyp.Einnahme,
                    Brutto = 1234.56m,
                    ZahlungsDatum = Von,
                    KontoNummer = "8400"
                }
            });

        var service = CreateService();
        var result = await service.ExportBuchungsstapelAsync(Von, Bis);

        var text = Encoding.GetEncoding(1252).GetString(result);
        text.Should().Contain("1234,56");  // Komma, kein Punkt
        text.Should().NotContain("1234.56");
    }
}
