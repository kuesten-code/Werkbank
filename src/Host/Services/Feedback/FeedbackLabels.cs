using Kuestencode.Shared.Contracts.Feedback;
using Kuestencode.Werkbank.Host.Models.Feedback;
using MudBlazor;

namespace Kuestencode.Werkbank.Host.Services.Feedback;

public static class FeedbackLabels
{
    public static string Of(FeedbackStatus status) => status switch
    {
        FeedbackStatus.Neu => "Neu",
        FeedbackStatus.Eingeordnet => "Eingeordnet",
        FeedbackStatus.InArbeit => "In Arbeit",
        FeedbackStatus.Erledigt => "Erledigt",
        FeedbackStatus.Abgelehnt => "Abgelehnt",
        _ => status.ToString()
    };

    public static Color ColorOf(FeedbackStatus status) => status switch
    {
        FeedbackStatus.Neu => Color.Info,
        FeedbackStatus.Eingeordnet => Color.Primary,
        FeedbackStatus.InArbeit => Color.Warning,
        FeedbackStatus.Erledigt => Color.Success,
        FeedbackStatus.Abgelehnt => Color.Default,
        _ => Color.Default
    };

    public static string Of(FeedbackType type) => type == FeedbackType.Bug ? "Bug" : "Wunsch";

    public static Color ColorOf(FeedbackType type) => type == FeedbackType.Bug ? Color.Error : Color.Secondary;

    public static string Of(FeedbackRole role) => role switch
    {
        FeedbackRole.Aus => "Aus",
        FeedbackRole.Client => "Kunde (Meldungen an einen Hub senden)",
        FeedbackRole.Hub => "Hub (Meldungen empfangen und bearbeiten)",
        _ => role.ToString()
    };

    public static string Of(FeedbackSyncState state) => state switch
    {
        FeedbackSyncState.Ausstehend => "Wird gesendet …",
        FeedbackSyncState.Gesendet => "Gesendet",
        FeedbackSyncState.Fehlgeschlagen => "Senden fehlgeschlagen",
        _ => state.ToString()
    };
}
