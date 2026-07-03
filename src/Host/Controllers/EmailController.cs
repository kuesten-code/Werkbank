using Microsoft.AspNetCore.Mvc;
using Kuestencode.Core.Interfaces;
using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Werkbank.Host.Auth;
using Kuestencode.Werkbank.Host.Models;

namespace Kuestencode.Werkbank.Host.Controllers;

/// <summary>
/// Zentraler Email-Versand-Endpoint. Alle Module senden E-Mails ausschließlich hierüber —
/// nur Host besitzt SMTP-Zugangsdaten und wendet das einheitliche Firmen-Layout an.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[RequireRole(UserRole.Admin)]
public class EmailController : ControllerBase
{
    private readonly IEmailEngine _emailEngine;

    public EmailController(IEmailEngine emailEngine)
    {
        _emailEngine = emailEngine;
    }

    [HttpPost("send")]
    public async Task<ActionResult<SendEmailResultDto>> Send([FromBody] SendEmailRequest request)
    {
        var attachments = request.Attachments.Select(a => new EmailAttachment
        {
            FileName = a.FileName,
            Content = a.Content,
            ContentType = a.ContentType
        });

        var success = await _emailEngine.SendEmailAsync(
            request.RecipientEmail,
            request.Subject,
            request.ContentHtml,
            request.ContentText,
            attachments,
            request.CcEmails,
            request.BccEmails,
            request.Greeting,
            request.IncludeClosing);

        return Ok(new SendEmailResultDto { Success = success });
    }

    [HttpGet("test-connection")]
    public async Task<ActionResult<SendEmailResultDto>> TestConnection()
    {
        var (success, errorMessage) = await _emailEngine.TestConnectionAsync();
        return Ok(new SendEmailResultDto { Success = success, ErrorMessage = errorMessage });
    }
}
