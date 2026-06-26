using Segaris.Api.Modules.Capex.Contracts;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Api.Modules.Opex.Contracts;
using Segaris.Api.Modules.Travel.Contracts;

namespace Segaris.Api.Modules.Analytics.Projection;

internal static class AnalyticsProjectionContracts
{
    public static readonly IReadOnlyList<Type> InitialProviderContracts =
    [
        typeof(ICapexFinancialProjectionProvider),
        typeof(IOpexFinancialProjectionProvider),
        typeof(IInventoryFinancialProjectionProvider),
        typeof(ITravelFinancialProjectionProvider),
    ];

    public static readonly Type CurrencyExchangeRateContract =
        typeof(ICurrencyExchangeRateProvider);
}

internal sealed record AnalyticsProjectionProviderSet(
    ICapexFinancialProjectionProvider? Capex,
    IOpexFinancialProjectionProvider? Opex,
    IInventoryFinancialProjectionProvider? Inventory,
    ITravelFinancialProjectionProvider? Travel,
    ICurrencyExchangeRateProvider? CurrencyExchangeRates);
