using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Opex.Domain;

/// <summary>
/// Verifies that the catalog references carried by an <see cref="OpexContractValues"/>
/// exist before a contract is persisted. The Opex-owned category is checked against
/// the module table; supplier, cost center, and currency are checked through the
/// Configuration module's published catalog contract.
/// </summary>
internal sealed class OpexCatalogValidator(
    IConfigurationCatalog configurationCatalog,
    SegarisDbContext database)
{
    public async Task ValidateAsync(OpexContractValues values, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (!await database.Set<OpexCategory>().AnyAsync(category => category.Id == values.CategoryId, cancellationToken)
            || !await configurationCatalog.CurrencyExistsAsync(values.CurrencyId, cancellationToken)
            || values.SupplierId is { } supplierId
                && !await configurationCatalog.SupplierExistsAsync(supplierId, cancellationToken)
            || values.CostCenterId is { } costCenterId
                && !await configurationCatalog.CostCenterExistsAsync(costCenterId, cancellationToken))
        {
            throw new OpexValidationException(
                "One or more shared catalog references are unknown.",
                OpexValidationReason.CatalogReference);
        }
    }
}
