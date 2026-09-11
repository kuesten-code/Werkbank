using FluentAssertions;
using Kuestencode.Werkbank.Acta.Controllers;
using Kuestencode.Werkbank.Acta.Domain.Entities;
using Kuestencode.Werkbank.Acta.Domain.Enums;
using Kuestencode.Werkbank.Acta.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Kuestencode.Werkbank.Acta.Tests.Controllers;

public class PinnedProjectsControllerTests
{
    private readonly Mock<IPinnedProjectService> _pinnedProjectService = new();

    private PinnedProjectsController CreateController() => new(_pinnedProjectService.Object);

    private static Project MakeProject(string number = "P-0001") => new()
    {
        Id = Guid.NewGuid(),
        ProjectNumber = number,
        Name = "Testprojekt",
        CustomerId = 1,
        Status = ProjectStatus.Active,
        TargetDate = new DateOnly(2026, 12, 1),
        BudgetNet = 5000m
    };

    [Fact]
    public async Task GetPinned_GibtGepinnteProjekteAlsDtoZurueck()
    {
        var project = MakeProject();
        _pinnedProjectService.Setup(s => s.GetPinnedProjectsAsync()).ReturnsAsync([project]);

        var result = await CreateController().GetPinned();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dtos = ok.Value.Should().BeAssignableTo<List<PinnedProjectDto>>().Subject;
        dtos.Should().ContainSingle(d => d.Id == project.Id && d.ProjectNumber == project.ProjectNumber && d.BudgetNet == 5000m);
    }

    [Fact]
    public async Task GetPinnable_GibtPinnbareProjekteAlsDtoZurueck()
    {
        var project = MakeProject("P-0002");
        _pinnedProjectService.Setup(s => s.GetPinnableActiveProjectsAsync()).ReturnsAsync([project]);

        var result = await CreateController().GetPinnable();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dtos = ok.Value.Should().BeAssignableTo<List<PinnedProjectDto>>().Subject;
        dtos.Should().ContainSingle(d => d.Id == project.Id);
    }

    [Fact]
    public async Task Pin_RuftServiceAufUndGibtNoContentZurueck()
    {
        var projectId = Guid.NewGuid();

        var result = await CreateController().Pin(projectId);

        result.Should().BeOfType<NoContentResult>();
        _pinnedProjectService.Verify(s => s.PinAsync(projectId), Times.Once);
    }

    [Fact]
    public async Task Unpin_RuftServiceAufUndGibtNoContentZurueck()
    {
        var projectId = Guid.NewGuid();

        var result = await CreateController().Unpin(projectId);

        result.Should().BeOfType<NoContentResult>();
        _pinnedProjectService.Verify(s => s.UnpinAsync(projectId), Times.Once);
    }
}
