using FluentAssertions;
using Kuestencode.Werkbank.Acta.Domain.Entities;
using Kuestencode.Werkbank.Acta.Domain.Enums;
using Xunit;

namespace Kuestencode.Werkbank.Acta.Tests.Domain;

public class ProjectEntityTests
{
    private static Project MakeProject(ProjectStatus status = ProjectStatus.Draft) =>
        new()
        {
            Id = Guid.NewGuid(),
            ProjectNumber = "P-2026-0001",
            Name = "Testprojekt",
            CustomerId = 1,
            Status = status
        };

    private static ProjectTask MakeTask(ProjectTaskStatus status) =>
        new() { Id = Guid.NewGuid(), Title = "Aufgabe", Status = status };

    // ─── IsEditable ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ProjectStatus.Draft,  true)]
    [InlineData(ProjectStatus.Active, true)]
    [InlineData(ProjectStatus.Paused, true)]
    [InlineData(ProjectStatus.Completed, false)]
    [InlineData(ProjectStatus.Archived,  false)]
    public void IsEditable_KorrektNachStatus(ProjectStatus status, bool erwartet)
    {
        var project = MakeProject(status);
        project.IsEditable.Should().Be(erwartet);
    }

    // ─── IsTerminal ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ProjectStatus.Completed, true)]
    [InlineData(ProjectStatus.Archived,  true)]
    [InlineData(ProjectStatus.Draft,     false)]
    [InlineData(ProjectStatus.Active,    false)]
    [InlineData(ProjectStatus.Paused,    false)]
    public void IsTerminal_KorrektNachStatus(ProjectStatus status, bool erwartet)
    {
        var project = MakeProject(status);
        project.IsTerminal.Should().Be(erwartet);
    }

    // ─── OpenTasksCount ───────────────────────────────────────────────────────

    [Fact]
    public void OpenTasksCount_ZaehltNurOffeneAufgaben()
    {
        var project = MakeProject();
        project.Tasks.Add(MakeTask(ProjectTaskStatus.Open));
        project.Tasks.Add(MakeTask(ProjectTaskStatus.Open));
        project.Tasks.Add(MakeTask(ProjectTaskStatus.Completed));

        project.OpenTasksCount.Should().Be(2);
    }

    [Fact]
    public void OpenTasksCount_OhneAufgaben_Null()
    {
        MakeProject().OpenTasksCount.Should().Be(0);
    }

    // ─── CompletedTasksCount ──────────────────────────────────────────────────

    [Fact]
    public void CompletedTasksCount_ZaehltNurErledigteAufgaben()
    {
        var project = MakeProject();
        project.Tasks.Add(MakeTask(ProjectTaskStatus.Open));
        project.Tasks.Add(MakeTask(ProjectTaskStatus.Completed));
        project.Tasks.Add(MakeTask(ProjectTaskStatus.Completed));

        project.CompletedTasksCount.Should().Be(2);
    }

    // ─── ProgressPercent ──────────────────────────────────────────────────────

    [Fact]
    public void ProgressPercent_OhneAufgaben_Null()
    {
        MakeProject().ProgressPercent.Should().Be(0);
    }

    [Fact]
    public void ProgressPercent_AlleErledigt_100()
    {
        var project = MakeProject();
        project.Tasks.Add(MakeTask(ProjectTaskStatus.Completed));
        project.Tasks.Add(MakeTask(ProjectTaskStatus.Completed));

        project.ProgressPercent.Should().Be(100);
    }

    [Fact]
    public void ProgressPercent_HaelftErledigt_50()
    {
        var project = MakeProject();
        project.Tasks.Add(MakeTask(ProjectTaskStatus.Open));
        project.Tasks.Add(MakeTask(ProjectTaskStatus.Completed));

        project.ProgressPercent.Should().Be(50);
    }

    [Fact]
    public void ProgressPercent_EineVonDrei_33()
    {
        var project = MakeProject();
        project.Tasks.Add(MakeTask(ProjectTaskStatus.Open));
        project.Tasks.Add(MakeTask(ProjectTaskStatus.Open));
        project.Tasks.Add(MakeTask(ProjectTaskStatus.Completed));

        project.ProgressPercent.Should().Be(33);
    }
}
