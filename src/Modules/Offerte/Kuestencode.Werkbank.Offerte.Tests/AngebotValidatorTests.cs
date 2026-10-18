using Kuestencode.Werkbank.Offerte.Domain.Entities;
using Kuestencode.Werkbank.Offerte.Domain.Enums;
using Kuestencode.Werkbank.Offerte.Domain.Validation;
using FluentAssertions;
using Xunit;

namespace Kuestencode.Werkbank.Offerte.Tests;

public class AngebotValidatorTests
{
    private readonly AngebotValidator _validator = new();

    [Fact]
    public void Validieren_GueltigesAngebot_KeineFehler()
    {
        // Arrange
        var angebot = CreateValidAngebot();

        // Act
        var fehler = _validator.Validieren(angebot);

        // Assert
        fehler.Should().BeEmpty();
    }

    [Fact]
    public void Validieren_OhneKunde_Fehler()
    {
        // Arrange
        var angebot = CreateValidAngebot();
        angebot.KundeId = 0;

        // Act
        var fehler = _validator.Validieren(angebot);

        // Assert
        fehler.Should().Contain(f => f.Contains("Kunde"));
    }

    [Fact]
    public void Validieren_OhneAngebotsnummer_Fehler()
    {
        // Arrange
        var angebot = CreateValidAngebot();
        angebot.Angebotsnummer = "";

        // Act
        var fehler = _validator.Validieren(angebot);

        // Assert
        fehler.Should().Contain(f => f.Contains("Angebotsnummer"));
    }

    [Fact]
    public void Validieren_OhnePositionen_Fehler()
    {
        // Arrange
        var angebot = CreateValidAngebot();
        angebot.Positionen.Clear();

        // Act
        var fehler = _validator.Validieren(angebot);

        // Assert
        fehler.Should().Contain(f => f.Contains("Position"));
    }

    [Fact]
    public void Validieren_NeuMitGueltigBisInVergangenheit_Fehler()
    {
        // Arrange
        var angebot = CreateValidAngebot();
        angebot.GueltigBis = DateTime.UtcNow.AddDays(-1);

        // Act
        var fehler = _validator.Validieren(angebot, istNeu: true);

        // Assert
        fehler.Should().Contain(f => f.Contains("Gültigkeitsdatum"));
    }

    [Fact]
    public void Validieren_BestehendesMitGueltigBisInVergangenheit_KeinFehler()
    {
        // Arrange
        var angebot = CreateValidAngebot();
        angebot.GueltigBis = DateTime.UtcNow.AddDays(-1);

        // Act
        var fehler = _validator.Validieren(angebot, istNeu: false);

        // Assert
        fehler.Should().NotContain(f => f.Contains("Gültigkeitsdatum"));
    }

    [Fact]
    public void Validieren_UeberschriftAnErsterStelle_KeineFehler()
    {
        // Arrange
        var angebot = CreateValidAngebot();
        angebot.Positionen[0].Position = 2;
        angebot.Positionen.Insert(0, CreateUeberschrift(position: 1));

        // Act
        var fehler = _validator.Validieren(angebot);

        // Assert
        fehler.Should().BeEmpty();
    }

    [Fact]
    public void Validieren_UeberschriftZwischenPositionen_KeineFehler()
    {
        // Arrange
        var angebot = CreateValidAngebot();
        angebot.Positionen[0].Position = 1;
        angebot.Positionen.Add(CreateUeberschrift(position: 2));
        angebot.Positionen.Add(new Angebotsposition { Position = 3, Text = "Zweite", Menge = 2, Einzelpreis = 50, Steuersatz = 19 });

        // Act
        var fehler = _validator.Validieren(angebot);

        // Assert
        fehler.Should().BeEmpty();
    }

    [Fact]
    public void Validieren_NurUeberschriften_Fehler()
    {
        // Arrange
        var angebot = CreateValidAngebot();
        angebot.Positionen.Clear();
        angebot.Positionen.Add(CreateUeberschrift(position: 1));

        // Act
        var fehler = _validator.Validieren(angebot);

        // Assert
        fehler.Should().ContainSingle().Which.Should().Contain("mindestens eine Position");
    }

    [Fact]
    public void Validieren_FehlerNachUeberschrift_NummeriertOhneUeberschriften()
    {
        // Arrange
        var angebot = CreateValidAngebot();
        angebot.Positionen[0].Position = 2;
        angebot.Positionen[0].Menge = 0;
        angebot.Positionen.Insert(0, CreateUeberschrift(position: 1));

        // Act
        var fehler = _validator.Validieren(angebot);

        // Assert
        fehler.Should().ContainSingle().Which.Should().StartWith("Position 1:");
    }

    [Fact]
    public void ValidierenPosition_OhneText_Fehler()
    {
        // Arrange
        var position = new Angebotsposition
        {
            Text = "",
            Menge = 1,
            Einzelpreis = 100
        };

        // Act
        var fehler = _validator.ValidierenPosition(position, 1);

        // Assert
        fehler.Should().Contain(f => f.Contains("Text"));
    }

    [Fact]
    public void ValidierenPosition_MengeNull_Fehler()
    {
        // Arrange
        var position = new Angebotsposition
        {
            Text = "Test",
            Menge = 0,
            Einzelpreis = 100
        };

        // Act
        var fehler = _validator.ValidierenPosition(position, 1);

        // Assert
        fehler.Should().Contain(f => f.Contains("Menge"));
    }

    [Fact]
    public void ValidierenPosition_RabattUeber100_Fehler()
    {
        // Arrange
        var position = new Angebotsposition
        {
            Text = "Test",
            Menge = 1,
            Einzelpreis = 100,
            Rabatt = 150
        };

        // Act
        var fehler = _validator.ValidierenPosition(position, 1);

        // Assert
        fehler.Should().Contain(f => f.Contains("Rabatt"));
    }

    [Fact]
    public void IstGueltig_GueltigesAngebot_True()
    {
        // Arrange
        var angebot = CreateValidAngebot();

        // Act & Assert
        _validator.IstGueltig(angebot).Should().BeTrue();
    }

    [Fact]
    public void IstGueltig_UngueltigesAngebot_False()
    {
        // Arrange
        var angebot = CreateValidAngebot();
        angebot.KundeId = 0;

        // Act & Assert
        _validator.IstGueltig(angebot).Should().BeFalse();
    }

    private static Angebotsposition CreateUeberschrift(int position) => new()
    {
        Id = Guid.NewGuid(),
        Position = position,
        Text = "Abschnitt",
        IsHeader = true,
        Menge = 0,
        Einzelpreis = 0,
        Steuersatz = 0
    };

    private static Angebot CreateValidAngebot()
    {
        return new Angebot
        {
            Id = Guid.NewGuid(),
            Angebotsnummer = "ANG-2024-0001",
            KundeId = 1,
            Status = AngebotStatus.Entwurf,
            Erstelldatum = DateTime.UtcNow,
            GueltigBis = DateTime.UtcNow.AddDays(30),
            Positionen = new List<Angebotsposition>
            {
                new Angebotsposition
                {
                    Id = Guid.NewGuid(),
                    Text = "Testposition",
                    Menge = 1,
                    Einzelpreis = 100,
                    Steuersatz = 19
                }
            }
        };
    }
}
