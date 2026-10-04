using System.Text.Json;
using FluentAssertions;
using Kuestencode.Shared.Contracts.Feedback;
using Kuestencode.Werkbank.Host.Auth;
using Kuestencode.Werkbank.Host.Controllers;
using Kuestencode.Werkbank.Host.Models.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback.Hub;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Kuestencode.Host.Tests.Controllers;

public class FeedbackApiControllerTests
{
    private readonly Mock<IFeedbackHubApiService> _hubApiService = new();
    private readonly FeedbackApiController _controller;

    public FeedbackApiControllerTests()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Items[FeedbackApiKeyFilter.InstanceItemKey] = new FeedbackInstance { Id = 3, Name = "Kunde A" };
        _controller = new FeedbackApiController(_hubApiService.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private static string Json(object value) => JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    [Fact]
    public void GetInstance_LiefertNamenDerAufgeloestenInstanz()
    {
        var result = _controller.GetInstance();

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<FeedbackInstanceInfoDto>()
            .Which.Name.Should().Be("Kunde A");
    }

    [Fact]
    public async Task CreateReport_NeueMeldung_Liefert201MitInstanzAusKey()
    {
        var dto = new FeedbackReportDto { Id = 1 };
        _hubApiService.Setup(s => s.CreateReportAsync(3, It.IsAny<CreateFeedbackReportRequest>(), It.IsAny<IReadOnlyList<FeedbackUpload>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((dto, true));
        var file = new FormFile(new MemoryStream(new byte[] { 1, 2 }), 0, 2, "files", "a.png");

        var result = await _controller.CreateReport(Json(new CreateFeedbackReportRequest { Title = "x" }), new List<IFormFile> { file }, CancellationToken.None);

        result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(201);
        _hubApiService.Verify(s => s.CreateReportAsync(3,
            It.Is<CreateFeedbackReportRequest>(r => r.Title == "x"),
            It.Is<IReadOnlyList<FeedbackUpload>>(u => u.Single().FileName == "a.png" && u.Single().Content.Length == 2),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task CreateReport_BekannteMeldung_Liefert200()
    {
        _hubApiService.Setup(s => s.CreateReportAsync(3, It.IsAny<CreateFeedbackReportRequest>(), It.IsAny<IReadOnlyList<FeedbackUpload>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new FeedbackReportDto { Id = 1 }, false));

        var result = await _controller.CreateReport(Json(new CreateFeedbackReportRequest()), null, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Theory]
    [InlineData("kein json")]
    [InlineData("null")]
    public async Task CreateReport_UngueltigesJson_Liefert400(string report)
    {
        var result = await _controller.CreateReport(report, null, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateReport_Validierungsfehler_Liefert400()
    {
        _hubApiService.Setup(s => s.CreateReportAsync(It.IsAny<int>(), It.IsAny<CreateFeedbackReportRequest>(), It.IsAny<IReadOnlyList<FeedbackUpload>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FeedbackValidationException(new[] { "Titel ist erforderlich." }));

        var result = await _controller.CreateReport(Json(new CreateFeedbackReportRequest()), null, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetReports_ReichtInstanzUndSinceWeiter()
    {
        var since = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        _hubApiService.Setup(s => s.GetChangesAsync(3, since)).ReturnsAsync(new FeedbackSyncResponse());

        var result = await _controller.GetReports(since);

        result.Result.Should().BeOfType<OkObjectResult>();
        _hubApiService.Verify(s => s.GetChangesAsync(3, since));
    }

    [Fact]
    public async Task AddComment_FremdeOderUnbekannteMeldung_Liefert404()
    {
        _hubApiService.Setup(s => s.AddCustomerCommentAsync(3, 99, It.IsAny<CreateFeedbackCommentRequest>()))
            .ReturnsAsync((FeedbackCommentDto?)null);

        var result = await _controller.AddComment(99, new CreateFeedbackCommentRequest { Text = "x" });

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task AddComment_Erfolgreich_Liefert200()
    {
        _hubApiService.Setup(s => s.AddCustomerCommentAsync(3, 1, It.IsAny<CreateFeedbackCommentRequest>()))
            .ReturnsAsync(new FeedbackCommentDto { Id = 5 });

        var result = await _controller.AddComment(1, new CreateFeedbackCommentRequest { Text = "x" });

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AddComment_Validierungsfehler_Liefert400()
    {
        _hubApiService.Setup(s => s.AddCustomerCommentAsync(3, 1, It.IsAny<CreateFeedbackCommentRequest>()))
            .ThrowsAsync(new FeedbackValidationException(new[] { "leer" }));

        var result = await _controller.AddComment(1, new CreateFeedbackCommentRequest());

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }
}
