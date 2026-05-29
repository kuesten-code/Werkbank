using Kuestencode.Werkbank.Contracta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kuestencode.Werkbank.Contracta.Data.Configuration;

public class AbrechnungshistorieConfiguration : IEntityTypeConfiguration<Abrechnungshistorie>
{
    public void Configure(EntityTypeBuilder<Abrechnungshistorie> builder)
    {
        builder.ToTable("Abrechnungshistorie");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Betrag).HasPrecision(18, 2);
        builder.HasIndex(e => e.WartungsvertragId);
    }
}
