using Segaris.Shared.Api;

namespace Segaris.Api.Modules.Capex;

/// <summary>
/// Stable, Capex-specific <see cref="ErrorCode"/> values returned through
/// <c>ApiProblemException</c>. Generic transport failures continue to use the
/// platform <c>ApiErrorCodes</c>; these codes name the Capex domain failures
/// that clients and tests can rely on across Waves.
/// </summary>
internal static class CapexErrorCodes
{
    /// <summary>The entry is absent or hidden from the current user.</summary>
    public static readonly ErrorCode EntryNotFound = new("capex.entry.not_found");

    /// <summary>The entry payload failed Capex validation; carries field errors.</summary>
    public static readonly ErrorCode EntryValidation = new("capex.entry.validation");

    /// <summary>A referenced category, supplier, cost center, or currency does not exist.</summary>
    public static readonly ErrorCode UnknownCatalogReference = new("capex.catalog.unknown_reference");

    /// <summary>Only the creator may change an entry's visibility in either direction.</summary>
    public static readonly ErrorCode VisibilityForbidden = new("capex.entry.visibility_forbidden");

    /// <summary>The attachment is absent or not owned by the addressed entry.</summary>
    public static readonly ErrorCode AttachmentNotFound = new("capex.attachment.not_found");

    /// <summary>The uploaded attachment failed platform file validation.</summary>
    public static readonly ErrorCode AttachmentInvalid = new("capex.attachment.invalid");

    /// <summary>The addressed Capex category does not exist.</summary>
    public static readonly ErrorCode CategoryNotFound = new("capex.category.not_found");

    /// <summary>The category request failed validation; may carry field errors.</summary>
    public static readonly ErrorCode CategoryValidation = new("capex.category.validation");

    /// <summary>Another category already uses the name (case-insensitive).</summary>
    public static readonly ErrorCode CategoryDuplicateName = new("capex.category.duplicate_name");

    /// <summary>The last remaining category cannot be removed; categories are required.</summary>
    public static readonly ErrorCode CategoryRequiredNotEmpty = new("capex.category.required_not_empty");

    /// <summary>A direct delete was attempted on a category that is still referenced.</summary>
    public static readonly ErrorCode CategoryReferenced = new("capex.category.referenced");

    /// <summary>The requested replacement category is missing, equal to the source, or invalid.</summary>
    public static readonly ErrorCode CategoryInvalidReplacement = new("capex.category.invalid_replacement");

    /// <summary>A concurrent change invalidated the source, replacement, or references.</summary>
    public static readonly ErrorCode CategoryMigrationConflict = new("capex.category.migration_conflict");
}
