using System.Net.Http.Headers;
using Kuestencode.Shared.UI.Auth;
using Microsoft.AspNetCore.Http;

namespace Kuestencode.Shared.UI.Handlers;

/// <summary>
/// Reicht bei ausgehenden Aufrufen an Host oder andere Module das Nutzer-Token des aktuellen Requests weiter.
/// Ohne Nutzerkontext (Hintergrunddienste, Registrierung) wird stattdessen ein Service-Token mitgeschickt.
/// </summary>
public class AuthTokenDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ServiceTokenProvider _serviceTokenProvider;

    public AuthTokenDelegatingHandler(IHttpContextAccessor httpContextAccessor, ServiceTokenProvider serviceTokenProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _serviceTokenProvider = serviceTokenProvider;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var token = (httpContext != null ? WerkbankJwt.ExtractToken(httpContext) : null)
            ?? _serviceTokenProvider.GetToken();

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return base.SendAsync(request, cancellationToken);
    }
}
