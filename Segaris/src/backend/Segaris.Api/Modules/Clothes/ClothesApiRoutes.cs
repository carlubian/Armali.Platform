namespace Segaris.Api.Modules.Clothes;

/// <summary>Frozen route shapes for the Clothes HTTP surface.</summary>
internal static class ClothesApiRoutes
{
    public const string Tag = "Clothes";

    public const string Garments = "clothes/garments";
    public const string GarmentById = "/{garmentId:int}";
    public const string GarmentAttachments = "/{garmentId:int}/attachments";
    public const string GarmentAttachmentById = "/{garmentId:int}/attachments/{attachmentId}";
    public const string GarmentPrimaryAttachment = "/{garmentId:int}/attachments/{attachmentId}/primary";

    public const string Categories = "clothes/categories";
    public const string CategoryById = "/{categoryId:int}";
    public const string CategoryMove = "/{categoryId:int}/move";
    public const string CategoryDeletionImpact = "/{categoryId:int}/deletion-impact";
    public const string CategoryReplaceAndDelete = "/{categoryId:int}/replace-and-delete";

    public const string Colors = "clothes/colors";
    public const string ColorById = "/{colorId:int}";
    public const string ColorMove = "/{colorId:int}/move";
    public const string ColorDeletionImpact = "/{colorId:int}/deletion-impact";
    public const string ColorReplaceAndDelete = "/{colorId:int}/replace-and-delete";
}
