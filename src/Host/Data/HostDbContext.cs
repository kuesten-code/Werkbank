using Kuestencode.Core.Models;
using Kuestencode.Werkbank.Host.Models;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Host.Data;

/// <summary>
/// DbContext für plattformweite Daten (Companies, Customers).
/// Verwendet das Schema "host".
/// </summary>
public class HostDbContext : DbContext
{
    public HostDbContext(DbContextOptions<HostDbContext> options) : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<AdditionalBankAccount> AdditionalBankAccounts => Set<AdditionalBankAccount>();
    public DbSet<NumberFormatSettings> NumberFormatSettings => Set<NumberFormatSettings>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<WerkbankSettings> WerkbankSettings => Set<WerkbankSettings>();
    public DbSet<MitarbeiterRolle> MitarbeiterRollen => Set<MitarbeiterRolle>();
    public DbSet<BackupSettings> BackupSettings => Set<BackupSettings>();
    public DbSet<BackupTarget> BackupTargets => Set<BackupTarget>();
    public DbSet<BackupHistory> BackupHistory => Set<BackupHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Schema-Trennung
        modelBuilder.HasDefaultSchema("host");

        // AdditionalBankAccount Konfiguration
        modelBuilder.Entity<AdditionalBankAccount>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne<Company>()
                .WithMany(c => c.AdditionalBankAccounts)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Company Konfiguration
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // String-Längen für Enums als int speichern
            entity.Property(e => e.EmailLayout)
                .HasConversion<int>();

            entity.Property(e => e.PdfLayout)
                .HasConversion<int>();
        });

        // NumberFormatSettings Konfiguration
        modelBuilder.Entity<NumberFormatSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        // Customer Konfiguration
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.CustomerNumber)
                .IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // TeamMember Konfiguration
        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.DisplayName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Email)
                .HasMaxLength(200);

            entity.HasIndex(e => e.Email)
                .IsUnique();

            entity.Property(e => e.Role)
                .HasConversion<int>()
                .HasDefaultValue(UserRole.Mitarbeiter)
                .HasSentinel((UserRole)(-1));

            entity.Property(e => e.FailedLoginAttempts)
                .HasDefaultValue(0);

            entity.Property(e => e.IsLockedByAdmin)
                .HasDefaultValue(false);

            entity.Ignore(e => e.HasCompletedSetup);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // MitarbeiterRolle Konfiguration
        modelBuilder.Entity<MitarbeiterRolle>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.SortOrder).HasDefaultValue(0);
        });

        // TeamMember FK zu MitarbeiterRolle
        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.HasOne(m => m.MitarbeiterRolle)
                .WithMany()
                .HasForeignKey(m => m.MitarbeiterRolleId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(m => m.Kostensatz).HasPrecision(10, 2).HasDefaultValue(0m);
        });

        // WerkbankSettings Konfiguration
        modelBuilder.Entity<WerkbankSettings>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.AuthEnabled)
                .HasDefaultValue(false);
        });

        // BackupSettings Konfiguration
        modelBuilder.Entity<BackupSettings>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Schedule).HasMaxLength(50).HasDefaultValue("0 3 * * *");
            entity.Property(e => e.EncryptionPassword).HasMaxLength(500);
            entity.Property(e => e.AlertEmail).HasMaxLength(200);
            entity.Property(e => e.AlertWebhookUrl).HasMaxLength(500);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // BackupTarget Konfiguration
        modelBuilder.Entity<BackupTarget>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Type).HasConversion<int>();
            entity.Property(e => e.Path).HasMaxLength(500);
            entity.Property(e => e.Host).HasMaxLength(200);
            entity.Property(e => e.Username).HasMaxLength(100);
            entity.Property(e => e.Password).HasMaxLength(500);
            entity.Property(e => e.AccessKey).HasMaxLength(200);
            entity.Property(e => e.SecretKey).HasMaxLength(500);
            entity.Property(e => e.Region).HasMaxLength(50);
            entity.Property(e => e.LastBackupError).HasMaxLength(1000);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne<BackupSettings>()
                .WithMany(s => s.Targets)
                .HasForeignKey(e => e.BackupSettingsId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // BackupHistory Konfiguration
        modelBuilder.Entity<BackupHistory>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.Type).HasConversion<int>();
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.Property(e => e.FileName).HasMaxLength(200).IsRequired();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Target)
                .WithMany()
                .HasForeignKey(e => e.BackupTargetId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        // Team members are stored in Host but don't derive from Core BaseEntity (Guid key),
        // so update timestamps explicitly.
        var teamEntries = ChangeTracker.Entries<TeamMember>();
        foreach (var entry in teamEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
