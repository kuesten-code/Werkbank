using System.Net;
using Amazon.Runtime.Internal;
using Amazon.Runtime.Internal.Transform;
using FluentAssertions;
using Kuestencode.Werkbank.Host.Services;
using Moq;
using Xunit;

namespace Kuestencode.Host.Tests.Services;

public class ExceptionHelperTests
{
    [Fact]
    public void Describe_HttpErrorResponseException_LiestStatuscodeAusResponseStattGenerischerMessage()
    {
        // Das AWS SDK wirft HttpErrorResponseException ohne eigene Message ("Exception of type
        // '...' was thrown.") - die eigentliche Information steckt nur in .Response.StatusCode.
        var response = new Mock<IWebResponseData>();
        response.Setup(r => r.StatusCode).Returns(HttpStatusCode.Forbidden);
        var ex = new HttpErrorResponseException(response.Object);

        var result = ExceptionHelper.Describe(ex);

        result.Should().Contain("403").And.Contain("Forbidden");
    }

    [Fact]
    public void Describe_HttpErrorResponseExceptionAlsInnerException_WirdTrotzdemGefunden()
    {
        var response = new Mock<IWebResponseData>();
        response.Setup(r => r.StatusCode).Returns(HttpStatusCode.NotFound);
        var inner = new HttpErrorResponseException(response.Object);
        var outer = new InvalidOperationException("Wrapper", inner);

        var result = ExceptionHelper.Describe(outer);

        result.Should().Contain("404");
    }

    [Fact]
    public void Describe_NormaleException_FaelltAufInnerExceptionMessageZurueck()
    {
        var inner = new InvalidOperationException("Der eigentliche Grund");
        var outer = new Exception("Wrapper-Text", inner);

        var result = ExceptionHelper.Describe(outer);

        result.Should().Be("Der eigentliche Grund");
    }

    [Fact]
    public void Describe_ExceptionOhneInnerException_GibtEigeneMessageZurueck()
    {
        var ex = new InvalidOperationException("Alleinstehender Fehler");

        var result = ExceptionHelper.Describe(ex);

        result.Should().Be("Alleinstehender Fehler");
    }
}
