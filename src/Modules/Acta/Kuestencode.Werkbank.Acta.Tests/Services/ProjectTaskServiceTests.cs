using FluentAssertions;
using Kuestencode.Werkbank.Acta.Data.Repositories;
using Kuestencode.Werkbank.Acta.Domain.Dtos;
using Kuestencode.Werkbank.Acta.Domain.Entities;
using Kuestencode.Werkbank.Acta.Domain.Enums;
using Kuestencode.Werkbank.Acta.Domain.Services;
using Kuestencode.Werkbank.Acta.Services;
using Moq;
using Xunit;

namespace Kuestencode.Werkbank.Acta.Tests.Services;

public class ProjectTaskServiceTests
{
    private readonly Mock<IProjectTaskRepository> _taskRepo = new();
    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly ProjectStatusService _statusService = new();

    private ProjectTaskService CreateService() =>
        new(_taskRepo.Object, _projectRepo.Object, _statusService);

    private static Project MakeProject(ProjectStatus status = ProjectStatus.Active) =>
        new()
        {
            Id = Guid.NewGuid(),
            ProjectNumber = "P-2026-0001",
            Name = "Testprojekt",
            CustomerId = 1,
            Status = status,
            Tasks = new List<ProjectTask>()
        };

    private static ProjectTask MakeTask(
        ProjectTaskStatus status = ProjectTaskStatus.Open,
        Guid? projectId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = "Test-Aufgabe",
            ProjectId = projectId ?? Guid.NewGuid(),
            Status = status,
            SortOrder = 0
        };

    // ─── GetByProjectIdAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetByProjectId_DelegiertAnRepository()
    {
        var projectId = Guid.NewGuid();
        var tasks = new List<ProjectTask> { MakeTask(), MakeTask() };
        _taskRepo.Setup(r => r.GetByProjectIdAsync(projectId)).ReturnsAsync(tasks);

        var result = await CreateService().GetByProjectIdAsync(projectId);

        result.Should().HaveCount(2);
    }

    // ─── GetByAssignedUserIdAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetByAssignedUserId_DelegiertAnRepository()
    {
        var userId = Guid.NewGuid();
        _taskRepo.Setup(r => r.GetByAssignedUserIdAsync(userId))
            .ReturnsAsync(new List<ProjectTask> { MakeTask() });

        var result = await CreateService().GetByAssignedUserIdAsync(userId);

        result.Should().HaveCount(1);
    }

    // ─── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_VorhandeneId_GibtAufgabeZurueck()
    {
        var id = Guid.NewGuid();
        var task = MakeTask();
        task.Id = id;
        _taskRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(task);

        var result = await CreateService().GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetById_UnbekannteId_GibtNullZurueck()
    {
        _taskRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ProjectTask?)null);

        var result = await CreateService().GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ─── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_GueltigesDaten_ErstelltAufgabe()
    {
        var project = MakeProject(ProjectStatus.Active);
        _projectRepo.Setup(r => r.GetByIdAsync(project.Id)).ReturnsAsync(project);
        _taskRepo.Setup(r => r.GetNextSortOrderAsync(project.Id)).ReturnsAsync(3);
        _taskRepo.Setup(r => r.AddAsync(It.IsAny<ProjectTask>())).Returns(Task.CompletedTask);

        var dto = new CreateProjectTaskDto
        {
            Title = "Neue Aufgabe",
            Notes = "Notizen",
            TargetDate = new DateOnly(2026, 12, 31)
        };

        var result = await CreateService().CreateAsync(project.Id, dto);

        result.Title.Should().Be("Neue Aufgabe");
        result.Status.Should().Be(ProjectTaskStatus.Open);
        result.SortOrder.Should().Be(3);
        result.ProjectId.Should().Be(project.Id);
        _taskRepo.Verify(r => r.AddAsync(It.IsAny<ProjectTask>()), Times.Once);
    }

