using System.Text;
using FluentAssertions;
using Kuestencode.Werkbank.Recepta.Controllers;
using Kuestencode.Werkbank.Recepta.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Kuestencode.Werkbank.Recepta.Tests;

/// <summary>
/// Security-Audit H3 (Stored XSS): gespeicherte Dateien werden nur als PDF/Bild inline ausgeliefert,
/// alles andere als Anhang, unbekannte Typen als application/octet-stream.
/// </summary>
public class FilesControllerDownloadTests
{
    private static (FilesController Controller, HttpResponse Response) Create(string storedContentType)
    {
        var fileService = new Mock<IDocumentFileService>();
        fileService.Setup(s => s.DownloadAsync(It.IsAny<Guid>()))
            .ReturnsAsync(((Stream)new MemoryStream(Encoding.UTF8.GetBytes("x")), "beleg.dat", storedContentType));

        var controller = new FilesController(fileService.Object, Mock.Of<IDocumentService>(), Mock.Of<IOcrService>(),
            NullLogger<FilesController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        return (controller, controller.HttpContext.Response);
    }

    [Fact]
    public async Task Download_AltlastHtmlInline_KommtAlsAnhangMitNeutralemTyp()
    {
        var (controller, _) = Create("text/html");

        var result = await controller.Download(Guid.NewGuid(), inline: true);

        var file = result.Should().BeOfType<FileStreamResult>().Subject;
        file.ContentType.Should().Be("application/octet-stream");
        file.FileDownloadName.Should().Be("beleg.dat");
    }

    [Fact]
    public async Task Download_XmlInline_KommtAlsAnhang()
    {
        var (controller, _) = Create("application/xml");

        var file = (await controller.Download(Guid.NewGuid(), inline: true)).Should().BeOfType<FileStreamResult>().Subject;

        file.ContentType.Should().Be("application/xml");
        file.FileDownloadName.Should().Be("beleg.dat");
    }

    [Fact]
    public async Task Download_PdfInline_WirdInlineAngezeigt()
    {
        var (controller, response) = Create("application/pdf");

        var file = (await controller.Download(Guid.NewGuid(), inline: true)).Should().BeOfType<FileStreamResult>().Subject;

        file.ContentType.Should().Be("application/pdf");
        file.FileDownloadName.Should().BeNullOrEmpty();
        response.Headers.ContentDisposition.ToString().Should().StartWith("inline");
    }

    private static IFormFile FormFile(string fileName, string contentType)
    {
        var bytes = Encoding.UTF8.GetBytes("%PDF");
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    [Fact]
    public async Task Upload_ServiceLehntTypAb_Gibt400()
    {
        var fileService = new Mock<IDocumentFileService>();
        fileService.Setup(s => s.UploadAsync(It.IsAny<Guid>(), It.IsAny<Stream>(), "x.html", "text/html"))
            .ThrowsAsync(new InvalidOperationException("Nur PDF, JPEG, PNG, TIFF und XML sind erlaubt."));
        var controller = new FilesController(fileService.Object, Mock.Of<IDocumentService>(), Mock.Of<IOcrService>(),
            NullLogger<FilesController>.Instance);

        var result = await controller.Upload(Guid.NewGuid(), FormFile("x.html", "text/html"), ocr: false);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Upload_OcrNutztBereinigtenDateinamen()
    {
        var documentId = Guid.NewGuid();
        var stored = new Kuestencode.Werkbank.Recepta.Domain.Dtos.DocumentFileDto { Id = Guid.NewGuid(), DocumentId = documentId, FileName = "evil.pdf" };
        var fileService = new Mock<IDocumentFileService>();
        fileService.Setup(s => s.UploadAsync(documentId, It.IsAny<Stream>(), "../../evil.pdf", "application/pdf")).ReturnsAsync(stored);
        fileService.Setup(s => s.DownloadAsync(stored.Id))
            .ReturnsAsync(((Stream)new MemoryStream(), "evil.pdf", "application/pdf"));
        var ocr = new Mock<IOcrService>();
        var controller = new FilesController(fileService.Object, Mock.Of<IDocumentService>(), ocr.Object,
            NullLogger<FilesController>.Instance);

        var result = await controller.Upload(documentId, FormFile("../../evil.pdf", "application/pdf"));

        result.Result.Should().BeOfType<CreatedResult>();
        ocr.Verify(o => o.ExtractTextAsync(It.IsAny<Stream>(), "evil.pdf"), Times.Once);
    }
}
