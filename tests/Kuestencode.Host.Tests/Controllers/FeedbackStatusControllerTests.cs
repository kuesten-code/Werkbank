using FluentAssertions;
using Kuestencode.Shared.Contracts.Feedback;
using Kuestencode.Werkbank.Host.Controllers;
using Kuestencode.Werkbank.Host.Models.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Kuestencode.Host.Tests.Controllers;

public class FeedbackStatusControllerTests
{
    [Theory]
    [InlineData(FeedbackRole.Client, true)]
    [InlineData(FeedbackRole.Hub, false)]
    [InlineData(FeedbackRole.Aus, false)]
    public void Get_MeldenNurAlsKundeMoeglich(FeedbackRole role, bool expected)
    {
        var modeState = new FeedbackModeState();
        modeState.SetRole(role);

        var result = new FeedbackStatusController(modeState).Get();

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<FeedbackStatusDto>()
            .Which.ReportingEnabled.Should().Be(expected);
    }
}
