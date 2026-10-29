using Kuestencode.Werkbank.Locatio.Domain.Dtos;
using Kuestencode.Werkbank.Locatio.Domain.Entities;

namespace Kuestencode.Werkbank.Locatio.Services;

public interface IUebergabeprotokollService
{
    Task<List<Uebergabeprotokoll>> GetByEinheitAsync(Guid einheitId);

    /// <summary>Ein neues Protokoll braucht eine Datei; beim Bearbeiten ersetzt eine Datei die bisherige.</summary>
    Task<Uebergabeprotokoll> SaveAsync(Uebergabeprotokoll protokoll, DateiUpload? datei);

    Task DeleteAsync(Guid id);
}
