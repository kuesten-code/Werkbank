using FluentAssertions;
using Kuestencode.Core.Auth;
using Kuestencode.Werkbank.Acta.Data.Repositories;
using Kuestencode.Werkbank.Acta.Domain.Entities;
using Kuestencode.Werkbank.Acta.Domain.Enums;
using Kuestencode.Werkbank.Acta.Services;
using Moq;
using Xunit;

namespace Kuestencode.Werkbank.Acta.Tests.Services;

public class PinnedProjectServiceTests
{
    private readonly Mock<IPinnedProjectRepository> _pinnedRepo = new();
    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly Mock<ICurrentUserAccessor> _currentUser = new();
    private readonly Guid _userId = Guid.NewGuid();

    public PinnedProjectServiceTests()
    {
        _currentUser.Setup(c => c.Get()).Returns(new CurrentUser(_userId, "Test User"));
    }

    private PinnedProjectService CreateService() => new(_pinnedRepo.Object, _projectRepo.Object, _currentUser.Object);

    private static Project MakeProject(Guid? id = null, ProjectStatus status = ProjectStatus.Active) => new()
    {
        Id = id ?? Guid.NewGuid(),
        ProjectNumber = "P-0001",
        Name = "Testprojekt",
        CustomerId = 1,
        Status = status
    };

    [Fact]
    public async Task PinAsync_ProjektNochNichtGepinnt_FuegtEintragHinzu()
    {
        var project = MakeProject();
        _pinnedRepo.Setup(r => r.ExistsAsync(_userId, project.Id)).ReturnsAsync(false);

        await CreateService().PinAsync(project.Id);

        _pinnedRepo.Verify(r => r.AddAsync(It.Is<PinnedProject>(p => p.UserId == _userId && p.ProjectId == project.Id)), Times.Once);
    }

    [Fact]
    public async Task PinAsync_ProjektBereitsGepinnt_IstIdempotentUndFuegtNichtsHinzu()
    {
        var project = MakeProject();
        _pinnedRepo.Setup(r => r.ExistsAsync(_userId, project.Id)).ReturnsAsync(true);

        await CreateService().PinAsync(project.Id);

        _pinnedRepo.Verify(r => r.AddAsync(It.IsAny<PinnedProject>()), Times.Never);
    }

    [Fact]
    public async Task UnpinAsync_RuftRemoveMitAktuellerUserIdAuf()
    {
        var projectId = Guid.NewGuid();

        await CreateService().UnpinAsync(projectId);

        _pinnedRepo.Verify(r => r.RemoveAsync(_userId, projectId), Times.Once);
    }

    [Fact]
    public async Task GetPinnableActiveProjectsAsync_SchliesstBereitsGepinnteUndNichtAktiveAus()
    {
        var pinned = MakeProject();
        var pinnableActive = MakeProject();
        var draftProject = MakeProject(status: ProjectStatus.Draft);

        _projectRepo.Setup(r => r.GetByStatusAsync(ProjectStatus.Active))
            .ReturnsAsync([pinned, pinnableActive]);
        _pinnedRepo.Setup(r => r.GetByUserIdAsync(_userId))
            .ReturnsAsync([new PinnedProject { Id = Guid.NewGuid(), UserId = _userId, ProjectId = pinned.Id, PinnedAt = DateTime.UtcNow, Project = pinned }]);

        var result = await CreateService().GetPinnableActiveProjectsAsync();

        result.Should().ContainSingle(p => p.Id == pinnableActive.Id);
        result.Should().NotContain(p => p.Id == pinned.Id);
        result.Should().NotContain(p => p.Id == draftProject.Id);
    }

    [Fact]
    public async Task ZweiNutzer_HabenVoellingUnabhaengigeGepinnteProjekte()
    {
        var userA = _userId;
        var userB = Guid.NewGuid();
        var projectA = MakeProject();
        var projectB = MakeProject();

        _pinnedRepo.Setup(r => r.GetByUserIdAsync(userA))
            .ReturnsAsync([new PinnedProject { Id = Guid.NewGuid(), UserId = userA, ProjectId = projectA.Id, PinnedAt = DateTime.UtcNow, Project = projectA }]);
        _pinnedRepo.Setup(r => r.GetByUserIdAsync(userB))
            .ReturnsAsync([new PinnedProject { Id = Guid.NewGuid(), UserId = userB, ProjectId = projectB.Id, PinnedAt = DateTime.UtcNow, Project = projectB }]);

        var serviceForA = CreateService();
        var pinnedForA = await serviceForA.GetPinnedProjectsAsync();

        _currentUser.Setup(c => c.Get()).Returns(new CurrentUser(userB, "Anderer Nutzer"));
        var serviceForB = CreateService();
        var pinnedForB = await serviceForB.GetPinnedProjectsAsync();

        pinnedForA.Should().ContainSingle(p => p.Id == projectA.Id);
        pinnedForB.Should().ContainSingle(p => p.Id == projectB.Id);
        pinnedForA.Should().NotContain(p => p.Id == projectB.Id);
        pinnedForB.Should().NotContain(p => p.Id == projectA.Id);
    }
}
