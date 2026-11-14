using FluentAssertions;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Kuestencode.Host.Tests.Services;

/// <summary>
/// Security-Audit M3: wer nur den Mobile-Link kennt, darf keine bestehende PIN überschreiben.
/// </summary>
public sealed class MobileTokenServiceTests : IDisposable
{
    private readonly HostDbContext _db = new(new DbContextOptionsBuilder<HostDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private readonly MobileTokenService _service;
    private readonly TeamMember _member = new() { Id = Guid.NewGuid(), DisplayName = "Paul" };

    public MobileTokenServiceTests()
    {
        _db.TeamMembers.Add(_member);
        _db.SaveChanges();
        _service = new MobileTokenService(_db, new PasswordService(), Mock.Of<IJwtTokenService>(),
            NullLogger<MobileTokenService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task SetPinAsync_ErsteVergabe_SetztPin()
    {
        var token = await _service.CreateMobileAccessAsync(_member.Id);

        await _service.SetPinAsync(token, "4711");

        _member.MobilePinSet.Should().BeTrue();
    }

    [Fact]
    public async Task SetPinAsync_PinBereitsGesetzt_WirdAbgelehntUndBleibtUnveraendert()
    {
        var token = await _service.CreateMobileAccessAsync(_member.Id);
        await _service.SetPinAsync(token, "4711");
        var originalHash = _member.PinHash;

        var act = () => _service.SetPinAsync(token, "8315");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*bereits gesetzt*");
        _member.PinHash.Should().Be(originalHash);
    }

    [Fact]
    public async Task SetPinAsync_NachZuruecksetzenDurchAdmin_NeuePinMoeglich()
    {
        var token = await _service.CreateMobileAccessAsync(_member.Id);
        await _service.SetPinAsync(token, "4711");

        await _service.ResetMobileAccessAsync(_member.Id);
        await _service.SetPinAsync(_member.MobileToken!, "8315");

        _member.MobilePinSet.Should().BeTrue();
    }
}
