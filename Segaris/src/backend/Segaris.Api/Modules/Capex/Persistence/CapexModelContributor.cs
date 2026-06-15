using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Capex.Domain;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Identity;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Capex.Persistence;

internal sealed class CapexModelContributor : ISegarisModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CapexCategory>(builder =>
        {
            builder.ToTable("capex_categories");
            builder.HasKey(category => category.Id);
            builder.Property(category => category.Id).ValueGeneratedOnAdd();
            builder.Property(category => category.Name)
                .HasMaxLength(CapexCategoryNormalization.NameMaximumLength).IsRequired();
            builder.Property(category => category.NormalizedName)
                .HasMaxLength(CapexCategoryNormalization.NameMaximumLength).IsRequired();
            builder.Property(category => category.SortOrder).IsRequired();
            builder.Property(category => category.CreatedAt).IsRequired();
            builder.Property(category => category.UpdatedAt).IsRequired();
            builder.HasIndex(category => category.NormalizedName).IsUnique();
            builder.HasIndex(category => category.SortOrder);
        });

        modelBuilder.Entity<CapexEntry>(builder =>
        {
            builder.ToTable("capex_entries");
            builder.HasKey(entry => entry.Id);
            builder.Property(entry => entry.Id).ValueGeneratedOnAdd();
            builder.Property(entry => entry.Title).HasMaxLength(200).IsRequired();
            builder.Property(entry => entry.MovementType).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(entry => entry.Status).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(entry => entry.DueDate).IsRequired();
            builder.Property(entry => entry.Notes).HasMaxLength(4000);
            builder.Property(entry => entry.Visibility).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(entry => entry.TotalAmount).HasPrecision(18, 2).IsRequired();
            builder.Property(entry => entry.CreatedAt).IsRequired();
            builder.Property(entry => entry.UpdatedAt).IsRequired();
            builder.ToTable(table =>
            {
                table.HasCheckConstraint("CK_capex_entries_movement_type", "\"MovementType\" IN ('Income', 'Expense')");
                table.HasCheckConstraint("CK_capex_entries_status", "\"Status\" IN ('Planning', 'Completed', 'Canceled')");
                table.HasCheckConstraint("CK_capex_entries_visibility", "\"Visibility\" IN ('Public', 'Private')");
                table.HasCheckConstraint("CK_capex_entries_total_amount", "\"TotalAmount\" >= 0");
            });
            builder.HasMany(entry => entry.Items).WithOne().HasForeignKey(item => item.EntryId).OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(entry => entry.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.HasOne<CapexCategory>().WithMany().HasForeignKey(entry => entry.CategoryId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisSupplier>().WithMany().HasForeignKey(entry => entry.SupplierId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisCostCenter>().WithMany().HasForeignKey(entry => entry.CostCenterId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisCurrency>().WithMany().HasForeignKey(entry => entry.CurrencyId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(entry => entry.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(entry => entry.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(entry => new { entry.DueDate, entry.Id });
            builder.HasIndex(entry => new { entry.Status, entry.DueDate, entry.Id });
            builder.HasIndex(entry => new { entry.CreatedBy, entry.Visibility, entry.Id });
            builder.HasIndex(entry => entry.CategoryId);
            builder.HasIndex(entry => entry.SupplierId);
            builder.HasIndex(entry => entry.CostCenterId);
            builder.HasIndex(entry => entry.CurrencyId);
        });

        modelBuilder.Entity<CapexItem>(builder =>
        {
            builder.ToTable("capex_items");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Id).ValueGeneratedOnAdd();
            builder.Property(item => item.Description).HasMaxLength(300).IsRequired();
            builder.Property(item => item.Quantity).HasPrecision(18, 2).IsRequired();
            builder.Property(item => item.UnitAmount).HasPrecision(18, 2).IsRequired();
            builder.Property(item => item.LineAmount).HasPrecision(18, 2).IsRequired();
            builder.ToTable(table =>
            {
                table.HasCheckConstraint("CK_capex_items_position", "\"Position\" >= 0");
                table.HasCheckConstraint("CK_capex_items_quantity", "\"Quantity\" > 0");
                table.HasCheckConstraint("CK_capex_items_unit_amount", "\"UnitAmount\" >= 0");
                table.HasCheckConstraint("CK_capex_items_line_amount", "\"LineAmount\" >= 0");
            });
            builder.HasIndex(item => new { item.EntryId, item.Position }).IsUnique();
        });
    }
}
