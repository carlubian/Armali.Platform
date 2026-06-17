using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Inventory.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Inventory.Persistence;

internal sealed class InventoryModelContributor : ISegarisModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        ConfigureCatalog<InventoryCategory>(modelBuilder, "inventory_categories");
        ConfigureCatalog<InventoryLocation>(modelBuilder, "inventory_locations");

        modelBuilder.Entity<InventoryItem>(builder =>
        {
            builder.ToTable("inventory_items");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Id).ValueGeneratedOnAdd();
            builder.Property(item => item.Name)
                .HasMaxLength(InventoryValidation.ItemNameMaximumLength).IsRequired();
            builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(item => item.Notes).HasMaxLength(InventoryValidation.NotesMaximumLength);
            builder.Property(item => item.CurrentStock).HasPrecision(18, 2).IsRequired();
            builder.Property(item => item.MinimumStock).HasPrecision(18, 2).IsRequired();
            builder.Property(item => item.Visibility).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(item => item.CreatedAt).IsRequired();
            builder.Property(item => item.UpdatedAt).IsRequired();
            builder.ToTable(table =>
            {
                table.HasCheckConstraint("CK_inventory_items_status", "\"Status\" IN ('Candidate', 'Active', 'Deprecated')");
                table.HasCheckConstraint("CK_inventory_items_visibility", "\"Visibility\" IN ('Public', 'Private')");
                table.HasCheckConstraint("CK_inventory_items_current_stock", "\"CurrentStock\" >= 0");
                table.HasCheckConstraint("CK_inventory_items_minimum_stock", "\"MinimumStock\" >= 0");
            });
            builder.HasMany(item => item.Suppliers).WithOne().HasForeignKey(association => association.ItemId).OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(item => item.Suppliers).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.HasOne<InventoryCategory>().WithMany().HasForeignKey(item => item.CategoryId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<InventoryLocation>().WithMany().HasForeignKey(item => item.LocationId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(item => item.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(item => item.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
            // Default item ordering (name, identifier) and creator/visibility access.
            builder.HasIndex(item => new { item.Name, item.Id });
            builder.HasIndex(item => new { item.CreatedBy, item.Visibility, item.Id });
            // Launcher attention scans accessible active low-stock items.
            builder.HasIndex(item => new { item.Status, item.Visibility });
            // Exact filters and reference migration.
            builder.HasIndex(item => item.CategoryId);
            builder.HasIndex(item => item.LocationId);
            builder.HasIndex(item => item.Visibility);
            builder.HasIndex(item => item.UpdatedBy);
        });

        modelBuilder.Entity<InventoryItemSupplier>(builder =>
        {
            builder.ToTable("inventory_item_suppliers");
            builder.HasKey(association => new { association.ItemId, association.SupplierId });
            builder.HasOne<SegarisSupplier>().WithMany().HasForeignKey(association => association.SupplierId).OnDelete(DeleteBehavior.Restrict);
            // Supplier eligibility lookups and reference migration.
            builder.HasIndex(association => association.SupplierId);
        });

        modelBuilder.Entity<InventoryOrder>(builder =>
        {
            builder.ToTable("inventory_orders");
            builder.HasKey(order => order.Id);
            builder.Property(order => order.Id).ValueGeneratedOnAdd();
            builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(order => order.OrderDate);
            builder.Property(order => order.ExpectedReceiptDate);
            builder.Property(order => order.Notes).HasMaxLength(InventoryValidation.NotesMaximumLength);
            builder.Property(order => order.Visibility).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(order => order.CreatedAt).IsRequired();
            builder.Property(order => order.UpdatedAt).IsRequired();
            builder.ToTable(table =>
            {
                table.HasCheckConstraint("CK_inventory_orders_status", "\"Status\" IN ('Planning', 'Active', 'Received', 'Cancelled')");
                table.HasCheckConstraint("CK_inventory_orders_visibility", "\"Visibility\" IN ('Public', 'Private')");
            });
            builder.HasMany(order => order.Lines).WithOne().HasForeignKey(line => line.OrderId).OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(order => order.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.HasOne<SegarisSupplier>().WithMany().HasForeignKey(order => order.SupplierId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisCurrency>().WithMany().HasForeignKey(order => order.CurrencyId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(order => order.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(order => order.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
            // Default order ordering (order date, identifier) and creator/visibility access.
            builder.HasIndex(order => new { order.OrderDate, order.Id });
            builder.HasIndex(order => new { order.CreatedBy, order.Visibility, order.Id });
            // Exact filters and reference migration.
            builder.HasIndex(order => order.SupplierId);
            builder.HasIndex(order => order.CurrencyId);
            builder.HasIndex(order => order.Status);
            builder.HasIndex(order => order.Visibility);
            builder.HasIndex(order => order.UpdatedBy);
        });

        modelBuilder.Entity<InventoryOrderLine>(builder =>
        {
            builder.ToTable("inventory_order_lines");
            builder.HasKey(line => line.Id);
            builder.Property(line => line.Id).ValueGeneratedOnAdd();
            builder.Property(line => line.OrderId).IsRequired();
            builder.Property(line => line.ItemId).IsRequired();
            builder.Property(line => line.Quantity).HasPrecision(18, 2).IsRequired();
            builder.Property(line => line.LineTotal).HasPrecision(18, 2).IsRequired();
            builder.ToTable(table =>
            {
                table.HasCheckConstraint("CK_inventory_order_lines_quantity", "\"Quantity\" > 0");
                table.HasCheckConstraint("CK_inventory_order_lines_line_total", "\"LineTotal\" >= 0");
            });
            builder.HasOne<InventoryItem>().WithMany().HasForeignKey(line => line.ItemId).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(line => new { line.OrderId, line.Id });
            // Item-referenced-by-order deletion guard and order search across item names.
            builder.HasIndex(line => line.ItemId);
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
            builder.Property<string>("Name").HasMaxLength(InventoryValidation.CategoryNameMaximumLength).IsRequired();
            builder.Property<string>("NormalizedName").HasMaxLength(InventoryValidation.CategoryNameMaximumLength).IsRequired();
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
