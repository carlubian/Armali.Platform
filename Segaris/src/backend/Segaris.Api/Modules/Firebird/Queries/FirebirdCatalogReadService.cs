using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Firebird.Contracts;
using Segaris.Api.Modules.Firebird.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Firebird.Queries;

internal sealed class FirebirdCatalogReadService(SegarisDbContext database)
{
    public async Task<IReadOnlyList<PersonCategoryResponse>> ListCategoriesAsync(CancellationToken cancellationToken) =>
        await database.Set<PersonCategory>()
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Id)
            .Select(category => new PersonCategoryResponse(category.Id, category.Name, category.SortOrder))
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<UsernamePlatformResponse>> ListPlatformsAsync(CancellationToken cancellationToken) =>
        await database.Set<UsernamePlatform>()
            .AsNoTracking()
            .OrderBy(platform => platform.SortOrder)
            .ThenBy(platform => platform.Id)
            .Select(platform => new UsernamePlatformResponse(platform.Id, platform.Name, platform.SortOrder))
            .ToArrayAsync(cancellationToken);
}

