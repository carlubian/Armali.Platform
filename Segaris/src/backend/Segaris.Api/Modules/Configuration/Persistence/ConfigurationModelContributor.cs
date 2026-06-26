using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Configuration.Persistence;

internal sealed class ConfigurationModelContributor : ISegarisModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        ConfigureSupplier(modelBuilder.Entity<SegarisSupplier>());
        ConfigureCostCenter(modelBuilder.Entity<SegarisCostCenter>());
        ConfigureCurrency(modelBuilder.Entity<SegarisCurrency>());
        ConfigureInitialization(modelBuilder.Entity<SegarisCatalogInitialization>());
    }

    private static void ConfigureSupplier(EntityTypeBuilder<SegarisSupplier> builder)
    {
        builder.ToTable("configuration_suppliers");
        ConfigureCatalog(builder);
    }

    private static void ConfigureCostCenter(EntityTypeBuilder<SegarisCostCenter> builder)
    {
        builder.ToTable("configuration_cost_centers");
        ConfigureCatalog(builder);
    }

    private static void ConfigureCurrency(EntityTypeBuilder<SegarisCurrency> builder)
    {
        builder.ToTable("configuration_currencies");
        ConfigureCatalog(builder);

        builder.Property(entity => entity.Code)
            .HasMaxLength(CatalogNormalization.CurrencyCodeLength)
            .IsFixedLength()
            .IsRequired();
        builder.Property(entity => entity.NormalizedCode)
            .HasMaxLength(CatalogNormalization.CurrencyCodeLength)
            .IsFixedLength()
            .IsRequired();
        builder.Property(entity => entity.ExchangeRateToEur)
            .HasPrecision(18, CatalogNormalization.ExchangeRateDecimalPlaces);
        builder.HasIndex(entity => entity.NormalizedCode).IsUnique();
    }

    private static void ConfigureInitialization(EntityTypeBuilder<SegarisCatalogInitialization> builder)
    {
        builder.ToTable("configuration_catalog_initializations");
        builder.HasKey(entity => entity.CatalogKey);
        builder.Property(entity => entity.CatalogKey).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.InitializedAt).IsRequired();
    }

    private static void ConfigureCatalog<TEntity>(EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IConfigurationCatalogEntity
    {
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedOnAdd();
        builder.Property(entity => entity.Name)
            .HasMaxLength(CatalogNormalization.NameMaximumLength)
            .IsRequired();
        builder.Property(entity => entity.NormalizedName)
            .HasMaxLength(CatalogNormalization.NameMaximumLength)
            .IsRequired();
        builder.Property(entity => entity.SortOrder).IsRequired();
        builder.Property(entity => entity.CreatedAt).IsRequired();
        builder.Property(entity => entity.CreatedBy);
        builder.Property(entity => entity.UpdatedAt).IsRequired();
        builder.Property(entity => entity.UpdatedBy);
        builder.HasIndex(entity => entity.NormalizedName).IsUnique();
        builder.HasIndex(entity => entity.SortOrder);
    }
}
