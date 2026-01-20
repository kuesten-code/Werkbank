using Kuestencode.Core.Models;

namespace Kuestencode.Core.Interfaces;

/// <summary>
/// Generische PDF-Engine für plattformweite PDF-Generierung.
/// Module registrieren eigene IPdfDocumentRenderer für ihre Dokumente.
/// </summary>
public interface IPdfEngine
{
    /// <summary>
    /// Generiert ein PDF für das angegebene Dokument.
    /// </summary>
    /// <typeparam name="TDocument">Der Typ des Dokuments</typeparam>
    /// <param name="document">Das zu rendernde Dokument</param>
    /// <param name="rendererName">Name des Renderers (z.B. "invoice-klar", "pflichtenheft")</param>
    /// <returns>PDF als Byte-Array</returns>
    byte[] GeneratePdf<TDocument>(TDocument document, string rendererName);

    /// <summary>
    /// Generiert ein PDF und speichert es.
    /// </summary>
    /// <typeparam name="TDocument">Der Typ des Dokuments</typeparam>
    /// <param name="document">Das zu rendernde Dokument</param>
    /// <param name="rendererName">Name des Renderers</param>
    /// <param name="outputPath">Ausgabepfad</param>
    /// <returns>Der vollständige Pfad zur gespeicherten Datei</returns>
    Task<string> GenerateAndSaveAsync<TDocument>(TDocument document, string rendererName, string outputPath);

    /// <summary>
    /// Prüft ob ein Renderer für den angegebenen Namen registriert ist.
    /// </summary>
    bool HasRenderer(string rendererName);

    /// <summary>
    /// Gibt alle registrierten Renderer-Namen zurück.
    /// </summary>
    IEnumerable<string> GetRegisteredRenderers();
}

/// <summary>
/// Interface für PDF-Dokument-Renderer.
/// Module registrieren ihre eigenen Renderer für spezifische Dokumenttypen.
/// </summary>
/// <typeparam name="TDocument">Der Typ des Dokuments, das gerendert wird</typeparam>
public interface IPdfDocumentRenderer<in TDocument>
{
    /// <summary>
    /// Der eindeutige Name des Renderers (z.B. "invoice-klar", "invoice-betont").
    /// </summary>
    string RendererName { get; }

    /// <summary>
    /// Rendert das Dokument zu einem PDF.
    /// </summary>
    /// <param name="document">Das zu rendernde Dokument</param>
    /// <param name="company">Die Firmendaten für Branding/Einstellungen</param>
    /// <returns>PDF als Byte-Array</returns>
    byte[] Render(TDocument document, Company company);
}
