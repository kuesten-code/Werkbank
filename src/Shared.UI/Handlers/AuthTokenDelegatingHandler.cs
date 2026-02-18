using Microsoft.AspNetCore.Http;

namespace Kuestencode.Shared.UI.Handlers;

/// <summary>
/// DelegatingHandler that forwards the Authorization header from the current HTTP context
/// to outgoing HTTP requests. This is needed when modules make API calls to the Host,
/// so that the Host can authenticate the requests.
/// </summary>
public class AuthTokenDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthTokenDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Get the current HTTP context
        var httpContext = _httpContextAccessor.HttpContext;

        // If we have an HTTP context, try to get the token
        if (httpContext != null)
        {
            // First try Authorization header
            var authHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader))
            {
                request.Headers.TryAddWithoutValidation("Authorization", authHeader);
            }
            else if (httpContext.Request.Cookies.TryGetValue("werkbank_auth_cookie", out var token))
            {
                // Fallback to cookie and add as Bearer token
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
