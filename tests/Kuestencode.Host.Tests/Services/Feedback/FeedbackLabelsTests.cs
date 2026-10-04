using FluentAssertions;
using Kuestencode.Shared.Contracts.Feedback;
using Kuestencode.Werkbank.Host.Models.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback;
using MudBlazor;
using Xunit;

namespace Kuestencode.Host.Tests.Services.Feedback;

public class FeedbackLabelsTests
{
    [Theory]
    [InlineData(FeedbackStatus.Neu, "Neu", Color.Info)]
    [InlineData(FeedbackStatus.Eingeordnet, "Eingeordnet", Color.Primary)]
    [InlineData(FeedbackStatus.InArbeit, "In Arbeit", Color.Warning)]
    [InlineData(FeedbackStatus.Erledigt, "Erledigt", Color.Success)]
    [InlineData(FeedbackStatus.Abgelehnt, "Abgelehnt", Color.Default)]
    [InlineData((FeedbackStatus)99, "99", Color.Default)]
    public void Status_HatTextUndFarbe(FeedbackStatus status, string label, Color color)
    {
        FeedbackLabels.Of(status).Should().Be(label);
        FeedbackLabels.ColorOf(status).Should().Be(color);
    }

    [Theory]
    [InlineData(FeedbackType.Bug, "Bug", Color.Error)]
    [InlineData(FeedbackType.Wunsch, "Wunsch", Color.Secondary)]
    public void Typ_HatTextUndFarbe(FeedbackType type, string label, Color color)
    {
        FeedbackLabels.Of(type).Should().Be(label);
        FeedbackLabels.ColorOf(type).Should().Be(color);
    }

    [Theory]
    [InlineData(FeedbackRole.Aus, "Aus")]
    [InlineData(FeedbackRole.Client, "Kunde")]
    [InlineData(FeedbackRole.Hub, "Hub")]
    [InlineData((FeedbackRole)99, "99")]
    public void Rolle_HatText(FeedbackRole role, string expectedStart)
    {
        FeedbackLabels.Of(role).Should().StartWith(expectedStart);
    }

    [Theory]
    [InlineData(FeedbackSyncState.Ausstehend, "Wird gesendet")]
    [InlineData(FeedbackSyncState.Gesendet, "Gesendet")]
    [InlineData(FeedbackSyncState.Fehlgeschlagen, "Senden fehlgeschlagen")]
    [InlineData((FeedbackSyncState)99, "99")]
    public void SyncZustand_HatText(FeedbackSyncState state, string expectedStart)
    {
        FeedbackLabels.Of(state).Should().StartWith(expectedStart);
    }
}
