namespace Kuestencode.Werkbank.Host.Services.Feedback;

/// <summary>Eine hochgeladene Datei, bevor Typ und Größe geprüft sind.</summary>
public record FeedbackUpload(string FileName, byte[] Content);

/// <summary>Eine geprüfte Datei: Typ anhand der Datei-Signatur erkannt, Name bereinigt.</summary>
public record ValidatedFeedbackUpload(string FileName, string ContentType, string Extension, byte[] Content);
