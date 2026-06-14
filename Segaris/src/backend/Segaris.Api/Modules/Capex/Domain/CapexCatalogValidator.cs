using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Capex.Domain;

internal sealed class CapexCatalogValidator(
    IConfigurationCatalog configurationCatalog,
    SegarisDbContext database)
{
    public async Task ValidateAsync(CapexEntryValues values, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (!await database.Set<CapexCategory>().AnyAsync(category => category.Id == values.CategoryId, cancellationToken)
            || !await configurationCatalog.CurrencyExistsAsync(values.CurrencyId, cancellationToken)
            || values.SupplierId is { } supplierId
                && !await configurationCatalog.SupplierExistsAsync(supplierId, cancellationToken)
            || values.CostCenterId is { } costCenterId
                && !await configurationCatalog.CostCenterExistsAsync(costCenterId, cancellationToken))
        {
            throw new CapexValidationException("One or more shared catalog references are unknown.");
        }
    }
}
