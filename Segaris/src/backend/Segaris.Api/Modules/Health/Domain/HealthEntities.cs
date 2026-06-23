using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Health.Domain;

/// <summary>
/// The Health-owned disease category catalogue row. It mirrors the module-owned
/// catalogue shape (display name, normalized name for case-insensitive uniqueness,
/// declaration order, and audit metadata) while remaining owned by Health and
/// surfaced through Configuration. Because every disease requires a category, a
/// referenced value may only be replaced; it is never cleared.
/// </summary>
internal sealed class DiseaseCategory
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
/// The Health-owned medicine category catalogue row. It mirrors the module-owned
/// catalogue shape and is required on every medicine, so referenced values may only
/// be replaced and never cleared.
/// </summary>
internal sealed class MedicineCategory
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

/// <summary>The editable fields of a disease, independent of audit metadata.</summary>
internal sealed record DiseaseValues(
    string? Name,
    int CategoryId,
    string? Symptoms,
    int? AverageDurationDays,
    string? Notes,
    RecordVisibility Visibility);

/// <summary>The editable fields of a medicine, independent of audit metadata.</summary>
internal sealed record MedicineValues(
    string? Name,
    int CategoryId,
    string? Posology,
    bool RequiresPrescription,
    int? InventoryItemId,
    string? Notes,
    RecordVisibility Visibility);

/// <summary>
/// A household disease entry. The category reference is required and points to the
/// module-owned <see cref="DiseaseCategory"/> catalogue. Symptoms, average duration,
/// and notes are optional. The disease owns standard audit metadata and visibility.
/// </summary>
internal sealed class Disease
{
    private Disease()
    {
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int CategoryId { get; private set; }
    public string? Symptoms { get; private set; }
    public int? AverageDurationDays { get; private set; }
    public string? Notes { get; private set; }
    public RecordVisibility Visibility { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int UpdatedBy { get; private set; }

    public static Disease Create(DiseaseValues values, UserId creatorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        var disease = new Disease
        {
            CreatedAt = now,
            CreatedBy = creatorId.Value,
            UpdatedAt = now,
            UpdatedBy = creatorId.Value,
        };
        disease.Apply(values, creatorId, now);
        return disease;
    }

    public void Update(DiseaseValues values, UserId actorId, DateTimeOffset now)
    {
        Apply(values, actorId, now);
    }

    /// <summary>
    /// Re-points the category reference to <paramref name="categoryId"/> during a
    /// Configuration category migration. The category is required, so it is replaced
    /// rather than cleared.
    /// </summary>
    internal void ReplaceCategory(int categoryId, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        if (categoryId <= 0)
        {
            throw new HealthValidationException("Category identifier must be positive.");
        }

        CategoryId = categoryId;
        StampModification(actorId, now);
    }

    private void Apply(DiseaseValues values, UserId actorId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(values);
        EnsureUtc(now);

        var name = HealthValidation.ValidateName(values.Name);
        if (values.CategoryId <= 0)
        {
            throw new HealthValidationException("A disease requires a valid category.");
        }

        var symptoms = HealthValidation.ValidateSymptoms(values.Symptoms);
        var averageDuration = HealthValidation.ValidateAverageDurationDays(values.AverageDurationDays);
        var notes = HealthValidation.ValidateNotes(values.Notes);
        if (!Enum.IsDefined(values.Visibility))
        {
            throw new HealthValidationException("Visibility is invalid.");
        }

        Name = name;
        CategoryId = values.CategoryId;
        Symptoms = symptoms;
        AverageDurationDays = averageDuration;
        Notes = notes;
        Visibility = values.Visibility;
        StampModification(actorId, now);
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
            throw new HealthValidationException("Technical timestamps must use UTC.");
        }
    }
}

/// <summary>
/// A household medicine entry. The category reference is required and points to the
/// module-owned <see cref="MedicineCategory"/> catalogue. Posology and notes are
/// optional. The <see cref="RequiresPrescription"/> flag defaults to <c>false</c>.
/// The optional <see cref="InventoryItemId"/> stores an opaque Inventory item
/// reference; its integrity is maintained by the deletion reference contract rather
/// than a foreign key constraint. The medicine owns standard audit metadata and
/// visibility.
/// </summary>
internal sealed class Medicine
{
    private Medicine()
    {
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int CategoryId { get; private set; }
    public string? Posology { get; private set; }
    public bool RequiresPrescription { get; private set; }
    public int? InventoryItemId { get; private set; }
    public string? Notes { get; private set; }
    public RecordVisibility Visibility { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int UpdatedBy { get; private set; }

    public static Medicine Create(MedicineValues values, UserId creatorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        var medicine = new Medicine
        {
            CreatedAt = now,
            CreatedBy = creatorId.Value,
            UpdatedAt = now,
            UpdatedBy = creatorId.Value,
        };
        medicine.Apply(values, creatorId, now);
        return medicine;
    }

    public void Update(MedicineValues values, UserId actorId, DateTimeOffset now)
    {
        Apply(values, actorId, now);
    }

    /// <summary>
    /// Re-points the category reference to <paramref name="categoryId"/> during a
    /// Configuration category migration. The category is required, so it is replaced
    /// rather than cleared.
    /// </summary>
    internal void ReplaceCategory(int categoryId, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        if (categoryId <= 0)
        {
            throw new HealthValidationException("Category identifier must be positive.");
        }

        CategoryId = categoryId;
        StampModification(actorId, now);
    }

    private void Apply(MedicineValues values, UserId actorId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(values);
        EnsureUtc(now);

        var name = HealthValidation.ValidateName(values.Name);
        if (values.CategoryId <= 0)
        {
            throw new HealthValidationException("A medicine requires a valid category.");
        }

        var posology = HealthValidation.ValidatePosology(values.Posology);
        var notes = HealthValidation.ValidateNotes(values.Notes);
        if (!Enum.IsDefined(values.Visibility))
        {
            throw new HealthValidationException("Visibility is invalid.");
        }

        Name = name;
        CategoryId = values.CategoryId;
        Posology = posology;
        RequiresPrescription = values.RequiresPrescription;
        InventoryItemId = values.InventoryItemId is { } itemId && itemId > 0 ? itemId : null;
        Notes = notes;
        Visibility = values.Visibility;
        StampModification(actorId, now);
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
            throw new HealthValidationException("Technical timestamps must use UTC.");
        }
    }
}

/// <summary>
/// The attribute-free join row of the symmetric disease-to-medicine relationship.
/// It stores only the disease and medicine identifiers; the unique pair constraint
/// prevents duplicates. The same row is reached from both sides
/// (<c>POST /diseases/{d}/medicines/{m}</c> equals <c>POST /medicines/{m}/diseases/{d}</c>).
/// Rows are removed when either endpoint disease or medicine is deleted.
/// </summary>
internal sealed class DiseaseMedicine
{
    public int Id { get; set; }
    public int DiseaseId { get; set; }
    public int MedicineId { get; set; }
}