    [Fact]
    public async Task Create_ProjektNichtGefunden_WirftException()
    {
        _projectRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Project?)null);

        var act = () => CreateService().CreateAsync(Guid.NewGuid(),
            new CreateProjectTaskDto { Title = "Test" });
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*nicht gefunden*");
    }

    [Fact]
    public async Task Create_ArchivedProjekt_WirftException()
    {
        var project = MakeProject(ProjectStatus.Archived);
        _projectRepo.Setup(r => r.GetByIdAsync(project.Id)).ReturnsAsync(project);

        var act = () => CreateService().CreateAsync(project.Id,
            new CreateProjectTaskDto { Title = "Test" });
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Projektstatus*");
    }

    [Fact]
    public async Task Create_LeererTitel_WirftException()
    {
        var project = MakeProject(ProjectStatus.Active);
        _projectRepo.Setup(r => r.GetByIdAsync(project.Id)).ReturnsAsync(project);

        var act = () => CreateService().CreateAsync(project.Id,
            new CreateProjectTaskDto { Title = "" });
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Titel*");
    }

    [Fact]
    public async Task Create_MitAssignedUser_SpeichertUserId()
    {
        var userId = Guid.NewGuid();
        var project = MakeProject(ProjectStatus.Draft);
        _projectRepo.Setup(r => r.GetByIdAsync(project.Id)).ReturnsAsync(project);
        _taskRepo.Setup(r => r.GetNextSortOrderAsync(project.Id)).ReturnsAsync(0);
        _taskRepo.Setup(r => r.AddAsync(It.IsAny<ProjectTask>())).Returns(Task.CompletedTask);

        var result = await CreateService().CreateAsync(project.Id,
            new CreateProjectTaskDto { Title = "Test", AssignedUserId = userId });

        result.AssignedUserId.Should().Be(userId);
    }

    // ─── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_GueltigesDaten_AktualisiertAufgabe()
    {
        var project = MakeProject(ProjectStatus.Active);
        var task = MakeTask(projectId: project.Id);
        _taskRepo.Setup(r => r.GetByIdAsync(task.Id)).ReturnsAsync(task);
        _projectRepo.Setup(r => r.GetByIdAsync(project.Id)).ReturnsAsync(project);
        _taskRepo.Setup(r => r.UpdateAsync(It.IsAny<ProjectTask>())).Returns(Task.CompletedTask);

        var dto = new UpdateProjectTaskDto
        {
            Title = "Geänderter Titel",
            Notes = "Neue Notizen",
            TargetDate = new DateOnly(2026, 6, 1)
        };

        var result = await CreateService().UpdateAsync(task.Id, dto);

        result.Title.Should().Be("Geänderter Titel");
        result.Notes.Should().Be("Neue Notizen");
        _taskRepo.Verify(r => r.UpdateAsync(task), Times.Once);
    }

    [Fact]
    public async Task Update_AufgabeNichtGefunden_WirftException()
    {
        _taskRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ProjectTask?)null);

        var act = () => CreateService().UpdateAsync(Guid.NewGuid(),
            new UpdateProjectTaskDto { Title = "Test" });
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*nicht gefunden*");
    }

    [Fact]
    public async Task Update_ProjektNichtBearbeitbar_WirftException()
    {
        var project = MakeProject(ProjectStatus.Archived);
        var task = MakeTask(projectId: project.Id);
        _taskRepo.Setup(r => r.GetByIdAsync(task.Id)).ReturnsAsync(task);
        _projectRepo.Setup(r => r.GetByIdAsync(project.Id)).ReturnsAsync(project);

        var act = () => CreateService().UpdateAsync(task.Id,
            new UpdateProjectTaskDto { Title = "Test" });
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Projektstatus*");
    }

    [Fact]
    public async Task Update_LeererTitel_WirftException()
    {
        var project = MakeProject(ProjectStatus.Active);
        var task = MakeTask(projectId: project.Id);
        _taskRepo.Setup(r => r.GetByIdAsync(task.Id)).ReturnsAsync(task);
        _projectRepo.Setup(r => r.GetByIdAsync(project.Id)).ReturnsAsync(project);

        var act = () => CreateService().UpdateAsync(task.Id,
            new UpdateProjectTaskDto { Title = "" });
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Titel*");
    }

    // ─── SetCompletedAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task SetCompleted_OffeneAufgabe_WirdErledigt()
    {
        var task = MakeTask(ProjectTaskStatus.Open);
        _taskRepo.Setup(r => r.GetByIdAsync(task.Id)).ReturnsAsync(task);
        _taskRepo.Setup(r => r.UpdateAsync(It.IsAny<ProjectTask>())).Returns(Task.CompletedTask);

        var result = await CreateService().SetCompletedAsync(task.Id);

        result.Status.Should().Be(ProjectTaskStatus.Completed);
        result.CompletedAt.Should().NotBeNull();
        _taskRepo.Verify(r => r.UpdateAsync(task), Times.Once);
    }

    [Fact]
    public async Task SetCompleted_BereitsErledigte_RuftUpdateNichtAuf()
    {
        var task = MakeTask(ProjectTaskStatus.Completed);
        task.CompletedAt = DateTime.UtcNow.AddDays(-1);
        _taskRepo.Setup(r => r.GetByIdAsync(task.Id)).ReturnsAsync(task);

        var result = await CreateService().SetCompletedAsync(task.Id);

        result.Status.Should().Be(ProjectTaskStatus.Completed);
        _taskRepo.Verify(r => r.UpdateAsync(It.IsAny<ProjectTask>()), Times.Never);
    }

    [Fact]
    public async Task SetCompleted_AufgabeNichtGefunden_WirftException()
    {
        _taskRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ProjectTask?)null);

        var act = () => CreateService().SetCompletedAsync(Guid.NewGuid());
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ─── SetOpenAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task SetOpen_ErledigteAufgabe_WirdWiederGeoeffnet()
    {
        var task = MakeTask(ProjectTaskStatus.Completed);
        task.CompletedAt = DateTime.UtcNow;
        _taskRepo.Setup(r => r.GetByIdAsync(task.Id)).ReturnsAsync(task);
        _taskRepo.Setup(r => r.UpdateAsync(It.IsAny<ProjectTask>())).Returns(Task.CompletedTask);

        var result = await CreateService().SetOpenAsync(task.Id);

        result.Status.Should().Be(ProjectTaskStatus.Open);
        result.CompletedAt.Should().BeNull();
        _taskRepo.Verify(r => r.UpdateAsync(task), Times.Once);
    }

    [Fact]
    public async Task SetOpen_BereitsOffene_RuftUpdateNichtAuf()
    {
        var task = MakeTask(ProjectTaskStatus.Open);
        _taskRepo.Setup(r => r.GetByIdAsync(task.Id)).ReturnsAsync(task);

        await CreateService().SetOpenAsync(task.Id);

        _taskRepo.Verify(r => r.UpdateAsync(It.IsAny<ProjectTask>()), Times.Never);
    }

    [Fact]
    public async Task SetOpen_AufgabeNichtGefunden_WirftException()
    {
        _taskRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ProjectTask?)null);

        var act = () => CreateService().SetOpenAsync(Guid.NewGuid());
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ─── ReorderAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Reorder_WeistSortOrderKorrektZu()
    {
        var project = MakeProject(ProjectStatus.Active);
        var t1 = MakeTask(projectId: project.Id);
        var t2 = MakeTask(projectId: project.Id);
        var t3 = MakeTask(projectId: project.Id);
        _projectRepo.Setup(r => r.GetByIdAsync(project.Id)).ReturnsAsync(project);
        _taskRepo.Setup(r => r.GetByProjectIdAsync(project.Id))
            .ReturnsAsync(new List<ProjectTask> { t1, t2, t3 });
        _taskRepo.Setup(r => r.UpdateRangeAsync(It.IsAny<List<ProjectTask>>())).Returns(Task.CompletedTask);

        // Neue Reihenfolge: t3, t1, t2
        await CreateService().ReorderAsync(project.Id, new List<Guid> { t3.Id, t1.Id, t2.Id });

        t3.SortOrder.Should().Be(0);
        t1.SortOrder.Should().Be(1);
        t2.SortOrder.Should().Be(2);
        _taskRepo.Verify(r => r.UpdateRangeAsync(It.Is<List<ProjectTask>>(l => l.Count == 3)), Times.Once);
    }

    [Fact]
    public async Task Reorder_ProjektNichtGefunden_WirftException()
    {
        _projectRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Project?)null);

        var act = () => CreateService().ReorderAsync(Guid.NewGuid(), new List<Guid>());
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Reorder_ArchivedProjekt_WirftException()
    {
        var project = MakeProject(ProjectStatus.Archived);
        _projectRepo.Setup(r => r.GetByIdAsync(project.Id)).ReturnsAsync(project);

        var act = () => CreateService().ReorderAsync(project.Id, new List<Guid>());
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*umsortiert*");
    }

    [Fact]
    public async Task Reorder_LeereIdsListe_RuftUpdateRangeNichtAuf()
    {
        var project = MakeProject(ProjectStatus.Active);
        _projectRepo.Setup(r => r.GetByIdAsync(project.Id)).ReturnsAsync(project);
        _taskRepo.Setup(r => r.GetByProjectIdAsync(project.Id)).ReturnsAsync(new List<ProjectTask>());

        await CreateService().ReorderAsync(project.Id, new List<Guid>());

        _taskRepo.Verify(r => r.UpdateRangeAsync(It.IsAny<List<ProjectTask>>()), Times.Never);
    }

    // ─── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_AufgabeInActivemProjekt_LoeschtAufgabe()
    {
        var project = MakeProject(ProjectStatus.Active);
        var task = MakeTask(projectId: project.Id);
        _taskRepo.Setup(r => r.GetByIdAsync(task.Id)).ReturnsAsync(task);
        _projectRepo.Setup(r => r.GetByIdAsync(project.Id)).ReturnsAsync(project);
        _taskRepo.Setup(r => r.DeleteAsync(task.Id)).Returns(Task.CompletedTask);

        await CreateService().DeleteAsync(task.Id);

        _taskRepo.Verify(r => r.DeleteAsync(task.Id), Times.Once);
    }

    [Fact]
    public async Task Delete_AufgabeNichtGefunden_WirftException()
    {
        _taskRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ProjectTask?)null);

        var act = () => CreateService().DeleteAsync(Guid.NewGuid());
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Delete_ArchivedProjekt_WirftException()
    {
        var project = MakeProject(ProjectStatus.Archived);
        var task = MakeTask(projectId: project.Id);
        _taskRepo.Setup(r => r.GetByIdAsync(task.Id)).ReturnsAsync(task);
        _projectRepo.Setup(r => r.GetByIdAsync(project.Id)).ReturnsAsync(project);

        var act = () => CreateService().DeleteAsync(task.Id);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*gelöscht*");
    }
}
