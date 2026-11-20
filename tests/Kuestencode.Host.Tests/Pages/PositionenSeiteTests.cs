using FluentAssertions;
using Kuestencode.Werkbank.Host.Pages.Settings;
using Kuestencode.Werkbank.Host.Services.Positionskatalog;
using Xunit;

namespace Kuestencode.Host.Tests.Pages;

public class PositionenSeiteTests
{
    [Theory]
    [InlineData(PositionsvorlageSpeichern.Stundeneinheit, "Stundenpositionen")]
    [InlineData(PositionsvorlageSpeichern.Dublette, "gibt es bereits")]
    [InlineData(PositionsvorlageSpeichern.NichtGefunden, "gelöscht")]
    [InlineData(PositionsvorlageSpeichern.Ungueltig, "Pflicht")]
    public void Meldung_ErklaertJedesErgebnis(PositionsvorlageSpeichern ergebnis, string enthaelt)
    {
        Positionen.Meldung(ergebnis).Should().Contain(enthaelt);
    }
}
