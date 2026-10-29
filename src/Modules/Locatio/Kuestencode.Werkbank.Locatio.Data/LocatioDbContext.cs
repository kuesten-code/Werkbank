using Kuestencode.Werkbank.Locatio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Kuestencode.Werkbank.Locatio.Data;

/// <summary>
/// DbContext für die Vermietungsverwaltung. Verwendet das Schema "locatio".
/// </summary>
public class LocatioDbContext : DbContext
{
    public LocatioDbContext(DbContextOptions<LocatioDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    public DbSet<Gebaeude> Gebaeude { get; set; } = null!;
    public DbSet<Mieteinheit> Mieteinheiten { get; set; } = null!;
    public DbSet<Kostenkategorie> Kostenkategorien { get; set; } = null!;
    public DbSet<Kostenposten> Kostenposten { get; set; } = null!;
    public DbSet<Darlehen> Darlehen { get; set; } = null!;
    public DbSet<Darlehenszahlung> Darlehenszahlungen { get; set; } = null!;
    public DbSet<Mieteingang> Mieteingaenge { get; set; } = null!;
    public DbSet<Nebenkostenabrechnung> Nebenkostenabrechnungen { get; set; } = null!;
    public DbSet<Dokument> Dokumente { get; set; } = null!;
    public DbSet<Uebergabeprotokoll> Uebergabeprotokolle { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("locatio");

        modelBuilder.Entity<Gebaeude>(entity =>
        {
            entity.ToTable("Gebaeude");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Strasse).HasMaxLength(200);
            entity.Property(e => e.Plz).HasMaxLength(10);
            entity.Property(e => e.Ort).HasMaxLength(100);
            entity.Property(e => e.AbrechnerUrl).HasMaxLength(500);
            ConfigureEigentumsobjekt(entity);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasMany(e => e.Einheiten)
                .WithOne(e => e.Gebaeude)
                .HasForeignKey(e => e.GebaeudeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Mieteinheit>(entity =>
        {
            entity.ToTable("Mieteinheiten");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Bezeichnung).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Art).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Flaeche).HasPrecision(10, 2);
            entity.Property(e => e.Kaltmiete).HasPrecision(18, 2);
            entity.Property(e => e.NebenkostenVorauszahlung).HasPrecision(18, 2);
            entity.Property(e => e.Miteigentumsanteil).HasMaxLength(50);
            entity.Property(e => e.Mieter).HasMaxLength(200);
            entity.Property(e => e.UstSatz).HasPrecision(5, 2);
            ConfigureEigentumsobjekt(entity);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Grundriss)
                .WithMany()
                .HasForeignKey(e => e.GrundrissId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Kostenkategorie>(entity =>
        {
            entity.ToTable("Kostenkategorien");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Typ).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.KontoNummer).HasMaxLength(10);
        });

        modelBuilder.Entity<Kostenposten>(entity =>
        {
            entity.ToTable("Kostenposten", t => t.HasCheckConstraint(
                "CK_Kostenposten_Zuordnung", "\"GebaeudeId\" IS NOT NULL OR \"MieteinheitId\" IS NOT NULL"));
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Zahlungsdatum);
            entity.Property(e => e.Betrag).HasPrecision(18, 2);
            entity.Property(e => e.Gesamtbetrag).HasPrecision(18, 2);
            entity.Property(e => e.Verteilschluessel).HasMaxLength(200);
            entity.Property(e => e.Beschreibung).HasMaxLength(300).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Gebaeude).WithMany().HasForeignKey(e => e.GebaeudeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Mieteinheit).WithMany().HasForeignKey(e => e.MieteinheitId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Kategorie).WithMany().HasForeignKey(e => e.KategorieId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Beleg).WithMany().HasForeignKey(e => e.BelegId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Darlehen>(entity =>
        {
            entity.ToTable("Darlehen");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Bezeichnung).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Kreditgeber).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Gebaeude).WithMany().HasForeignKey(e => e.GebaeudeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(e => e.Zahlungen)
                .WithOne(e => e.Darlehen)
                .HasForeignKey(e => e.DarlehenId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Darlehenszahlung>(entity =>
        {
            entity.ToTable("Darlehenszahlungen");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Zahlungsdatum);
            entity.Property(e => e.Zinsanteil).HasPrecision(18, 2);
            entity.Property(e => e.Tilgungsanteil).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<Mieteingang>(entity =>
        {
            entity.ToTable("Mieteingaenge");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Zahlungsdatum);
            entity.Property(e => e.Kaltmiete).HasPrecision(18, 2);
            entity.Property(e => e.Nebenkosten).HasPrecision(18, 2);
            entity.Property(e => e.UstSatz).HasPrecision(5, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Mieteinheit).WithMany().HasForeignKey(e => e.MieteinheitId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Nebenkostenabrechnung>(entity =>
        {
            entity.ToTable("Nebenkostenabrechnungen");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.MieteinheitId, e.Jahr }).IsUnique();
            entity.Property(e => e.Ergebnis).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Mieteinheit).WithMany().HasForeignKey(e => e.MieteinheitId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Dokument).WithMany().HasForeignKey(e => e.DokumentId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Uebergabeprotokoll>(entity =>
        {
            entity.ToTable("Uebergabeprotokolle");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MieteinheitId);
            entity.Property(e => e.Art).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Mieter).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Mieteinheit).WithMany().HasForeignKey(e => e.MieteinheitId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Dokument).WithMany().HasForeignKey(e => e.DokumentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Dokument>(entity =>
        {
            entity.ToTable("Dokumente");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Dateiname).HasMaxLength(255).IsRequired();
            entity.Property(e => e.ContentType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }

    private static void ConfigureEigentumsobjekt<T>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> entity)
        where T : Eigentumsobjekt
    {
        entity.Property(e => e.AnschaffungskostenGebaeude).HasPrecision(18, 2);
        entity.Property(e => e.AnschaffungskostenGrundUndBoden).HasPrecision(18, 2);
        entity.Property(e => e.Anschaffungsnebenkosten).HasPrecision(18, 2);
        entity.Property(e => e.Veraeusserungspreis).HasPrecision(18, 2);
        entity.Property(e => e.Veraeusserungskosten).HasPrecision(18, 2);
        entity.Property(e => e.AfaSatz).HasPrecision(5, 2);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            if (entry.Metadata.FindProperty("UpdatedAt") != null)
                entry.Property("UpdatedAt").CurrentValue = now;

            if (entry.State == EntityState.Added && entry.Metadata.FindProperty("CreatedAt") != null)
                entry.Property("CreatedAt").CurrentValue = now;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
