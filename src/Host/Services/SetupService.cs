using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Host.Services;

public class SetupService : ISetupService
{
    private readonly HostDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IWerkbankSettingsService _settingsService;
    private readonly ILogger<SetupService> _logger;

    public SetupService(
        HostDbContext context,
        IPasswordService passwordService,
        IWerkbankSettingsService settingsService,
        ILogger<SetupService> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task<bool> IsSetupRequiredAsync()
    {
        try
        {
            // Setup is required if no admin exists
            var hasAdmin = await _context.TeamMembers
                .AnyAsync(tm => tm.Role == UserRole.Admin);

            return !hasAdmin;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if setup is required");
            // If there's a database error, assume setup is required
            return true;
        }
    }

    public async Task<bool> IsSetupCompletedAsync()
    {
        return !await IsSetupRequiredAsync();
    }

    public async Task CompleteSetupAsync(SetupData setupData)
    {
        _logger.LogInformation("Starting initial setup");

        try
        {
            // Validate
            if (string.IsNullOrWhiteSpace(setupData.AdminName))
            {
                throw new ArgumentException("Admin name is required", nameof(setupData.AdminName));
            }

            if (string.IsNullOrWhiteSpace(setupData.AdminEmail))
            {
                throw new ArgumentException("Admin email is required", nameof(setupData.AdminEmail));
            }

            if (string.IsNullOrWhiteSpace(setupData.AdminPassword))
            {
                throw new ArgumentException("Admin password is required", nameof(setupData.AdminPassword));
            }

            if (setupData.AdminPassword.Length < 8)
            {
                throw new ArgumentException("Password must be at least 8 characters", nameof(setupData.AdminPassword));
            }

            // Check if setup already completed
            if (await IsSetupCompletedAsync())
            {
                _logger.LogWarning("Setup already completed, ignoring request");
                throw new InvalidOperationException("Setup has already been completed");
            }

            // Start transaction
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Create WerkbankSettings
                var settings = new WerkbankSettings
                {
                    Id = Guid.NewGuid(),
                    BaseUrl = string.IsNullOrWhiteSpace(setupData.BaseUrl) ? null : setupData.BaseUrl,
                    AuthEnabled = setupData.AuthEnabled
                };

                _context.WerkbankSettings.Add(settings);

                // 2. Create Admin TeamMember
                var passwordHash = _passwordService.HashPassword(setupData.AdminPassword);

                var admin = new TeamMember
                {
                    Id = Guid.NewGuid(),
                    DisplayName = setupData.AdminName,
                    Email = setupData.AdminEmail,
                    Role = UserRole.Admin,
                    PasswordHash = passwordHash,
                    IsActive = true,
                    InviteAcceptedAt = DateTime.UtcNow, // Mark as setup completed
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.TeamMembers.Add(admin);

                // Save changes
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Initial setup completed successfully. Admin: {AdminEmail}", setupData.AdminEmail);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing initial setup");
            throw;
        }
    }
}
