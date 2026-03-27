using FluentAssertions;
using Kuestencode.Werkbank.Acta.Domain.Enums;
using Kuestencode.Werkbank.Acta.Domain.Services;
using Xunit;

namespace Kuestencode.Werkbank.Acta.Tests.Domain;

public class ProjectStateMachineTests
{
    // ─── CanTransition ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ProjectStatus.Draft,     ProjectStatus.Active,    true)]
    [InlineData(ProjectStatus.Active,    ProjectStatus.Paused,    true)]
    [InlineData(ProjectStatus.Paused,    ProjectStatus.Active,    true)]
    [InlineData(ProjectStatus.Active,    ProjectStatus.Completed, true)]
    [InlineData(ProjectStatus.Completed, ProjectStatus.Active,    true)]
    [InlineData(ProjectStatus.Completed, ProjectStatus.Archived,  true)]
    public void CanTransition_ErlaubteUebergaenge_GibtTruezurueck(
        ProjectStatus von, ProjectStatus nach, bool erwartet)
    {
        ProjectStateMachine.CanTransition(von, nach).Should().Be(erwartet);
    }

    [Theory]
    [InlineData(ProjectStatus.Draft,     ProjectStatus.Completed)]
    [InlineData(ProjectStatus.Draft,     ProjectStatus.Paused)]
    [InlineData(ProjectStatus.Draft,     ProjectStatus.Archived)]
    [InlineData(ProjectStatus.Active,    ProjectStatus.Archived)]
    [InlineData(ProjectStatus.Active,    ProjectStatus.Draft)]
    [InlineData(ProjectStatus.Paused,    ProjectStatus.Completed)]
    [InlineData(ProjectStatus.Paused,    ProjectStatus.Archived)]
    [InlineData(ProjectStatus.Archived,  ProjectStatus.Active)]
    [InlineData(ProjectStatus.Archived,  ProjectStatus.Completed)]
    [InlineData(ProjectStatus.Completed, ProjectStatus.Draft)]
    [InlineData(ProjectStatus.Completed, ProjectStatus.Paused)]
    public void CanTransition_UnerlaubteUebergaenge_GibtFalseZurueck(
        ProjectStatus von, ProjectStatus nach)
    {
        ProjectStateMachine.CanTransition(von, nach).Should().BeFalse();
    }

    // ─── IsTerminal ───────────────────────────────────────────────────────────

    [Fact]
    public void IsTerminal_Archived_GibtTrueZurueck()
    {
        ProjectStateMachine.IsTerminal(ProjectStatus.Archived).Should().BeTrue();
    }

    [Theory]
    [InlineData(ProjectStatus.Draft)]
    [InlineData(ProjectStatus.Active)]
    [InlineData(ProjectStatus.Paused)]
    [InlineData(ProjectStatus.Completed)]
    public void IsTerminal_NichtArchived_GibtFalseZurueck(ProjectStatus status)
    {
        ProjectStateMachine.IsTerminal(status).Should().BeFalse();
    }

    // ─── IsEditable ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ProjectStatus.Draft)]
    [InlineData(ProjectStatus.Active)]
    [InlineData(ProjectStatus.Paused)]
    public void IsEditable_BearbeitbareStatus_GibtTrueZurueck(ProjectStatus status)
    {
        ProjectStateMachine.IsEditable(status).Should().BeTrue();
    }

    [Theory]
    [InlineData(ProjectStatus.Completed)]
    [InlineData(ProjectStatus.Archived)]
    public void IsEditable_NichtBearbeitbareStatus_GibtFalseZurueck(ProjectStatus status)
    {
        ProjectStateMachine.IsEditable(status).Should().BeFalse();
    }

    // ─── GetAvailableTransitions ──────────────────────────────────────────────

    [Fact]
    public void GetAvailableTransitions_Draft_NurFreigeben()
    {
        var result = ProjectStateMachine.GetAvailableTransitions(ProjectStatus.Draft);
        result.Should().HaveCount(1);
        result[0].TargetStatus.Should().Be(ProjectStatus.Active);
        result[0].ActionName.Should().Be("Freigeben");
    }

    [Fact]
    public void GetAvailableTransitions_Active_PausierenUndAbschliessen()
    {
        var result = ProjectStateMachine.GetAvailableTransitions(ProjectStatus.Active);
        result.Should().HaveCount(2);
        result.Should().Contain(t => t.TargetStatus == ProjectStatus.Paused);
        result.Should().Contain(t => t.TargetStatus == ProjectStatus.Completed);
    }

    [Fact]
    public void GetAvailableTransitions_Paused_NurFortsetzen()
    {
        var result = ProjectStateMachine.GetAvailableTransitions(ProjectStatus.Paused);
        result.Should().HaveCount(1);
        result[0].TargetStatus.Should().Be(ProjectStatus.Active);
        result[0].ActionName.Should().Be("Fortsetzen");
    }

    [Fact]
    public void GetAvailableTransitions_Completed_ReaktivierenUndArchivieren()
    {
        var result = ProjectStateMachine.GetAvailableTransitions(ProjectStatus.Completed);
        result.Should().HaveCount(2);
        result.Should().Contain(t => t.TargetStatus == ProjectStatus.Active);
        result.Should().Contain(t => t.TargetStatus == ProjectStatus.Archived);
    }

    [Fact]
    public void GetAvailableTransitions_Archived_Leer()
    {
        var result = ProjectStateMachine.GetAvailableTransitions(ProjectStatus.Archived);
        result.Should().BeEmpty();
    }

    // ─── GetActionName ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ProjectStatus.Draft,     ProjectStatus.Active,    "Freigeben")]
    [InlineData(ProjectStatus.Active,    ProjectStatus.Paused,    "Pausieren")]
    [InlineData(ProjectStatus.Paused,    ProjectStatus.Active,    "Fortsetzen")]
    [InlineData(ProjectStatus.Active,    ProjectStatus.Completed, "Abschließen")]
    [InlineData(ProjectStatus.Completed, ProjectStatus.Active,    "Reaktivieren")]
    [InlineData(ProjectStatus.Completed, ProjectStatus.Archived,  "Archivieren")]
    public void GetActionName_ErlaubterUebergang_GibtAktionsNameZurueck(
        ProjectStatus von, ProjectStatus nach, string erwartet)
    {
        ProjectStateMachine.GetActionName(von, nach).Should().Be(erwartet);
    }

    [Fact]
    public void GetActionName_UnerlaubterUebergang_GibtNullZurueck()
    {
        ProjectStateMachine.GetActionName(ProjectStatus.Archived, ProjectStatus.Active)
            .Should().BeNull();
    }
}
