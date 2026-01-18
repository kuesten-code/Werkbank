namespace Kuestencode.Core.Interfaces;

/// <summary>
/// Core interface for PDF generation operations.
/// Module-specific implementations can extend this for their own PDF needs.
/// </summary>
public interface IPdfService
{
    /// <summary>
    /// Generates a PDF document and returns the bytes.
    /// </summary>
    /// <param name="documentId">The ID of the document to generate PDF for</param>
    /// <returns>PDF file content as byte array</returns>
    byte[] GeneratePdf(int documentId);

    /// <summary>
    /// Generates a PDF and saves it to a file.
    /// </summary>
    /// <param name="documentId">The ID of the document to generate PDF for</param>
    /// <returns>The file name of the saved PDF</returns>
    Task<string> GenerateAndSaveAsync(int documentId);
}
