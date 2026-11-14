using FluentAssertions;
using Kuestencode.Core.Models;
using Kuestencode.Core.Services;
using Xunit;

namespace Kuestencode.Core.Tests.Services;

/// <summary>
/// Security-Audit N2: Firmen- und Anredetexte landen kodiert im HTML-Mailbody.
/// </summary>
public class EmailTemplateRendererTests
{
    private static Company Company(string name = "Nordlicht GmbH") => new()
    {
        OwnerFullName = "Max Mustermann",
        BusinessName = name,
        EmailSignature = "Max <b>Mustermann</b>\nGeschäftsführer",
        EmailPrimaryColor = "#123456\" onload=\"alert(1)"
    };

    [Fact]
    public void WrapHtml_MarkupInFirmenUndAnredetexten_WirdKodiert()
    {
        var html = EmailTemplateRenderer.WrapHtml(Company("<script>alert(1)</script>"), "<p>Inhalt</p>",
            greeting: "Hallo <img src=x onerror=alert(1)>,", includeClosing: true);

        html.Should().NotContain("<script>").And.NotContain("<img").And.NotContain("<b>");
        html.Should().Contain("&lt;script&gt;alert(1)&lt;/script&gt;");
        html.Should().Contain("Hallo &lt;img src=x onerror=alert(1)&gt;,");
        html.Should().NotContain("\" onload=\"");
    }

    [Fact]
    public void WrapHtml_InhaltDesModulsBleibtHtml()
    {
        var html = EmailTemplateRenderer.WrapHtml(Company(), "<p>Inhalt</p>", greeting: null, includeClosing: false);

        html.Should().Contain("<p>Inhalt</p>");
    }

    [Fact]
    public void WrapHtml_ZeilenumbruecheUndUmlauteBleibenLesbar()
    {
        var html = EmailTemplateRenderer.WrapHtml(Company(), "", greeting: null, includeClosing: false);

        html.Should().Contain("Max &lt;b&gt;Mustermann&lt;/b&gt;<br>Geschäftsführer");
    }

    [Theory]
    [InlineData("a & b", "a &amp; b")]
    [InlineData("\"'", "&quot;&#39;")]
    [InlineData(null, "")]
    public void Html_KodiertNurHtmlRelevanteZeichen(string? input, string expected)
    {
        EmailTemplateRenderer.Html(input).Should().Be(expected);
    }
}
