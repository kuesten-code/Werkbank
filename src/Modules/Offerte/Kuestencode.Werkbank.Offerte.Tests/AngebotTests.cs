using Kuestencode.Werkbank.Offerte.Domain.Entities;
using Kuestencode.Werkbank.Offerte.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Kuestencode.Werkbank.Offerte.Tests;

public class AngebotTests
{
    [Fact]
    public void Nettosumme_BerechnetSummeAllerPositionen()
    {
        // Arrange
        var angebot = CreateAngebot();
        angebot.Positionen.Add(new Angebotsposition
        {
            Menge = 10,
            Einzelpreis = 100,
            Steuersatz = 19
        });
        angebot.Positionen.Add(new Angebotsposition
        {
            Menge = 5,
            Einzelpreis = 50,
            Steuersatz = 19
        });

        // Act & Assert
        angebot.Nettosumme.Should().Be(1250); // 10*100 + 5*50
    }

    [Fact]
    public void Bruttosumme_BerechnetNettoUndSteuer()
    {
        // Arrange
        var angebot = CreateAngebot();
        angebot.Positionen.Add(new Angebotsposition
        {
            Menge = 10,
            Einzelpreis = 100,
            Steuersatz = 19
        });

        // Act & Assert
        angebot.Nettosumme.Should().Be(1000);
        angebot.Steuersumme.Should().Be(190);
        angebot.Bruttosumme.Should().Be(1190);
    }

    [Fact]
    public void IstTerminal_GibtTrueZurueckBeiEndstatus()
    {
        // Arrange
        var angenommen = CreateAngebot();
        angenommen.Status = AngebotStatus.Angenommen;

        var abgelehnt = CreateAngebot();
        abgelehnt.Status = AngebotStatus.Abgelehnt;

        var abgelaufen = CreateAngebot();
        abgelaufen.Status = AngebotStatus.Abgelaufen;

        var entwurf = CreateAngebot();
        entwurf.Status = AngebotStatus.Entwurf;

        // Act & Assert
        angenommen.IstTerminal.Should().BeTrue();
        abgelehnt.IstTerminal.Should().BeTrue();
        abgelaufen.IstTerminal.Should().BeTrue();
        entwurf.IstTerminal.Should().BeFalse();
    }

    [Fact]
    public void IstBearbeitbar_NurImEntwurfStatus()
    {
        // Arrange
        var entwurf = CreateAngebot();
        entwurf.Status = AngebotStatus.Entwurf;

        var versendet = CreateAngebot();
        versendet.Status = AngebotStatus.Versendet;

        // Act & Assert
        entwurf.IstBearbeitbar.Should().BeTrue();
        versendet.IstBearbeitbar.Should().BeFalse();
    }

    private static Angebot CreateAngebot()
    {
        return new Angebot
        {
            Id = Guid.NewGuid(),
            Angebotsnummer = "ANG-2024-0001",
            KundeId = 1,
            Status = AngebotStatus.Entwurf,
            Erstelldatum = DateTime.UtcNow,
            GueltigBis = DateTime.UtcNow.AddDays(30),
            Positionen = new List<Angebotsposition>()
        };
    }
}
