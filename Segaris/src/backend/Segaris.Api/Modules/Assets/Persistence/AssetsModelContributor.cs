using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Assets.Domain;
using Segaris.Api.Modules.Identity;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Assets.Persistence;

internal sealed class AssetsModelContributor : ISegarisModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        ConfigureCatalog<AssetCategory>(modelBuilder, "asset_categories");
        ConfigureCatalog<AssetLocation>(modelBuilder, "asset_locations");

        modelBuilder.Entity<Asset>(builder =>
        {
            builder.ToTable("assets");
            builder.HasKey(asset => asset.Id);
            builder.Property(asset => asset.Id).ValueGeneratedOnAdd();
            builder.Property(asset => asset.Name)
                .HasMaxLength(AssetValidation.NameMaximumLength).IsRequired();
            builder.Property(asset => asset.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(asset => asset.Code).HasMaxLength(AssetValidation.CodeMaximumLength);
            builder.Property(asset => asset.NormalizedCode).HasMaxLength(AssetValidation.CodeMaximumLength);
            builder.Property(asset => asset.BrandModel).HasMaxLength(AssetValidation.BrandModelMaximumLength);
            builder.Property(asset => asset.SerialNumber).HasMaxLength(AssetValidation.SerialNumberMaximumLength);
            builder.Property(asset => asset.AcquisitionDate);
            builder.Property(asset => asset.ExpectedEndOfLifeDate);
            builder.Property(asset => asset.Notes).HasMaxLength(AssetValidation.NotesMaximumLength);
            builder.Property(asset => asset.Visibility).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(asset => asset.PrimaryAttachmentId);
            builder.Property(asset => asset.CreatedAt).IsRequired();
            builder.Property(asset => asset.UpdatedAt).IsRequired();
            builder.ToTable(table =>
            {
                table.HasCheckConstraint("CK_assets_status", "\"Status\" IN ('Active', 'Stored', 'Retired')");
                table.HasCheckConstraint("CK_assets_visibility", "\"Visibility\" IN ('Public', 'Private')");
            });
            builder.HasOne<AssetCategory>().WithMany().HasForeignKey(asset => asset.CategoryId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<AssetLocation>().WithMany().HasForeignKey(asset => asset.LocationId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(asset => asset.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(asset => asset.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
            // Default asset ordering (name, identifier) and creator/visibility access.
            builder.HasIndex(asset => new { asset.Name, asset.Id });
            builder.HasIndex(asset => new { asset.CreatedBy, asset.Visibility, asset.Id });
            // Launcher attention scans accessible non-retired assets by expected end of life.
            builder.HasIndex(asset => new { asset.Status, asset.ExpectedEndOfLifeDate });
            // Case-insensitive code uniqueness (both providers treat NULLs as distinct).
            builder.HasIndex(asset => asset.NormalizedCode).IsUnique();
            // Exact filters and category/location reference migration.
            builder.HasIndex(asset => asset.CategoryId);
            builder.HasIndex(asset => asset.LocationId);
            builder.HasIndex(asset => asset.Visibility);
            builder.HasIndex(asset => asset.UpdatedBy);
        });
    }

    private static void ConfigureCatalog<TCatalog>(ModelBuilder modelBuilder, string tableName)
        where TCatalog : class
    {
        modelBuilder.Entity<TCatalog>(builder =>
        {
            builder.ToTable(tableName);
            builder.Property<int>("Id").ValueGeneratedOnAdd();
            builder.HasKey("Id");
            builder.Property<string>("Name").HasMaxLength(AssetValidation.CategoryNameMaximumLength).IsRequired();
            builder.Property<string>("NormalizedName").HasMaxLength(AssetValidation.CategoryNameMaximumLength).IsRequired();
            builder.Property<int>("SortOrder").IsRequired();
            builder.Property<DateTimeOffset>("CreatedAt").IsRequired();
            builder.Property<int?>("CreatedBy");
            builder.Property<DateTimeOffset>("UpdatedAt").IsRequired();
            builder.Property<int?>("UpdatedBy");
            builder.HasIndex("NormalizedName").IsUnique();
            builder.HasIndex("SortOrder");
        });
    }
}
