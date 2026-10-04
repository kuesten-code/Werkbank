using Kuestencode.Werkbank.Host.Auth;
using Kuestencode.Werkbank.Host.Services.Feedback.Hub;
using Microsoft.AspNetCore.Mvc;

namespace Kuestencode.Werkbank.Host.Controllers;

/// <summary>Screenshots für die Detailansicht im Hub (nur Admin).</summary>
[ApiController]
[Route("api/feedback/attachments")]
[RequireRole(UserRole.Admin)]
public class FeedbackAttachmentsController : ControllerBase
{
    private readonly IFeedbackBoardService _boardService;

    public FeedbackAttachmentsController(IFeedbackBoardService boardService)
    {
        _boardService = boardService;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var result = await _boardService.OpenAttachmentAsync(id);
        if (result == null)
            return NotFound();

        // Inline statt Download, damit Screenshots direkt angezeigt werden. Gespeichert werden
        // nur per Signatur geprüfte Bilder/PDFs; nosniff setzt die Pipeline global.
        return File(result.Value.Content, result.Value.Attachment.ContentType);
    }
}
