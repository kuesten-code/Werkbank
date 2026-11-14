using System.Text;
using FluentAssertions;
using Kuestencode.Werkbank.Recepta.Data.Repositories;
using Kuestencode.Werkbank.Recepta.Domain.Entities;
using Kuestencode.Werkbank.Recepta.Domain.Enums;
using Kuestencode.Werkbank.Recepta.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Kuestencode.Werkbank.Recepta.Tests.Services;

/// <summary>
/// Security-Audit H3: der Client-Dateiname darf nie aus dem Belegordner herausführen, und nur erlaubte
/// Typen bis zur Maximalgröße werden gespeichert.
/// </summary>
public sealed class DocumentFileUploadSecurityTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"recepta-tests-{Guid.NewGuid():N}");
    private readonly string _storage;
    private readonly Mock<IDocumentFileRepository> _fileRepository = new();
    private readonly Mock<IDocumentRepository> _documentRepository = new();
    private readonly DocumentFileService _service;
    private readonly Document _document = new()
    {
        Id = Guid.NewGuid(), DocumentNumber = "ER-2026-0001", InvoiceNumber = "RE-1", Status = DocumentStatus.Draft
    };

    public DocumentFileUploadSecurityTests()
    {
        _storage = Path.Combine(_root, "storage");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["FileStorage:Path"] = _storage })
            .Build();
        _documentRepository.Setup(r => r.GetByIdAsync(_document.Id)).ReturnsAsync(_document);
        _service = new DocumentFileService(_fileRepository.Object, _documentRepository.Object,
            NullLogger<DocumentFileService>.Instance, configuration);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private string DocumentDirectory =>
        Path.Combine(_storage, DateTime.UtcNow.Year.ToString(), _document.Id.ToString());

    private static MemoryStream Content(string text = "%PDF-1.4") => new(Encoding.UTF8.GetBytes(text));

    [Theory]
    [InlineData("../../../keys/evil.pdf", "evil.pdf")]
    [InlineData("..\\..\\keys\\evil.pdf", "evil.pdf")]
    [InlineData("/etc/cron.d/evil.pdf", "evil.pdf")]
    [InlineData("C:\\Windows\\evil.pdf", "evil.pdf")]
    [InlineData("..", UploadPolicy.FallbackFileName)]
    [InlineData("", UploadPolicy.FallbackFileName)]
    public async Task UploadAsync_PfadImDateinamen_SpeichertNurImBelegordner(string fileName, string expectedName)
    {
        var result = await _service.UploadAsync(_document.Id, Content(), fileName, "application/pdf");

        result.FileName.Should().Be(expectedName);
        File.Exists(Path.Combine(DocumentDirectory, expectedName)).Should().BeTrue();
        Directory.GetFiles(_root, "*", SearchOption.AllDirectories)
            .Should().OnlyContain(f => f.StartsWith(DocumentDirectory));
    }

    [Fact]
    public async Task UploadAsync_GleicherNameZweimal_HaengtZaehlerAn()
    {
        await _service.UploadAsync(_document.Id, Content(), "beleg.pdf", "application/pdf");
        var second = await _service.UploadAsync(_document.Id, Content(), "beleg.pdf", "application/pdf");

        _fileRepository.Verify(r => r.AddAsync(It.Is<DocumentFile>(f => f.StoragePath.EndsWith("beleg_1.pdf"))), Times.Once);
        second.FileName.Should().Be("beleg.pdf");
    }

    [Theory]
    [InlineData("text/html")]
    [InlineData("image/svg+xml")]
    [InlineData("application/javascript")]
    [InlineData("")]
    public async Task UploadAsync_NichtErlaubterTyp_WirdAbgelehntOhneDatei(string contentType)
    {
        var act = () => _service.UploadAsync(_document.Id, Content("<script>alert(1)</script>"), "x.html", contentType);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*erlaubt*");
        Directory.Exists(_root).Should().BeFalse();
    }

    [Theory]
    [InlineData("image/jpg", "image/jpeg")]
    [InlineData("TEXT/XML", "application/xml")]
    [InlineData("application/pdf", "application/pdf")]
    public async Task UploadAsync_ErlaubterTyp_WirdVereinheitlichtGespeichert(string contentType, string expected)
    {
        var result = await _service.UploadAsync(_document.Id, Content(), "beleg", contentType);

        result.ContentType.Should().Be(expected);
    }

    [Fact]
    public async Task UploadAsync_UeberMaximalgroesse_WirdAbgelehntUndTeildateiEntfernt()
    {
        var tooLarge = new MemoryStream(new byte[UploadPolicy.MaxFileSize + 1]);

        var act = () => _service.UploadAsync(_document.Id, tooLarge, "gross.pdf", "application/pdf");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*maximal*");
        Directory.GetFiles(DocumentDirectory).Should().BeEmpty();
        _fileRepository.Verify(r => r.AddAsync(It.IsAny<DocumentFile>()), Times.Never);
    }
}
