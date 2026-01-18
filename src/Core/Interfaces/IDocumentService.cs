namespace Kuestencode.Core.Interfaces;

/// <summary>
/// Core interface for document-related operations.
/// Can be used for invoices, offers, contracts, etc.
/// </summary>
public interface IDocumentService<TDocument> where TDocument : class
{
    /// <summary>
    /// Gets all documents.
    /// </summary>
    Task<List<TDocument>> GetAllAsync();

    /// <summary>
    /// Gets a document by ID.
    /// </summary>
    Task<TDocument?> GetByIdAsync(int id);

    /// <summary>
    /// Creates a new document.
    /// </summary>
    Task<TDocument> CreateAsync(TDocument document);

    /// <summary>
    /// Updates an existing document.
    /// </summary>
    Task UpdateAsync(TDocument document);

    /// <summary>
    /// Deletes a document.
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// Generates a unique document number.
    /// </summary>
    Task<string> GenerateDocumentNumberAsync();
}
