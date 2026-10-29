using FluentAssertions;
using Kuestencode.Werkbank.Locatio.Domain;
using Kuestencode.Werkbank.Locatio.Domain.Dtos;
using Kuestencode.Werkbank.Locatio.Domain.Entities;
using Kuestencode.Werkbank.Locatio.Domain.Enums;
using Xunit;

namespace Kuestencode.Werkbank.Locatio.Tests.Domain;

public class DomainTests
{
    private static Mieteinheit AfaEinheit(DateOnly anschaffung, DateOnly? verkauf = null) => new()
    {
        Bezeichnung = "Whg 1",
        Status = verkauf.HasValue ? EinheitStatus.Veraeussert : EinheitStatus.Vermietet,
        AnschaffungsDatum = anschaffung,
        AnschaffungskostenGebaeude = 120_000m,
        AfaSatz = 2m,
        VerkaufsDatum = verkauf
    };

    [Fact]
    public void AfaBasis_TeiltNebenkostenNachVerhaeltnisGebaeudeZuGrund()
    {
        var objekt = new Gebaeude
        {
            AnschaffungskostenGebaeude = 300_000m,
            AnschaffungskostenGrundUndBoden = 100_000m,
            Anschaffungsnebenkosten = 40_000m
        };

        objekt.AfaBasis.Should().Be(330_000m);
        objekt.AnschaffungskostenGesamt.Should().Be(440_000m);
    }

    [Fact]
    public void AfaBasis_OhneGebaeudeanteil_IstNull()
    {
        new Gebaeude { AnschaffungskostenGrundUndBoden = 100_000m }.AfaBasis.Should().Be(0m);
    }

    [Fact]
    public void Afa_VollesJahr()
    {
        var (monate, betrag, datum) = AfaRechner.Berechne(AfaEinheit(new DateOnly(2020, 5, 1)), 2026, 0m);

        monate.Should().Be(12);
        betrag.Should().Be(2_400m);
        datum.Should().Be(new DateOnly(2026, 12, 31));
    }

    [Fact]
    public void Afa_AnschaffungsjahrMonatsgenau()
    {
        var (monate, betrag, _) = AfaRechner.Berechne(AfaEinheit(new DateOnly(2026, 10, 15)), 2026, 0m);

        monate.Should().Be(3);
        betrag.Should().Be(600m);
    }

    [Fact]
    public void Afa_VerkaufsjahrEndetVorDemVerkaufstag()
    {
        var (monate, betrag, datum) = AfaRechner.Berechne(AfaEinheit(new DateOnly(2020, 1, 1), new DateOnly(2026, 7, 1)), 2026, 0m);

        monate.Should().Be(6);
        betrag.Should().Be(1_200m);
        datum.Should().Be(new DateOnly(2026, 6, 30));
    }

    [Fact]
    public void Afa_NachVerkaufsjahr_Keine()
    {
        AfaRechner.Berechne(AfaEinheit(new DateOnly(2020, 1, 1), new DateOnly(2025, 3, 1)), 2026, 0m).Monate.Should().Be(0);
    }

    [Fact]
    public void Afa_WirdAufRestwertBegrenzt()
    {
        AfaRechner.Berechne(AfaEinheit(new DateOnly(1970, 1, 1)), 2026, 119_000m).Betrag.Should().Be(1_000m);
    }

    [Fact]
    public void Afa_Fremdeigentum_Keine()
    {
        var einheit = AfaEinheit(new DateOnly(2020, 1, 1));
        einheit.Status = EinheitStatus.Fremdeigentum;

        AfaRechner.Berechne(einheit, 2026, 0m).Betrag.Should().Be(0m);
    }

    [Fact]
    public void Afa_VorAnschaffung_Keine()
    {
        AfaRechner.Berechne(AfaEinheit(new DateOnly(2027, 1, 1)), 2026, 0m).Monate.Should().Be(0);
    }

    [Fact]
    public void Afa_VerkaufAmErstenJanuar_KeineAfaImVerkaufsjahr()
    {
        AfaRechner.Berechne(AfaEinheit(new DateOnly(2020, 1, 1), new DateOnly(2026, 1, 1)), 2026, 0m).Monate.Should().Be(0);
    }

    private static Gebaeude GebaeudeMitAnschaffung(params Mieteinheit[] einheiten)
    {
        var gebaeude = new Gebaeude
        {
            AnschaffungsDatum = new DateOnly(2020, 1, 1),
            AnschaffungskostenGebaeude = 300_000m,
            AnschaffungskostenGrundUndBoden = 100_000m,
            AfaSatz = 2m,
            Einheiten = einheiten.ToList()
        };
        foreach (var einheit in einheiten)
            einheit.Gebaeude = gebaeude;
        return gebaeude;
    }

    private static Mieteinheit Einheit(decimal? flaeche, EinheitStatus status = EinheitStatus.Vermietet) =>
        new() { Bezeichnung = "Whg", Status = status, Flaeche = flaeche };

