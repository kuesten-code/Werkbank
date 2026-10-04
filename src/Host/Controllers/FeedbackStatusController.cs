using Kuestencode.Shared.Contracts.Feedback;
using Kuestencode.Werkbank.Host.Models.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback;
using Microsoft.AspNetCore.Mvc;

namespace Kuestencode.Werkbank.Host.Controllers;

/// <summary>
/// Von den Modul-Layouts abgefragt, um das "Problem melden"-Symbol einzublenden. Öffentlich,
/// weil die Module dafür keinen Nutzer-Token weiterreichen; verrät nur ein Ja/Nein.
/// </summary>
[ApiController]
[Route("api/feedback/status")]
public class FeedbackStatusController : ControllerBase
{
    private readonly IFeedbackModeState _modeState;

    public FeedbackStatusController(IFeedbackModeState modeState)
    {
        _modeState = modeState;
    }

    [HttpGet]
    public ActionResult<FeedbackStatusDto> Get() =>
        Ok(new FeedbackStatusDto { ReportingEnabled = _modeState.Role == FeedbackRole.Client });
}
