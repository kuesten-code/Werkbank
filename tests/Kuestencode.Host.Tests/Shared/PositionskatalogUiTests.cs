using FluentAssertions;
using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Shared.UI.Components.Positionskatalog;
using Xunit;

namespace Kuestencode.Host.Tests.Shared;

public class PositionskatalogUiTests
{
    private static readonly List<PositionsvorlageDto> DreiTreffer =
    [
        new() { Text = "A" }, new() { Text = "B" }, new() { Text = "C" }
    ];

    [Fact]
    public void Auswahl_Anfangs_ZuOhneMarkierung()
    {
        var auswahl = new VorschlagsAuswahl();

        auswahl.Offen.Should().BeFalse();
        auswahl.ListeAktiv.Should().BeFalse();
        auswahl.MarkierterTreffer.Should().BeNull();
    }

    [Fact]
    public void Auswahl_Runter_MarkiertDerReiheNachUndSpringtZumAnfang()
    {
        var auswahl = new VorschlagsAuswahl();
        auswahl.Anzeigen(DreiTreffer);

        auswahl.HatMarkierung.Should().BeFalse();
        auswahl.Runter();
        auswahl.MarkierterTreffer!.Text.Should().Be("A");
        auswahl.Runter();
        auswahl.Runter();
        auswahl.MarkierterTreffer!.Text.Should().Be("C");
        auswahl.Runter();
        auswahl.MarkierterTreffer!.Text.Should().Be("A");
    }

    [Fact]
    public void Auswahl_Hoch_OhneMarkierungSpringtZumEnde()
    {
        var auswahl = new VorschlagsAuswahl();
        auswahl.Anzeigen(DreiTreffer);

        auswahl.Hoch();
        auswahl.MarkierterTreffer!.Text.Should().Be("C");
        auswahl.Hoch();
        auswahl.MarkierterTreffer!.Text.Should().Be("B");
    }

    [Fact]
    public void Auswahl_NeueTreffer_SetzenMarkierungZurueck()
    {
        var auswahl = new VorschlagsAuswahl();
        auswahl.Anzeigen(DreiTreffer);
        auswahl.Runter();

        auswahl.Anzeigen(DreiTreffer);

        auswahl.HatMarkierung.Should().BeFalse();
    }

    [Fact]
    public void Auswahl_OhneTreffer_OffenAberPfeiltastenBleibenBeimTextfeld()
    {
        var auswahl = new VorschlagsAuswahl();
        auswahl.Anzeigen([]);

        auswahl.Runter();
        auswahl.Hoch();

        auswahl.Offen.Should().BeTrue();
        auswahl.ListeAktiv.Should().BeFalse();
        auswahl.HatMarkierung.Should().BeFalse();
    }

    [Fact]
    public void Auswahl_Schliessen_LeertAlles()
    {
        var auswahl = new VorschlagsAuswahl();
        auswahl.Anzeigen(DreiTreffer);
        auswahl.Runter();

        auswahl.Schliessen();

        auswahl.Offen.Should().BeFalse();
        auswahl.Treffer.Should().BeEmpty();
        auswahl.MarkierterTreffer.Should().BeNull();
    }

    [Fact]
    public void Einheiten_Zusammenfuehren_GrundlisteZuerstOhneDubletten()
    {
        var einheiten = PositionsEinheitFeld.Zusammenfuehren(["stk.", " Sack ", "", "m²"]);

        einheiten.Should().StartWith(PositionsEinheitFeld.Grundliste);
        einheiten.Should().ContainSingle(e => e == "Sack");
        einheiten.Should().HaveCount(PositionsEinheitFeld.Grundliste.Count + 1);
    }

    [Fact]
    public void Einheiten_Filtern_OhneTextAlles_SonstEnthaeltOhneGrossKlein()
    {
        var einheiten = new[] { "Stk.", "m", "m²", "Pauschal" };

        PositionsEinheitFeld.Filtern(einheiten, " ").Should().Equal(einheiten);
        PositionsEinheitFeld.Filtern(einheiten, "PAU").Should().Equal("Pauschal");
        PositionsEinheitFeld.Filtern(einheiten, "m").Should().Equal("m", "m²");
    }
}
