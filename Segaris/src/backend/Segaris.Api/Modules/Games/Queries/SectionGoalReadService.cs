using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Games.Contracts;
using Segaris.Api.Modules.Games.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Games.Queries;

/// <summary>
/// Read-side projections for playthrough-scoped sections and section-scoped goals.
/// Section progress is derived from current goals on every query.
/// </summary>
internal sealed class SectionGoalReadService(SegarisDbContext database)
{
    public async Task<IReadOnlyList<SectionResponse>?> ListSectionsAsync(
        int playthroughId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        if (!await PlaythroughAccessibleAsync(playthroughId, userId, cancellationToken))
        {
            return null;
        }

        var ordered = database.Set<Section>()
                .AsNoTracking()
                .Where(section => section.PlaythroughId == playthroughId)
            .OrderBy(section => section.SortOrder)
            .ThenBy(section => section.Id);

        return await ProjectSections(ordered)
            .ToListAsync(cancellationToken);
    }

    public async Task<SectionResponse?> GetSectionAsync(
        int playthroughId,
        int sectionId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        if (!await PlaythroughAccessibleAsync(playthroughId, userId, cancellationToken))
        {
            return null;
        }

        return await ProjectSections(database.Set<Section>()
                .AsNoTracking()
                .Where(section => section.PlaythroughId == playthroughId && section.Id == sectionId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GoalResponse>?> ListGoalsAsync(
        int playthroughId,
        int sectionId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        if (!await SectionAccessibleAsync(playthroughId, sectionId, userId, cancellationToken))
        {
            return null;
        }

        return await database.Set<Goal>()
            .AsNoTracking()
            .Where(goal => goal.SectionId == sectionId)
            .OrderBy(goal => goal.Position)
            .ThenBy(goal => goal.Id)
            .Select(goal => new GoalResponse(goal.Id, goal.Text, goal.Completed, goal.Position))
            .ToListAsync(cancellationToken);
    }

    public async Task<GoalResponse?> GetGoalAsync(
        int playthroughId,
        int sectionId,
        int goalId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        if (!await SectionAccessibleAsync(playthroughId, sectionId, userId, cancellationToken))
        {
            return null;
        }

        return await database.Set<Goal>()
            .AsNoTracking()
            .Where(goal => goal.SectionId == sectionId && goal.Id == goalId)
            .Select(goal => new GoalResponse(goal.Id, goal.Text, goal.Completed, goal.Position))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private IQueryable<SectionResponse> ProjectSections(IQueryable<Section> sections) =>
        sections.Select(section => new SectionResponse(
            section.Id,
            section.Name,
            section.Color.ToString(),
            section.SortOrder,
            new ProgressResponse(
                database.Set<Goal>().Count(goal => goal.SectionId == section.Id && goal.Completed),
                database.Set<Goal>().Count(goal => goal.SectionId == section.Id))));

    private Task<bool> SectionAccessibleAsync(
        int playthroughId,
        int sectionId,
        UserId userId,
        CancellationToken cancellationToken) =>
        database.Set<Section>()
            .AsNoTracking()
            .Where(section => section.Id == sectionId && section.PlaythroughId == playthroughId)
            .AnyAsync(section => database.Set<Playthrough>()
                .Where(PlaythroughPolicies.AccessibleTo(userId))
                .Any(playthrough => playthrough.Id == section.PlaythroughId), cancellationToken);

    private Task<bool> PlaythroughAccessibleAsync(
        int playthroughId,
        UserId userId,
        CancellationToken cancellationToken) =>
        database.Set<Playthrough>()
            .AsNoTracking()
            .Where(PlaythroughPolicies.AccessibleTo(userId))
            .AnyAsync(playthrough => playthrough.Id == playthroughId, cancellationToken);
}
