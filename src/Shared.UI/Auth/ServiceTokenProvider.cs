using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Kuestencode.Shared.Contracts.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Kuestencode.Shared.UI.Auth;

/// <summary>
/// Stellt kurzlebige Server-zu-Server-Tokens aus (Registrierung beim Host, Modul-zu-Modul-Aufrufe ohne
/// Nutzerkontext). Ersetzt das frühere Vertrauen anhand der Quell-IP: wer das Token vorzeigt, kennt das Secret.
/// </summary>
public sealed class ServiceTokenProvider
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RenewBefore = TimeSpan.FromMinutes(1);

    private readonly SigningCredentials _credentials;
    private readonly string _issuer;
    private readonly TimeProvider _timeProvider;
    private readonly object _lock = new();
    private (string Token, DateTimeOffset ExpiresAt)? _current;

    public ServiceTokenProvider(IConfiguration configuration) : this(configuration, TimeProvider.System)
    {
    }

    public ServiceTokenProvider(IConfiguration configuration, TimeProvider timeProvider)
    {
        _credentials = new SigningCredentials(
            WerkbankJwt.SigningKey(WerkbankJwt.RequireSecret(configuration)), SecurityAlgorithms.HmacSha256);
        _issuer = WerkbankJwt.Issuer(configuration);
        _timeProvider = timeProvider;
    }

    public string GetToken()
    {
        var now = _timeProvider.GetUtcNow();
        lock (_lock)
        {
            if (_current is { } current && current.ExpiresAt - now > RenewBefore)
                return current.Token;

            var expiresAt = now.Add(Lifetime);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.Empty.ToString()),
                new Claim(ClaimTypes.Name, "InternalService"),
                new Claim(ClaimTypes.Role, UserRole.Admin.ToString()),
                new Claim(WerkbankJwt.ServiceClaim, "true")
            };
            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: WerkbankJwt.ServiceAudience(_issuer),
                claims: claims,
                notBefore: now.UtcDateTime,
                expires: expiresAt.UtcDateTime,
                signingCredentials: _credentials);

            _current = (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
            return _current.Value.Token;
        }
    }
}
