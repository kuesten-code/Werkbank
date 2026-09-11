using FluentAssertions;
using Kuestencode.Werkbank.Acta.Data;
using Kuestencode.Werkbank.Acta.Data.Repositories;
using Kuestencode.Werkbank.Acta.Domain.Entities;
using Kuestencode.Werkbank.Acta.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kuestencode.Werkbank.Acta.Tests.Data;

public class PinnedProjectRepositoryTests
{
    private static Project MakeProject(string number = "P-0001") => new()
    {
        Id = Guid.NewGuid(),
        ProjectNumber = number,
        Name = "Testprojekt",
        CustomerId = 1,
        Status = ProjectStatus.Active
    };

    [Fact]
    public async Task GetByUserIdAsync_GibtNurZeilenDesEigenenNutzersZurueck()
    {
        var factory = TestDbContextFactory.CreateInMemory();
        var project1 = MakeProject("P-0001");
        var project2 = MakeProject("P-0002");
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        await using (var context = factory.CreateDbContext())
        {
            context.Projects.AddRange(project1, project2);
            context.PinnedProjects.AddRange(
                new PinnedProject { Id = Guid.NewGuid(), UserId = userA, ProjectId = project1.Id, PinnedAt = DateTime.UtcNow },
                new PinnedProject { Id = Guid.NewGuid(), UserId = userB, ProjectId = project2.Id, PinnedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();
        }

        var repository = new PinnedProjectRepository(factory);

        var pinsA = await repository.GetByUserIdAsync(userA);
        var pinsB = await repository.GetByUserIdAsync(userB);

        pinsA.Should().ContainSingle(p => p.ProjectId == project1.Id);
        pinsB.Should().ContainSingle(p => p.ProjectId == project2.Id);
    }

    [Fact]
    public async Task ExistsAsync_ErkenntBereitsGepinntesProjekt()
    {
        var factory = TestDbContextFactory.CreateInMemory();
        var project = MakeProject();
        var userId = Guid.NewGuid();
        var repository = new PinnedProjectRepository(factory);

        await using (var context = factory.CreateDbContext())
        {
            context.Projects.Add(project);
            await context.SaveChangesAsync();
        }

        (await repository.ExistsAsync(userId, project.Id)).Should().BeFalse();

        await repository.AddAsync(new PinnedProject { Id = Guid.NewGuid(), UserId = userId, ProjectId = project.Id, PinnedAt = DateTime.UtcNow });

        (await repository.ExistsAsync(userId, project.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task RemoveAsync_EntferntNurDieZeileDesAngegebenenNutzers()
    {
        var factory = TestDbContextFactory.CreateInMemory();
        var project = MakeProject();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var repository = new PinnedProjectRepository(factory);

        await using (var context = factory.CreateDbContext())
        {
            context.Projects.Add(project);
            await context.SaveChangesAsync();
        }

        await repository.AddAsync(new PinnedProject { Id = Guid.NewGuid(), UserId = userA, ProjectId = project.Id, PinnedAt = DateTime.UtcNow });
        await repository.AddAsync(new PinnedProject { Id = Guid.NewGuid(), UserId = userB, ProjectId = project.Id, PinnedAt = DateTime.UtcNow });

        var removed = await repository.RemoveAsync(userA, project.Id);

        removed.Should().BeTrue();
        (await repository.ExistsAsync(userA, project.Id)).Should().BeFalse();
        (await repository.ExistsAsync(userB, project.Id)).Should().BeTrue("das Entfernen darf nicht die Kachel eines anderen Nutzers mit entfernen");
    }

    [Fact]
    public async Task RemoveAsync_NichtGepinntesProjekt_GibtFalseZurueckOhneFehler()
    {
        var factory = TestDbContextFactory.CreateInMemory();
        var repository = new PinnedProjectRepository(factory);

        var removed = await repository.RemoveAsync(Guid.NewGuid(), Guid.NewGuid());

        removed.Should().BeFalse();
    }
}
