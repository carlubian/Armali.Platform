using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Destinations.Domain;

/// <summary>
/// The Destinations-owned destination category catalogue row. It mirrors the
/// module-owned catalogue shape (display name, normalized name for case-insensitive
/// uniqueness, declaration order, and audit metadata) while remaining owned by
/// Destinations and surfaced through Configuration. Because every destination requires
/// a category, a referenced value may only be replaced; it is never cleared.
/// </summary>
internal sealed class DestinationCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}

/// <summary>
/// The Destinations-owned place category catalogue row. It shares the module-owned
/// catalogue shape with <see cref="DestinationCategory"/>. Because every place requires
/// a category, a referenced value may only be replaced; it is never cleared.
/// </summary>
internal sealed class PlaceCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}

/// <summary>The editable fields of a destination, independent of audit metadata.</summary>
internal sealed record DestinationValues(
    string? Name,
    int CategoryId,
    string? Country,
    string? EntryRequirements,
    bool IsSchengenArea,
    string? Notes,
    RecordVisibility Visibility);

/// <summary>
/// A visited place such as a city, region, country, or natural area. The destination
/// owns a managed collection of <see cref="Place"/> sub-resources and zero or more
/// attachments with an optional primary image. The category reference is required and
/// points to the module-owned <see cref="DestinationCategory"/> catalogue. The
/// destination carries no rating of its own; its average is derived on demand from its
/// places and is never persisted.
/// </summary>
internal sealed class Destination
{
    private Destination()
    {
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int CategoryId { get; private set; }
    public string? Country { get; private set; }
    public string? EntryRequirements { get; private set; }
    public bool IsSchengenArea { get; private set; }
    public string? Notes { get; private set; }
    public RecordVisibility Visibility { get; private set; }
    public int? PrimaryAttachmentId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int UpdatedBy { get; private set; }

    public static Destination Create(DestinationValues values, UserId creatorId, DateTimeOffset now)
    {
        DestinationsValidation.EnsureUtc(now);
        var destination = new Destination
        {
            CreatedAt = now,
            CreatedBy = creatorId.Value,
            UpdatedAt = now,
            UpdatedBy = creatorId.Value,
        };
        destination.Apply(values, creatorId, now, isCreation: true);
        return destination;
    }

    public void Update(DestinationValues values, UserId actorId, DateTimeOffset now) =>
        Apply(values, actorId, now, isCreation: false);

    internal void SetPrimaryAttachment(int? attachmentId, UserId actorId, DateTimeOffset now)
    {
        DestinationsValidation.EnsureUtc(now);
        PrimaryAttachmentId = attachmentId;
        StampModification(actorId, now);
    }

    internal void ClearPrimaryAttachmentIf(int attachmentId, UserId actorId, DateTimeOffset now)
    {
        if (PrimaryAttachmentId != attachmentId)
        {
            return;
        }

        DestinationsValidation.EnsureUtc(now);
        PrimaryAttachmentId = null;
        StampModification(actorId, now);
    }

    /// <summary>
    /// Re-points the category reference to <paramref name="categoryId"/> during a
    /// Configuration category migration. The category is required, so it is replaced
    /// rather than cleared.
    /// </summary>
    internal void ReplaceCategory(int categoryId, UserId actorId, DateTimeOffset now)
    {
        DestinationsValidation.EnsureUtc(now);
        DestinationsValidation.EnsurePositiveIdentifier(categoryId, "Category identifier");
        CategoryId = categoryId;
        StampModification(actorId, now);
    }

    private void Apply(DestinationValues values, UserId actorId, DateTimeOffset now, bool isCreation)
    {
        ArgumentNullException.ThrowIfNull(values);
        DestinationsValidation.EnsureUtc(now);

        var name = DestinationsValidation.ValidateName(values.Name);
        DestinationsValidation.EnsurePositiveIdentifier(values.CategoryId, "Category identifier");
        var country = DestinationsValidation.ValidateCountry(values.Country);
        var entryRequirements = DestinationsValidation.ValidateEntryRequirements(values.EntryRequirements);
        var notes = DestinationsValidation.ValidateNotes(values.Notes);
        if (!Enum.IsDefined(values.Visibility))
        {
            throw new DestinationsValidationException("Visibility is invalid.");
        }

        if (!isCreation && values.Visibility != Visibility && actorId.Value != CreatedBy)
        {
            throw new DestinationsValidationException(
                "Only the creator may change destination visibility.",
                DestinationsValidationReason.VisibilityForbidden);
        }

        Name = name;
        CategoryId = values.CategoryId;
        Country = country;
        EntryRequirements = entryRequirements;
        IsSchengenArea = values.IsSchengenArea;
        Notes = notes;
        Visibility = values.Visibility;
        StampModification(actorId, now);
    }

    private void StampModification(UserId actorId, DateTimeOffset now)
    {
        UpdatedAt = now;
        UpdatedBy = actorId.Value;
    }
}

/// <summary>The editable fields of a place, independent of audit metadata.</summary>
internal sealed record PlaceValues(
    string? Name,
    int CategoryId,
    int? Rating,
    string? Review,
    string? Address);

/// <summary>
/// A rateable spot located inside exactly one destination, such as a hotel, restaurant,
/// bar, or museum. The place is a managed sub-resource edited individually and always
/// belongs to a single owning destination; it is never shared. The category reference
/// is required and points to the module-owned <see cref="PlaceCategory"/> catalogue.
/// The place carries one optional 1-5 rating and one optional review; it has no
/// attachments and no rating history. Places inherit the visibility and authorization
/// of their owning destination.
/// </summary>
internal sealed class Place
{
    private Place()
    {
    }

