using FluentAssertions;
using Kuestencode.Werkbank.Host.Services.Positionskatalog;
using Xunit;

namespace Kuestencode.Host.Tests.Services.Positionskatalog;

public class PositionsNormalisiererTests
{
    [Fact]
    public void Schluessel_IgnoriertGrossKleinUndLeerraum()
    {
        PositionsNormalisierer.Schluessel("  Fliesen   verlegen\n30x60 ", " M² ")
            .Should().Be(PositionsNormalisierer.Schluessel("fliesen verlegen 30x60", "m²"));
    }

    [Fact]
    public void Schluessel_UnterscheidetEinheit()
    {
        PositionsNormalisierer.Schluessel("Fliesen verlegen", "m²")
            .Should().NotBe(PositionsNormalisierer.Schluessel("Fliesen verlegen", "Pauschal"));
    }

    [Fact]
    public void Schluessel_OhneEinheit_IstGleichLeererEinheit()
    {
        PositionsNormalisierer.Schluessel("Anfahrt", null)
            .Should().Be(PositionsNormalisierer.Schluessel("Anfahrt", "  "));
    }

    [Theory]
    [InlineData("h")]
    [InlineData("H")]
    [InlineData("Std")]
    [InlineData("Std.")]
    [InlineData(" Stunde ")]
    [InlineData("Stunden")]
    public void IstStundenEinheit_ErkenntStunden(string einheit)
    {
        PositionsNormalisierer.IstStundenEinheit(einheit).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Stk.")]
    [InlineData("m²")]
    [InlineData("Pauschal")]
    public void IstStundenEinheit_AndereEinheiten_False(string? einheit)
    {
        PositionsNormalisierer.IstStundenEinheit(einheit).Should().BeFalse();
    }

    [Fact]
    public void WoerterFuerSuche_SchreibtUmlauteAusUndTrenntAnSatzzeichen()
    {
        PositionsNormalisierer.WoerterFuerSuche("Fußbodenheizung, prüfen (Wärme)")
            .Should().Equal("fussbodenheizung", "pruefen", "waerme");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("--")]
    public void WoerterFuerSuche_OhneInhalt_Leer(string? text)
    {
        PositionsNormalisierer.WoerterFuerSuche(text).Should().BeEmpty();
    }
}
