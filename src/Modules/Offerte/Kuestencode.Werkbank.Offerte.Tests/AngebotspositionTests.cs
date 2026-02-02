using Kuestencode.Werkbank.Offerte.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Kuestencode.Werkbank.Offerte.Tests;

public class AngebotspositionTests
{
    [Fact]
    public void Nettosumme_BerechnetMengeMultipliziertMitPreis()
    {
        // Arrange
        var position = new Angebotsposition
        {
            Menge = 10,
            Einzelpreis = 100,
            Steuersatz = 19
        };

        // Act & Assert
        position.Nettosumme.Should().Be(1000);
    }

    [Fact]
    public void Nettosumme_BeruecksichtigtRabatt()
    {
        // Arrange
        var position = new Angebotsposition
        {
            Menge = 10,
            Einzelpreis = 100,
            Steuersatz = 19,
            Rabatt = 10 // 10%
        };

        // Act & Assert
        position.Nettosumme.Should().Be(900); // 1000 - 10% = 900
    }

    [Fact]
    public void Steuerbetrag_BerechnetSteuerAufNettosumme()
    {
        // Arrange
        var position = new Angebotsposition
        {
            Menge = 10,
            Einzelpreis = 100,
            Steuersatz = 19
        };

        // Act & Assert
        position.Steuerbetrag.Should().Be(190); // 1000 * 19%
    }

    [Fact]
    public void Bruttosumme_BerechnetNettoUndSteuer()
    {
        // Arrange
        var position = new Angebotsposition
        {
            Menge = 10,
            Einzelpreis = 100,
            Steuersatz = 19
        };

        // Act & Assert
        position.Bruttosumme.Should().Be(1190); // 1000 + 190
    }

    [Fact]
    public void Steuerbetrag_MitRabattBeruecksichtigtReduzierteNettosumme()
    {
        // Arrange
        var position = new Angebotsposition
        {
            Menge = 10,
            Einzelpreis = 100,
            Steuersatz = 19,
            Rabatt = 10
        };

        // Act & Assert
        position.Nettosumme.Should().Be(900);
        position.Steuerbetrag.Should().Be(171); // 900 * 19%
        position.Bruttosumme.Should().Be(1071);
    }
}
