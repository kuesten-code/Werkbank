using Kuestencode.Shared.Contracts.Feedback;

namespace Kuestencode.Werkbank.Host.Services.Feedback;

/// <summary>
/// Pflichtfeld- und Längenregeln einer Meldung. Gilt am Hub (Systemgrenze) und im
/// Kunden-Formular, damit beide Seiten dieselben Regeln anwenden.
/// </summary>
public static class FeedbackReportValidator
{
    public const int TitleMaxLength = 200;
    public const int ModuleMaxLength = 100;
    public const int TextMaxLength = 10000;

    public static List<string> Validate(CreateFeedbackReportRequest request)
    {
        var errors = new List<string>();

        if (request.ClientReportId == Guid.Empty)
            errors.Add("ClientReportId fehlt.");
        if (!Enum.IsDefined(request.Type))
            errors.Add("Unbekannter Meldungstyp.");

        if (string.IsNullOrWhiteSpace(request.Title))
            errors.Add("Titel ist erforderlich.");
        else if (request.Title.Length > TitleMaxLength)
            errors.Add($"Titel darf höchstens {TitleMaxLength} Zeichen lang sein.");

        if (string.IsNullOrWhiteSpace(request.Actual))
            errors.Add(request.Type == FeedbackType.Bug ? "Ist-Zustand ist erforderlich." : "Beschreibung ist erforderlich.");
        else if (request.Actual.Length > TextMaxLength)
            errors.Add($"Beschreibung darf höchstens {TextMaxLength} Zeichen lang sein.");

        if (request.Expected?.Length > TextMaxLength)
            errors.Add($"Erwartung darf höchstens {TextMaxLength} Zeichen lang sein.");
        if (request.Module?.Length > ModuleMaxLength)
            errors.Add($"Modul darf höchstens {ModuleMaxLength} Zeichen lang sein.");

        if (request.Type == FeedbackType.Bug)
        {
            if (string.IsNullOrWhiteSpace(request.Module))
                errors.Add("Modul ist bei einem Bug erforderlich.");
            if (string.IsNullOrWhiteSpace(request.Expected))
                errors.Add("Erwartung ist bei einem Bug erforderlich.");
        }

        return errors;
    }

    public static List<string> ValidateComment(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string> { "Kommentar darf nicht leer sein." };
        if (text.Length > TextMaxLength)
            return new List<string> { $"Kommentar darf höchstens {TextMaxLength} Zeichen lang sein." };
        return new List<string>();
    }
}
