using System.Text.Json;
using Kuestencode.Shared.Contracts.Feedback;
using Kuestencode.Werkbank.Host.Auth;
using Kuestencode.Werkbank.Host.Models.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback.Hub;
using Microsoft.AspNetCore.Mvc;

namespace Kuestencode.Werkbank.Host.Controllers;

/// <summary>
/// Hub-API für Kunden-Instanzen (Rolle Hub). Wird ausschließlich von Kunden-Werkbänken
/// aufgerufen (ausgehend, HTTPS); der Hub ruft nie bei Kunden an.
/// </summary>
[ApiController]
[Route("api/v1")]
[FeedbackApiKey]
public class FeedbackApiController : ControllerBase
{
    private const long MaxRequestBytes = 64L * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IFeedbackHubApiService _hubApiService;

    public FeedbackApiController(IFeedbackHubApiService hubApiService)
    {
        _hubApiService = hubApiService;
    }

    private FeedbackInstance CurrentInstance => (FeedbackInstance)HttpContext.Items[FeedbackApiKeyFilter.InstanceItemKey]!;

    [HttpGet("instance")]
    public ActionResult<FeedbackInstanceInfoDto> GetInstance() =>
        Ok(new FeedbackInstanceInfoDto { Name = CurrentInstance.Name });

    [HttpPost("reports")]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    public async Task<ActionResult<FeedbackReportDto>> CreateReport(
        [FromForm(Name = "report")] string report,
        [FromForm(Name = "files")] List<IFormFile>? files,
        CancellationToken ct)
    {
        CreateFeedbackReportRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<CreateFeedbackReportRequest>(report, JsonOptions);
        }
        catch (JsonException)
        {
            request = null;
        }

        if (request == null)
            return BadRequest(new { errors = new[] { "Ungültiger Meldungs-JSON." } });

        var uploads = new List<FeedbackUpload>();
        foreach (var file in files ?? new List<IFormFile>())
        {
            using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, ct);
            uploads.Add(new FeedbackUpload(file.FileName, buffer.ToArray()));
        }

        try
        {
            var (dto, created) = await _hubApiService.CreateReportAsync(CurrentInstance.Id, request, uploads, ct);
            return created ? StatusCode(StatusCodes.Status201Created, dto) : Ok(dto);
        }
        catch (FeedbackValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors });
        }
    }

    [HttpGet("reports")]
    public async Task<ActionResult<FeedbackSyncResponse>> GetReports([FromQuery] DateTime? since) =>
        Ok(await _hubApiService.GetChangesAsync(CurrentInstance.Id, since));

    [HttpPost("reports/{id:int}/comments")]
    public async Task<ActionResult<FeedbackCommentDto>> AddComment(int id, [FromBody] CreateFeedbackCommentRequest request)
    {
        try
        {
            var comment = await _hubApiService.AddCustomerCommentAsync(CurrentInstance.Id, id, request);
            return comment == null ? NotFound() : Ok(comment);
        }
        catch (FeedbackValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors });
        }
    }
}
