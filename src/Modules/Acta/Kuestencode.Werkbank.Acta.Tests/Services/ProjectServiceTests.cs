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

public class ProjectServiceTests
{
    private readonly Mock<IProjectRepository> _repo = new();
    private readonly ProjectStatusService _statusService = new();

    private ProjectService CreateService() => new(_repo.Object, _statusService);

    private static Project MakeProject(
        ProjectStatus status = ProjectStatus.Draft,
        string number = "P-2026-0001",
        int openTasks = 0) =>
        new()
        {
            Id = Guid.NewGuid(),
            ProjectNumber = number,
            Name = "Testprojekt",
            CustomerId = 1,
            Status = status,
            Tasks = Enumerable.Range(0, openTasks)
                .Select(_ => new ProjectTask { Status = ProjectTaskStatus.Open })
                .ToList()
        };

    // ─── GetAllAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_OhneFilter_GibtAlleZurueck()
    {
        _repo.Setup(r => r.GetAllAsync(null, null))
            .ReturnsAsync(new List<Project> { MakeProject(), MakeProject() });

        var service = CreateService();
        var result = await service.GetAllAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_MitStatusFilter_DelegiertFilter()
    {
        _repo.Setup(r => r.GetAllAsync(ProjectStatus.Active, null))
            .ReturnsAsync(new List<Project> { MakeProject(ProjectStatus.Active) });

        var service = CreateService();
        var result = await service.GetAllAsync(new ProjectFilterDto { Status = ProjectStatus.Active });

        result.Should().HaveCount(1);
        _repo.Verify(r => r.GetAllAsync(ProjectStatus.Active, null), Times.Once);
    }

    [Fact]
    public async Task GetAll_MitKundenFilter_DelegiertFilter()
    {
        _repo.Setup(r => r.GetAllAsync(null, 42))
            .ReturnsAsync(new List<Project>());

        var service = CreateService();
        await service.GetAllAsync(new ProjectFilterDto { CustomerId = 42 });

        _repo.Verify(r => r.GetAllAsync(null, 42), Times.Once);
    }

    // ─── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_VorhandenesId_GibtProjektZurueck()
    {
        var id = Guid.NewGuid();
        var project = MakeProject();
        project.Id = id;
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(project);

        var result = await CreateService().GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetById_UnbekanntesId_GibtNullZurueck()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Project?)null);

        var result = await CreateService().GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ─── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_GueltigesDaten_ErstelltProjekt()
    {
        _repo.Setup(r => r.ExistsNumberAsync("P-2026-0001")).ReturnsAsync(false);
        _repo.Setup(r => r.GetNextExternalIdAsync()).ReturnsAsync(1);
        _repo.Setup(r => r.AddAsync(It.IsAny<Project>())).Returns(Task.CompletedTask);

        var dto = new CreateProjectDto
        {
            ProjectNumber = "P-2026-0001",
            Name = "Neues Projekt",
            CustomerId = 5,
            BudgetNet = 10000m
        };

        var result = await CreateService().CreateAsync(dto);

        result.ProjectNumber.Should().Be("P-2026-0001");
        result.Name.Should().Be("Neues Projekt");
        result.Status.Should().Be(ProjectStatus.Draft);
        result.ExternalId.Should().Be(1);
        _repo.Verify(r => r.AddAsync(It.Is<Project>(p =>
            p.ProjectNumber == "P-2026-0001" && p.CustomerId == 5)), Times.Once);
    }

    [Fact]
    public async Task Create_LeereProjektnummer_WirftException()
    {
        var dto = new CreateProjectDto { ProjectNumber = "", Name = "Test", CustomerId = 1 };
        var act = () => CreateService().CreateAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Projektnummer*");
    }

    [Fact]
    public async Task Create_DoppelteProjektnummer_WirftException()
    {
        _repo.Setup(r => r.ExistsNumberAsync("P-DOPPELT")).ReturnsAsync(true);
        var dto = new CreateProjectDto { ProjectNumber = "P-DOPPELT", Name = "Test", CustomerId = 1 };
        var act = () => CreateService().CreateAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*P-DOPPELT*");
    }

