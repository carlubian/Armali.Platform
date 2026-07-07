using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Games.Contracts;
using Segaris.Api.Modules.Games.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Games.Mutations;

internal sealed class GameManagementService(SegarisDbContext database, IClock clock)
{
    public async Task<GameResponse> CreateAsync(CreateGameRequest request, UserId actor, CancellationToken token)
    {
        var (display, normalized, platform) = Validate(request.Name, request.Platform);
        await EnsureUniqueAsync(null, normalized, token);
        var now = clock.UtcNow;
        var sortOrder = (await database.Set<Game>().Select(game => (int?)game.SortOrder).MaxAsync(token) ?? -1) + 1;
        var game = new Game
        {
            Name = display,
            NormalizedName = normalized,
            Platform = platform,
            SortOrder = sortOrder,
            CreatedAt = now,
            CreatedBy = actor.Value,
            UpdatedAt = now,
            UpdatedBy = actor.Value,
        };
        database.Add(game);
        await SaveAsync(token);
        return ToResponse(game);
    }

    public async Task<GameResponse> UpdateAsync(int id, UpdateGameRequest request, UserId actor, CancellationToken token)
    {
        var game = await FindAsync(id, token);
        var (display, normalized, platform) = Validate(request.Name, request.Platform);
        await EnsureUniqueAsync(id, normalized, token);
        game.Name = display;
        game.NormalizedName = normalized;
        game.Platform = platform;
        game.UpdatedAt = clock.UtcNow;
        game.UpdatedBy = actor.Value;
        await SaveAsync(token);
        return ToResponse(game);
    }

    public async Task MoveAsync(int id, CatalogMoveDirection direction, CancellationToken token)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var ordered = await database.Set<Game>()
            .OrderBy(game => game.SortOrder)
            .ThenBy(game => game.Id)
            .ToListAsync(token);
        var index = ordered.FindIndex(game => game.Id == id);
        if (index < 0) throw GameProblem.NotFound();
        var target = direction == CatalogMoveDirection.Up ? index - 1 : index + 1;
        if (target < 0 || target >= ordered.Count)
        {
            throw GameProblem.Validation("direction", "The game cannot move beyond the catalogue boundary.");
        }

        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        for (var position = 0; position < ordered.Count; position++) ordered[position].SortOrder = position;
        await database.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
    }

    public async Task<CatalogDeletionImpactResponse> ImpactAsync(int id, CancellationToken token)
    {
        await FindAsync(id, token, tracked: false);
        var referenced = await database.Set<Playthrough>().AnyAsync(playthrough => playthrough.GameId == id, token);
        var count = await database.Set<Game>().CountAsync(token);
        return new(
            IsReferenced: referenced,
            CanDeleteDirectly: !referenced,
            CanClearReferences: false,
            RequiresExchangeRate: false,
            HasReplacementCandidates: count > 1);
    }

    public async Task DeleteAsync(int id, CancellationToken token)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var game = await FindAsync(id, token);
        if (await database.Set<Playthrough>().AnyAsync(playthrough => playthrough.GameId == id, token))
        {
            throw GameProblem.Referenced();
        }

        database.Remove(game);
        var remaining = await database.Set<Game>()
            .Where(value => value.Id != id)
            .OrderBy(value => value.SortOrder)
            .ThenBy(value => value.Id)
            .ToListAsync(token);
        for (var position = 0; position < remaining.Count; position++) remaining[position].SortOrder = position;

        try
        {
            await database.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
        }
        catch (DbUpdateException)
        {
            throw GameProblem.Referenced();
        }
    }

    public async Task ReplaceAndDeleteAsync(int id, CatalogReplacementRequest request, UserId actor, CancellationToken token)
    {
        if (request.ClearReferences
            || request.ExchangeRate is not null
            || request.ReplacementId is not { } replacementId
            || replacementId <= 0
            || replacementId == id)
        {
            throw GameProblem.InvalidReplacement();
        }

        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var source = await FindAsync(id, token);
        if (!await database.Set<Game>().AnyAsync(game => game.Id == replacementId, token))
        {
            throw GameProblem.InvalidReplacement();
        }

        try
        {
            var playthroughs = await database.Set<Playthrough>()
                .Where(playthrough => playthrough.GameId == id)
                .ToListAsync(token);
            var occurredAt = clock.UtcNow;
            foreach (var playthrough in playthroughs)
            {
                playthrough.ReplaceGame(replacementId, actor, occurredAt);
            }

            database.Remove(source);
            var remaining = await database.Set<Game>()
                .Where(game => game.Id != id)
                .OrderBy(game => game.SortOrder)
                .ThenBy(game => game.Id)
                .ToListAsync(token);
            for (var position = 0; position < remaining.Count; position++) remaining[position].SortOrder = position;

            await database.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
        }
        catch (DbUpdateException)
        {
            throw GameProblem.MigrationConflict();
        }
    }

    private async Task<Game> FindAsync(int id, CancellationToken token, bool tracked = true)
    {
        IQueryable<Game> query = database.Set<Game>();
        if (!tracked) query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(game => game.Id == id, token) ?? throw GameProblem.NotFound();
    }

    private async Task EnsureUniqueAsync(int? id, string normalized, CancellationToken token)
    {
        if (await database.Set<Game>().AnyAsync(game => game.NormalizedName == normalized && game.Id != id, token))
        {
            throw GameProblem.DuplicateName();
        }
    }

    private async Task SaveAsync(CancellationToken token)
    {
        try
        {
            await database.SaveChangesAsync(token);
        }
        catch (DbUpdateException)
        {
            throw GameProblem.DuplicateName();
        }
    }

    private static (string Display, string Normalized, GamePlatform Platform) Validate(string? name, string? platform)
    {
        try
        {
            var display = GamesValidation.ValidateName(name);
            return (display, GamesValidation.NormalizeName(display), GamesValidation.ValidatePlatform(platform));
        }
        catch (GamesValidationException exception)
        {
            throw GameProblem.Validation(exception.Field ?? "name", exception.Message);
        }
    }

    private static GameResponse ToResponse(Game game) =>
        new(game.Id, game.Name, game.Platform.ToString(), game.SortOrder);
}
