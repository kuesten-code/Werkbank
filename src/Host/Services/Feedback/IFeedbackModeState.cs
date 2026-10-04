using Kuestencode.Werkbank.Host.Models.Feedback;

namespace Kuestencode.Werkbank.Host.Services.Feedback;

/// <summary>
/// Prozessweit gecachte Feedback-Rolle. Die Navigation (Singleton) und der API-Key-Filter
/// brauchen sie synchron und bei jedem Request — ein DB-Zugriff pro Aufruf wäre unnötig.
/// </summary>
public interface IFeedbackModeState
{
    FeedbackRole Role { get; }
    void SetRole(FeedbackRole role);
}

public class FeedbackModeState : IFeedbackModeState
{
    private volatile int _role;

    public FeedbackRole Role => (FeedbackRole)_role;

    public void SetRole(FeedbackRole role) => _role = (int)role;
}
