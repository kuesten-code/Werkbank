using Kuestencode.Faktura.Models;
using QuestPDF.Infrastructure;

namespace Kuestencode.Faktura.Services.Pdf;

/// <summary>
/// Interface for PDF layout renderers. Each layout style implements this interface.
/// </summary>
public interface IPdfLayoutRenderer
{
    /// <summary>
    /// Renders the header section of the PDF.
    /// </summary>
    void RenderHeader(IContainer container, Invoice invoice, Company company);

    /// <summary>
    /// Renders the content section of the PDF including items table and summary.
    /// </summary>
    void RenderContent(IContainer container, Invoice invoice, Company company);

    /// <summary>
    /// Renders the footer section of the PDF.
    /// </summary>
    void RenderFooter(IContainer container, Company company);
}
