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
    }

    private static void ConfigureSupplier(EntityTypeBuilder<SegarisSupplier> builder)
    {
        builder.ToTable("configuration_suppliers");
        ConfigureCatalog(builder, 40);
    }

    private static void ConfigureCostCenter(EntityTypeBuilder<SegarisCostCenter> builder)
    {
        builder.ToTable("configuration_cost_centers");
        ConfigureCatalog(builder, 40);
    }

    private static void ConfigureCurrency(EntityTypeBuilder<SegarisCurrency> builder)
    {
        builder.ToTable("configuration_currencies");
        ConfigureCatalog(builder, 3);
        builder.Property(entity => entity.Code).IsFixedLength();
    }

    private static void ConfigureCatalog<TEntity>(
        EntityTypeBuilder<TEntity> builder,
        int codeMaximumLength)
        where TEntity : class, IConfigurationCatalogEntity
    {
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedOnAdd();
        builder.Property(entity => entity.Code).HasMaxLength(codeMaximumLength).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.CreatedAt).IsRequired();
        builder.Property(entity => entity.CreatedBy);
        builder.Property(entity => entity.UpdatedAt).IsRequired();
        builder.Property(entity => entity.UpdatedBy);
        builder.HasIndex(entity => entity.Code).IsUnique();
    }
}
