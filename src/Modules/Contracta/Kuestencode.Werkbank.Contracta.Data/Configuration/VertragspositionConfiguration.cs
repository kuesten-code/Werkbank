using Kuestencode.Werkbank.Contracta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kuestencode.Werkbank.Contracta.Data.Configuration;

public class VertragspositionConfiguration : IEntityTypeConfiguration<Vertragsposition>
{
    public void Configure(EntityTypeBuilder<Vertragsposition> builder)
    {
        builder.ToTable("Vertragspositionen");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Text).IsRequired();
        builder.Property(e => e.Menge).HasPrecision(18, 4);
        builder.Property(e => e.Einzelpreis).HasPrecision(18, 4);
        builder.Property(e => e.Steuersatz).HasPrecision(5, 2);
        builder.Ignore(e => e.Positionssumme);
    }
}
