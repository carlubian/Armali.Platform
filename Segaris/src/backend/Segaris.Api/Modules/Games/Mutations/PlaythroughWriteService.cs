using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Games.Contracts;
using Segaris.Api.Modules.Games.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Games.Mutations;

/// <summary>
/// Write-side operations for playthroughs. Public playthroughs are collaboratively
/// mutable; private playthroughs remain creator-only and only the creator may change
/// visibility. Tags are normalized to child rows on every save, and deleting a
/// playthrough removes its owned tags, sections, and goals.
/// </summary>
internal sealed class PlaythroughWriteService(SegarisDbContext database, IClock clock)
{
    public async Task<int> CreateAsync(
        CreatePlaythroughRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tags = GamesTagNormalization.Normalize(request.Tags);
        var values = new PlaythroughValues(
            request.Name,
            request.GameId,
            request.StartYear,
            request.StartMonth,
            request.Status,
            tags,
            request.Visibility);
        var playthrough = Playthrough.Create(values, actorId, clock.UtcNow);
        await EnsureGameExistsAsync(request.GameId, cancellationToken);

        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        database.Add(playthrough);
        await database.SaveChangesAsync(cancellationToken);
        AddTags(playthrough.Id, tags);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return playthrough.Id;
    }

    public async Task<bool> UpdateAsync(
        int playthroughId,
        UpdatePlaythroughRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var playthrough = await database.Set<Playthrough>()
            .Where(PlaythroughPolicies.MutableBy(actorId))
            .FirstOrDefaultAsync(candidate => candidate.Id == playthroughId, cancellationToken);
        if (playthrough is null)
        {
            return false;
        }

        var tags = GamesTagNormalization.Normalize(request.Tags);
        var values = new PlaythroughValues(
            request.Name,
            request.GameId,
            request.StartYear,
            request.StartMonth,
            request.Status,
            tags,
            request.Visibility);
        playthrough.Update(values, actorId, clock.UtcNow);
        await EnsureGameExistsAsync(request.GameId, cancellationToken);

        var existing = await database.Set<PlaythroughTag>()
            .Where(tag => tag.PlaythroughId == playthroughId)
            .ToListAsync(cancellationToken);
        database.RemoveRange(existing);
        await database.SaveChangesAsync(cancellationToken);

        AddTags(playthroughId, tags);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(
        int playthroughId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var playthrough = await database.Set<Playthrough>()
            .Where(PlaythroughPolicies.MutableBy(actorId))
            .FirstOrDefaultAsync(candidate => candidate.Id == playthroughId, cancellationToken);
        if (playthrough is null)
        {
            return false;
        }

        // Owned tags, sections, and goals are removed by the configured cascade delete.
        database.Remove(playthrough);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private void AddTags(int playthroughId, IReadOnlyList<string> tags)
    {
        for (var position = 0; position < tags.Count; position++)
        {
            database.Add(PlaythroughTag.Create(playthroughId, tags[position], position));
        }
    }

    private async Task EnsureGameExistsAsync(int gameId, CancellationToken cancellationToken)
    {
        if (!await database.Set<Game>().AnyAsync(game => game.Id == gameId, cancellationToken))
        {
            throw new GamesValidationException(
                "The selected game does not exist.",
                GamesValidationReason.UnknownGame,
                "gameId");
        }
    }
}
