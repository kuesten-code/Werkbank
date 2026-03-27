using FluentAssertions;
using Kuestencode.Werkbank.Acta.Domain.Entities;
using Kuestencode.Werkbank.Acta.Domain.Enums;
using Kuestencode.Werkbank.Acta.Domain.Services;
using Xunit;

namespace Kuestencode.Werkbank.Acta.Tests.Domain;

public class ProjectStatusServiceTests
{
    private readonly ProjectStatusService _service = new();

    private static Project MakeProject(ProjectStatus status, int openTasks = 0) =>
        new()
        {
            Id = Guid.NewGuid(),
            ProjectNumber = "P-2026-0001",
            Name = "Testprojekt",
            CustomerId = 1,
            Status = status,
            Tasks = Enumerable.Range(0, openTasks)
                .Select(_ => new ProjectTask { Status = ProjectTaskStatus.Open })
                .ToList()
        };

    // ─── KannXxxWerden ────────────────────────────────────────────────────────

    [Fact]
    public void KannFreigegebenWerden_Draft_True() =>
        _service.KannFreigegebenWerden(MakeProject(ProjectStatus.Draft)).Should().BeTrue();

    [Fact]
    public void KannFreigegebenWerden_Active_False() =>
        _service.KannFreigegebenWerden(MakeProject(ProjectStatus.Active)).Should().BeFalse();

    [Fact]
    public void KannPausiertWerden_Active_True() =>
        _service.KannPausiertWerden(MakeProject(ProjectStatus.Active)).Should().BeTrue();

    [Fact]
    public void KannPausiertWerden_Draft_False() =>
        _service.KannPausiertWerden(MakeProject(ProjectStatus.Draft)).Should().BeFalse();

    [Fact]
    public void KannFortgesetztWerden_Paused_True() =>
        _service.KannFortgesetztWerden(MakeProject(ProjectStatus.Paused)).Should().BeTrue();

    [Fact]
    public void KannFortgesetztWerden_Draft_False() =>
        _service.KannFortgesetztWerden(MakeProject(ProjectStatus.Draft)).Should().BeFalse();

    [Fact]
    public void KannAbgeschlossenWerden_Active_True() =>
        _service.KannAbgeschlossenWerden(MakeProject(ProjectStatus.Active)).Should().BeTrue();

    [Fact]
    public void KannAbgeschlossenWerden_Draft_False() =>
        _service.KannAbgeschlossenWerden(MakeProject(ProjectStatus.Draft)).Should().BeFalse();

    [Fact]
    public void KannReaktiviertWerden_Completed_True() =>
        _service.KannReaktiviertWerden(MakeProject(ProjectStatus.Completed)).Should().BeTrue();

    [Fact]
    public void KannReaktiviertWerden_Active_False() =>
        _service.KannReaktiviertWerden(MakeProject(ProjectStatus.Active)).Should().BeFalse();

    [Fact]
    public void KannArchiviertWerden_Completed_True() =>
        _service.KannArchiviertWerden(MakeProject(ProjectStatus.Completed)).Should().BeTrue();

    [Fact]
    public void KannArchiviertWerden_Active_False() =>
        _service.KannArchiviertWerden(MakeProject(ProjectStatus.Active)).Should().BeFalse();

    [Theory]
    [InlineData(ProjectStatus.Draft)]
    [InlineData(ProjectStatus.Active)]
    [InlineData(ProjectStatus.Paused)]
    public void KannBearbeitetWerden_BearbeitbareStatus_True(ProjectStatus status) =>
        _service.KannBearbeitetWerden(MakeProject(status)).Should().BeTrue();

    [Theory]
    [InlineData(ProjectStatus.Completed)]
    [InlineData(ProjectStatus.Archived)]
    public void KannBearbeitetWerden_NichtBearbeitbareStatus_False(ProjectStatus status) =>
        _service.KannBearbeitetWerden(MakeProject(status)).Should().BeFalse();

    // ─── Freigeben ────────────────────────────────────────────────────────────

    [Fact]
    public void Freigeben_DraftProjekt_WirdActive()
    {
        var project = MakeProject(ProjectStatus.Draft);
        _service.Freigeben(project);
        project.Status.Should().Be(ProjectStatus.Active);
    }

    [Fact]
    public void Freigeben_ActiveProjekt_WirftException()
    {
        var project = MakeProject(ProjectStatus.Active);
        var act = () => _service.Freigeben(project);
        act.Should().Throw<InvalidOperationException>().WithMessage("*freigegeben*");
    }

    // ─── Pausieren ────────────────────────────────────────────────────────────

    [Fact]
    public void Pausieren_ActiveProjekt_WirdPaused()
    {
        var project = MakeProject(ProjectStatus.Active);
        _service.Pausieren(project);
        project.Status.Should().Be(ProjectStatus.Paused);
    }

    [Fact]
    public void Pausieren_DraftProjekt_WirftException()
    {
        var project = MakeProject(ProjectStatus.Draft);
        var act = () => _service.Pausieren(project);
        act.Should().Throw<InvalidOperationException>();
    }

