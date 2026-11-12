using System.Net;
using FluentAssertions;
using Kuestencode.Shared.Contracts.Navigation;
using Kuestencode.Shared.UI.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Kuestencode.Host.Tests.Services;

public class ModuleHealthMonitorTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(respond(request));
        }
    }

    private static ModuleHealthMonitor CreateMonitor(HttpMessageHandler handler)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(ModuleHealthMonitor.HostClientName))
            .Returns(new HttpClient(handler) { BaseAddress = new Uri("http://host:8080") });

        return new ModuleHealthMonitor(NullLogger<ModuleHealthMonitor>.Instance, factory.Object, "Faktura",
            () => new ModuleInfoDto { ModuleName = "Faktura" });
    }

    [Fact]
    public async Task RegisterWithHostAsync_HostAntwortetOk_PostetModulInfoAnRegisterEndpunkt()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

        var result = await CreateMonitor(handler).RegisterWithHostAsync(CancellationToken.None);

        result.Should().BeTrue();
        handler.Request!.Method.Should().Be(HttpMethod.Post);
        handler.Request.RequestUri!.ToString().Should().Be("http://host:8080/api/modules/register");
    }

    [Fact]
    public async Task RegisterWithHostAsync_HostLehntAb_LiefertFalse()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        (await CreateMonitor(handler).RegisterWithHostAsync(CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task RegisterWithHostAsync_HostNichtErreichbar_LiefertFalseStattException()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("connection refused"));

        (await CreateMonitor(handler).RegisterWithHostAsync(CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task RegisterWithHostAsync_Zeitueberschreitung_LiefertFalseStattException()
    {
        var handler = new StubHandler(_ => throw new TaskCanceledException("timeout"));

        (await CreateMonitor(handler).RegisterWithHostAsync(CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_RegistriertNachStartverzoegerung()
    {
        var registered = new TaskCompletionSource();
        var handler = new StubHandler(_ =>
        {
            registered.TrySetResult();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var monitor = CreateMonitor(handler);
        monitor.RecordHealthCheck();

        await monitor.StartAsync(CancellationToken.None);
        var completed = await Task.WhenAny(registered.Task, Task.Delay(TimeSpan.FromSeconds(10)));
        await monitor.StopAsync(CancellationToken.None);

        completed.Should().Be(registered.Task);
    }
}
