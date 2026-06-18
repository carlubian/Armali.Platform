using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Clothes.Domain;
using Segaris.Api.Modules.Identity;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Clothes.Persistence;

internal sealed class ClothesModelContributor : ISegarisModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        ConfigureCategory(modelBuilder);
        ConfigureColor(modelBuilder);

        modelBuilder.Entity<ClothesGarment>(builder =>
        {
            builder.ToTable("clothes_garments");
            builder.HasKey(garment => garment.Id);
            builder.Property(garment => garment.Id).ValueGeneratedOnAdd();
            builder.Property(garment => garment.Name)
                .HasMaxLength(ClothesValidation.GarmentNameMaximumLength).IsRequired();
            builder.Property(garment => garment.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(garment => garment.Size).HasMaxLength(ClothesValidation.SizeMaximumLength);
            builder.Property(garment => garment.WashingCare).HasConversion<string>().HasMaxLength(20);
            builder.Property(garment => garment.DryingCare).HasConversion<string>().HasMaxLength(20);
            builder.Property(garment => garment.IroningCare).HasConversion<string>().HasMaxLength(20);
            builder.Property(garment => garment.DryCleaningCare).HasConversion<string>().HasMaxLength(20);
            builder.Property(garment => garment.Notes).HasMaxLength(ClothesValidation.NotesMaximumLength);
            builder.Property(garment => garment.Visibility).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(garment => garment.PrimaryAttachmentId);
            builder.Property(garment => garment.CreatedAt).IsRequired();
            builder.Property(garment => garment.UpdatedAt).IsRequired();
            builder.ToTable(table =>
            {
                table.HasCheckConstraint("CK_clothes_garments_status", "\"Status\" IN ('Active', 'Unavailable', 'Deprecated')");
                table.HasCheckConstraint("CK_clothes_garments_visibility", "\"Visibility\" IN ('Public', 'Private')");
                table.HasCheckConstraint(
                    "CK_clothes_garments_washing_care",
                    "\"WashingCare\" IS NULL OR \"WashingCare\" IN ('Any', 'Wash30', 'Wash30Delicate', 'Wash40', 'Wash40Delicate', 'Wash50', 'Wash50Delicate', 'Wash60', 'Wash60Delicate', 'HandWash', 'DoNotWash')");
                table.HasCheckConstraint(
                    "CK_clothes_garments_drying_care",
                    "\"DryingCare\" IS NULL OR \"DryingCare\" IN ('Any', 'Delicate', 'VeryDelicate')");
                table.HasCheckConstraint(
                    "CK_clothes_garments_ironing_care",
                    "\"IroningCare\" IS NULL OR \"IroningCare\" IN ('Any', 'Low', 'Medium', 'DoNotIron')");
                table.HasCheckConstraint(
                    "CK_clothes_garments_dry_cleaning_care",
                    "\"DryCleaningCare\" IS NULL OR \"DryCleaningCare\" IN ('Any', 'DoNotDryClean')");
            });
            builder.HasMany(garment => garment.Colors).WithOne().HasForeignKey(association => association.GarmentId).OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(garment => garment.Colors).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.HasOne<ClothingCategory>().WithMany().HasForeignKey(garment => garment.CategoryId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(garment => garment.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(garment => garment.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
            // Default gallery ordering (name, identifier) and creator/visibility access.
            builder.HasIndex(garment => new { garment.Name, garment.Id });
            builder.HasIndex(garment => new { garment.CreatedBy, garment.Visibility, garment.Id });
            // Exact filters and category reference migration.
            builder.HasIndex(garment => garment.CategoryId);
            builder.HasIndex(garment => garment.Status);
            builder.HasIndex(garment => garment.Visibility);
            builder.HasIndex(garment => garment.UpdatedBy);
        });

        modelBuilder.Entity<ClothesGarmentColor>(builder =>
        {
            builder.ToTable("clothes_garment_colors");
            builder.HasKey(association => new { association.GarmentId, association.ColorId });
            builder.HasOne<ClothingColor>().WithMany().HasForeignKey(association => association.ColorId).OnDelete(DeleteBehavior.Restrict);
            // Colour filtering and colour reference migration.
            builder.HasIndex(association => association.ColorId);
        });
    }

    private static void ConfigureCategory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClothingCategory>(builder =>
        {
            builder.ToTable("clothing_categories");
            builder.HasKey(category => category.Id);
            builder.Property(category => category.Id).ValueGeneratedOnAdd();
            builder.Property(category => category.Name).HasMaxLength(ClothesValidation.CategoryNameMaximumLength).IsRequired();
            builder.Property(category => category.NormalizedName).HasMaxLength(ClothesValidation.CategoryNameMaximumLength).IsRequired();
            builder.Property(category => category.SortOrder).IsRequired();
            builder.Property(category => category.CreatedAt).IsRequired();
            builder.Property(category => category.CreatedBy);
            builder.Property(category => category.UpdatedAt).IsRequired();
            builder.Property(category => category.UpdatedBy);
            builder.HasIndex(category => category.NormalizedName).IsUnique();
            builder.HasIndex(category => category.SortOrder);
        });
    }

    private static void ConfigureColor(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClothingColor>(builder =>
        {
            builder.ToTable("clothing_colors");
            builder.HasKey(color => color.Id);
            builder.Property(color => color.Id).ValueGeneratedOnAdd();
            builder.Property(color => color.Name).HasMaxLength(ClothesValidation.ColorNameMaximumLength).IsRequired();
            builder.Property(color => color.NormalizedName).HasMaxLength(ClothesValidation.ColorNameMaximumLength).IsRequired();
            builder.Property(color => color.ColorValue).HasMaxLength(ClothesValidation.ColorValueLength).IsRequired();
            builder.Property(color => color.SortOrder).IsRequired();
            builder.Property(color => color.CreatedAt).IsRequired();
            builder.Property(color => color.CreatedBy);
            builder.Property(color => color.UpdatedAt).IsRequired();
            builder.Property(color => color.UpdatedBy);
            builder.HasIndex(color => color.NormalizedName).IsUnique();
            builder.HasIndex(color => color.SortOrder);
        });
    }
}