    // ─── Fortsetzen ───────────────────────────────────────────────────────────

    [Fact]
    public void Fortsetzen_PausedProjekt_WirdActive()
    {
        var project = MakeProject(ProjectStatus.Paused);
        _service.Fortsetzen(project);
        project.Status.Should().Be(ProjectStatus.Active);
    }

    [Fact]
    public void Fortsetzen_ArchivedProjekt_WirftException()
    {
        var project = MakeProject(ProjectStatus.Archived);
        var act = () => _service.Fortsetzen(project);
        act.Should().Throw<InvalidOperationException>();
    }

    // ─── Abschliessen ─────────────────────────────────────────────────────────

    [Fact]
    public void Abschliessen_ActiveOhneOffeneAufgaben_WirdCompleted()
    {
        var project = MakeProject(ProjectStatus.Active, openTasks: 0);
        _service.Abschliessen(project);
        project.Status.Should().Be(ProjectStatus.Completed);
        project.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Abschliessen_ActiveMitOffenenAufgaben_WirftException()
    {
        var project = MakeProject(ProjectStatus.Active, openTasks: 2);
        var act = () => _service.Abschliessen(project);
        act.Should().Throw<InvalidOperationException>().WithMessage("*2 offene*");
    }

    [Fact]
    public void Abschliessen_DraftProjekt_WirftException()
    {
        var project = MakeProject(ProjectStatus.Draft);
        var act = () => _service.Abschliessen(project);
        act.Should().Throw<InvalidOperationException>();
    }

    // ─── Reaktivieren ─────────────────────────────────────────────────────────

    [Fact]
    public void Reaktivieren_CompletedProjekt_WirdActiveUndCompletedAtNull()
    {
        var project = MakeProject(ProjectStatus.Completed);
        project.CompletedAt = DateTime.UtcNow;

        _service.Reaktivieren(project);

        project.Status.Should().Be(ProjectStatus.Active);
        project.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Reaktivieren_ActiveProjekt_WirftException()
    {
        var project = MakeProject(ProjectStatus.Active);
        var act = () => _service.Reaktivieren(project);
        act.Should().Throw<InvalidOperationException>();
    }

    // ─── Archivieren ──────────────────────────────────────────────────────────

    [Fact]
    public void Archivieren_CompletedProjekt_WirdArchived()
    {
        var project = MakeProject(ProjectStatus.Completed);
        _service.Archivieren(project);
        project.Status.Should().Be(ProjectStatus.Archived);
    }

    [Fact]
    public void Archivieren_ActiveProjekt_WirftException()
    {
        var project = MakeProject(ProjectStatus.Active);
        var act = () => _service.Archivieren(project);
        act.Should().Throw<InvalidOperationException>();
    }

    // ─── TransitionTo ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ProjectStatus.Draft,     ProjectStatus.Active)]
    [InlineData(ProjectStatus.Active,    ProjectStatus.Paused)]
    [InlineData(ProjectStatus.Paused,    ProjectStatus.Active)]
    [InlineData(ProjectStatus.Completed, ProjectStatus.Archived)]
    public void TransitionTo_ErlaubteUebergaenge_AendertStatus(
        ProjectStatus von, ProjectStatus nach)
    {
        var project = MakeProject(von);
        _service.TransitionTo(project, nach);
        project.Status.Should().Be(nach);
    }

    [Fact]
    public void TransitionTo_ActiveNachCompleted_OhneOffeneAufgaben_Klappt()
    {
        var project = MakeProject(ProjectStatus.Active, openTasks: 0);
        _service.TransitionTo(project, ProjectStatus.Completed);
        project.Status.Should().Be(ProjectStatus.Completed);
    }

    [Fact]
    public void TransitionTo_CompletedNachActive_Reaktiviert()
    {
        var project = MakeProject(ProjectStatus.Completed);
        project.CompletedAt = DateTime.UtcNow;
        _service.TransitionTo(project, ProjectStatus.Active);
        project.Status.Should().Be(ProjectStatus.Active);
        project.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void TransitionTo_UnerlaubterUebergang_WirftException()
    {
        var project = MakeProject(ProjectStatus.Archived);
        var act = () => _service.TransitionTo(project, ProjectStatus.Active);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void TransitionTo_DraftNachCompleted_WirftException()
    {
        var project = MakeProject(ProjectStatus.Draft);
        var act = () => _service.TransitionTo(project, ProjectStatus.Completed);
        act.Should().Throw<InvalidOperationException>();
    }

    // ─── GetVerfuegbareUebergaenge ────────────────────────────────────────────

    [Fact]
    public void GetVerfuegbareUebergaenge_DelegiertAnStateMachine()
    {
        var project = MakeProject(ProjectStatus.Active);
        var result = _service.GetVerfuegbareUebergaenge(project);
        result.Should().HaveCount(2);
    }
}
