using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Host.Services;

public class TeamMemberService : ITeamMemberService
{
    private readonly HostDbContext _context;
    private readonly ILogger<TeamMemberService> _logger;

    public TeamMemberService(HostDbContext context, ILogger<TeamMemberService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<TeamMember>> GetAllAsync(bool includeInactive = false)
    {
        try
        {
            var query = _context.TeamMembers.AsNoTracking();
            if (!includeInactive)
            {
                query = query.Where(m => m.IsActive);
            }

            return await query
                .OrderBy(m => m.DisplayName)
                .ToListAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Abrufen der Mitarbeiterliste");
            throw;
        }
    }

    public async Task<TeamMember?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _context.TeamMembers.FirstOrDefaultAsync(m => m.Id == id).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Abrufen des Mitarbeiters {TeamMemberId}", id);
            throw;
        }
    }

    public async Task<TeamMember> CreateAsync(TeamMember member)
    {
        try
        {
            if (member.Id == Guid.Empty)
            {
                member.Id = Guid.NewGuid();
            }

            if (string.IsNullOrWhiteSpace(member.DisplayName))
            {
                throw new InvalidOperationException("Name ist erforderlich.");
            }

            if (!string.IsNullOrWhiteSpace(member.Email))
            {
                var emailExists = await _context.TeamMembers
                    .AnyAsync(m => m.Email != null && m.Email == member.Email)
                    .ConfigureAwait(false);
                if (emailExists)
                {
                    throw new InvalidOperationException($"Email '{member.Email}' existiert bereits.");
                }
            }

            _context.TeamMembers.Add(member);
            await _context.SaveChangesAsync().ConfigureAwait(false);
            return member;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Erstellen eines Mitarbeiters");
            throw;
        }
    }

    public async Task UpdateAsync(TeamMember member)
    {
        try
        {
            var existing = await _context.TeamMembers.FirstOrDefaultAsync(m => m.Id == member.Id).ConfigureAwait(false);
            if (existing == null)
            {
                throw new InvalidOperationException($"Mitarbeiter mit ID {member.Id} wurde nicht gefunden.");
            }

            if (string.IsNullOrWhiteSpace(member.DisplayName))
            {
                throw new InvalidOperationException("Name ist erforderlich.");
            }

            if (!string.IsNullOrWhiteSpace(member.Email))
            {
                var emailExists = await _context.TeamMembers
                    .AnyAsync(m => m.Id != member.Id && m.Email != null && m.Email == member.Email)
                    .ConfigureAwait(false);
                if (emailExists)
                {
                    throw new InvalidOperationException($"Email '{member.Email}' existiert bereits.");
                }
            }

            existing.DisplayName = member.DisplayName;
            existing.Email = string.IsNullOrWhiteSpace(member.Email) ? null : member.Email;
            existing.IsActive = member.IsActive;
            existing.Role = member.Role;
            existing.IsLockedByAdmin = member.IsLockedByAdmin;
            existing.FailedLoginAttempts = member.FailedLoginAttempts;
            existing.LockoutUntil = member.LockoutUntil;

            await _context.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Aktualisieren des Mitarbeiters {TeamMemberId}", member.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        try
        {
            var existing = await _context.TeamMembers.FirstOrDefaultAsync(m => m.Id == id).ConfigureAwait(false);
            if (existing == null)
            {
                throw new InvalidOperationException($"Mitarbeiter mit ID {id} wurde nicht gefunden.");
            }

            _context.TeamMembers.Remove(existing);
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Löschen des Mitarbeiters {TeamMemberId}", id);
            throw;
        }
    }
}

