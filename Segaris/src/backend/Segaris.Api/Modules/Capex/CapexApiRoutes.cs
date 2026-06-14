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

    public const string Entries = "capex/entries";

    public const string EntryById = "/{entryId:int}";

    public const string EntryAttachments = "/{entryId:int}/attachments";

    public const string EntryAttachmentById = "/{entryId:int}/attachments/{attachmentId}";
}
