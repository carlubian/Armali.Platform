namespace Segaris.Api.Modules.Capex;

/// <summary>
/// Frozen route shapes for the Capex endpoints. Prefixes are relative to
/// <c>/api</c> as required by <c>MapSegarisApiGroup</c>; the templates document
/// the per-route patterns used when the endpoints are mapped in Waves 3-4.
/// </summary>
internal static class CapexApiRoutes
{
    public const string Tag = "Capex";

    public const string Categories = "capex/categories";

    /// <summary>Update path relative to the category collection.</summary>
    public const string CategoryById = "/{categoryId:int}";

    /// <summary>Move-up/move-down path relative to the category collection.</summary>
    public const string CategoryMove = "/{categoryId:int}/move";

    /// <summary>Privacy-neutral deletion-impact path relative to the category collection.</summary>
    public const string CategoryDeletionImpact = "/{categoryId:int}/deletion-impact";

    /// <summary>Replace-and-delete path relative to the category collection.</summary>
    public const string CategoryReplaceAndDelete = "/{categoryId:int}/replace-and-delete";

    public const string Entries = "capex/entries";

    public const string EntryById = "/{entryId:int}";

    public const string EntryAttachments = "/{entryId:int}/attachments";

    public const string EntryAttachmentById = "/{entryId:int}/attachments/{attachmentId}";
}
