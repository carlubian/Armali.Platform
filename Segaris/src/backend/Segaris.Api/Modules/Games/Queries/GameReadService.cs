using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Games.Contracts;
using Segaris.Api.Modules.Games.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Games.Queries;

internal sealed class GameReadService(SegarisDbContext database)
{
    public async Task<IReadOnlyList<GameResponse>> ListAsync(CancellationToken cancellationToken)
    {
        return await database.Set<Game>()
            .AsNoTracking()
            .OrderBy(game => game.SortOrder)
            .ThenBy(game => game.Id)
            .Select(game => new GameResponse(game.Id, game.Name, game.Platform.ToString(), game.SortOrder))
            .ToListAsync(cancellationToken);
    }
}
