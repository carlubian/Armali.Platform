using Segaris.Shared.Api;

namespace Segaris.Api.Modules.Maintenance;

/// <summary>Stable machine-readable Maintenance failures.</summary>
internal static class MaintenanceErrorCodes
{
    public static readonly ErrorCode TaskNotFound = new("maintenance.task.not_found");
    public static readonly ErrorCode TaskValidation = new("maintenance.task.validation");
    public static readonly ErrorCode TaskVisibilityForbidden = new("maintenance.task.visibility_forbidden");

    public static readonly ErrorCode UnknownType = new("maintenance.task.unknown_type");
    public static readonly ErrorCode AssetReferenceInvalid = new("maintenance.task.asset_invalid");
    public static readonly ErrorCode AssetVisibilityForbidden = new("maintenance.task.asset_visibility_forbidden");

    public static readonly ErrorCode AttachmentNotFound = new("maintenance.attachment.not_found");
    public static readonly ErrorCode AttachmentInvalid = new("maintenance.attachment.invalid");

    public static readonly ErrorCode TypeNotFound = new("maintenance.type.not_found");
    public static readonly ErrorCode TypeValidation = new("maintenance.type.validation");
    public static readonly ErrorCode TypeDuplicateName = new("maintenance.type.duplicate_name");
    public static readonly ErrorCode TypeRequiredNotEmpty = new("maintenance.type.required_not_empty");
    public static readonly ErrorCode TypeReferenced = new("maintenance.type.referenced");
    public static readonly ErrorCode TypeInvalidReplacement = new("maintenance.type.invalid_replacement");
    public static readonly ErrorCode TypeMigrationConflict = new("maintenance.type.migration_conflict");

    public static readonly ErrorCode AssetDeletionBlocked = new("maintenance.asset_deletion.blocked");
}
