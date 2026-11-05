using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using Kuestencode.Shared.UI.Auth;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Kuestencode.Faktura.Tests;

public class InternalRequestDetectorTests
{
    private const string ModulIp = "172.21.0.2";
    private const string AndereIp = "172.21.0.4";

    private static HttpContext Anfrage(string? remoteIp)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = remoteIp == null ? null : IPAddress.Parse(remoteIp);
        return context;
    }

    private static Func<Task<IPAddress[]>> HostUnter(string ip) =>
        () => Task.FromResult(new[] { IPAddress.Parse(ip) });

    [Theory]
    [InlineData("172.21.0.2")]
    [InlineData("10.0.0.5")]
    [InlineData("192.168.1.5")]
    [InlineData("fd00::5")]
    [InlineData("fe80::5")]
    public async Task IsInternalAsync_ModulImPrivatenNetz_IstIntern(string ip)
    {
        var result = await InternalRequestDetector.IsInternalAsync(Anfrage(ip), HostUnter("172.21.0.99"));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsInternalAsync_HostContainer_IstNichtIntern()
    {
        var result = await InternalRequestDetector.IsInternalAsync(Anfrage(ModulIp), HostUnter(ModulIp));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsInternalAsync_HostHatNeueIp_AlteHostIpGehoertJetztEinemModul()
    {
        var context = Anfrage(ModulIp);

        var vorUpdate = await InternalRequestDetector.IsInternalAsync(context, HostUnter(ModulIp));
        var nachUpdate = await InternalRequestDetector.IsInternalAsync(context, HostUnter(AndereIp));

        vorUpdate.Should().BeFalse();
        nachUpdate.Should().BeTrue();
    }

    [Fact]
    public async Task IsInternalAsync_ModulUebernimmtErstSpaeterHostIp_WirdAlsHostErkannt()
    {
        var context = Anfrage(ModulIp);

        var vorUpdate = await InternalRequestDetector.IsInternalAsync(context, HostUnter(AndereIp));
        var nachUpdate = await InternalRequestDetector.IsInternalAsync(context, HostUnter(ModulIp));

        vorUpdate.Should().BeTrue();
        nachUpdate.Should().BeFalse();
    }

    [Fact]
    public async Task IsInternalAsync_HostNichtAufloesbar_IstNichtIntern()
    {
        var result = await InternalRequestDetector.IsInternalAsync(
            Anfrage(ModulIp),
            () => Task.FromException<IPAddress[]>(new SocketException((int)SocketError.HostNotFound)));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsInternalAsync_HostHatMehrereAdressen_JedeZaehltAlsHost()
    {
        var result = await InternalRequestDetector.IsInternalAsync(
            Anfrage(ModulIp),
            () => Task.FromResult(new[] { IPAddress.Parse(AndereIp), IPAddress.Parse(ModulIp) }));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsInternalAsync_Ipv4MappedAdresse_WirdNormalisiert()
    {
        var mapped = IPAddress.Parse(ModulIp).MapToIPv6().ToString();

        var alsHost = await InternalRequestDetector.IsInternalAsync(Anfrage(mapped), HostUnter(ModulIp));
        var alsModul = await InternalRequestDetector.IsInternalAsync(Anfrage(mapped), HostUnter(AndereIp));

        alsHost.Should().BeFalse();
        alsModul.Should().BeTrue();
    }

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("127.0.0.1")]
    [InlineData(null)]
    public async Task IsInternalAsync_NichtPrivateOderFehlendeAdresse_IstNichtInternOhneDnsAbfrage(string? ip)
    {
        var aufgeloest = false;

        var result = await InternalRequestDetector.IsInternalAsync(Anfrage(ip), () =>
        {
            aufgeloest = true;
            return Task.FromResult(Array.Empty<IPAddress>());
        });

        result.Should().BeFalse();
        aufgeloest.Should().BeFalse();
    }
}
