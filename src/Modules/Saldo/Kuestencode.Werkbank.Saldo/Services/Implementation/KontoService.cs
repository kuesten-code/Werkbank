using Kuestencode.Werkbank.Saldo.Data.Repositories;
using Kuestencode.Werkbank.Saldo.Domain.Dtos;
using Kuestencode.Werkbank.Saldo.Domain.Entities;
using Kuestencode.Werkbank.Saldo.Domain.Enums;

namespace Kuestencode.Werkbank.Saldo.Services;

public class KontoService : IKontoService
{
    private readonly IKontoRepository _kontoRepo;
    private readonly IKategorieKontoMappingRepository _mappingRepo;

    public KontoService(IKontoRepository kontoRepo, IKategorieKontoMappingRepository mappingRepo)
    {
        _kontoRepo = kontoRepo;
        _mappingRepo = mappingRepo;
    }

    public async Task<List<KontoDto>> GetKontenAsync(string? kontenrahmen = null)
    {
        var konten = await _kontoRepo.GetAllAsync(kontenrahmen);
        return konten.Select(MapKontoToDto).ToList();
    }

    public async Task<List<KategorieKontoMappingDto>> GetMappingsAsync(string? kontenrahmen = null)
    {
        var mappings = await _mappingRepo.GetAllAsync(kontenrahmen);
        return mappings.Select(MapMappingToDto).ToList();
    }

    public async Task<KategorieKontoMappingDto?> UpdateMappingAsync(Guid id, UpdateKategorieKontoMappingDto dto)
    {
        var mapping = await _mappingRepo.GetByIdAsync(id);
        if (mapping == null) return null;

        // Verify the target Konto exists in the same Kontenrahmen
        if (!await _kontoRepo.ExistsAsync(mapping.Kontenrahmen, dto.KontoNummer))
            throw new InvalidOperationException($"Konto {dto.KontoNummer} nicht im Kontenrahmen {mapping.Kontenrahmen} gefunden.");

        mapping.KontoNummer = dto.KontoNummer;
        mapping.IsCustom = true;
        mapping = await _mappingRepo.UpdateAsync(mapping);

        // Reload with Konto navigation
        var updated = await _mappingRepo.GetByIdAsync(id);
        return updated == null ? null : MapMappingToDto(updated);
    }

    public async Task<KontoDto> CreateKontoAsync(CreateKontoDto dto)
    {
        if (!Enum.TryParse<KontoTyp>(dto.KontoTyp, out var kontoTyp))
            throw new InvalidOperationException($"Unbekannter Kontotyp: {dto.KontoTyp}");

        if (await _kontoRepo.ExistsAsync(dto.Kontenrahmen, dto.KontoNummer))
            throw new InvalidOperationException($"Konto {dto.KontoNummer} existiert bereits im Kontenrahmen {dto.Kontenrahmen}.");

        var konto = new Konto
        {
            Id = Guid.NewGuid(),
            Kontenrahmen = dto.Kontenrahmen,
            KontoNummer = dto.KontoNummer,
            KontoBezeichnung = dto.KontoBezeichnung,
            KontoTyp = kontoTyp,
            UstSatz = dto.UstSatz,
            IsActive = true
        };

        konto = await _kontoRepo.AddAsync(konto);
        return MapKontoToDto(konto);
    }

    public async Task<KontoDto?> UpdateKontoAsync(Guid id, UpdateKontoDto dto)
    {
        var konto = await _kontoRepo.GetByIdAsync(id);
        if (konto == null) return null;

        if (!Enum.TryParse<KontoTyp>(dto.KontoTyp, out var kontoTyp))
            throw new InvalidOperationException($"Unbekannter Kontotyp: {dto.KontoTyp}");

        konto.KontoBezeichnung = dto.KontoBezeichnung;
        konto.KontoTyp = kontoTyp;
        konto.UstSatz = dto.UstSatz;
        konto.IsActive = dto.IsActive;

        konto = await _kontoRepo.UpdateAsync(konto);
        return MapKontoToDto(konto);
    }

    private static KontoDto MapKontoToDto(Konto k) => new()
    {
        Id = k.Id,
        Kontenrahmen = k.Kontenrahmen,
        KontoNummer = k.KontoNummer,
        KontoBezeichnung = k.KontoBezeichnung,
        KontoTyp = k.KontoTyp.ToString(),
        UstSatz = k.UstSatz,
        IsActive = k.IsActive
    };

    private static KategorieKontoMappingDto MapMappingToDto(KategorieKontoMapping m) => new()
    {
        Id = m.Id,
        Kontenrahmen = m.Kontenrahmen,
        ReceiptaKategorie = m.ReceiptaKategorie,
        KontoNummer = m.KontoNummer,
        KontoBezeichnung = m.Konto?.KontoBezeichnung ?? string.Empty,
        IsCustom = m.IsCustom
    };
}
