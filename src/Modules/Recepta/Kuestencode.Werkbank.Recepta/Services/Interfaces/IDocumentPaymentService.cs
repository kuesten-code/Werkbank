using Kuestencode.Shared.Contracts.Recepta;
using Kuestencode.Werkbank.Recepta.Domain.Entities;

namespace Kuestencode.Werkbank.Recepta.Services.Interfaces;

public interface IDocumentPaymentService
{
    Task<IEnumerable<DocumentPayment>> GetZahlungenAsync(Guid documentId);
    Task ZahlungErfassenAsync(Guid documentId, decimal betrag, DateOnly datum, string? notiz);
    Task ZahlungLoeschenAsync(Guid zahlungId);
    Task<IEnumerable<ReceptaPaymentDto>> GetEuerPaymentsAsync(DateOnly von, DateOnly bis);
}
