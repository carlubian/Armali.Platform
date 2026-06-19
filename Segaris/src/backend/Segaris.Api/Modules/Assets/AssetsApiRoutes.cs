namespace Segaris.Api.Modules.Assets;

/// <summary>Frozen route shapes for the Assets HTTP surface.</summary>
internal static class AssetsApiRoutes
{
    public const string Tag = "Assets";

    public const string Items = "assets/items";
    public const string ItemById = "/{assetId:int}";
    public const string ItemDeletionImpact = "/{assetId:int}/deletion-impact";
    public const string ItemReassignAndDelete = "/{assetId:int}/reassign-and-delete";
    public const string ItemAttachments = "/{assetId:int}/attachments";
    public const string ItemAttachmentById = "/{assetId:int}/attachments/{attachmentId}";
    public const string ItemPrimaryAttachment = "/{assetId:int}/attachments/{attachmentId}/primary";

    public const string Categories = "assets/categories";
    public const string CategoryById = "/{categoryId:int}";
    public const string CategoryMove = "/{categoryId:int}/move";
    public const string CategoryDeletionImpact = "/{categoryId:int}/deletion-impact";
    public const string CategoryReplaceAndDelete = "/{categoryId:int}/replace-and-delete";

    public const string Locations = "assets/locations";
    public const string LocationById = "/{locationId:int}";
    public const string LocationMove = "/{locationId:int}/move";
    public const string LocationDeletionImpact = "/{locationId:int}/deletion-impact";
    public const string LocationReplaceAndDelete = "/{locationId:int}/replace-and-delete";
}
