using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Games.Contracts;
using Segaris.Api.Modules.Games.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Games.Mutations;

/// <summary>
/// Write-side operations for sections and goals. Every operation is scoped through
/// an accessible playthrough, so private children are never disclosed separately.
/// </summary>
internal sealed class SectionGoalWriteService(SegarisDbContext database, IClock clock)
{
    public async Task<int?> CreateSectionAsync(
        int playthroughId,
        CreateSectionRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        if (!await PlaythroughMutableAsync(playthroughId, actorId, cancellationToken))
        {
            return null;
        }

        var normalizedName = GamesValidation.NormalizeName(GamesValidation.ValidateName(request.Name));
        if (await SectionNameExistsAsync(playthroughId, normalizedName, exceptSectionId: null, cancellationToken))
        {
            throw SectionGoalProblem.SectionDuplicateName();
        }

        var sortOrder = await NextSectionSortOrderAsync(playthroughId, cancellationToken);
        var section = Section.Create(playthroughId, request.Name, request.Color, sortOrder, actorId, clock.UtcNow);
        database.Add(section);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return section.Id;
    }

    public async Task<bool> UpdateSectionAsync(
        int playthroughId,
        int sectionId,
        UpdateSectionRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var section = await MutableSectionAsync(playthroughId, sectionId, actorId, cancellationToken);
        if (section is null)
        {
            return false;
        }

        var normalizedName = GamesValidation.NormalizeName(GamesValidation.ValidateName(request.Name));
        if (await SectionNameExistsAsync(playthroughId, normalizedName, sectionId, cancellationToken))
        {
            throw SectionGoalProblem.SectionDuplicateName();
        }

        section.Update(request.Name, request.Color, actorId, clock.UtcNow);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteSectionAsync(
        int playthroughId,
        int sectionId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var section = await MutableSectionAsync(playthroughId, sectionId, actorId, cancellationToken);
        if (section is null)
        {
            return false;
        }

        database.Remove(section);
        await database.SaveChangesAsync(cancellationToken);
        await NormalizeSectionOrderAsync(playthroughId, actorId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ReorderSectionsAsync(
        int playthroughId,
        SectionOrderRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        if (!await PlaythroughMutableAsync(playthroughId, actorId, cancellationToken))
        {
            return false;
        }

        var sections = await database.Set<Section>()
            .Where(section => section.PlaythroughId == playthroughId)
            .OrderBy(section => section.SortOrder)
            .ThenBy(section => section.Id)
            .ToListAsync(cancellationToken);
        var requested = request.SectionIds ?? [];
        if (requested.Count != sections.Count || requested.Distinct().Count() != requested.Count)
        {
            throw SectionGoalProblem.SectionInvalidOrder();
        }

        var byId = sections.ToDictionary(section => section.Id);
        if (requested.Any(id => !byId.ContainsKey(id)))
        {
            throw SectionGoalProblem.SectionInvalidOrder();
        }

        for (var index = 0; index < requested.Count; index++)
        {
            byId[requested[index]].Reposition(index, actorId, clock.UtcNow);
        }

        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<int?> CreateGoalAsync(
        int playthroughId,
        int sectionId,
        CreateGoalRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        if (!await SectionMutableAsync(playthroughId, sectionId, actorId, cancellationToken))
        {
            return null;
        }

        var position = await NextGoalPositionAsync(sectionId, cancellationToken);
        var goal = Goal.Create(sectionId, request.Text, position, actorId, clock.UtcNow);
        database.Add(goal);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return goal.Id;
    }

    public async Task<bool> UpdateGoalAsync(
        int playthroughId,
        int sectionId,
        int goalId,
        UpdateGoalRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var goal = await MutableGoalAsync(playthroughId, sectionId, goalId, actorId, cancellationToken);
        if (goal is null)
        {
            return false;
        }

        goal.Update(request.Text, actorId, clock.UtcNow);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetGoalCompletionAsync(
        int playthroughId,
        int sectionId,
        int goalId,
        GoalCompletionRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var goal = await MutableGoalAsync(playthroughId, sectionId, goalId, actorId, cancellationToken);
        if (goal is null)
        {
            return false;
        }

        goal.SetCompletion(request.Completed, actorId, clock.UtcNow);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteGoalAsync(
        int playthroughId,
        int sectionId,
        int goalId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var goal = await MutableGoalAsync(playthroughId, sectionId, goalId, actorId, cancellationToken);
        if (goal is null)
        {
            return false;
        }

        database.Remove(goal);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<Section?> MutableSectionAsync(
        int playthroughId,
        int sectionId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        if (!await PlaythroughMutableAsync(playthroughId, actorId, cancellationToken))
        {
            return null;
        }

        return await database.Set<Section>()
            .FirstOrDefaultAsync(section => section.Id == sectionId && section.PlaythroughId == playthroughId, cancellationToken);
    }

    private Task<bool> SectionMutableAsync(
        int playthroughId,
        int sectionId,
        UserId actorId,
        CancellationToken cancellationToken) =>
        database.Set<Section>()
            .AsNoTracking()
            .Where(section => section.Id == sectionId && section.PlaythroughId == playthroughId)
            .AnyAsync(section => database.Set<Playthrough>()
                .Where(PlaythroughPolicies.MutableBy(actorId))
                .Any(playthrough => playthrough.Id == section.PlaythroughId), cancellationToken);

    private Task<Goal?> MutableGoalAsync(
        int playthroughId,
        int sectionId,
        int goalId,
        UserId actorId,
        CancellationToken cancellationToken) =>
        database.Set<Goal>()
            .Where(goal => goal.Id == goalId && goal.SectionId == sectionId)
            .Where(goal => database.Set<Section>().Any(section =>
                section.Id == goal.SectionId
                && section.Id == sectionId
                && section.PlaythroughId == playthroughId
                && database.Set<Playthrough>().Where(PlaythroughPolicies.MutableBy(actorId)).Any(playthrough => playthrough.Id == section.PlaythroughId)))
            .FirstOrDefaultAsync(cancellationToken);

    private Task<bool> PlaythroughMutableAsync(
        int playthroughId,
        UserId actorId,
        CancellationToken cancellationToken) =>
        database.Set<Playthrough>()
            .AsNoTracking()
            .Where(PlaythroughPolicies.MutableBy(actorId))
            .AnyAsync(playthrough => playthrough.Id == playthroughId, cancellationToken);

    private Task<bool> SectionNameExistsAsync(
        int playthroughId,
        string normalizedName,
        int? exceptSectionId,
        CancellationToken cancellationToken) =>
        database.Set<Section>()
            .AnyAsync(section =>
                section.PlaythroughId == playthroughId
                && section.NormalizedName == normalizedName
                && (exceptSectionId == null || section.Id != exceptSectionId.Value),
                cancellationToken);

    private async Task<int> NextSectionSortOrderAsync(int playthroughId, CancellationToken cancellationToken) =>
        await database.Set<Section>()
            .Where(section => section.PlaythroughId == playthroughId)
            .MaxAsync(section => (int?)section.SortOrder, cancellationToken) is { } current
            ? current + 1
            : 0;

    private async Task<int> NextGoalPositionAsync(int sectionId, CancellationToken cancellationToken) =>
        await database.Set<Goal>()
            .Where(goal => goal.SectionId == sectionId)
            .MaxAsync(goal => (int?)goal.Position, cancellationToken) is { } current
            ? current + 1
            : 0;

    private async Task NormalizeSectionOrderAsync(
        int playthroughId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var sections = await database.Set<Section>()
            .Where(section => section.PlaythroughId == playthroughId)
            .OrderBy(section => section.SortOrder)
            .ThenBy(section => section.Id)
            .ToListAsync(cancellationToken);

        for (var index = 0; index < sections.Count; index++)
        {
            if (sections[index].SortOrder != index)
            {
                sections[index].Reposition(index, actorId, clock.UtcNow);
            }
        }

        await database.SaveChangesAsync(cancellationToken);
    }
}
