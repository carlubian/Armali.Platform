namespace Segaris.Api.Modules.Opex;

/// <summary>Frozen route shapes for the Opex HTTP surface.</summary>
internal static class OpexApiRoutes
{
    public const string Tag = "Opex";
    public const string Contracts = "opex/contracts";
    public const string ContractById = "/{contractId:int}";
    public const string ContractAttachments = "/{contractId:int}/attachments";
    public const string ContractAttachmentById = "/{contractId:int}/attachments/{attachmentId}";
    public const string Occurrences = "/{contractId:int}/occurrences";
    public const string OccurrenceById = "/{contractId:int}/occurrences/{occurrenceId:int}";
    public const string OccurrenceAttachments = "/{contractId:int}/occurrences/{occurrenceId:int}/attachments";
    public const string OccurrenceAttachmentById = "/{contractId:int}/occurrences/{occurrenceId:int}/attachments/{attachmentId}";
    public const string Categories = "opex/categories";
    public const string CategoryById = "/{categoryId:int}";
    public const string CategoryMove = "/{categoryId:int}/move";
    public const string CategoryDeletionImpact = "/{categoryId:int}/deletion-impact";
    public const string CategoryReplaceAndDelete = "/{categoryId:int}/replace-and-delete";
}
