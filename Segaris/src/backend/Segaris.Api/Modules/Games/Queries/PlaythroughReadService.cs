using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Games.Contracts;
using Segaris.Api.Modules.Games.Domain;
using Segaris.Api.Modules.Identity;
using Segaris.Persistence;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Games.Queries;

/// <summary>
/// Read-side projections for accessible playthroughs. Progress is derived on demand
/// from current goals through correlated counts and is never persisted.
/// </summary>
internal sealed class PlaythroughReadService(SegarisDbContext database)
{
    public async Task<PaginatedResponse<PlaythroughSummaryResponse>> ListPlaythroughsAsync(
        PlaythroughFilter filter,
        PaginationRequest pagination,
        SortRequest sort,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var filtered = ApplyFilters(
            database.Set<Playthrough>().AsNoTracking().Where(PlaythroughPolicies.AccessibleTo(userId)),
            filter);

        var totalCount = await filtered.CountAsync(cancellationToken);
        var rows = await Project(ApplySort(filtered, sort)
                .Skip(pagination.Offset)
                .Take(pagination.PageSize))
            .ToListAsync(cancellationToken);

        var tags = await TagsByPlaythroughAsync(rows.Select(row => row.Id), cancellationToken);
        var page = rows
            .Select(row => new PlaythroughSummaryResponse(
                row.Id,
                row.Name,
                row.GameId,
                row.GameName,
                row.Platform.ToString(),
                row.Status.ToString(),
                row.StartYear,
                row.StartMonth,
                tags.TryGetValue(row.Id, out var values) ? values : [],
                new ProgressResponse(row.CompletedGoals, row.TotalGoals),
                row.Visibility.ToString(),
                row.CreatorId,
                row.CreatorName))
            .ToArray();

        return PaginatedResponse<PlaythroughSummaryResponse>.Create(page, pagination, totalCount);
    }

