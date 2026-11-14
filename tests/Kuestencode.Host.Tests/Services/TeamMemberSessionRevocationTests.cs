using FluentAssertions;
using Kuestencode.Core.Interfaces;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using UserRole = Kuestencode.Shared.Contracts.Host.UserRole;

namespace Kuestencode.Host.Tests.Services;

/// <summary>
/// Security-Audit M1: welche Änderungen am Konto bestehende Sessions widerrufen.
/// </summary>
public sealed class TeamMemberSessionRevocationTests : IDisposable
{
    private readonly HostDbContext _db = new(new DbContextOptionsBuilder<HostDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private readonly TeamMemberService _service;
    private readonly TeamMember _member;

    public TeamMemberSessionRevocationTests()
    {
        _service = new TeamMemberService(_db, NullLogger<TeamMemberService>.Instance);
        _db.TeamMembers.Add(new TeamMember { Id = Guid.NewGuid(), DisplayName = "Admin", Email = "a@x.de", Role = UserRole.Admin });
        _member = new TeamMember { Id = Guid.NewGuid(), DisplayName = "Erika", Email = "e@x.de", Role = UserRole.Buero };
        _db.TeamMembers.Add(_member);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
    }

    public void Dispose() => _db.Dispose();

    private TeamMember Copy() => new()
    {
        Id = _member.Id, DisplayName = _member.DisplayName, Email = _member.Email, Role = _member.Role, IsActive = true
    };

    private int VersionAfter(Action<TeamMember> change)
    {
        var updated = Copy();
        change(updated);
        _service.UpdateAsync(updated).GetAwaiter().GetResult();
        _db.ChangeTracker.Clear();
        return _db.TeamMembers.Single(m => m.Id == _member.Id).SessionVersion;
    }

    [Fact]
    public void Deaktivieren_WiderruftSessions() => VersionAfter(m => m.IsActive = false).Should().Be(1);

    [Fact]
    public void Sperren_WiderruftSessions() => VersionAfter(m => m.IsLockedByAdmin = true).Should().Be(1);

    [Fact]
    public void Rollenwechsel_WiderruftSessions() => VersionAfter(m => m.Role = UserRole.Mitarbeiter).Should().Be(1);

    [Fact]
    public void OfflineSchalten_WiderruftSessions() => VersionAfter(m => m.IsOffline = true).Should().Be(1);

    [Fact]
    public void NamensAenderung_LaesstSessionsBestehen() => VersionAfter(m => m.DisplayName = "Erika M.").Should().Be(0);

    [Fact]
    public async Task PasswortReset_WiderruftSessions()
    {
        var tracked = _db.TeamMembers.Single(m => m.Id == _member.Id);
        tracked.ResetToken = "reset-token";
        tracked.ResetTokenExpires = DateTime.UtcNow.AddHours(1);
        await _db.SaveChangesAsync();
        var service = new PasswordResetService(_db, new PasswordService(), Mock.Of<IEmailEngine>(),
            NullLogger<PasswordResetService>.Instance);

        await service.ResetPasswordAsync("reset-token", "NeuesPasswort1!");

        _db.TeamMembers.Single(m => m.Id == _member.Id).SessionVersion.Should().Be(1);
    }
}
