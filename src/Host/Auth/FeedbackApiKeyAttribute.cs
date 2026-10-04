using Kuestencode.Werkbank.Host.Models.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback.Hub;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kuestencode.Werkbank.Host.Auth;

/// <summary>
/// Authentifiziert Kunden-Instanzen an der Hub-API über den Header <c>X-Api-Key</c>. Die
/// Instanz ergibt sich ausschließlich aus dem Key und wird in <see cref="HttpContext.Items"/>
/// abgelegt. Ist die Werkbank nicht in der Rolle Hub, existiert die API nach außen nicht (404).
/// </summary>
public class FeedbackApiKeyAttribute : TypeFilterAttribute
{
    public FeedbackApiKeyAttribute() : base(typeof(FeedbackApiKeyFilter))
    {
    }
}

/// <summary>
/// Bewusst ein Authorization-Filter: er läuft vor dem Model-Binding, sodass Uploads ohne
/// gültigen Key gar nicht erst gelesen und gepuffert werden.
/// </summary>
public class FeedbackApiKeyFilter : IAsyncAuthorizationFilter
{
    public const string HeaderName = "X-Api-Key";
    public const string InstanceItemKey = "FeedbackInstance";

    private readonly IFeedbackModeState _modeState;
    private readonly IFeedbackInstanceService _instanceService;

    public FeedbackApiKeyFilter(IFeedbackModeState modeState, IFeedbackInstanceService instanceService)
    {
        _modeState = modeState;
        _instanceService = instanceService;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (_modeState.Role != FeedbackRole.Hub)
        {
            context.Result = new NotFoundResult();
            return;
        }

        var apiKey = context.HttpContext.Request.Headers[HeaderName].FirstOrDefault();
        var instance = string.IsNullOrWhiteSpace(apiKey) ? null : await _instanceService.FindActiveByApiKeyAsync(apiKey);
        if (instance == null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        context.HttpContext.Items[InstanceItemKey] = instance;
    }
}
