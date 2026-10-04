namespace Kuestencode.Shared.Contracts.Feedback;

/// <summary>Ob auf dieser Werkbank Meldungen erfasst werden können (Rolle Kunde).</summary>
public class FeedbackStatusDto
{
    public bool ReportingEnabled { get; set; }
}