    public int Id { get; private set; }
    public int DestinationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int CategoryId { get; private set; }
    public int? Rating { get; private set; }
    public string? Review { get; private set; }
    public string? Address { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int UpdatedBy { get; private set; }

    public static Place Create(int destinationId, PlaceValues values, UserId creatorId, DateTimeOffset now)
    {
        DestinationsValidation.EnsureUtc(now);
        DestinationsValidation.EnsurePositiveIdentifier(destinationId, "Destination identifier");
        var place = new Place
        {
            DestinationId = destinationId,
            CreatedAt = now,
            CreatedBy = creatorId.Value,
            UpdatedAt = now,
            UpdatedBy = creatorId.Value,
        };
        place.Apply(values, creatorId, now);
        return place;
    }

    public void Update(PlaceValues values, UserId actorId, DateTimeOffset now) =>
        Apply(values, actorId, now);

    /// <summary>
    /// Re-points the category reference to <paramref name="categoryId"/> during a
    /// Configuration category migration. The category is required, so it is replaced
    /// rather than cleared.
    /// </summary>
    internal void ReplaceCategory(int categoryId, UserId actorId, DateTimeOffset now)
    {
        DestinationsValidation.EnsureUtc(now);
        DestinationsValidation.EnsurePositiveIdentifier(categoryId, "Category identifier");
        CategoryId = categoryId;
        StampModification(actorId, now);
    }

    private void Apply(PlaceValues values, UserId actorId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(values);
        DestinationsValidation.EnsureUtc(now);

        var name = DestinationsValidation.ValidatePlaceName(values.Name);
        DestinationsValidation.EnsurePositiveIdentifier(values.CategoryId, "Category identifier");
        var rating = DestinationsValidation.ValidateRating(values.Rating);
        var review = DestinationsValidation.ValidateReview(values.Review);
        var address = DestinationsValidation.ValidateAddress(values.Address);

        Name = name;
        CategoryId = values.CategoryId;
        Rating = rating;
        Review = review;
        Address = address;
        StampModification(actorId, now);
    }

    private void StampModification(UserId actorId, DateTimeOffset now)
    {
        UpdatedAt = now;
        UpdatedBy = actorId.Value;
    }
}
