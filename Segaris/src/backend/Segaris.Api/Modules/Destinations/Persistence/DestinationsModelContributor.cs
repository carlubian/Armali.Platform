using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Destinations.Domain;
using Segaris.Api.Modules.Identity;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Destinations.Persistence;

internal sealed class DestinationsModelContributor : ISegarisModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        ConfigureDestinationCategory(modelBuilder);
        ConfigurePlaceCategory(modelBuilder);
        ConfigureDestination(modelBuilder);
        ConfigurePlace(modelBuilder);
    }

    private static void ConfigureDestinationCategory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DestinationCategory>(builder =>
        {
            builder.ToTable("destination_categories");
            builder.HasKey(category => category.Id);
            builder.Property(category => category.Id).ValueGeneratedOnAdd();
            builder.Property(category => category.Name)
                .HasMaxLength(DestinationsDefaults.CategoryNameMaximumLength).IsRequired();
            builder.Property(category => category.NormalizedName)
                .HasMaxLength(DestinationsDefaults.CategoryNameMaximumLength).IsRequired();
            builder.Property(category => category.SortOrder).IsRequired();
            builder.Property(category => category.CreatedAt).IsRequired();
            builder.Property(category => category.UpdatedAt).IsRequired();
            builder.Property(category => category.CreatedBy);
            builder.Property(category => category.UpdatedBy);
            builder.HasIndex(category => category.NormalizedName).IsUnique();
            builder.HasIndex(category => category.SortOrder);
        });
    }

    private static void ConfigurePlaceCategory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlaceCategory>(builder =>
        {
            builder.ToTable("place_categories");
            builder.HasKey(category => category.Id);
            builder.Property(category => category.Id).ValueGeneratedOnAdd();
            builder.Property(category => category.Name)
                .HasMaxLength(DestinationsDefaults.PlaceCategoryNameMaximumLength).IsRequired();
            builder.Property(category => category.NormalizedName)
                .HasMaxLength(DestinationsDefaults.PlaceCategoryNameMaximumLength).IsRequired();
            builder.Property(category => category.SortOrder).IsRequired();
            builder.Property(category => category.CreatedAt).IsRequired();
            builder.Property(category => category.UpdatedAt).IsRequired();
            builder.Property(category => category.CreatedBy);
            builder.Property(category => category.UpdatedBy);
            builder.HasIndex(category => category.NormalizedName).IsUnique();
            builder.HasIndex(category => category.SortOrder);
        });
    }

    private static void ConfigureDestination(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Destination>(builder =>
        {
            builder.ToTable("destinations");
            builder.HasKey(destination => destination.Id);
            builder.Property(destination => destination.Id).ValueGeneratedOnAdd();
            builder.Property(destination => destination.Name)
                .HasMaxLength(DestinationsDefaults.NameMaximumLength).IsRequired();
            builder.Property(destination => destination.Country)
                .HasMaxLength(DestinationsDefaults.CountryMaximumLength);
            builder.Property(destination => destination.EntryRequirements)
                .HasMaxLength(DestinationsDefaults.EntryRequirementsMaximumLength);
            builder.Property(destination => destination.IsSchengenArea).IsRequired();
            builder.Property(destination => destination.Notes)
                .HasMaxLength(DestinationsDefaults.NotesMaximumLength);
            builder.Property(destination => destination.Visibility)
                .HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(destination => destination.PrimaryAttachmentId);
            builder.Property(destination => destination.CreatedAt).IsRequired();
            builder.Property(destination => destination.UpdatedAt).IsRequired();
            builder.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_destinations_visibility",
                    "\"Visibility\" IN ('Public', 'Private')");
            });
            builder.HasOne<DestinationCategory>()
                .WithMany()
                .HasForeignKey(destination => destination.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>()
                .WithMany()
                .HasForeignKey(destination => destination.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>()
                .WithMany()
                .HasForeignKey(destination => destination.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict);
            // Default destination ordering (name, identifier) and creator/visibility access.
            builder.HasIndex(destination => new { destination.Name, destination.Id });
            builder.HasIndex(destination => new { destination.CreatedBy, destination.Visibility, destination.Id });
            // Category reference migration and category filter.
            builder.HasIndex(destination => destination.CategoryId);
            // Exact filters.
            builder.HasIndex(destination => destination.Visibility);
            builder.HasIndex(destination => destination.IsSchengenArea);
        });
    }

    private static void ConfigurePlace(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Place>(builder =>
        {
            builder.ToTable("destination_places");
            builder.HasKey(place => place.Id);
            builder.Property(place => place.Id).ValueGeneratedOnAdd();
            builder.Property(place => place.DestinationId).IsRequired();
            builder.Property(place => place.Name)
                .HasMaxLength(DestinationsDefaults.PlaceNameMaximumLength).IsRequired();
            builder.Property(place => place.Review)
                .HasMaxLength(DestinationsDefaults.PlaceReviewMaximumLength);
            builder.Property(place => place.Address)
                .HasMaxLength(DestinationsDefaults.PlaceAddressMaximumLength);
            builder.Property(place => place.CreatedAt).IsRequired();
            builder.Property(place => place.UpdatedAt).IsRequired();
            builder.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_destination_places_rating",
                    "\"Rating\" IS NULL OR \"Rating\" BETWEEN 1 AND 5");
            });
            builder.HasOne<Destination>()
                .WithMany()
                .HasForeignKey(place => place.DestinationId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<PlaceCategory>()
                .WithMany()
                .HasForeignKey(place => place.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>()
                .WithMany()
                .HasForeignKey(place => place.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>()
                .WithMany()
                .HasForeignKey(place => place.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict);
            // Destination-scoped default ordering (name, identifier) and scope lookup.
            builder.HasIndex(place => new { place.DestinationId, place.Name, place.Id });
            // Scoped category filter.
            builder.HasIndex(place => new { place.DestinationId, place.CategoryId });
            // Scoped rating filter and sort.
            builder.HasIndex(place => new { place.DestinationId, place.Rating });
            // Place category reference migration.
            builder.HasIndex(place => place.CategoryId);
        });
    }
}
