namespace Segaris.Api.Modules.Capex.Domain;

internal static class CapexCalculations
{
    public static decimal CalculateLineAmount(decimal quantity, decimal unitAmount) =>
        decimal.Round(quantity * unitAmount, 2, MidpointRounding.AwayFromZero);

    public static decimal CalculateTotal(IEnumerable<CapexItem> items) =>
        items.Sum(item => item.LineAmount);
}
