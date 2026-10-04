namespace Kuestencode.Shared.Contracts.Feedback;

public enum FeedbackType
{
    Bug = 0,
    Wunsch = 1
}

public enum FeedbackStatus
{
    Neu = 0,
    Eingeordnet = 1,
    InArbeit = 2,
    Erledigt = 3,
    Abgelehnt = 4
}

public enum FeedbackCommentAuthor
{
    Kunde = 0,
    Hub = 1
}
