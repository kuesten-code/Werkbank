using FluentAssertions;
using Kuestencode.Werkbank.Offerte.Domain.Entities;
using Kuestencode.Werkbank.Offerte.Domain.Enums;
using Kuestencode.Werkbank.Offerte.Domain.Validation;
using Xunit;

namespace Kuestencode.Werkbank.Offerte.Tests.Domain;

/// <summary>
/// Additional validator tests for ValidierenFuerVersand and edge cases
/// not covered by the existing AngebotValidatorTests.
/// </summary>
public class AngebotValidatorErweiterungTests
{
    private readonly AngebotValidator _validator = new();

    private static Angebot MakeGueltigesAngebot() =>
        new()
        {
            Id = Guid.NewGuid(),
            Angebotsnummer = "ANG-2026-00001",
            KundeId = 1,
            Status = AngebotStatus.Entwurf,
            Erstelldatum = DateTime.UtcNow,
            GueltigBis = DateTime.UtcNow.AddDays(30),
            Positionen = new List<Angebotsposition>
            {
                new() { Text = "Test", Menge = 1, Einzelpreis = 100, Steuersatz = 19 }
            }
        };

    // ─── ValidierenFuerVersand ────────────────────────────────────────────────

    [Fact]
    public void ValidierenFuerVersand_GueltigesAngebot_KeineFehler()
    {
        var angebot = MakeGueltigesAngebot();

        var fehler = _validator.ValidierenFuerVersand(angebot);

        fehler.Should().BeEmpty();
    }

    [Fact]
    public void ValidierenFuerVersand_AbgelaufeneGueltigBis_Fehler()
    {
        var angebot = MakeGueltigesAngebot();
        angebot.GueltigBis = DateTime.UtcNow.AddDays(-1);

        var fehler = _validator.ValidierenFuerVersand(angebot);

        fehler.Should().Contain(f => f.Contains("abgelaufen"));
    }

    [Fact]
    public void ValidierenFuerVersand_BruttoNull_Fehler()
    {
        var angebot = MakeGueltigesAngebot();
        angebot.Positionen[0].Einzelpreis = 0;

        var fehler = _validator.ValidierenFuerVersand(angebot);

        fehler.Should().Contain(f => f.Contains("Gesamtbetrag"));
    }

    [Fact]
    public void ValidierenFuerVersand_KeinKundeUndAbgelaufen_MehrereFehler()
    {
        var angebot = MakeGueltigesAngebot();
        angebot.KundeId = 0;
        angebot.GueltigBis = DateTime.UtcNow.AddDays(-1);

        var fehler = _validator.ValidierenFuerVersand(angebot);

        fehler.Should().HaveCountGreaterThan(1);
    }

    // ─── ValidierenPosition edge cases ────────────────────────────────────────

    [Fact]
    public void ValidierenPosition_NegativerEinzelpreis_Fehler()
    {
        var pos = new Angebotsposition { Text = "Test", Menge = 1, Einzelpreis = -10, Steuersatz = 19 };

        var fehler = _validator.ValidierenPosition(pos, 1);

        fehler.Should().Contain(f => f.Contains("Einzelpreis"));
    }

    [Fact]
    public void ValidierenPosition_SteuersatzUeber100_Fehler()
    {
        var pos = new Angebotsposition { Text = "Test", Menge = 1, Einzelpreis = 100, Steuersatz = 101 };

        var fehler = _validator.ValidierenPosition(pos, 1);

        fehler.Should().Contain(f => f.Contains("Steuersatz"));
    }

    [Fact]
    public void ValidierenPosition_NegativerSteuersatz_Fehler()
    {
        var pos = new Angebotsposition { Text = "Test", Menge = 1, Einzelpreis = 100, Steuersatz = -1 };

        var fehler = _validator.ValidierenPosition(pos, 1);

        fehler.Should().Contain(f => f.Contains("Steuersatz"));
    }

    [Fact]
    public void ValidierenPosition_NegativerRabatt_Fehler()
    {
        var pos = new Angebotsposition { Text = "Test", Menge = 1, Einzelpreis = 100, Steuersatz = 19, Rabatt = -5 };

        var fehler = _validator.ValidierenPosition(pos, 1);

        fehler.Should().Contain(f => f.Contains("Rabatt"));
    }

    [Fact]
    public void ValidierenPosition_GueltigePosition_KeineFehler()
    {
        var pos = new Angebotsposition { Text = "Test", Menge = 1.5m, Einzelpreis = 0, Steuersatz = 0 };

        var fehler = _validator.ValidierenPosition(pos, 1);

        fehler.Should().BeEmpty();
    }

    // ─── AngebotStatus edge cases ─────────────────────────────────────────────

    [Fact]
    public void Validieren_AngebotMitMehrerenUngueltigePositionen_MehrereFehler()
    {
        var angebot = MakeGueltigesAngebot();
        angebot.Positionen.Add(new Angebotsposition { Text = "", Menge = 0, Einzelpreis = -1, Steuersatz = 19 });

        var fehler = _validator.Validieren(angebot);

        fehler.Should().HaveCountGreaterThanOrEqualTo(3); // text, menge, einzelpreis
    }
}