    [Fact]
    public async Task Create_LeererName_WirftException()
    {
        _repo.Setup(r => r.ExistsNumberAsync(It.IsAny<string>())).ReturnsAsync(false);
        var dto = new CreateProjectDto { ProjectNumber = "P-001", Name = "", CustomerId = 1 };
        var act = () => CreateService().CreateAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Projektname*");
    }

    [Fact]
    public async Task Create_KeinKunde_WirftException()
    {
        _repo.Setup(r => r.ExistsNumberAsync(It.IsAny<string>())).ReturnsAsync(false);
        var dto = new CreateProjectDto { ProjectNumber = "P-001", Name = "Test", CustomerId = 0 };
        var act = () => CreateService().CreateAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Kunde*");
    }

    // ─── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_DraftProjekt_AktualisiertEigenschaften()
    {
        var id = Guid.NewGuid();
        var project = MakeProject(ProjectStatus.Draft);
        project.Id = id;
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(project);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Project>())).Returns(Task.CompletedTask);

        var dto = new UpdateProjectDto
        {
            Name = "Geänderter Name",
            CustomerId = 10,
            Address = "Musterstr. 1",
            City = "Hamburg",
            BudgetNet = 5000m
        };

        var result = await CreateService().UpdateAsync(id, dto);

        result.Name.Should().Be("Geänderter Name");
        result.CustomerId.Should().Be(10);
        result.Address.Should().Be("Musterstr. 1");
        result.City.Should().Be("Hamburg");
        _repo.Verify(r => r.UpdateAsync(project), Times.Once);
    }

    [Fact]
    public async Task Update_NichtVorhandenesProjekt_WirftException()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Project?)null);
        var act = () => CreateService().UpdateAsync(Guid.NewGuid(), new UpdateProjectDto { Name = "X", CustomerId = 1 });
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Update_ArchivedProjekt_WirftException()
    {
        var id = Guid.NewGuid();
        var project = MakeProject(ProjectStatus.Archived);
        project.Id = id;
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(project);

        var act = () => CreateService().UpdateAsync(id, new UpdateProjectDto { Name = "X", CustomerId = 1 });
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*bearbeitet*");
    }

    [Fact]
    public async Task Update_LeererName_WirftException()
    {
        var id = Guid.NewGuid();
        var project = MakeProject(ProjectStatus.Draft);
        project.Id = id;
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(project);

        var act = () => CreateService().UpdateAsync(id, new UpdateProjectDto { Name = "", CustomerId = 1 });
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Projektname*");
    }

    [Fact]
    public async Task Update_KeinKunde_WirftException()
    {
        var id = Guid.NewGuid();
        var project = MakeProject(ProjectStatus.Draft);
        project.Id = id;
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(project);

        var act = () => CreateService().UpdateAsync(id, new UpdateProjectDto { Name = "Test", CustomerId = 0 });
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Kunde*");
    }

    // ─── ChangeStatusAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ChangeStatus_DraftNachActive_AendertStatus()
    {
        var id = Guid.NewGuid();
        var project = MakeProject(ProjectStatus.Draft);
        project.Id = id;
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(project);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Project>())).Returns(Task.CompletedTask);

        var result = await CreateService().ChangeStatusAsync(id, ProjectStatus.Active);

        result.Status.Should().Be(ProjectStatus.Active);
        _repo.Verify(r => r.UpdateAsync(project), Times.Once);
    }

    [Fact]
    public async Task ChangeStatus_ActiveNachCompleted_OhneOffeneAufgaben_Klappt()
    {
        var id = Guid.NewGuid();
        var project = MakeProject(ProjectStatus.Active, openTasks: 0);
        project.Id = id;
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(project);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Project>())).Returns(Task.CompletedTask);

        var result = await CreateService().ChangeStatusAsync(id, ProjectStatus.Completed);

        result.Status.Should().Be(ProjectStatus.Completed);
        result.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ChangeStatus_ActiveNachCompleted_MitOffenenAufgaben_WirftException()
    {
        var id = Guid.NewGuid();
        var project = MakeProject(ProjectStatus.Active, openTasks: 3);
        project.Id = id;
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(project);

        var act = () => CreateService().ChangeStatusAsync(id, ProjectStatus.Completed);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*3 offene*");
    }

    [Fact]
    public async Task ChangeStatus_NichtVorhandenesProjekt_WirftException()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Project?)null);
        var act = () => CreateService().ChangeStatusAsync(Guid.NewGuid(), ProjectStatus.Active);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ChangeStatus_UnerlaubterUebergang_WirftException()
    {
        var id = Guid.NewGuid();
        var project = MakeProject(ProjectStatus.Archived);
        project.Id = id;
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(project);

        var act = () => CreateService().ChangeStatusAsync(id, ProjectStatus.Active);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ─── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_RuftRepositoryAuf()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.DeleteAsync(id)).Returns(Task.CompletedTask);

        await CreateService().DeleteAsync(id);

        _repo.Verify(r => r.DeleteAsync(id), Times.Once);
    }

    // ─── ProjectNumberExistsAsync ─────────────────────────────────────────────

    [Fact]
    public async Task ProjectNumberExists_DelegiertAnRepository()
    {
        _repo.Setup(r => r.ExistsNumberAsync("P-2026-0001")).ReturnsAsync(true);

        var result = await CreateService().ProjectNumberExistsAsync("P-2026-0001");

        result.Should().BeTrue();
    }

    // ─── GetAvailableTransitions ──────────────────────────────────────────────

    [Fact]
    public void GetAvailableTransitions_DraftProjekt_NurFreigeben()
    {
        var project = MakeProject(ProjectStatus.Draft);
        var result = CreateService().GetAvailableTransitions(project);
        result.Should().HaveCount(1);
        result[0].TargetStatus.Should().Be(ProjectStatus.Active);
    }

    // ─── GenerateProjectNumberAsync ───────────────────────────────────────────

    [Fact]
    public async Task GenerateProjectNumber_DelegiertAnRepository()
    {
        _repo.Setup(r => r.GenerateProjectNumberAsync()).ReturnsAsync("P-2026-0042");

        var result = await CreateService().GenerateProjectNumberAsync();

        result.Should().Be("P-2026-0042");
    }

    // ─── EnsureExternalIdsAsync ───────────────────────────────────────────────

    [Fact]
    public async Task EnsureExternalIds_AlleHabenExternalId_TutNichts()
    {
        _repo.Setup(r => r.GetAllAsync(null, null))
            .ReturnsAsync(new List<Project>
            {
                new() { ExternalId = 1, Name = "P1", CustomerId = 1, ProjectNumber = "P1" },
                new() { ExternalId = 2, Name = "P2", CustomerId = 1, ProjectNumber = "P2" }
            });

        await CreateService().EnsureExternalIdsAsync();

        _repo.Verify(r => r.UpdateAsync(It.IsAny<Project>()), Times.Never);
    }

    [Fact]
    public async Task EnsureExternalIds_EinProjektOhneId_WeistIdZu()
    {
        var project = new Project { ExternalId = null, Name = "P1", CustomerId = 1, ProjectNumber = "P1" };
        _repo.Setup(r => r.GetAllAsync(null, null)).ReturnsAsync(new List<Project> { project });
        _repo.Setup(r => r.GetNextExternalIdAsync()).ReturnsAsync(10);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Project>())).Returns(Task.CompletedTask);

        await CreateService().EnsureExternalIdsAsync();

        project.ExternalId.Should().Be(10);
        _repo.Verify(r => r.UpdateAsync(project), Times.Once);
    }

    [Fact]
    public async Task EnsureExternalIds_MehrereOhneId_WeistAufsteigendeIdsZu()
    {
        var p1 = new Project { ExternalId = null, Name = "P1", CustomerId = 1, ProjectNumber = "P1" };
        var p2 = new Project { ExternalId = null, Name = "P2", CustomerId = 1, ProjectNumber = "P2" };
        _repo.Setup(r => r.GetAllAsync(null, null)).ReturnsAsync(new List<Project> { p1, p2 });
        _repo.Setup(r => r.GetNextExternalIdAsync()).ReturnsAsync(5);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Project>())).Returns(Task.CompletedTask);

        await CreateService().EnsureExternalIdsAsync();

        p1.ExternalId.Should().Be(5);
        p2.ExternalId.Should().Be(6);
        _repo.Verify(r => r.UpdateAsync(It.IsAny<Project>()), Times.Exactly(2));
    }
}
