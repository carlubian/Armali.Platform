using Microsoft.EntityFrameworkCore;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Assets.Domain;

/// <summary>
/// Verifies that the required category and location references carried by an
/// <see cref="AssetValues"/> exist before an asset is persisted. Both catalogs are
/// owned by Assets, so each is checked against its module table; an unknown
/// reference surfaces as <see cref="AssetValidationReason.CatalogReference"/>.
/// </summary>
internal sealed class AssetCatalogValidator(SegarisDbContext database)
{
    public async Task ValidateAsync(AssetValues values, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (!await database.Set<AssetCategory>().AnyAsync(category => category.Id == values.CategoryId, cancellationToken)
            || !await database.Set<AssetLocation>().AnyAsync(location => location.Id == values.LocationId, cancellationToken))
        {
            throw new AssetValidationException(
                "One or more catalog references are unknown.",
                AssetValidationReason.CatalogReference);
        }
    }
}
