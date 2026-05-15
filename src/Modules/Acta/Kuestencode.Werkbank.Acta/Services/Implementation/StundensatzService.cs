using Kuestencode.Shared.ApiClients;
using Kuestencode.Werkbank.Acta.Data.Repositories;
using Kuestencode.Werkbank.Acta.Domain.Dtos;
using Kuestencode.Werkbank.Acta.Domain.Entities;

namespace Kuestencode.Werkbank.Acta.Services;

public class StundensatzService : IStundensatzService
{
    private readonly IProjektStundensatzRepository _repo;
    private readonly IProjectRepository _projectRepo;
    private readonly IRapportApiClient _rapportClient;
    private readonly IReceptaApiClient _receptaClient;

    public StundensatzService(
        IProjektStundensatzRepository repo,
        IProjectRepository projectRepo,
        IRapportApiClient rapportClient,
        IReceptaApiClient receptaClient)
    {
        _repo = repo;
        _projectRepo = projectRepo;
        _rapportClient = rapportClient;
        _receptaClient = receptaClient;
    }

    public async Task<List<StundensatzDto>> GetStundensaetzeAsync(Guid projektId)
    {
        var gespeichert = await _repo.GetByProjektIdAsync(projektId);
        return gespeichert
            .Select(s => new StundensatzDto
            {
                RolleId = s.RolleId,
                RolleName = s.RolleName,
                Stundensatz = s.Stundensatz
            })
            .ToList();
    }

    public async Task UpsertStundensatzAsync(Guid projektId, int rolleId, string rolleName, decimal stundensatz)
    {
        var existing = await _repo.GetByProjektIdAndRolleAsync(projektId, rolleId);

        if (existing != null)
        {
            existing.RolleName = rolleName;
            existing.Stundensatz = stundensatz;
            await _repo.UpdateAsync(existing);
        }
        else
        {
            await _repo.AddAsync(new ProjektStundensatz
            {
                Id = Guid.NewGuid(),
                ProjectId = projektId,
                RolleId = rolleId,
                RolleName = rolleName,
                Stundensatz = stundensatz
            });
        }
    }

    public async Task<ProjektAbrechnung> GetProjektAbrechnungAsync(Guid projektId)
    {
        var abrechnung = new ProjektAbrechnung();

        var saetze = await _repo.GetByProjektIdAsync(projektId);
        var satzLookup = saetze.ToDictionary(s => s.RolleId);

        var project = await _projectRepo.GetByIdAsync(projektId);
        if (project?.ExternalId.HasValue == true)
        {
            try
            {
                var stunden = await _rapportClient.GetProjectHoursByTypeAsync(project.ExternalId.Value);
                if (stunden != null)
                {
                    foreach (var rolleStunden in stunden.StundenByRolle)
                    {
                        satzLookup.TryGetValue(rolleStunden.RolleId, out var satz);
                        abrechnung.Positionen.Add(new AbrechnungsPosition
                        {
                            RolleId = rolleStunden.RolleId,
                            RolleName = rolleStunden.RolleName,
                            Stunden = rolleStunden.Stunden,
                            Stundensatz = satz?.Stundensatz ?? 0
                        });
                    }

                    foreach (var rolleStunden in stunden.InvoicedStundenByRolle)
                    {
                        satzLookup.TryGetValue(rolleStunden.RolleId, out var satz);
                        abrechnung.BerechnetePositionen.Add(new AbrechnungsPosition
                        {
                            RolleId = rolleStunden.RolleId,
                            RolleName = rolleStunden.RolleName,
                            Stunden = rolleStunden.Stunden,
                            Stundensatz = satz?.Stundensatz ?? 0
                        });
                    }
                }
            }
            catch
            {
                // Rapport nicht erreichbar → Stunden bleiben 0
            }
        }

        // Rollen mit Stundensatz aber ohne Rapport-Stunden auch anzeigen
        foreach (var satz in saetze.Where(s => abrechnung.Positionen.All(p => p.RolleId != s.RolleId)))
        {
            abrechnung.Positionen.Add(new AbrechnungsPosition
            {
                RolleId = satz.RolleId,
                RolleName = satz.RolleName,
                Stunden = 0,
                Stundensatz = satz.Stundensatz
            });
        }

        try
        {
            var receptaId = project?.ExternalId.HasValue == true
                ? DeterministicGuid(project.ExternalId.Value)
                : projektId;
            var expenses = await _receptaClient.GetProjectExpensesAsync(receptaId);
            if (expenses != null)
            {
                abrechnung.MaterialNetto = expenses.TotalNet;
                abrechnung.MaterialBrutto = expenses.TotalGross;
            }
        }
        catch
        {
            // Recepta nicht erreichbar → Material bleibt 0
        }

        return abrechnung;
    }

    public async Task MarkProjectTimeEntriesAsInvoicedAsync(int externalProjectId)
    {
        await _rapportClient.MarkProjectTimeEntriesAsInvoicedAsync(externalProjectId);
    }

    private static Guid DeterministicGuid(int id)
    {
        var bytes = new byte[16];
        BitConverter.GetBytes(id).CopyTo(bytes, 0);
        bytes[4] = 0xAC;
        bytes[5] = 0x7A;
        return new Guid(bytes);
    }
}
