namespace Segaris.Api.Modules.Capex.Domain;

/// <summary>
/// Fixed Capex lifecycle status. Status and <c>DueDate</c> are independent; the
/// system never changes status automatically. These are domain values, not an
/// administrator-managed catalog, persisted as bounded strings using the member
/// names and exchanged on the wire using the same names.
/// </summary>
internal enum CapexEntryStatus
{
    Planning,
    Completed,
    Canceled,
}
