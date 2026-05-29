using Kuestencode.Werkbank.Contracta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Kuestencode.Werkbank.Contracta.Data;

public class ContractaDbContext : DbContext
{
    public ContractaDbContext(DbContextOptions<ContractaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Wartungsvertrag> Wartungsvertraege => Set<Wartungsvertrag>();
    public DbSet<Vertragsposition> Vertragspositionen => Set<Vertragsposition>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("contracta");

        modelBuilder.Entity<Wartungsvertrag>(entity =>
        {
            entity.ToTable("Wartungsvertraege");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Vertragsnummer).IsRequired();
            entity.Property(e => e.Bezeichnung).IsRequired();
            entity.Property(e => e.Intervall).HasConversion<int>();
            entity.Property(e => e.Status).HasConversion<int>();

            entity.HasMany(e => e.Positionen)
                .WithOne()
                .HasForeignKey(p => p.WartungsvertragId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Vertragsposition>(entity =>
        {
            entity.ToTable("Vertragspositionen");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Text).IsRequired();
            entity.Property(e => e.Menge).HasPrecision(18, 4);
            entity.Property(e => e.Einzelpreis).HasPrecision(18, 4);
            entity.Property(e => e.Steuersatz).HasPrecision(5, 2);
            entity.Ignore(e => e.Positionssumme);
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity.GetType().GetProperty("GeaendertAm") != null)
                entry.Property("GeaendertAm").CurrentValue = DateTime.UtcNow;

            if (entry.State == EntityState.Added && entry.Entity.GetType().GetProperty("ErstelltAm") != null)
                entry.Property("ErstelltAm").CurrentValue = DateTime.UtcNow;
        }
    }
}
