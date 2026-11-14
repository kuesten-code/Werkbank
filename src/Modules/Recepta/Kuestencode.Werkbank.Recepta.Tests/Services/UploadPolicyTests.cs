using FluentAssertions;
using Kuestencode.Werkbank.Recepta.Services;
using Xunit;

namespace Kuestencode.Werkbank.Recepta.Tests.Services;

public class UploadPolicyTests
{
    [Theory]
    [InlineData("rechnung.pdf", "rechnung.pdf")]
    [InlineData("a/b/c.pdf", "c.pdf")]
    [InlineData("a\\b\\c.pdf", "c.pdf")]
    [InlineData("  name.pdf  ", "name.pdf")]
    [InlineData("na\u0000me.pdf", "name.pdf")]
    [InlineData(null, UploadPolicy.FallbackFileName)]
    [InlineData(".", UploadPolicy.FallbackFileName)]
    [InlineData("dir/", UploadPolicy.FallbackFileName)]
    public void SanitizeFileName_EntferntPfadanteileUndSteuerzeichen(string? input, string expected)
    {
        UploadPolicy.SanitizeFileName(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("application/pdf", true)]
    [InlineData("image/png", true)]
    [InlineData("application/xml", false)]
    [InlineData("application/octet-stream", false)]
    public void MayDisplayInline_NurPdfUndBilder(string contentType, bool expected)
    {
        UploadPolicy.MayDisplayInline(contentType).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("text/html")]
    public void NormalizeContentType_Unbekannt_LiefertNull(string? contentType)
    {
        UploadPolicy.NormalizeContentType(contentType).Should().BeNull();
    }
}
