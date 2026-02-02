using QuestPDF.Infrastructure;

namespace Kuestencode.Shared.Pdf.Core;

/// <summary>
/// Interface für PDF-Dokument-Renderer.
/// Jedes Modul (Faktura, Offerte) implementiert dieses Interface.
/// </summary>
public interface IPdfDocumentRenderer
{
    /// <summary>
    /// Rendert den Header-Bereich des PDFs.
    /// </summary>
    void RenderHeader(IContainer container);

    /// <summary>
    /// Rendert den Hauptinhalt des PDFs (Positionen, Summen, etc.)
    /// </summary>
    void RenderContent(IContainer container);

    /// <summary>
    /// Rendert den Footer-Bereich des PDFs.
    /// </summary>
    void RenderFooter(IContainer container);
}
