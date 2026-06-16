using System.Globalization;
using Segaris.Shared.Attachments;

namespace Segaris.Api.Modules.Opex;

/// <summary>Frozen attachment owner kinds for contracts and occurrences.</summary>
internal static class OpexAttachments
{
    public const string Module = "Opex";
    public const string ContractEntityType = "Contract";
    public const string OccurrenceEntityType = "Occurrence";

    public static AttachmentOwner ContractOwner(int contractId) =>
        new(Module, ContractEntityType, contractId.ToString(CultureInfo.InvariantCulture));

    public static AttachmentOwner OccurrenceOwner(int occurrenceId) =>
        new(Module, OccurrenceEntityType, occurrenceId.ToString(CultureInfo.InvariantCulture));
}
