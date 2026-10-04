namespace Kuestencode.Werkbank.Host.Services.Feedback;

public class FeedbackValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public FeedbackValidationException(IReadOnlyList<string> errors)
        : base(string.Join(" ", errors))
    {
        Errors = errors;
    }
}
