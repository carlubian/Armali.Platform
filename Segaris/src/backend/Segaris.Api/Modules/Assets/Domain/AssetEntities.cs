using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Assets.Domain;

/// <summary>
/// An Assets-owned catalog row (category or location). It mirrors the shared-catalog
/// shape (display name, normalized name for case-insensitive uniqueness, declaration
/// order, and audit metadata) while remaining owned by Assets and surfaced through
/// Configuration.
/// </summary>
internal sealed class AssetCategory
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

/// <summary>The Assets-owned location catalog row. See <see cref="AssetCategory"/>.</summary>
internal sealed class AssetLocation
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

/// <summary>The editable fields of an asset, independent of audit metadata.</summary>
internal sealed record AssetValues(
    string Name,
    int CategoryId,
    int LocationId,
    AssetStatus Status,
    string? Code,
    string? BrandModel,
    string? SerialNumber,
    DateOnly? AcquisitionDate,
    DateOnly? ExpectedEndOfLifeDate,
    string? Notes,
    RecordVisibility Visibility);

/// <summary>
/// An individually identified durable object. The asset owns its required category
/// and location references, its descriptive status, its optional household code,
/// optional identification and date fields, its visibility, and an optional reference
/// to the attachment marked as its primary image. The asset carries no stock,
/// monetary value, cost, or reference to any other business module.
/// </summary>
internal sealed class Asset
{
    private Asset()
    {
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int CategoryId { get; private set; }
    public int LocationId { get; private set; }
    public AssetStatus Status { get; private set; }
    public string? Code { get; private set; }
    public string? NormalizedCode { get; private set; }
    public string? BrandModel { get; private set; }
    public string? SerialNumber { get; private set; }
    public DateOnly? AcquisitionDate { get; private set; }
    public DateOnly? ExpectedEndOfLifeDate { get; private set; }
    public string? Notes { get; private set; }
    public RecordVisibility Visibility { get; private set; }
    public int? PrimaryAttachmentId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int UpdatedBy { get; private set; }

    public static Asset Create(AssetValues values, UserId creatorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        var asset = new Asset
        {
            CreatedAt = now,
            CreatedBy = creatorId.Value,
            UpdatedAt = now,
            UpdatedBy = creatorId.Value,
        };
        asset.Apply(values, creatorId, now, isCreation: true);
        return asset;
    }

    public void Update(AssetValues values, UserId actorId, DateTimeOffset now)
    {
        Apply(values, actorId, now, isCreation: false);
    }

    /// <summary>Marks the attachment that resolves the table thumbnail (Wave 3).</summary>
    internal void SetPrimaryAttachment(int? attachmentId, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        PrimaryAttachmentId = attachmentId;
        StampModification(actorId, now);
    }

    /// <summary>Clears the primary image when the referenced attachment is deleted (Wave 3).</summary>
    internal void ClearPrimaryAttachmentIf(int attachmentId, UserId actorId, DateTimeOffset now)
    {
        if (PrimaryAttachmentId != attachmentId)
        {
            return;
        }

        EnsureUtc(now);
        PrimaryAttachmentId = null;
        StampModification(actorId, now);
    }

    /// <summary>Re-points the required category during a Configuration migration.</summary>
    internal void ReplaceCategory(int categoryId, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        if (categoryId <= 0)
        {
            throw new AssetValidationException("Catalog identifiers must be positive.");
        }

        CategoryId = categoryId;
        StampModification(actorId, now);
    }

    /// <summary>Re-points the required location during a Configuration migration.</summary>
    internal void ReplaceLocation(int locationId, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        if (locationId <= 0)
        {
            throw new AssetValidationException("Catalog identifiers must be positive.");
        }

        LocationId = locationId;
        StampModification(actorId, now);
    }

    private void Apply(AssetValues values, UserId actorId, DateTimeOffset now, bool isCreation)
    {
        ArgumentNullException.ThrowIfNull(values);
        EnsureUtc(now);

        var name = AssetValidation.ValidateName(values.Name);
        var (code, normalizedCode) = AssetValidation.ValidateCode(values.Code);
        var brandModel = AssetValidation.ValidateOptionalText(
            values.BrandModel, AssetValidation.BrandModelMaximumLength, "Brand/model");
        var serialNumber = AssetValidation.ValidateOptionalText(
            values.SerialNumber, AssetValidation.SerialNumberMaximumLength, "Serial number");
        var notes = AssetValidation.ValidateNotes(values.Notes);
        if (!Enum.IsDefined(values.Status) || !Enum.IsDefined(values.Visibility))
        {
            throw new AssetValidationException("Status or visibility is invalid.");
        }

        if (values.CategoryId <= 0 || values.LocationId <= 0)
        {
            throw new AssetValidationException("Catalog identifiers must be positive.");
        }

        // Public records collaborate (any user may edit) but only the creator may
        // change an asset's visibility, mirroring the platform visibility baseline.
        if (!isCreation && values.Visibility != Visibility && actorId.Value != CreatedBy)
        {
            throw new AssetValidationException(
                "Only the creator may change asset visibility.",
                AssetValidationReason.VisibilityForbidden);
        }

        Name = name;
        CategoryId = values.CategoryId;
        LocationId = values.LocationId;
        Status = values.Status;
        Code = code;
        NormalizedCode = normalizedCode;
        BrandModel = brandModel;
        SerialNumber = serialNumber;
        AcquisitionDate = values.AcquisitionDate;
        ExpectedEndOfLifeDate = values.ExpectedEndOfLifeDate;
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
            throw new AssetValidationException("Technical timestamps must use UTC.");
        }
    }
}
