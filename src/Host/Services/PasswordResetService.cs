using Kuestencode.Core.Interfaces;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Host.Services;

public class PasswordResetService : IPasswordResetService
{
    private readonly HostDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IEmailEngine _emailEngine;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        HostDbContext context,
        IPasswordService passwordService,
        IEmailEngine emailEngine,
        ILogger<PasswordResetService> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _emailEngine = emailEngine;
        _logger = logger;
    }

    public async Task<bool> RequestResetAsync(string email)
    {
        var member = await _context.TeamMembers
            .FirstOrDefaultAsync(m => m.Email == email && m.IsActive);

        if (member == null)
        {
            // Kein Fehler zurückgeben um E-Mail-Enumeration zu verhindern
            _logger.LogWarning("Passwort-Reset angefordert für unbekannte E-Mail: {Email}", email);
            return true;
        }

        if (!member.HasCompletedSetup)
        {
            _logger.LogWarning("Passwort-Reset angefordert für nicht eingerichteten Account: {Email}", email);
            return true;
        }

        var settings = await _context.WerkbankSettings.FirstOrDefaultAsync();
        if (settings == null || string.IsNullOrWhiteSpace(settings.BaseUrl))
        {
            _logger.LogError("BaseUrl ist nicht konfiguriert. Passwort-Reset nicht möglich.");
            return false;
        }

        var token = Guid.NewGuid().ToString("N");
        member.ResetToken = token;
        member.ResetTokenExpires = DateTime.UtcNow.AddHours(1);

        await _context.SaveChangesAsync();

        await SendResetEmailAsync(member, settings.BaseUrl, token);

        _logger.LogInformation("Passwort-Reset-Token erstellt für {Email}", email);
        return true;
    }

    public async Task<TeamMember?> ValidateResetTokenAsync(string token)
    {
        var member = await _context.TeamMembers
            .FirstOrDefaultAsync(m => m.ResetToken == token);

        if (member == null)
            return null;

        if (member.ResetTokenExpires < DateTime.UtcNow)
            return null;

        return member;
    }

    public async Task ResetPasswordAsync(string token, string newPassword)
    {
        var member = await ValidateResetTokenAsync(token);
        if (member == null)
            throw new InvalidOperationException("Ungültiger oder abgelaufener Reset-Link.");

        if (newPassword.Length < 8)
            throw new InvalidOperationException("Passwort muss mindestens 8 Zeichen lang sein.");

        member.PasswordHash = _passwordService.HashPassword(newPassword);
        member.ResetToken = null;
        member.ResetTokenExpires = null;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Passwort zurückgesetzt für {Email}", member.Email);
    }

    private async Task SendResetEmailAsync(TeamMember member, string baseUrl, string token)
    {
        var link = $"{baseUrl.TrimEnd('/')}/reset/{token}";

        var htmlBody = $"""
            <p>Hallo {member.DisplayName},</p>
            <p>du hast angefordert, dein Passwort zurückzusetzen.</p>
            <p>Klicke auf den folgenden Link:<br>
            <a href="{link}">{link}</a></p>
            <p>Der Link ist 1 Stunde gültig.</p>
            <p>Falls du das nicht angefordert hast, ignoriere diese E-Mail.</p>
            """;

        var plainBody = $"""
            Hallo {member.DisplayName},

            du hast angefordert, dein Passwort zurückzusetzen.

            Klicke auf den folgenden Link:
            {link}

            Der Link ist 1 Stunde gültig.

            Falls du das nicht angefordert hast, ignoriere diese E-Mail.
            """;

        var success = await _emailEngine.SendEmailAsync(
            member.Email!,
            "Passwort zurücksetzen – Küstencode Werkbank",
            htmlBody,
            plainBody);

        if (!success)
            _logger.LogWarning("Reset-E-Mail konnte nicht an {Email} gesendet werden", member.Email);
    }
}
