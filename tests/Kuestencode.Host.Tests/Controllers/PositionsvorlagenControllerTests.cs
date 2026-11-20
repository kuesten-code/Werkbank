using FluentAssertions;
using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Werkbank.Host.Auth;
using Kuestencode.Werkbank.Host.Controllers;
using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Services.Positionskatalog;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Kuestencode.Host.Tests.Controllers;

public class PositionsvorlagenControllerTests
{
    private readonly Mock<IPositionskatalogService> _katalog = new();
    private readonly PositionsvorlagenController _controller;

    public PositionsvorlagenControllerTests()
    {
        _controller = new PositionsvorlagenController(_katalog.Object);
    }

    [Fact]
    public async Task Suchen_MapptTrefferAufDtos()
    {
        _katalog.Setup(k => k.SucheAsync("sili", 10)).ReturnsAsync(
        [
            new Positionsvorlage { Id = 7, Text = "Silikonfuge erneuern", Einheit = "lfm", Einzelpreis = 12.5m }
        ]);

        var result = await _controller.Suchen("sili");

        var dtos = result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeAssignableTo<List<PositionsvorlageDto>>().Subject;
        dtos.Should().ContainSingle().Which.Should().BeEquivalentTo(new PositionsvorlageDto
        {
            Id = 7, Text = "Silikonfuge erneuern", Einheit = "lfm", Einzelpreis = 12.5m
        });
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(500, 50)]
    [InlineData(8, 8)]
    public async Task Suchen_BegrenztLimit(int angefragt, int erwartet)
    {
        _katalog.Setup(k => k.SucheAsync(It.IsAny<string?>(), It.IsAny<int>())).ReturnsAsync([]);

        await _controller.Suchen(null, angefragt);

        _katalog.Verify(k => k.SucheAsync(null, erwartet));
    }

    [Fact]
    public async Task Einheiten_LiefertListe()
    {
        _katalog.Setup(k => k.GetEinheitenAsync()).ReturnsAsync(["m", "Stk."]);

        var result = await _controller.Einheiten();

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(new[] { "m", "Stk." });
    }

    [Fact]
    public async Task Merken_ReichtWeiterUndAntwortetNoContent()
    {
        var positionen = new List<PositionsvorlageMerkenDto> { new() { Text = "Anfahrt", Einzelpreis = 35m } };

        var result = await _controller.Merken(positionen);

        result.Should().BeOfType<NoContentResult>();
        _katalog.Verify(k => k.MerkenAsync(positionen));
    }

    [Fact]
    public void Controller_NurFuerAdminUndBuero()
    {
        var attribut = typeof(PositionsvorlagenController)
            .GetCustomAttributes(typeof(RequireRoleAttribute), false)
            .Cast<RequireRoleAttribute>()
            .Single();

        attribut.AllowedRoles.Should().BeEquivalentTo([UserRole.Admin, UserRole.Buero]);
    }
}