    [Fact]
    public void Grundlage_VerteiltGebaeudeNachFlaeche_OhneFremdeigentumUndEinzelerwerb()
    {
        var gross = Einheit(75m);
        var klein = Einheit(25m, EinheitStatus.Veraeussert);
        var fremd = Einheit(null, EinheitStatus.Fremdeigentum);
        var einzeln = Einheit(null);
        einzeln.AnschaffungsDatum = new DateOnly(2022, 1, 1);
        einzeln.AnschaffungskostenGebaeude = 90_000m;
        einzeln.AfaSatz = 3m;
        GebaeudeMitAnschaffung(gross, klein, fremd, einzeln);

        var grundlage = AfaRechner.Grundlage(gross)!;
        grundlage.AusGebaeude.Should().BeTrue();
        grundlage.Anteil.Should().Be(0.75m);
        grundlage.AfaBasis.Should().Be(225_000m);
        grundlage.Anschaffungskosten.Should().Be(300_000m);
        grundlage.AnschaffungsDatum.Should().Be(new DateOnly(2020, 1, 1));
        grundlage.AfaSatz.Should().Be(2m);

        var eigene = AfaRechner.Grundlage(einzeln)!;
        eigene.AusGebaeude.Should().BeFalse();
        eigene.AfaBasis.Should().Be(90_000m);
        eigene.AfaSatz.Should().Be(3m);

        AfaRechner.Grundlage(fremd).Should().BeNull();
        AfaRechner.Berechne(gross, 2026, 0m).Betrag.Should().Be(4_500m);
    }

    [Fact]
    public void Grundlage_FehlendeFlaeche_KeineVerteilung()
    {
        var mitFlaeche = Einheit(60m);
        var ohneFlaeche = Einheit(null);
        var gebaeude = GebaeudeMitAnschaffung(mitFlaeche, ohneFlaeche);

        AfaRechner.Grundlage(mitFlaeche).Should().BeNull();
        AfaRechner.AnGebaeudeBeteiligt(gebaeude).Should().HaveCount(2);
        AfaRechner.Berechne(mitFlaeche, 2026, 0m).Monate.Should().Be(0);
    }

    [Fact]
    public void Grundlage_GebaeudeOhneAnschaffung_Keine()
    {
        var einheit = Einheit(60m);
        einheit.Gebaeude = new Gebaeude { Einheiten = [einheit] };

        AfaRechner.Grundlage(einheit).Should().BeNull();
        AfaRechner.Grundlage(Einheit(60m)).Should().BeNull();
    }

    [Fact]
    public void Mieteinheit_UstNurBeiGewerbeMitOption()
    {
        var wohnung = new Mieteinheit { Art = EinheitArt.Wohnung, UstOption = true, Kaltmiete = 1000m };
        var gewerbe = new Mieteinheit { Art = EinheitArt.Gewerbe, UstOption = true, Kaltmiete = 1000m, NebenkostenVorauszahlung = 100m };

        wohnung.MonatsmieteBrutto.Should().Be(1000m);
        gewerbe.MonatsmieteUst.Should().Be(209m);
        gewerbe.MonatsmieteBrutto.Should().Be(1309m);
    }

    [Fact]
    public void Mieteinheit_GesperrtAb_UebernimmtGebaeudeverkauf()
    {
        var einheit = new Mieteinheit { Gebaeude = new Gebaeude { VerkaufsDatum = new DateOnly(2026, 5, 1) } };

        einheit.GesperrtAb.Should().Be(new DateOnly(2026, 5, 1));
    }

    [Fact]
    public void Buchungssperre_Fremdeigentum_WirftException()
    {
        var act = () => Buchungssperre.Pruefe(new Mieteinheit { Bezeichnung = "Whg 5", Status = EinheitStatus.Fremdeigentum }, new DateOnly(2026, 1, 1));

        act.Should().Throw<InvalidOperationException>().WithMessage("*Fremdeigentum*");
    }

    [Fact]
    public void Gebaeude_Adresse_LaesstLeereTeileWeg()
    {
        new Gebaeude { Strasse = "Deichstraße 1", Plz = "25813", Ort = "Husum" }.Adresse.Should().Be("Deichstraße 1, 25813 Husum");
        new Gebaeude { Ort = "Husum" }.Adresse.Should().Be("Husum");
    }

    [Fact]
    public void DateiUpload_PrueftTypUndGroesse()
    {
        var ok = new DateiUpload("ordner/grundriss.pdf", "application/pdf", [1, 2, 3]).ZuDokument();
        ok.Dateiname.Should().Be("grundriss.pdf");
        ok.Groesse.Should().Be(3);

        var falscherTyp = () => new DateiUpload("x.exe", "application/octet-stream", [1]).ZuDokument();
        var zuGross = () => new DateiUpload("x.pdf", "application/pdf", new byte[DateiUpload.MaxGroesse + 1]).ZuDokument();
        falscherTyp.Should().Throw<InvalidOperationException>();
        zuGross.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Veraeusserungsgewinn_UeberschlagUndHaltedauer()
    {
        var dto = new VeraeusserungsgewinnDto
        {
            HaltedauerMonate = 100,
            Veraeusserungspreis = 400_000m,
            Veraeusserungskosten = 10_000m,
            Anschaffungskosten = 300_000m,
            KumulierteAfa = 50_000m
        };

        dto.Restbuchwert.Should().Be(250_000m);
        dto.Gewinn.Should().Be(140_000m);
        dto.UnterZehnJahren.Should().BeTrue();
    }

    [Fact]
    public void KategorieTyp_Anzeigename()
    {
        KategorieTyp.Tilgung.Anzeigename().Should().Contain("nicht abziehbar");
        EinheitStatus.Veraeussert.Anzeigename().Should().Be("veräußert");
    }
}
