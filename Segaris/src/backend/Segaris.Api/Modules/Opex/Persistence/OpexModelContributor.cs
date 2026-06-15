using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Opex.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Opex.Persistence;

internal sealed class OpexModelContributor : ISegarisModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OpexCategory>(builder =>
        {
            builder.ToTable("opex_categories");
            builder.HasKey(category => category.Id);
            builder.Property(category => category.Id).ValueGeneratedOnAdd();
            builder.Property(category => category.Name)
                .HasMaxLength(OpexCategoryNormalization.NameMaximumLength).IsRequired();
            builder.Property(category => category.NormalizedName)
                .HasMaxLength(OpexCategoryNormalization.NameMaximumLength).IsRequired();
            builder.Property(category => category.SortOrder).IsRequired();
            builder.Property(category => category.CreatedAt).IsRequired();
            builder.Property(category => category.UpdatedAt).IsRequired();
            builder.HasIndex(category => category.NormalizedName).IsUnique();
            builder.HasIndex(category => category.SortOrder);
        });

        modelBuilder.Entity<OpexContract>(builder =>
        {
            builder.ToTable("opex_contracts");
            builder.HasKey(contract => contract.Id);
            builder.Property(contract => contract.Id).ValueGeneratedOnAdd();
            builder.Property(contract => contract.Name)
                .HasMaxLength(OpexValidation.ContractNameMaximumLength).IsRequired();
            builder.Property(contract => contract.NormalizedName)
                .HasMaxLength(OpexValidation.ContractNameMaximumLength).IsRequired();
            builder.Property(contract => contract.MovementType).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(contract => contract.Status).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(contract => contract.ExpectedFrequency).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(contract => contract.StartDate);
            builder.Property(contract => contract.ClosedDate);
            builder.Property(contract => contract.EstimatedAnnualAmount).HasPrecision(18, 2);
            builder.Property(contract => contract.Notes).HasMaxLength(OpexValidation.NotesMaximumLength);
            builder.Property(contract => contract.Visibility).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(contract => contract.CreatedAt).IsRequired();
            builder.Property(contract => contract.UpdatedAt).IsRequired();
            builder.ToTable(table =>
            {
                table.HasCheckConstraint("CK_opex_contracts_movement_type", "\"MovementType\" IN ('Income', 'Expense')");
                table.HasCheckConstraint("CK_opex_contracts_status", "\"Status\" IN ('Planning', 'Active', 'OnHold', 'Closed')");
                table.HasCheckConstraint("CK_opex_contracts_frequency", "\"ExpectedFrequency\" IN ('None', 'Weekly', 'Monthly', 'Quarterly', 'SemiAnnual', 'Annual', 'Irregular')");
                table.HasCheckConstraint("CK_opex_contracts_visibility", "\"Visibility\" IN ('Public', 'Private')");
                table.HasCheckConstraint("CK_opex_contracts_estimated_annual_amount", "\"EstimatedAnnualAmount\" IS NULL OR \"EstimatedAnnualAmount\" >= 0");
            });
            builder.HasMany(contract => contract.Occurrences).WithOne().HasForeignKey(occurrence => occurrence.ContractId).OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(contract => contract.Occurrences).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.HasOne<OpexCategory>().WithMany().HasForeignKey(contract => contract.CategoryId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisSupplier>().WithMany().HasForeignKey(contract => contract.SupplierId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisCostCenter>().WithMany().HasForeignKey(contract => contract.CostCenterId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisCurrency>().WithMany().HasForeignKey(contract => contract.CurrencyId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(contract => contract.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(contract => contract.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(contract => contract.NormalizedName).IsUnique();
            builder.HasIndex(contract => new { contract.Name, contract.Id });
            builder.HasIndex(contract => new { contract.CreatedBy, contract.Visibility, contract.Id });
            builder.HasIndex(contract => contract.CategoryId);
            builder.HasIndex(contract => contract.SupplierId);
            builder.HasIndex(contract => contract.CostCenterId);
            builder.HasIndex(contract => contract.CurrencyId);
            builder.HasIndex(contract => contract.UpdatedBy);
        });

        modelBuilder.Entity<OpexOccurrence>(builder =>
        {
            builder.ToTable("opex_occurrences");
            builder.HasKey(occurrence => occurrence.Id);
            builder.Property(occurrence => occurrence.Id).ValueGeneratedOnAdd();
            builder.Property(occurrence => occurrence.ContractId).IsRequired();
            builder.Property(occurrence => occurrence.EffectiveDate).IsRequired();
            builder.Property(occurrence => occurrence.ActualAmount).HasPrecision(18, 2).IsRequired();
            builder.Property(occurrence => occurrence.Description).HasMaxLength(OpexValidation.DescriptionMaximumLength);
            builder.Property(occurrence => occurrence.Notes).HasMaxLength(OpexValidation.NotesMaximumLength);
            builder.Property(occurrence => occurrence.CreatedAt).IsRequired();
            builder.Property(occurrence => occurrence.UpdatedAt).IsRequired();
            builder.ToTable(table =>
            {
                table.HasCheckConstraint("CK_opex_occurrences_actual_amount", "\"ActualAmount\" >= 0");
            });
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(occurrence => occurrence.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(occurrence => occurrence.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(occurrence => new { occurrence.ContractId, occurrence.EffectiveDate, occurrence.Id });
            builder.HasIndex(occurrence => occurrence.CreatedBy);
            builder.HasIndex(occurrence => occurrence.UpdatedBy);
        });
    }
}
