using FluentAssertions;
using Kuestencode.Werkbank.Host.Controllers;
using Kuestencode.Werkbank.Host.Models.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback.Hub;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Kuestencode.Host.Tests.Controllers;

public class FeedbackAttachmentsControllerTests
{
    private readonly Mock<IFeedbackBoardService> _boardService = new();
    private readonly FeedbackAttachmentsController _controller;

    public FeedbackAttachmentsControllerTests()
    {
        _controller = new FeedbackAttachmentsController(_boardService.Object);
    }

    [Fact]
    public async Task Get_LiefertDateiMitGespeichertemContentType()
    {
        var attachment = new FeedbackAttachment { Id = 1, ContentType = "image/webp" };
        _boardService.Setup(s => s.OpenAttachmentAsync(1)).ReturnsAsync((attachment, (Stream)new MemoryStream(new byte[] { 1 })));

        var result = await _controller.Get(1);

        result.Should().BeOfType<FileStreamResult>().Which.ContentType.Should().Be("image/webp");
    }

    [Fact]
    public async Task Get_Unbekannt_Liefert404()
    {
        _boardService.Setup(s => s.OpenAttachmentAsync(9)).ReturnsAsync(((FeedbackAttachment, Stream)?)null);

        (await _controller.Get(9)).Should().BeOfType<NotFoundResult>();
    }
}
