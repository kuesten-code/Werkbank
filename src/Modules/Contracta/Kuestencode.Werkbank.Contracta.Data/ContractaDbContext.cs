using Kuestencode.Werkbank.Contracta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Reflection;

namespace Kuestencode.Werkbank.Contracta.Data;

public class ContractaDbContext : DbContext
{
    public ContractaDbContext(DbContextOptions<ContractaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Wartungsvertrag> Wartungsvertraege => Set<Wartungsvertrag>();
    public DbSet<Vertragsposition> Vertragspositionen => Set<Vertragsposition>();
    public DbSet<Abrechnungshistorie> Abrechnungshistorie => Set<Abrechnungshistorie>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("contracta");
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
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
