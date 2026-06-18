using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Mood.Contracts;
using Segaris.Api.Modules.Mood.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Mood.Mutations;

internal sealed class MoodEntryWriteService(SegarisDbContext database, IClock clock)
{
    public async Task<int> CreateAsync(
        CreateMoodEntryRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entry = MoodEntry.Create(Map(request), actorId, clock.UtcNow);
        database.Add(entry);
        await database.SaveChangesAsync(cancellationToken);
        return entry.Id;
    }

    public async Task<bool> UpdateAsync(
        int entryId,
        UpdateMoodEntryRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entry = await database.Set<MoodEntry>()
            .Where(candidate => candidate.Id == entryId && candidate.CreatedBy == actorId.Value)
            .FirstOrDefaultAsync(cancellationToken);
        if (entry is null)
        {
            return false;
        }

        entry.Update(Map(request), actorId, clock.UtcNow);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(
        int entryId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var entry = await database.Set<MoodEntry>()
            .Where(candidate => candidate.Id == entryId && candidate.CreatedBy == actorId.Value)
            .FirstOrDefaultAsync(cancellationToken);
        if (entry is null)
        {
            return false;
        }

        database.Remove(entry);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static MoodEntryValues Map(CreateMoodEntryRequest request) => new(
        request.EntryDate,
        request.Score,
        ParseEnum<MoodEnergy>(request.Energy, "energy"),
        ParseEnum<MoodAlignment>(request.Alignment, "alignment"),
        ParseEnum<MoodDirection>(request.Direction, "direction"),
        ParseEnum<MoodSource>(request.Source, "source"),
        request.Notes);

    private static MoodEntryValues Map(UpdateMoodEntryRequest request) => new(
        request.EntryDate,
        request.Score,
        ParseEnum<MoodEnergy>(request.Energy, "energy"),
        ParseEnum<MoodAlignment>(request.Alignment, "alignment"),
        ParseEnum<MoodDirection>(request.Direction, "direction"),
        ParseEnum<MoodSource>(request.Source, "source"),
        request.Notes);

    private static TEnum ParseEnum<TEnum>(string? value, string field)
        where TEnum : struct, Enum
    {
        if (!string.IsNullOrWhiteSpace(value)
            && Enum.TryParse<TEnum>(value.Trim(), ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new MoodValidationException(
            $"The {field} criterion is not a recognized value.",
            MoodValidationReason.Criteria);
    }
}
