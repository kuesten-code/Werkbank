using Kuestencode.Werkbank.Offerte.Domain.Entities;
using Kuestencode.Werkbank.Offerte.Domain.Enums;
using Kuestencode.Werkbank.Offerte.Domain.Services;
using FluentAssertions;
using Xunit;

namespace Kuestencode.Werkbank.Offerte.Tests;

public class AngebotStatusServiceTests
{
    private readonly AngebotStatusService _service = new();

    [Fact]
    public void KannVersendetWerden_NurImEntwurfStatus()
    {
        // Arrange
        var entwurf = CreateAngebot(AngebotStatus.Entwurf);
        var versendet = CreateAngebot(AngebotStatus.Versendet);

        // Act & Assert
        _service.KannVersendetWerden(entwurf).Should().BeTrue();
        _service.KannVersendetWerden(versendet).Should().BeFalse();
    }

    [Fact]
    public void KannAngenommenWerden_NurWennVersendetUndNichtAbgelaufen()
    {
        // Arrange
        var versendet = CreateAngebot(AngebotStatus.Versendet);
        versendet.GueltigBis = DateTime.UtcNow.AddDays(30);

        var versendetAbgelaufen = CreateAngebot(AngebotStatus.Versendet);
        versendetAbgelaufen.GueltigBis = DateTime.UtcNow.AddDays(-1);

        var entwurf = CreateAngebot(AngebotStatus.Entwurf);

        // Act & Assert
        _service.KannAngenommenWerden(versendet).Should().BeTrue();
        _service.KannAngenommenWerden(versendetAbgelaufen).Should().BeFalse();
        _service.KannAngenommenWerden(entwurf).Should().BeFalse();
    }

    [Fact]
    public void KannAbgelehntWerden_NurWennVersendet()
    {
        // Arrange
        var versendet = CreateAngebot(AngebotStatus.Versendet);
        var entwurf = CreateAngebot(AngebotStatus.Entwurf);

        // Act & Assert
        _service.KannAbgelehntWerden(versendet).Should().BeTrue();
        _service.KannAbgelehntWerden(entwurf).Should().BeFalse();
    }

    [Fact]
    public void Versenden_AendertStatusAufVersendet()
    {
        // Arrange
        var angebot = CreateAngebot(AngebotStatus.Entwurf);

        // Act
        _service.Versenden(angebot);

        // Assert
        angebot.Status.Should().Be(AngebotStatus.Versendet);
        angebot.VersendetAm.Should().NotBeNull();
    }

    [Fact]
    public void Versenden_WirftExceptionWennNichtImEntwurf()
    {
        // Arrange
        var angebot = CreateAngebot(AngebotStatus.Versendet);

        // Act & Assert
        var act = () => _service.Versenden(angebot);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Annehmen_AendertStatusAufAngenommen()
    {
        // Arrange
        var angebot = CreateAngebot(AngebotStatus.Versendet);
        angebot.GueltigBis = DateTime.UtcNow.AddDays(30);

        // Act
        _service.Annehmen(angebot);

        // Assert
        angebot.Status.Should().Be(AngebotStatus.Angenommen);
        angebot.AngenommenAm.Should().NotBeNull();
    }

    [Fact]
    public void Ablehnen_AendertStatusAufAbgelehnt()
    {
        // Arrange
        var angebot = CreateAngebot(AngebotStatus.Versendet);

        // Act
        _service.Ablehnen(angebot);

        // Assert
        angebot.Status.Should().Be(AngebotStatus.Abgelehnt);
        angebot.AbgelehntAm.Should().NotBeNull();
    }

    [Fact]
    public void AlsAbgelaufenMarkieren_AendertStatusAufAbgelaufen()
    {
        // Arrange
        var angebot = CreateAngebot(AngebotStatus.Versendet);
        angebot.GueltigBis = DateTime.UtcNow.AddDays(-1);

        // Act
        _service.AlsAbgelaufenMarkieren(angebot);

        // Assert
        angebot.Status.Should().Be(AngebotStatus.Abgelaufen);
        angebot.AbgelaufenAm.Should().NotBeNull();
    }

    [Fact]
    public void GetErlaubteUebergaenge_GibtKorrekteUebergaengeZurueck()
    {
        // Arrange
        var entwurf = CreateAngebot(AngebotStatus.Entwurf);
        var versendet = CreateAngebot(AngebotStatus.Versendet);
        versendet.GueltigBis = DateTime.UtcNow.AddDays(30);

        // Act
        var entwurfUebergaenge = _service.GetErlaubteUebergaenge(entwurf);
        var versendetUebergaenge = _service.GetErlaubteUebergaenge(versendet);

        // Assert
        entwurfUebergaenge.Should().Contain(AngebotStatus.Versendet);
        versendetUebergaenge.Should().Contain(AngebotStatus.Angenommen);
        versendetUebergaenge.Should().Contain(AngebotStatus.Abgelehnt);
    }

    private static Angebot CreateAngebot(AngebotStatus status)
    {
        return new Angebot
        {
            Id = Guid.NewGuid(),
            Angebotsnummer = "ANG-2024-0001",
            KundeId = 1,
            Status = status,
            Erstelldatum = DateTime.UtcNow,
            GueltigBis = DateTime.UtcNow.AddDays(30),
            Positionen = new List<Angebotsposition>()
        };
    }
}
