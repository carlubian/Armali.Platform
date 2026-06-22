namespace Segaris.Api.Modules.Destinations;

/// <summary>Frozen route shapes for the Destinations HTTP surface.</summary>
internal static class DestinationsApiRoutes
{
    public const string Tag = "Destinations";

    public const string Destinations = "destinations";
    public const string DestinationById = "/{destinationId:int}";

    public const string DestinationAttachments = "/{destinationId:int}/attachments";
    public const string DestinationAttachmentById = "/{destinationId:int}/attachments/{attachmentId}";
    public const string DestinationPrimaryAttachment = "/{destinationId:int}/attachments/{attachmentId}/primary";

    public const string Places = "/{destinationId:int}/places";
    public const string PlaceById = "/{destinationId:int}/places/{placeId:int}";

    public const string Categories = "destinations/categories";
    public const string CategoryById = "/{categoryId:int}";
    public const string CategoryMove = "/{categoryId:int}/move";
    public const string CategoryDeletionImpact = "/{categoryId:int}/deletion-impact";
    public const string CategoryReplaceAndDelete = "/{categoryId:int}/replace-and-delete";

    public const string PlaceCategories = "destinations/place-categories";
    public const string PlaceCategoryById = "/{placeCategoryId:int}";
    public const string PlaceCategoryMove = "/{placeCategoryId:int}/move";
    public const string PlaceCategoryDeletionImpact = "/{placeCategoryId:int}/deletion-impact";
    public const string PlaceCategoryReplaceAndDelete = "/{placeCategoryId:int}/replace-and-delete";
}
