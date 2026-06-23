using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Health.Domain;
using Segaris.Api.Modules.Identity;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Health.Persistence;

internal sealed class HealthModelContributor : ISegarisModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        ConfigureDiseaseCategory(modelBuilder);
        ConfigureMedicineCategory(modelBuilder);
        ConfigureDisease(modelBuilder);
        ConfigureMedicine(modelBuilder);
        ConfigureDiseaseMedicine(modelBuilder);
    }

    private static void ConfigureDiseaseCategory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DiseaseCategory>(builder =>
        {
            builder.ToTable("health_disease_categories");
            builder.HasKey(category => category.Id);
            builder.Property(category => category.Id).ValueGeneratedOnAdd();
            builder.Property(category => category.Name)
                .HasMaxLength(HealthDefaults.CategoryNameMaximumLength).IsRequired();
            builder.Property(category => category.NormalizedName)
                .HasMaxLength(HealthDefaults.CategoryNameMaximumLength).IsRequired();
            builder.Property(category => category.SortOrder).IsRequired();
            builder.Property(category => category.CreatedAt).IsRequired();
            builder.Property(category => category.UpdatedAt).IsRequired();
            builder.Property(category => category.CreatedBy);
            builder.Property(category => category.UpdatedBy);
            builder.HasIndex(category => category.NormalizedName).IsUnique();
            builder.HasIndex(category => category.SortOrder);
        });
    }

    private static void ConfigureMedicineCategory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MedicineCategory>(builder =>
        {
            builder.ToTable("health_medicine_categories");
            builder.HasKey(category => category.Id);
            builder.Property(category => category.Id).ValueGeneratedOnAdd();
            builder.Property(category => category.Name)
                .HasMaxLength(HealthDefaults.CategoryNameMaximumLength).IsRequired();
            builder.Property(category => category.NormalizedName)
                .HasMaxLength(HealthDefaults.CategoryNameMaximumLength).IsRequired();
            builder.Property(category => category.SortOrder).IsRequired();
            builder.Property(category => category.CreatedAt).IsRequired();
            builder.Property(category => category.UpdatedAt).IsRequired();
            builder.Property(category => category.CreatedBy);
            builder.Property(category => category.UpdatedBy);
            builder.HasIndex(category => category.NormalizedName).IsUnique();
            builder.HasIndex(category => category.SortOrder);
        });
    }

    private static void ConfigureDisease(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Disease>(builder =>
        {
            builder.ToTable("health_diseases");
            builder.HasKey(disease => disease.Id);
            builder.Property(disease => disease.Id).ValueGeneratedOnAdd();
            builder.Property(disease => disease.Name)
                .HasMaxLength(HealthDefaults.NameMaximumLength).IsRequired();
            builder.Property(disease => disease.Symptoms)
                .HasMaxLength(HealthDefaults.SymptomsMaximumLength);
            builder.Property(disease => disease.Notes)
                .HasMaxLength(HealthDefaults.NotesMaximumLength);
            builder.Property(disease => disease.Visibility)
                .HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(disease => disease.CreatedAt).IsRequired();
            builder.Property(disease => disease.UpdatedAt).IsRequired();
            builder.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_health_diseases_visibility",
                    "\"Visibility\" IN ('Public', 'Private')");
                table.HasCheckConstraint(
                    "CK_health_diseases_average_duration_days",
                    "\"AverageDurationDays\" IS NULL OR \"AverageDurationDays\" BETWEEN 1 AND 100000");
            });
            builder.HasOne<DiseaseCategory>()
                .WithMany()
                .HasForeignKey(disease => disease.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>()
                .WithMany()
                .HasForeignKey(disease => disease.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>()
                .WithMany()
                .HasForeignKey(disease => disease.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict);
            // Default disease ordering (name, identifier) and creator/visibility access.
            builder.HasIndex(disease => new { disease.Name, disease.Id });
            builder.HasIndex(disease => new { disease.CreatedBy, disease.Visibility, disease.Id });
            // Category reference migration and exact category filter.
            builder.HasIndex(disease => disease.CategoryId);
            // Visibility filter.
            builder.HasIndex(disease => disease.Visibility);
        });
    }

    private static void ConfigureMedicine(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Medicine>(builder =>
        {
            builder.ToTable("health_medicines");
            builder.HasKey(medicine => medicine.Id);
            builder.Property(medicine => medicine.Id).ValueGeneratedOnAdd();
            builder.Property(medicine => medicine.Name)
                .HasMaxLength(HealthDefaults.NameMaximumLength).IsRequired();
            builder.Property(medicine => medicine.Posology)
                .HasMaxLength(HealthDefaults.PosologyMaximumLength);
            builder.Property(medicine => medicine.RequiresPrescription).IsRequired();
            builder.Property(medicine => medicine.InventoryItemId);
            builder.Property(medicine => medicine.Notes)
                .HasMaxLength(HealthDefaults.NotesMaximumLength);
            builder.Property(medicine => medicine.Visibility)
                .HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(medicine => medicine.PrimaryAttachmentId);
            builder.Property(medicine => medicine.CreatedAt).IsRequired();
            builder.Property(medicine => medicine.UpdatedAt).IsRequired();
            builder.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_health_medicines_visibility",
                    "\"Visibility\" IN ('Public', 'Private')");
            });
            builder.HasOne<MedicineCategory>()
                .WithMany()
                .HasForeignKey(medicine => medicine.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>()
                .WithMany()
                .HasForeignKey(medicine => medicine.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>()
                .WithMany()
                .HasForeignKey(medicine => medicine.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict);
            // Default medicine ordering (name, identifier) and creator/visibility access.
            builder.HasIndex(medicine => new { medicine.Name, medicine.Id });
            builder.HasIndex(medicine => new { medicine.CreatedBy, medicine.Visibility, medicine.Id });
            // Category reference migration and exact category filter.
            builder.HasIndex(medicine => medicine.CategoryId);
            // Exact prescription and visibility filters.
            builder.HasIndex(medicine => medicine.RequiresPrescription);
            builder.HasIndex(medicine => medicine.Visibility);
            // Medicine item-reference lookup for the Inventory deletion contract (Wave 6).
            builder.HasIndex(medicine => medicine.InventoryItemId)
                .HasFilter("\"InventoryItemId\" IS NOT NULL");
        });
    }

    private static void ConfigureDiseaseMedicine(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DiseaseMedicine>(builder =>
        {
            builder.ToTable("health_disease_medicines");
            builder.HasKey(association => association.Id);
            builder.Property(association => association.Id).ValueGeneratedOnAdd();
            builder.Property(association => association.DiseaseId).IsRequired();
            builder.Property(association => association.MedicineId).IsRequired();
            builder.HasOne<Disease>()
                .WithMany()
                .HasForeignKey(association => association.DiseaseId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<Medicine>()
                .WithMany()
                .HasForeignKey(association => association.MedicineId)
                .OnDelete(DeleteBehavior.Cascade);
            // Unique pair and disease-side join lookup.
            builder.HasIndex(association => new { association.DiseaseId, association.MedicineId }).IsUnique();
            // Medicine-side join lookup.
            builder.HasIndex(association => association.MedicineId);
        });
    }
}
