using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Travel.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Travel.Persistence;

internal sealed class TravelModelContributor : ISegarisModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        ConfigureCatalog<TravelTripType>(modelBuilder, "travel_trip_types");
        ConfigureCatalog<TravelExpenseCategory>(modelBuilder, "travel_expense_categories");

        modelBuilder.Entity<TravelTrip>(builder =>
        {
            builder.ToTable("travel_trips");
            builder.HasKey(trip => trip.Id);
            builder.Property(trip => trip.Id).ValueGeneratedOnAdd();
            builder.Property(trip => trip.Name).HasMaxLength(TravelValidation.NameMaxLength).IsRequired();
            builder.Property(trip => trip.Destination).HasMaxLength(TravelValidation.DestinationMaxLength);
            builder.Property(trip => trip.StartDate).IsRequired();
            builder.Property(trip => trip.EndDate).IsRequired();
            builder.Property(trip => trip.Status).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(trip => trip.Notes).HasMaxLength(TravelValidation.NotesMaxLength);
            builder.Property(trip => trip.Visibility).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(trip => trip.CreatedAt).IsRequired();
            builder.Property(trip => trip.UpdatedAt);
            builder.ToTable(table =>
            {
                table.HasCheckConstraint("CK_travel_trips_status", "\"Status\" IN ('Planned', 'Ongoing', 'Completed', 'Cancelled')");
                table.HasCheckConstraint("CK_travel_trips_visibility", "\"Visibility\" IN ('Public', 'Private')");
                table.HasCheckConstraint("CK_travel_trips_dates", "\"EndDate\" >= \"StartDate\"");
            });
            builder.HasMany(trip => trip.Itinerary).WithOne().HasForeignKey(entry => entry.TripId).OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(trip => trip.Itinerary).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.HasOne<TravelTripType>().WithMany().HasForeignKey(trip => trip.TripTypeId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(trip => trip.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(trip => trip.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
            // Default trip ordering (start date descending, identifier descending), the
            // launcher attention scan (status plus start date), and creator/visibility access.
            builder.HasIndex(trip => new { trip.StartDate, trip.Id });
            builder.HasIndex(trip => new { trip.CreatedBy, trip.Visibility, trip.Id });
            builder.HasIndex(trip => new { trip.Status, trip.StartDate });
            // Exact filters and reference migration.
            builder.HasIndex(trip => trip.TripTypeId);
            builder.HasIndex(trip => trip.Visibility);
            builder.HasIndex(trip => trip.UpdatedBy);
        });

        modelBuilder.Entity<TravelItineraryEntry>(builder =>
        {
            builder.ToTable("travel_itinerary_entries");
            builder.HasKey(entry => entry.Id);
            builder.Property(entry => entry.Id).ValueGeneratedOnAdd();
            builder.Property(entry => entry.TripId).IsRequired();
            builder.Property(entry => entry.Date).IsRequired();
            builder.Property(entry => entry.Time);
            builder.Property(entry => entry.Title).HasMaxLength(TravelValidation.ItineraryTitleMaxLength).IsRequired();
            builder.Property(entry => entry.Place).HasMaxLength(TravelValidation.ItineraryPlaceMaxLength);
            builder.Property(entry => entry.ReservationLocator).HasMaxLength(TravelValidation.ItineraryReservationLocatorMaxLength);
            builder.Property(entry => entry.Note).HasMaxLength(TravelValidation.ItineraryNoteMaxLength);
            builder.Property(entry => entry.SortOrder).IsRequired();
            // Deterministic per-trip itinerary ordering (date, time, stable insertion sequence).
            builder.HasIndex(entry => new { entry.TripId, entry.Date, entry.Time, entry.SortOrder });
        });

        modelBuilder.Entity<TravelExpense>(builder =>
        {
            builder.ToTable("travel_expenses");
            builder.HasKey(expense => expense.Id);
            builder.Property(expense => expense.Id).ValueGeneratedOnAdd();
            builder.Property(expense => expense.TripId).IsRequired();
            builder.Property(expense => expense.Description).HasMaxLength(TravelValidation.ExpenseDescriptionMaxLength).IsRequired();
            builder.Property(expense => expense.Date).IsRequired();
            builder.Property(expense => expense.Amount).HasPrecision(18, 2).IsRequired();
            builder.Property(expense => expense.Notes).HasMaxLength(TravelValidation.ExpenseNotesMaxLength);
            builder.Property(expense => expense.CreatedAt).IsRequired();
            builder.Property(expense => expense.UpdatedAt);
            builder.ToTable(table =>
            {
                table.HasCheckConstraint("CK_travel_expenses_amount", "\"Amount\" >= 0");
            });
            builder.HasOne<TravelTrip>().WithMany().HasForeignKey(expense => expense.TripId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<TravelExpenseCategory>().WithMany().HasForeignKey(expense => expense.ExpenseCategoryId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisCurrency>().WithMany().HasForeignKey(expense => expense.CurrencyId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisSupplier>().WithMany().HasForeignKey(expense => expense.SupplierId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisCostCenter>().WithMany().HasForeignKey(expense => expense.CostCenterId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(expense => expense.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(expense => expense.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
            // Per-trip expense lookups, default ordering, and per-currency total aggregation.
            builder.HasIndex(expense => new { expense.TripId, expense.Id });
            builder.HasIndex(expense => new { expense.TripId, expense.CurrencyId });
            // Exact filters and reference migration.
            builder.HasIndex(expense => expense.ExpenseCategoryId);
            builder.HasIndex(expense => expense.CurrencyId);
            builder.HasIndex(expense => expense.SupplierId);
            builder.HasIndex(expense => expense.CostCenterId);
            builder.HasIndex(expense => expense.UpdatedBy);
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
            builder.Property<string>("Name").HasMaxLength(TravelValidation.CatalogNameMaxLength).IsRequired();
            builder.Property<string>("NormalizedName").HasMaxLength(TravelValidation.CatalogNameMaxLength).IsRequired();
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
