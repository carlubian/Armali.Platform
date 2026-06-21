namespace Segaris.Api.Modules.Firebird;

/// <summary>
/// Frozen route shapes for the Firebird HTTP surface. Firebird uses the
/// module-independent <c>people</c> resource name for clarity.
/// </summary>
internal static class FirebirdApiRoutes
{
    public const string Tag = "Firebird";

    public const string People = "people";
    public const string PersonById = "/{personId:int}";

    public const string PersonAvatar = "/{personId:int}/avatar";

    public const string PersonUsernames = "/{personId:int}/usernames";
    public const string PersonUsernameById = "/{personId:int}/usernames/{usernameId:int}";

    public const string PersonInteractions = "/{personId:int}/interactions";
    public const string PersonInteractionById = "/{personId:int}/interactions/{interactionId:int}";

    public const string Categories = "people/categories";
    public const string CategoryById = "/{categoryId:int}";
    public const string CategoryMove = "/{categoryId:int}/move";
    public const string CategoryDeletionImpact = "/{categoryId:int}/deletion-impact";
    public const string CategoryReplaceAndDelete = "/{categoryId:int}/replace-and-delete";

    public const string Platforms = "people/platforms";
    public const string PlatformById = "/{platformId:int}";
    public const string PlatformMove = "/{platformId:int}/move";
    public const string PlatformDeletionImpact = "/{platformId:int}/deletion-impact";
    public const string PlatformReplaceAndDelete = "/{platformId:int}/replace-and-delete";
}
