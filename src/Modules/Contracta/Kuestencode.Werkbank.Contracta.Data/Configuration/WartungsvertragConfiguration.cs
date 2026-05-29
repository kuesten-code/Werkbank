using Kuestencode.Werkbank.Contracta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kuestencode.Werkbank.Contracta.Data.Configuration;

public class WartungsvertragConfiguration : IEntityTypeConfiguration<Wartungsvertrag>
{
    public void Configure(EntityTypeBuilder<Wartungsvertrag> builder)
    {
        builder.ToTable("Wartungsvertraege");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Vertragsnummer).IsRequired();
        builder.Property(e => e.Bezeichnung).IsRequired();
        builder.Property(e => e.Intervall).HasConversion<int>();
        builder.Property(e => e.Status).HasConversion<int>();

        builder.HasIndex(e => e.Vertragsnummer).IsUnique();
        builder.HasIndex(e => e.KundeId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.NaechsteAbrechnung);

        builder.HasMany(e => e.Positionen)
            .WithOne()
            .HasForeignKey(p => p.WartungsvertragId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Historien)
            .WithOne()
            .HasForeignKey(h => h.WartungsvertragId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
