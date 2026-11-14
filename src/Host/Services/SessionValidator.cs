using System.Security.Claims;
using Kuestencode.Shared.UI.Auth;
using Kuestencode.Werkbank.Host.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Host.Services;

public interface ISessionValidator
{
    /// <summary>
    /// Wie <see cref="IJwtTokenService.ValidateToken"/>, zusätzlich muss das Konto noch anmeldefähig sein und die
    /// Session-Version im Token zur aktuellen passen. So wirken Deaktivieren, Sperren, Rollen- und
    /// Passwortwechsel sofort statt erst beim Ablauf des Tokens.
    /// </summary>
    Task<ClaimsPrincipal?> ValidateAsync(string token);
}

public class SessionValidator : ISessionValidator
{
    public const string SessionVersionClaim = "sv";

    private readonly IJwtTokenService _jwtTokenService;
    private readonly IServiceScopeFactory _scopeFactory;

    public SessionValidator(IJwtTokenService jwtTokenService, IServiceScopeFactory scopeFactory)
    {
        _jwtTokenService = jwtTokenService;
        _scopeFactory = scopeFactory;
    }

    public async Task<ClaimsPrincipal?> ValidateAsync(string token)
    {
        var principal = _jwtTokenService.ValidateToken(token);
        if (principal == null)
            return null;

        // Service-Tokens und das Token für deaktivierte Anmeldung gehören zu keinem Konto.
        if (principal.HasClaim(WerkbankJwt.ServiceClaim, "true")
            || !Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var memberId)
            || memberId == Guid.Empty)
            return principal;

        // Vor Einführung der Session-Version ausgestellte Tokens gelten als Version 0.
        var tokenVersion = int.TryParse(principal.FindFirstValue(SessionVersionClaim), out var version) ? version : 0;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HostDbContext>();
        var member = await db.TeamMembers.AsNoTracking()
            .Where(m => m.Id == memberId)
            .Select(m => new { m.IsActive, m.IsOffline, m.IsLockedByAdmin, m.SessionVersion })
            .FirstOrDefaultAsync();

        var stillValid = member is { IsActive: true, IsOffline: false, IsLockedByAdmin: false }
                         && member.SessionVersion == tokenVersion;
        return stillValid ? principal : null;
    }
}
