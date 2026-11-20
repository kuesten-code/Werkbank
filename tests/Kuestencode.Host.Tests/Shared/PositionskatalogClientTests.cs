using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Shared.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Kuestencode.Host.Tests.Shared;

public class PositionskatalogClientTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _antwort;
        public List<HttpRequestMessage> Anfragen { get; } = [];

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> antwort) => _antwort = antwort;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Anfragen.Add(request);
            return Task.FromResult(_antwort(request));
        }
    }

    private static (HostApiClient Client, StubHandler Handler) Client(Func<HttpRequestMessage, HttpResponseMessage> antwort)
    {
        var handler = new StubHandler(antwort);
        return (new HostApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://host") }), handler);
    }

    private static PositionsvorlageMerkenDto Position(int nr) => new() { Text = $"Position {nr}", Einzelpreis = nr };

    [Fact]
    public async Task Suche_KodiertBegriffUndLiefertTreffer()
    {
        var (client, handler) = Client(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<PositionsvorlageDto> { new() { Text = "Fliesen & Fugen" } })
        });

        var treffer = await client.SuchePositionsvorlagenAsync("Fliesen & Fugen", 8);

        treffer.Should().ContainSingle().Which.Text.Should().Be("Fliesen & Fugen");
        handler.Anfragen.Single().RequestUri!.PathAndQuery
            .Should().Be("/api/positionsvorlagen?suche=Fliesen%20%26%20Fugen&limit=8");
    }

    [Fact]
    public async Task Suche_Fehlerstatus_LeereListe()
    {
        var (client, _) = Client(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        (await client.SuchePositionsvorlagenAsync("x", 8)).Should().BeEmpty();
    }

    [Fact]
    public async Task Suche_HostNichtErreichbar_LeereListe()
    {
        var (client, _) = Client(_ => throw new HttpRequestException("weg"));

        (await client.SuchePositionsvorlagenAsync("x", 8)).Should().BeEmpty();
    }

    [Fact]
    public async Task Suche_Abgebrochen_WirftWeiter()
    {
        var (client, _) = Client(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var aufruf = () => client.SuchePositionsvorlagenAsync("x", 8, cts.Token);

        await aufruf.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Einheiten_LiefertListe_UndBeiFehlerLeer()
    {
        var (ok, _) = Client(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new[] { "m", "Stk." }) });
        var (fehler, _) = Client(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));
        var (weg, _) = Client(_ => throw new HttpRequestException("weg"));

        (await ok.GetPositionsEinheitenAsync()).Should().Equal("m", "Stk.");
        (await fehler.GetPositionsEinheitenAsync()).Should().BeEmpty();
        (await weg.GetPositionsEinheitenAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Merken_PostetUndWirftBeiFehler()
    {
        var (ok, handler) = Client(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var (fehler, _) = Client(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        await ok.MerkePositionsvorlagenAsync([Position(1)]);
        var aufruf = () => fehler.MerkePositionsvorlagenAsync([Position(1)]);

        handler.Anfragen.Single().Method.Should().Be(HttpMethod.Post);
        handler.Anfragen.Single().RequestUri!.AbsolutePath.Should().Be("/api/positionsvorlagen/merken");
        await aufruf.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task Lerner_TeiltInPakete()
    {
        var host = new Mock<IHostApiClient>();
        var lerner = new PositionskatalogLerner(host.Object, NullLogger<PositionskatalogLerner>.Instance);
        var positionen = Enumerable.Range(1, PositionskatalogLerner.PaketGroesse + 1).Select(Position).ToList();

        (await lerner.MerkenAsync(positionen)).Should().BeTrue();

        host.Verify(h => h.MerkePositionsvorlagenAsync(It.Is<List<PositionsvorlageMerkenDto>>(l => l.Count == PositionskatalogLerner.PaketGroesse)), Times.Once);
        host.Verify(h => h.MerkePositionsvorlagenAsync(It.Is<List<PositionsvorlageMerkenDto>>(l => l.Count == 1)), Times.Once);
    }

    [Fact]
    public async Task Lerner_LeereListe_SendetNichts()
    {
        var host = new Mock<IHostApiClient>();
        var lerner = new PositionskatalogLerner(host.Object, NullLogger<PositionskatalogLerner>.Instance);

        (await lerner.MerkenAsync([])).Should().BeTrue();

        host.Verify(h => h.MerkePositionsvorlagenAsync(It.IsAny<List<PositionsvorlageMerkenDto>>()), Times.Never);
    }

    [Fact]
    public async Task Lerner_HostFehler_WirftNichtUndMeldetFalse()
    {
        var host = new Mock<IHostApiClient>();
        host.Setup(h => h.MerkePositionsvorlagenAsync(It.IsAny<List<PositionsvorlageMerkenDto>>()))
            .ThrowsAsync(new HttpRequestException("Host weg"));
        var lerner = new PositionskatalogLerner(host.Object, NullLogger<PositionskatalogLerner>.Instance);

        (await lerner.MerkenAsync([Position(1)])).Should().BeFalse();
    }

    private static PositionskatalogAbgleichService Abgleich(IPositionskatalogQuelle quelle, IPositionskatalogLerner lerner)
    {
        var services = new ServiceCollection()
            .AddSingleton(quelle)
            .AddSingleton(lerner)
            .BuildServiceProvider();
        return new PositionskatalogAbgleichService(services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<PositionskatalogAbgleichService>.Instance);
    }

    [Fact]
    public async Task Abgleich_UebergibtAltbestandAnLerner()
    {
        var positionen = new List<PositionsvorlageMerkenDto> { Position(1), Position(2) };
        var quelle = new Mock<IPositionskatalogQuelle>();
        quelle.Setup(q => q.GetPositionenAsync()).ReturnsAsync(positionen);
        var lerner = new Mock<IPositionskatalogLerner>();
        lerner.Setup(l => l.MerkenAsync(positionen)).ReturnsAsync(true);

        (await Abgleich(quelle.Object, lerner.Object).AbgleichenAsync()).Should().BeTrue();

        lerner.Verify(l => l.MerkenAsync(positionen), Times.Once);
    }

    [Fact]
    public async Task Abgleich_LernerScheitert_MeldetFalse()
    {
        var quelle = new Mock<IPositionskatalogQuelle>();
        quelle.Setup(q => q.GetPositionenAsync()).ReturnsAsync([Position(1)]);
        var lerner = new Mock<IPositionskatalogLerner>();
        lerner.Setup(l => l.MerkenAsync(It.IsAny<IReadOnlyCollection<PositionsvorlageMerkenDto>>())).ReturnsAsync(false);

        (await Abgleich(quelle.Object, lerner.Object).AbgleichenAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Abgleich_QuelleWirft_MeldetFalseOhneAusnahme()
    {
        var quelle = new Mock<IPositionskatalogQuelle>();
        quelle.Setup(q => q.GetPositionenAsync()).ThrowsAsync(new InvalidOperationException("DB noch nicht migriert"));

        (await Abgleich(quelle.Object, Mock.Of<IPositionskatalogLerner>()).AbgleichenAsync()).Should().BeFalse();
    }
}
