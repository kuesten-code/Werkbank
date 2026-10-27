using Kuestencode.Werkbank.Locatio.Domain.Dtos;
using Kuestencode.Werkbank.Locatio.Domain.Entities;

namespace Kuestencode.Werkbank.Locatio.Services;

public interface IKostenService
{
    /// <summary>Kostenposten eines Jahres (nach Zahlungsdatum), optional nur für ein Gebäude.</summary>
    Task<List<Kostenposten>> GetAsync(int jahr, Guid? gebaeudeId = null);

    Task<Kostenposten> SaveAsync(Kostenposten posten, DateiUpload? beleg = null);
    Task DeleteAsync(Guid id);
}
