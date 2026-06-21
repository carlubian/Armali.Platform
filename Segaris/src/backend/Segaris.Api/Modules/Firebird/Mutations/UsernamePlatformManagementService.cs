using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Firebird.Contracts;
using Segaris.Api.Modules.Firebird.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Firebird.Mutations;

internal sealed class UsernamePlatformManagementService(SegarisDbContext database, IClock clock)
{
    public async Task<UsernamePlatformResponse> CreateAsync(UsernamePlatformRequest request, UserId actor, CancellationToken token)
    {
        var name = ValidateName(request.Name);
        await EnsureUniqueAsync(null, name.Normalized, token);
        var now = clock.UtcNow;
        var sortOrder = (await database.Set<UsernamePlatform>().Select(value => (int?)value.SortOrder).MaxAsync(token) ?? -1) + 1;
        var platform = new UsernamePlatform { Name = name.Display, NormalizedName = name.Normalized, SortOrder = sortOrder, CreatedAt = now, CreatedBy = actor.Value, UpdatedAt = now, UpdatedBy = actor.Value };
        database.Add(platform);
        await SaveAsync(token);
        return ToResponse(platform);
    }

    public async Task<UsernamePlatformResponse> UpdateAsync(int id, UsernamePlatformRequest request, UserId actor, CancellationToken token)
    {
        var platform = await FindAsync(id, token);
        var name = ValidateName(request.Name);
        await EnsureUniqueAsync(id, name.Normalized, token);
        platform.Name = name.Display;
        platform.NormalizedName = name.Normalized;
        platform.UpdatedAt = clock.UtcNow;
        platform.UpdatedBy = actor.Value;
        await SaveAsync(token);
        return ToResponse(platform);
    }

    public async Task MoveAsync(int id, CatalogMoveDirection direction, CancellationToken token)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var ordered = await database.Set<UsernamePlatform>().OrderBy(value => value.SortOrder).ThenBy(value => value.Id).ToListAsync(token);
        var index = ordered.FindIndex(value => value.Id == id);
        if (index < 0) throw UsernamePlatformProblem.NotFound();
        var target = direction == CatalogMoveDirection.Up ? index - 1 : index + 1;
        if (target < 0 || target >= ordered.Count) throw UsernamePlatformProblem.Validation("direction", "The username platform cannot move beyond the catalogue boundary.");
        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        for (var position = 0; position < ordered.Count; position++) ordered[position].SortOrder = position;
        await database.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
    }

    public async Task<CatalogDeletionImpactResponse> ImpactAsync(int id, CancellationToken token)
    {
        await FindAsync(id, token, tracked: false);
        var referenced = await database.Set<Username>().AnyAsync(username => username.PlatformId == id, token);
        var count = await database.Set<UsernamePlatform>().CountAsync(token);
        return new(referenced, !referenced && count > 1, false, false, count > 1);
    }

    public async Task DeleteAsync(int id, CancellationToken token)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var platform = await FindAsync(id, token);
        if (await database.Set<UsernamePlatform>().CountAsync(token) <= 1) throw UsernamePlatformProblem.RequiredNotEmpty();
        if (await database.Set<Username>().AnyAsync(username => username.PlatformId == id, token)) throw UsernamePlatformProblem.Referenced();
        database.Remove(platform);
        var remaining = await database.Set<UsernamePlatform>().Where(value => value.Id != id).OrderBy(value => value.SortOrder).ThenBy(value => value.Id).ToListAsync(token);
        for (var position = 0; position < remaining.Count; position++) remaining[position].SortOrder = position;
        try { await database.SaveChangesAsync(token); await transaction.CommitAsync(token); }
        catch (DbUpdateException) { throw UsernamePlatformProblem.Referenced(); }
    }

    public async Task ReplaceAndDeleteAsync(int id, CatalogReplacementRequest request, UserId actor, CancellationToken token)
    {
        if (request.ClearReferences
            || request.ExchangeRate is not null
            || request.ReplacementId is not { } replacementId
            || replacementId <= 0
            || replacementId == id)
        {
            throw UsernamePlatformProblem.InvalidReplacement();
        }

        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var source = await FindAsync(id, token);
        if (!await database.Set<UsernamePlatform>().AnyAsync(value => value.Id == replacementId, token))
        {
            throw UsernamePlatformProblem.InvalidReplacement();
        }

        if (await database.Set<UsernamePlatform>().CountAsync(token) <= 1)
        {
            throw UsernamePlatformProblem.RequiredNotEmpty();
        }

        try
        {
            var usernames = await database.Set<Username>()
                .Where(username => username.PlatformId == id)
                .ToListAsync(token);
            var occurredAt = clock.UtcNow;
            foreach (var username in usernames)
            {
                username.ReplacePlatform(replacementId, actor, occurredAt);
            }

            database.Remove(source);
            var remaining = await database.Set<UsernamePlatform>()
                .Where(value => value.Id != id)
                .OrderBy(value => value.SortOrder)
                .ThenBy(value => value.Id)
                .ToListAsync(token);
            for (var position = 0; position < remaining.Count; position++) remaining[position].SortOrder = position;

            await database.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
        }
        catch (DbUpdateException)
        {
            throw UsernamePlatformProblem.MigrationConflict();
        }
    }

    private async Task<UsernamePlatform> FindAsync(int id, CancellationToken token, bool tracked = true)
    {
        IQueryable<UsernamePlatform> query = database.Set<UsernamePlatform>();
        if (!tracked) query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(value => value.Id == id, token) ?? throw UsernamePlatformProblem.NotFound();
    }

    private async Task EnsureUniqueAsync(int? id, string normalized, CancellationToken token)
    {
        if (await database.Set<UsernamePlatform>().AnyAsync(value => value.NormalizedName == normalized && value.Id != id, token))
            throw UsernamePlatformProblem.DuplicateName();
    }

    private async Task SaveAsync(CancellationToken token) { try { await database.SaveChangesAsync(token); } catch (DbUpdateException) { throw UsernamePlatformProblem.DuplicateName(); } }

    private static (string Display, string Normalized) ValidateName(string? value)
    {
        var display = value?.Trim();
        if (string.IsNullOrWhiteSpace(display) || display.Length > FirebirdValidation.CatalogNameMaximumLength) throw UsernamePlatformProblem.Validation("name", $"Name is required and may contain at most {FirebirdValidation.CatalogNameMaximumLength} characters.");
        return (display, FirebirdCatalogNormalization.Normalize(display));
    }

    private static UsernamePlatformResponse ToResponse(UsernamePlatform value) => new(value.Id, value.Name, value.SortOrder);
}

