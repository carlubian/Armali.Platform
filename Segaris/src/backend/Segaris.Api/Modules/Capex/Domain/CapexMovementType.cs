namespace Segaris.Api.Modules.Capex.Domain;

/// <summary>
/// Fixed Capex movement direction. These are domain values, not an
/// administrator-managed catalog. They are persisted as bounded strings using
/// the member names and exchanged on the wire using the same names.
/// </summary>
internal enum CapexMovementType
{
    Income,
    Expense,
}