    public async Task<PlaythroughResponse?> GetPlaythroughAsync(
        int playthroughId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var row = await database.Set<Playthrough>()
            .AsNoTracking()
            .Where(PlaythroughPolicies.AccessibleTo(userId))
            .Where(playthrough => playthrough.Id == playthroughId)
            .Select(playthrough => new PlaythroughDetailRow(
                playthrough.Id,
                playthrough.Name,
                playthrough.GameId,
                database.Set<Game>().Where(game => game.Id == playthrough.GameId).Select(game => game.Name).First(),
                database.Set<Game>().Where(game => game.Id == playthrough.GameId).Select(game => game.Platform).First(),
                playthrough.Status,
                playthrough.StartYear,
                playthrough.StartMonth,
                playthrough.Visibility,
                playthrough.CreatedBy,
                database.Set<SegarisUser>().Where(user => user.Id == playthrough.CreatedBy).Select(user => user.DisplayName).First(),
                database.Set<Goal>().Count(goal =>
                    goal.Completed
                    && database.Set<Section>().Any(section => section.PlaythroughId == playthrough.Id && section.Id == goal.SectionId)),
                database.Set<Goal>().Count(goal =>
                    database.Set<Section>().Any(section => section.PlaythroughId == playthrough.Id && section.Id == goal.SectionId)),
                playthrough.CreatedAt,
                playthrough.UpdatedBy,
                database.Set<SegarisUser>().Where(user => user.Id == playthrough.UpdatedBy).Select(user => user.DisplayName).FirstOrDefault(),
                playthrough.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var tags = await TagsByPlaythroughAsync([row.Id], cancellationToken);
        return new PlaythroughResponse(
            row.Id,
            row.Name,
            row.GameId,
            row.GameName,
            row.Platform.ToString(),
            row.Status.ToString(),
            row.StartYear,
            row.StartMonth,
            tags.TryGetValue(row.Id, out var values) ? values : [],
            new ProgressResponse(row.CompletedGoals, row.TotalGoals),
            row.Visibility.ToString(),
            row.CreatorId,
            row.CreatorName,
            row.CreatedAt,
            row.UpdatedById,
            row.UpdatedByName,
            row.UpdatedAt);
    }

    public Task<bool> PlaythroughAccessibleAsync(
        int playthroughId,
        UserId userId,
        CancellationToken cancellationToken) =>
        database.Set<Playthrough>()
            .AsNoTracking()
            .Where(PlaythroughPolicies.AccessibleTo(userId))
            .AnyAsync(playthrough => playthrough.Id == playthroughId, cancellationToken);

    private IQueryable<PlaythroughSummaryRow> Project(IQueryable<Playthrough> playthroughs) =>
        playthroughs.Select(playthrough => new PlaythroughSummaryRow(
            playthrough.Id,
            playthrough.Name,
            playthrough.GameId,
            database.Set<Game>().Where(game => game.Id == playthrough.GameId).Select(game => game.Name).First(),
            database.Set<Game>().Where(game => game.Id == playthrough.GameId).Select(game => game.Platform).First(),
            playthrough.Status,
            playthrough.StartYear,
            playthrough.StartMonth,
            playthrough.Visibility,
            playthrough.CreatedBy,
            database.Set<SegarisUser>().Where(user => user.Id == playthrough.CreatedBy).Select(user => user.DisplayName).First(),
            database.Set<Goal>().Count(goal =>
                goal.Completed
                && database.Set<Section>().Any(section => section.PlaythroughId == playthrough.Id && section.Id == goal.SectionId)),
            database.Set<Goal>().Count(goal =>
                database.Set<Section>().Any(section => section.PlaythroughId == playthrough.Id && section.Id == goal.SectionId))));

    private IQueryable<Playthrough> ApplyFilters(IQueryable<Playthrough> playthroughs, PlaythroughFilter filter)
    {
        if (filter.Search is { } search)
        {
            var pattern = $"%{Escape(search.ToLowerInvariant())}%";
            playthroughs = playthroughs.Where(playthrough =>
                EF.Functions.Like(playthrough.Name.ToLower(), pattern, "\\")
                || database.Set<Game>().Any(game =>
                    game.Id == playthrough.GameId && EF.Functions.Like(game.Name.ToLower(), pattern, "\\")));
        }

        if (filter.GameId is { } gameId)
        {
            playthroughs = playthroughs.Where(playthrough => playthrough.GameId == gameId);
        }

        if (filter.Platform is { } platform)
        {
            playthroughs = playthroughs.Where(playthrough =>
                database.Set<Game>().Any(game => game.Id == playthrough.GameId && game.Platform == platform));
        }

        if (filter.Status is { } status)
        {
            playthroughs = playthroughs.Where(playthrough => playthrough.Status == status);
        }

        if (filter.NormalizedTag is { } tag)
        {
            playthroughs = playthroughs.Where(playthrough =>
                database.Set<PlaythroughTag>().Any(row => row.PlaythroughId == playthrough.Id && row.NormalizedValue == tag));
        }

        if (filter.CreatorId is { } creatorId)
        {
            playthroughs = playthroughs.Where(playthrough => playthrough.CreatedBy == creatorId);
        }

        if (filter.Visibility is { } visibility)
        {
            playthroughs = playthroughs.Where(playthrough => playthrough.Visibility == visibility);
        }

        return playthroughs;
    }

    private IQueryable<Playthrough> ApplySort(IQueryable<Playthrough> playthroughs, SortRequest sort)
    {
        var ascending = sort.Direction == SortDirection.Ascending;
        Expression<Func<Playthrough, double>> progressRatio = playthrough =>
            database.Set<Goal>().Count(goal =>
                database.Set<Section>().Any(section => section.PlaythroughId == playthrough.Id && section.Id == goal.SectionId)) == 0
                ? 0.0
                : (double)database.Set<Goal>().Count(goal =>
                    goal.Completed
                    && database.Set<Section>().Any(section => section.PlaythroughId == playthrough.Id && section.Id == goal.SectionId))
                    / database.Set<Goal>().Count(goal =>
                        database.Set<Section>().Any(section => section.PlaythroughId == playthrough.Id && section.Id == goal.SectionId));

        IOrderedQueryable<Playthrough> ordered = sort.Field switch
        {
            PlaythroughQuery.SortFields.Game => ascending
                ? playthroughs.OrderBy(playthrough =>
                    database.Set<Game>().Where(game => game.Id == playthrough.GameId).Select(game => game.Name).First())
                : playthroughs.OrderByDescending(playthrough =>
                    database.Set<Game>().Where(game => game.Id == playthrough.GameId).Select(game => game.Name).First()),
            PlaythroughQuery.SortFields.StartDate => ascending
                ? playthroughs.OrderBy(playthrough => playthrough.StartYear).ThenBy(playthrough => playthrough.StartMonth)
                : playthroughs.OrderByDescending(playthrough => playthrough.StartYear).ThenByDescending(playthrough => playthrough.StartMonth),
            PlaythroughQuery.SortFields.Status => ascending
                ? playthroughs.OrderBy(playthrough => playthrough.Status)
                : playthroughs.OrderByDescending(playthrough => playthrough.Status),
            PlaythroughQuery.SortFields.Progress => ascending
                ? playthroughs.OrderBy(progressRatio)
                : playthroughs.OrderByDescending(progressRatio),
            PlaythroughQuery.SortFields.Id => ascending
                ? playthroughs.OrderBy(playthrough => playthrough.Id)
                : playthroughs.OrderByDescending(playthrough => playthrough.Id),
            _ => ascending
                ? playthroughs.OrderBy(playthrough => playthrough.Name)
                : playthroughs.OrderByDescending(playthrough => playthrough.Name),
        };

        return ascending ? ordered.ThenBy(playthrough => playthrough.Id) : ordered.ThenByDescending(playthrough => playthrough.Id);
    }

    private async Task<IReadOnlyDictionary<int, IReadOnlyList<string>>> TagsByPlaythroughAsync(
        IEnumerable<int> playthroughIds,
        CancellationToken cancellationToken)
    {
        var ids = playthroughIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<int, IReadOnlyList<string>>();
        }

        var rows = await database.Set<PlaythroughTag>()
            .AsNoTracking()
            .Where(tag => ids.Contains(tag.PlaythroughId))
            .OrderBy(tag => tag.PlaythroughId)
            .ThenBy(tag => tag.SortOrder)
            .ThenBy(tag => tag.Id)
            .Select(tag => new { tag.PlaythroughId, tag.Value })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.PlaythroughId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group.Select(row => row.Value).ToArray());
    }

    private static string Escape(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);

    private sealed record PlaythroughSummaryRow(
        int Id,
        string Name,
        int GameId,
        string GameName,
        GamePlatform Platform,
        PlaythroughStatus Status,
        int StartYear,
        int StartMonth,
        RecordVisibility Visibility,
        int CreatorId,
        string CreatorName,
        int CompletedGoals,
        int TotalGoals);

    private sealed record PlaythroughDetailRow(
        int Id,
        string Name,
        int GameId,
        string GameName,
        GamePlatform Platform,
        PlaythroughStatus Status,
        int StartYear,
        int StartMonth,
        RecordVisibility Visibility,
        int CreatorId,
        string CreatorName,
        int CompletedGoals,
        int TotalGoals,
        DateTimeOffset CreatedAt,
        int? UpdatedById,
        string? UpdatedByName,
        DateTimeOffset? UpdatedAt);
}
