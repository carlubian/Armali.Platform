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
}
