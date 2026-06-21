using Segaris.Shared.Api;

namespace Segaris.Api.Modules.Firebird;

/// <summary>Stable machine-readable Firebird failures.</summary>
internal static class FirebirdErrorCodes
{
    public static readonly ErrorCode PersonNotFound = new("firebird.person.not_found");
    public static readonly ErrorCode PersonValidation = new("firebird.person.validation");
    public static readonly ErrorCode PersonVisibilityForbidden = new("firebird.person.visibility_forbidden");

    public static readonly ErrorCode AvatarNotFound = new("firebird.avatar.not_found");
    public static readonly ErrorCode AvatarInvalid = new("firebird.avatar.invalid");

    public static readonly ErrorCode UsernameNotFound = new("firebird.username.not_found");
    public static readonly ErrorCode UsernameValidation = new("firebird.username.validation");

    public static readonly ErrorCode InteractionNotFound = new("firebird.interaction.not_found");
    public static readonly ErrorCode InteractionValidation = new("firebird.interaction.validation");

    public static readonly ErrorCode UnknownCatalogReference = new("firebird.catalog.unknown_reference");

    public static readonly ErrorCode CategoryNotFound = new("firebird.category.not_found");
    public static readonly ErrorCode CategoryValidation = new("firebird.category.validation");
    public static readonly ErrorCode CategoryDuplicateName = new("firebird.category.duplicate_name");
    public static readonly ErrorCode CategoryRequiredNotEmpty = new("firebird.category.required_not_empty");
    public static readonly ErrorCode CategoryReferenced = new("firebird.category.referenced");
    public static readonly ErrorCode CategoryInvalidReplacement = new("firebird.category.invalid_replacement");
    public static readonly ErrorCode CategoryMigrationConflict = new("firebird.category.migration_conflict");

    public static readonly ErrorCode PlatformNotFound = new("firebird.platform.not_found");
    public static readonly ErrorCode PlatformValidation = new("firebird.platform.validation");
    public static readonly ErrorCode PlatformDuplicateName = new("firebird.platform.duplicate_name");
    public static readonly ErrorCode PlatformRequiredNotEmpty = new("firebird.platform.required_not_empty");
    public static readonly ErrorCode PlatformReferenced = new("firebird.platform.referenced");
    public static readonly ErrorCode PlatformInvalidReplacement = new("firebird.platform.invalid_replacement");
    public static readonly ErrorCode PlatformMigrationConflict = new("firebird.platform.migration_conflict");
}
