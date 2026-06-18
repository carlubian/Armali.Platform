using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Clothes.Domain;

/// <summary>
/// A Clothes-owned catalog row (category or colour). It mirrors the shared-catalog
/// shape (display name, normalized name for case-insensitive uniqueness, declaration
/// order, and audit metadata) while remaining owned by Clothes and surfaced through
/// Configuration.
/// </summary>
internal sealed class ClothingCategory
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
/// The Clothes-owned colour catalog row. Unlike the other module-owned catalogs it
/// carries a <see cref="ColorValue"/> (a canonical <c>#RRGGBB</c> hex string) used to
/// render a swatch in the editor, the gallery, and Configuration. See
/// <see cref="ClothingCategory"/> for the shared shape.
/// </summary>
internal sealed class ClothingColor
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string ColorValue { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}

/// <summary>The editable fields of a garment, independent of audit metadata.</summary>
internal sealed record ClothesGarmentValues(
    string Name,
    int CategoryId,
    ClothesGarmentStatus Status,
    string? Size,
    IReadOnlyList<int> ColorIds,
    WashingCare? WashingCare,
    DryingCare? DryingCare,
    IroningCare? IroningCare,
    DryCleaningCare? DryCleaningCare,
    string? Notes,
    RecordVisibility Visibility);

/// <summary>
/// A single garment or accessory in the household wardrobe. The garment owns its
/// category reference, descriptive attributes, the four independent textile-care
/// axes, its optional colour set, its visibility, and an optional primary-image
/// reference resolved by the attachment subsystem. There is no laundry state,
/// purchase or cost information, and no outfit grouping.
/// </summary>
internal sealed class ClothesGarment
{
    private readonly List<ClothesGarmentColor> colors = [];

    private ClothesGarment()
    {
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int CategoryId { get; private set; }
    public ClothesGarmentStatus Status { get; private set; }
    public string? Size { get; private set; }
    public WashingCare? WashingCare { get; private set; }
    public DryingCare? DryingCare { get; private set; }
    public IroningCare? IroningCare { get; private set; }
    public DryCleaningCare? DryCleaningCare { get; private set; }
    public string? Notes { get; private set; }
    public RecordVisibility Visibility { get; private set; }
    public int? PrimaryAttachmentId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int UpdatedBy { get; private set; }
    public IReadOnlyList<ClothesGarmentColor> Colors => colors;

    public static ClothesGarment Create(ClothesGarmentValues values, UserId creatorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        var garment = new ClothesGarment
        {
            CreatedAt = now,
            CreatedBy = creatorId.Value,
            UpdatedAt = now,
            UpdatedBy = creatorId.Value,
        };
        garment.Apply(values, creatorId, now);
        return garment;
    }

    public void Update(ClothesGarmentValues values, UserId actorId, DateTimeOffset now)
    {
        Apply(values, actorId, now);
    }

    /// <summary>
    /// Re-points the required category to <paramref name="categoryId"/> during a
    /// Configuration category migration. The category is required, so it is replaced
    /// rather than cleared.
    /// </summary>
    internal void ReplaceCategory(int categoryId, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        if (categoryId <= 0)
        {
            throw new ClothesValidationException("Catalog identifiers must be positive.");
        }

        CategoryId = categoryId;
        StampModification(actorId, now);
    }

    /// <summary>
    /// Re-points the optional colour association from <paramref name="sourceColorId"/>
    /// to <paramref name="targetColorId"/> during a Configuration colour migration. The
    /// source association is removed and the target added unless the garment already
    /// references it, so the garment never gains a duplicate colour. The garment is
    /// untouched and unstamped when it did not reference the source colour.
    /// </summary>
    internal void ReplaceColor(int sourceColorId, int targetColorId, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        if (sourceColorId <= 0 || targetColorId <= 0)
        {
            throw new ClothesValidationException("Catalog identifiers must be positive.");
        }

        if (colors.RemoveAll(association => association.ColorId == sourceColorId) == 0)
        {
            return;
        }

        if (colors.All(association => association.ColorId != targetColorId))
        {
            colors.Add(new ClothesGarmentColor { ColorId = targetColorId });
        }

        StampModification(actorId, now);
    }

    /// <summary>
    /// Removes the optional colour association <paramref name="colorId"/> during a
    /// Configuration colour-clearing migration. The garment is untouched and unstamped
    /// when it did not reference the colour.
    /// </summary>
    internal void ClearColor(int colorId, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        if (colorId <= 0)
        {
            throw new ClothesValidationException("Catalog identifiers must be positive.");
        }

        if (colors.RemoveAll(association => association.ColorId == colorId) > 0)
        {
            StampModification(actorId, now);
        }
    }

    private void Apply(ClothesGarmentValues values, UserId actorId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(values);
        EnsureUtc(now);

        var name = ClothesValidation.ValidateGarmentName(values.Name);
        var size = ClothesValidation.ValidateSize(values.Size);
        var notes = ClothesValidation.ValidateNotes(values.Notes);

        if (!Enum.IsDefined(values.Status) || !Enum.IsDefined(values.Visibility))
        {
            throw new ClothesValidationException("Status or visibility is invalid.");
        }

        ClothesValidation.ValidateCareAxes(
            values.WashingCare,
            values.DryingCare,
            values.IroningCare,
            values.DryCleaningCare);

        if (values.CategoryId <= 0)
        {
            throw new ClothesValidationException("Catalog identifiers must be positive.");
        }

        ReconcileColors(values.ColorIds);

        Name = name;
        CategoryId = values.CategoryId;
        Status = values.Status;
        Size = size;
        WashingCare = values.WashingCare;
        DryingCare = values.DryingCare;
        IroningCare = values.IroningCare;
        DryCleaningCare = values.DryCleaningCare;
        Notes = notes;
        Visibility = values.Visibility;
        StampModification(actorId, now);
    }

    /// <summary>
    /// Replaces the colour set with the distinct requested colours, deduplicating
    /// repeated identifiers. Colour never blocks saving: an empty set is valid.
    /// </summary>
    private void ReconcileColors(IReadOnlyList<int> colorIds)
    {
        ArgumentNullException.ThrowIfNull(colorIds);
        if (colorIds.Any(id => id <= 0))
        {
            throw new ClothesValidationException("Catalog identifiers must be positive.");
        }

        var requested = new HashSet<int>(colorIds);
        colors.RemoveAll(association => !requested.Contains(association.ColorId));
        foreach (var colorId in requested)
        {
            if (colors.All(association => association.ColorId != colorId))
            {
                colors.Add(new ClothesGarmentColor { ColorId = colorId });
            }
        }
    }

    private void StampModification(UserId actorId, DateTimeOffset now)
    {
        UpdatedAt = now;
        UpdatedBy = actorId.Value;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ClothesValidationException("Technical timestamps must use UTC.");
        }
    }
}

/// <summary>
/// The many-to-many association between a garment and a colour from the
/// <see cref="ClothingColor"/> catalog. It carries no audit metadata of its own; the
/// owning garment's modification metadata records changes to the colour set, and a
/// per-pair uniqueness constraint prevents a garment referencing a colour twice.
/// </summary>
internal sealed class ClothesGarmentColor
{
    public int GarmentId { get; set; }
    public int ColorId { get; set; }
}
