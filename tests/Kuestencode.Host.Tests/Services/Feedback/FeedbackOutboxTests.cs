using FluentAssertions;
using Kuestencode.Werkbank.Host.Services.Feedback.Client;
using Xunit;

namespace Kuestencode.Host.Tests.Services.Feedback;

public class FeedbackOutboxTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 4)]
    [InlineData(6, 32)]
    [InlineData(7, 60)]
    [InlineData(50, 60)]
    public void Backoff_VerdoppeltBisMaximal60Minuten(int failedAttempts, int expectedMinutes)
    {
        FeedbackBackoff.DelayAfter(failedAttempts).Should().Be(TimeSpan.FromMinutes(expectedMinutes));
    }

    [Fact]
    public async Task Signal_WecktWartendenSofortAuf()
    {
        var signal = new FeedbackOutboxSignal();
        signal.Signal();
        signal.Signal();

        (await signal.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None)).Should().BeTrue();
        (await signal.WaitAsync(TimeSpan.FromMilliseconds(10), CancellationToken.None)).Should().BeFalse();
    }
}
