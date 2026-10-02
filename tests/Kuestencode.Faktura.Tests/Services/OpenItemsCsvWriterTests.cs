using System.Text;
using FluentAssertions;
using Kuestencode.Faktura.Models;
using Kuestencode.Faktura.Services;
using Xunit;

namespace Kuestencode.Faktura.Tests.Services;

public class OpenItemsCsvWriterTests
{
    private static string[] WriteLines(params OpenItem[] items)
    {
        var bytes = OpenItemsCsvWriter.Write(items);
        return Encoding.UTF8.GetString(bytes.AsSpan(3))
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
    }

    private static OpenItem MakeItem(string customerName = "Hafen GmbH", DateTime? dueDate = null) =>
        new("R-2026-0001", new DateTime(2026, 3, 5), customerName, dueDate, 1190.5m, 500m, 690.5m);

    [Fact]
    public void Write_BeginntMitUtf8Bom()
    {
        var bytes = OpenItemsCsvWriter.Write([]);

        bytes.Take(3).Should().Equal(0xEF, 0xBB, 0xBF);
    }

    [Fact]
    public void Write_SchreibtKopfzeile()
    {
        var lines = WriteLines();

        lines.Should().ContainSingle()
            .Which.Should().Be("Rechnungsdatum;Kundenname;Fälligkeitsdatum;Brutto;Gezahlt;Offener Betrag");
    }

    [Fact]
    public void Write_FormatiertDatumUndBetraegeDeutsch()
    {
        var lines = WriteLines(MakeItem(dueDate: new DateTime(2026, 4, 4)));

        lines[1].Should().Be("05.03.2026;Hafen GmbH;04.04.2026;1190,50;500,00;690,50");
    }

    [Fact]
    public void Write_OhneFaelligkeitsdatum_LaesstSpalteLeer()
    {
        var lines = WriteLines(MakeItem());

        lines[1].Split(';')[2].Should().BeEmpty();
    }

    [Fact]
    public void Write_KundennameMitSemikolonUndAnfuehrungszeichen_WirdMaskiert()
    {
        var lines = WriteLines(MakeItem(customerName: "Meyer; \"Werft\" KG"));

        lines[1].Should().StartWith("05.03.2026;\"Meyer; \"\"Werft\"\" KG\";");
    }

    [Fact]
    public void FileName_EnthaeltZeitraum()
    {
        OpenItemsCsvWriter.FileName(new DateTime(2026, 1, 1), new DateTime(2026, 3, 31))
            .Should().Be("OP-Liste_2026-01-01_2026-03-31.csv");
    }
}
