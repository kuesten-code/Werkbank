using FluentAssertions;
using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Services.Positionskatalog;
using Xunit;

namespace Kuestencode.Host.Tests.Services.Positionskatalog;

public class PositionsSucheTests
{
    private static readonly DateTime Basis = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Positionsvorlage Vorlage(string text, int tageAlt = 0) => new()
    {
        Text = text,
        ZuletztVerwendetAm = Basis.AddDays(-tageAlt)
    };

    private static readonly List<Positionsvorlage> Katalog =
    [
        Vorlage("Silikonfuge erneuern", 5),
        Vorlage("Fliesen verlegen 30x60", 1),
        Vorlage("Fliesen verlegen 60x120", 2),
        Vorlage("Anfahrt", 3),
        Vorlage("Fußbodenheizung prüfen", 4)
    ];

    private static List<string> Suche(string? begriff, int limit = 10)
        => PositionsSuche.Suchen(Katalog, begriff, limit).Select(v => v.Text).ToList();

    [Fact]
    public void Praefix_FindetTreffer()
    {
        Suche("silik").Should().Equal("Silikonfuge erneuern");
    }

    [Fact]
    public void EinTippfehler_WirdToleriert()
    {
        Suche("silikn").Should().Equal("Silikonfuge erneuern");
    }

    [Fact]
    public void ZweiTippfehler_BeiLangenWoertern_WerdenToleriert()
    {
        Suche("fussbodnheitzung").Should().Equal("Fußbodenheizung prüfen");
    }

    [Fact]
    public void VertauschteBuchstaben_ZaehlenAlsEinFehler()
    {
        Suche("flisen").Should().HaveCount(2);
        Suche("fleisen").Should().HaveCount(2);
    }

    [Fact]
    public void KurzeWoerter_OhneToleranz()
    {
        Suche("anf").Should().Equal("Anfahrt");
        Suche("anx").Should().BeEmpty();
    }

    [Fact]
    public void Umlaute_WerdenAusgeschriebenVerglichen()
    {
        Suche("pruefen").Should().Equal("Fußbodenheizung prüfen");
        Suche("prüf").Should().Equal("Fußbodenheizung prüfen");
    }

    [Fact]
    public void MehrereSuchwoerter_MuessenAlleTreffen()
    {
        Suche("fliesen 60x120").Should().Equal("Fliesen verlegen 60x120");
        Suche("fliesen silikon").Should().BeEmpty();
    }

    [Fact]
    public void ExakterPraefix_RangiertVorTippfehler_DannZuletztVerwendet()
    {
        var katalog = new List<Positionsvorlage>
        {
            Vorlage("Kabelkanal montieren", 10),
            Vorlage("Kabel verlegen", 0),
            Vorlage("Kabwl Tippfehler-Eintrag", 0)
        };

        PositionsSuche.Suchen(katalog, "kabel", 10).Select(v => v.Text)
            .Should().Equal("Kabel verlegen", "Kabelkanal montieren", "Kabwl Tippfehler-Eintrag");
    }

    [Fact]
    public void Limit_BegrenztTreffer()
    {
        Suche("fliesen", limit: 1).Should().Equal("Fliesen verlegen 30x60");
    }

    [Fact]
    public void LeereSuche_LiefertZuletztVerwendete()
    {
        Suche("  ", limit: 2).Should().Equal("Fliesen verlegen 30x60", "Fliesen verlegen 60x120");
    }

    [Theory]
    [InlineData("abc", 0)]
    [InlineData("abcd", 1)]
    [InlineData("abcdefg", 1)]
    [InlineData("abcdefgh", 2)]
    public void ErlaubteFehler_NachWortlaenge(string wort, int erwartet)
    {
        PositionsSuche.ErlaubteFehler(wort.Length).Should().Be(erwartet);
    }

    [Theory]
    [InlineData("kitten", "sitting", 3)]
    [InlineData("abcd", "abdc", 1)]
    [InlineData("", "abc", 3)]
    [InlineData("gleich", "gleich", 0)]
    public void Distanz_DamerauLevenshtein(string a, string b, int erwartet)
    {
        PositionsSuche.Distanz(a, b).Should().Be(erwartet);
    }

    [Fact]
    public void WortWertung_KeinTreffer_Null()
    {
        PositionsSuche.WortWertung("zement", "fliesen").Should().Be(0);
    }
}
