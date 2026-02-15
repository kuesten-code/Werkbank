using Kuestencode.Core.Interfaces;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Host.Services;

public class InviteService : IInviteService
{
    private readonly HostDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IEmailEngine _emailEngine;
    private readonly ILogger<InviteService> _logger;

    public InviteService(
        HostDbContext context,
        IPasswordService passwordService,
        IEmailEngine emailEngine,
        ILogger<InviteService> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _emailEngine = emailEngine;
        _logger = logger;
    }

    public async Task<string> CreateInviteAsync(Guid teamMemberId)
    {
        var member = await _context.TeamMembers.FirstOrDefaultAsync(m => m.Id == teamMemberId);
        if (member == null)
            throw new InvalidOperationException($"Mitarbeiter mit ID {teamMemberId} nicht gefunden.");

        if (string.IsNullOrWhiteSpace(member.Email))
            throw new InvalidOperationException("Mitarbeiter hat keine E-Mail-Adresse.");

        var settings = await _context.WerkbankSettings.FirstOrDefaultAsync();
        if (settings == null || string.IsNullOrWhiteSpace(settings.BaseUrl))
            throw new InvalidOperationException("BaseUrl ist nicht konfiguriert. Bitte zuerst in den Einstellungen setzen.");

        var token = Guid.NewGuid().ToString("N");
        member.InviteToken = token;
        member.InviteTokenExpires = DateTime.UtcNow.AddHours(48);

        await _context.SaveChangesAsync();

        await SendInviteEmailAsync(member, settings.BaseUrl, token);

        _logger.LogInformation("Einladung erstellt für {Email}", member.Email);
        return token;
    }

    public async Task<TeamMember?> ValidateInviteTokenAsync(string token)
    {
        var member = await _context.TeamMembers
            .FirstOrDefaultAsync(m => m.InviteToken == token);

        if (member == null)
            return null;

        if (member.InviteTokenExpires < DateTime.UtcNow)
            return null;

        return member;
    }

    public async Task AcceptInviteAsync(string token, string password)
    {
        var member = await ValidateInviteTokenAsync(token);
        if (member == null)
            throw new InvalidOperationException("Ungültiger oder abgelaufener Einladungslink.");

        if (password.Length < 8)
            throw new InvalidOperationException("Passwort muss mindestens 8 Zeichen lang sein.");

        member.PasswordHash = _passwordService.HashPassword(password);
        member.InviteToken = null;
        member.InviteTokenExpires = null;
        member.InviteAcceptedAt = DateTime.UtcNow;
        member.IsActive = true;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Einladung angenommen von {Email}", member.Email);
    }

    public async Task ResendInviteAsync(Guid teamMemberId)
    {
        await CreateInviteAsync(teamMemberId);
    }

    private async Task SendInviteEmailAsync(TeamMember member, string baseUrl, string token)
    {
        var link = $"{baseUrl.TrimEnd('/')}/invite/{token}";

        var htmlBody = $"""
            <p>Hallo {member.DisplayName},</p>
            <p>du wurdest zur Küstencode Werkbank eingeladen.</p>
            <p>Klicke auf den folgenden Link, um dein Passwort zu setzen:<br>
            <a href="{link}">{link}</a></p>
            <p>Der Link ist 48 Stunden gültig.</p>
            <p>Bei Fragen wende dich an deinen Administrator.</p>
            """;

        var plainBody = $"""
            Hallo {member.DisplayName},

            du wurdest zur Küstencode Werkbank eingeladen.

            Klicke auf den folgenden Link, um dein Passwort zu setzen:
            {link}

            Der Link ist 48 Stunden gültig.

            Bei Fragen wende dich an deinen Administrator.
            """;

        var success = await _emailEngine.SendEmailAsync(
            member.Email!,
            "Einladung zur Küstencode Werkbank",
            htmlBody,
            plainBody);

        if (!success)
            _logger.LogWarning("Einladungs-E-Mail konnte nicht an {Email} gesendet werden", member.Email);
    }
}
