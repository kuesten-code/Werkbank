using FluentAssertions;
using Kuestencode.Werkbank.Host.Services.Feedback;
using Xunit;

namespace Kuestencode.Host.Tests.Services.Feedback;

public class FeedbackUploadPolicyTests
{
    public static TheoryData<byte[], string, string> AllowedFiles => new()
    {
        { FeedbackTestData.Png, "image/png", ".png" },
        { FeedbackTestData.Jpeg, "image/jpeg", ".jpg" },
        { FeedbackTestData.Webp, "image/webp", ".webp" },
        { FeedbackTestData.Pdf, "application/pdf", ".pdf" }
    };

    [Theory]
    [MemberData(nameof(AllowedFiles))]
    public void Validate_ErlaubteTypen_WerdenAnhandDerSignaturErkannt(byte[] content, string contentType, string extension)
    {
        var (errors, files) = FeedbackUploadPolicy.Validate(new[] { new FeedbackUpload("datei.bin", content) }, 5, 20);

        errors.Should().BeEmpty();
        files.Single().ContentType.Should().Be(contentType);
        files.Single().Extension.Should().Be(extension);
    }

    [Fact]
    public void Validate_ExeMitPngEndung_WirdAbgelehnt()
    {
        var (errors, files) = FeedbackUploadPolicy.Validate(new[] { new FeedbackUpload("bild.png", FeedbackTestData.Exe) }, 5, 20);

        errors.Should().ContainSingle().Which.Should().Contain("keinen erlaubten Typ");
        files.Should().BeEmpty();
    }

    [Fact]
    public void Validate_LeereDatei_WirdAbgelehnt()
    {
        var (errors, _) = FeedbackUploadPolicy.Validate(new[] { new FeedbackUpload("leer.png", Array.Empty<byte>()) }, 5, 20);

        errors.Should().ContainSingle().Which.Should().Contain("ist leer");
    }

    [Fact]
    public void Validate_DateiUeberEinzellimit_WirdAbgelehnt()
    {
        var big = FeedbackTestData.Png.Concat(new byte[1024 * 1024]).ToArray();

        var (errors, _) = FeedbackUploadPolicy.Validate(new[] { new FeedbackUpload("gross.png", big) }, 1, 20);

        errors.Should().ContainSingle().Which.Should().Contain("größer als 1 MB");
    }

    [Fact]
    public void Validate_SummeUeberMeldungslimit_WirdAbgelehnt()
    {
        var almostOneMb = FeedbackTestData.Png.Concat(new byte[900 * 1024]).ToArray();
        var uploads = Enumerable.Range(0, 3).Select(i => new FeedbackUpload($"{i}.png", almostOneMb)).ToList();

        var (errors, _) = FeedbackUploadPolicy.Validate(uploads, 1, 2);

        errors.Should().ContainSingle().Which.Should().Contain("zusammen");
    }

    [Fact]
    public void Validate_ZuVieleDateien_WirdAbgelehnt()
    {
        var uploads = Enumerable.Range(0, FeedbackUploadPolicy.MaxFiles + 1)
            .Select(i => new FeedbackUpload($"{i}.png", FeedbackTestData.Png)).ToList();

        var (errors, _) = FeedbackUploadPolicy.Validate(uploads, 5, 20);

        errors.Should().Contain(e => e.Contains("Höchstens"));
    }

    [Theory]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData("C:\\temp\\bild.png", "bild.png")]
    [InlineData("..", "datei")]
    [InlineData("", "datei")]
    [InlineData(null, "datei")]
    [InlineData("a\u0001b.png", "a_b.png")]
    public void SanitizeFileName_EntferntPfadanteileUndSteuerzeichen(string? input, string expected)
    {
        FeedbackUploadPolicy.SanitizeFileName(input).Should().Be(expected);
    }

    [Fact]
    public void SanitizeFileName_KuerztAuf200Zeichen()
    {
        FeedbackUploadPolicy.SanitizeFileName(new string('a', 300) + ".png").Should().HaveLength(200);
    }
}
