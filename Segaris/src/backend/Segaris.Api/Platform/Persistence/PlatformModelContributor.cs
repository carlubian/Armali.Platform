using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Segaris.Persistence;

namespace Segaris.Api.Platform.Persistence;

internal sealed class PlatformModelContributor : ISegarisModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        ConfigureCompatibilityRecord(modelBuilder.Entity<PersistenceCompatibilityRecord>());
    }

    private static void ConfigureCompatibilityRecord(
        EntityTypeBuilder<PersistenceCompatibilityRecord> builder)
    {
        builder.ToTable("platform_persistence_compatibility");
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Id).ValueGeneratedOnAdd();
        builder.Property(record => record.Name).HasMaxLength(120).IsRequired();
        builder.Property(record => record.CivilDate).HasColumnType("date");
        builder.Property(record => record.Amount).HasPrecision(18, 2);
        builder.Property(record => record.CurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(record => record.CreatedAt).IsRequired();
        builder.HasIndex(record => record.Name).IsUnique();
    }
}
