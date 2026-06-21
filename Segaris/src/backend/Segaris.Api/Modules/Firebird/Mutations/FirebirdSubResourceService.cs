using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Firebird.Contracts;
using Segaris.Api.Modules.Firebird.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Firebird.Mutations;

internal sealed class FirebirdSubResourceService(SegarisDbContext database, IClock clock)
{
    public async Task<IReadOnlyList<UsernameResponse>?> ListUsernamesAsync(
        int personId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        if (!await PersonAccessibleAsync(personId, actorId, cancellationToken))
        {
            return null;
        }

        return await UsernameResponses(personId)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<UsernameResponse?> CreateUsernameAsync(
        int personId,
        UsernameRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await PersonMutableAsync(personId, actorId, cancellationToken))
        {
            return null;
        }

        var values = new UsernameValues(request.PlatformId, request.Handle, request.Notes);
        await ValidatePlatformAsync(values.PlatformId, cancellationToken);

        var username = Username.Create(personId, values, actorId, clock.UtcNow);
        database.Add(username);
        await database.SaveChangesAsync(cancellationToken);
        return await UsernameResponse(personId, username.Id)
            .SingleAsync(cancellationToken);
    }

    public async Task<UsernameResponse?> UpdateUsernameAsync(
        int personId,
        int usernameId,
        UsernameRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await PersonMutableAsync(personId, actorId, cancellationToken))
        {
            return null;
        }

        var username = await database.Set<Username>()
            .Where(candidate => candidate.PersonId == personId && candidate.Id == usernameId)
            .SingleOrDefaultAsync(cancellationToken);
        if (username is null)
        {
            return null;
        }

        var values = new UsernameValues(request.PlatformId, request.Handle, request.Notes);
        await ValidatePlatformAsync(values.PlatformId, cancellationToken);

        username.Update(values, actorId, clock.UtcNow);
        await database.SaveChangesAsync(cancellationToken);
        return await UsernameResponse(personId, usernameId)
            .SingleAsync(cancellationToken);
    }

    public async Task<bool> DeleteUsernameAsync(
        int personId,
        int usernameId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        if (!await PersonMutableAsync(personId, actorId, cancellationToken))
        {
            return false;
        }

        var username = await database.Set<Username>()
            .Where(candidate => candidate.PersonId == personId && candidate.Id == usernameId)
            .SingleOrDefaultAsync(cancellationToken);
        if (username is null)
        {
            return false;
        }

        database.Remove(username);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<InteractionResponse>?> ListInteractionsAsync(
        int personId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        if (!await PersonAccessibleAsync(personId, actorId, cancellationToken))
        {
            return null;
        }

        return await InteractionResponses(personId)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<InteractionResponse?> CreateInteractionAsync(
        int personId,
        InteractionRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await PersonMutableAsync(personId, actorId, cancellationToken))
        {
            return null;
        }

        var interaction = Interaction.Create(
            personId,
            new InteractionValues(request.Date, request.Description),
            actorId,
            clock.UtcNow,
            FirebirdCivilDate.Today(clock));
        database.Add(interaction);
        await database.SaveChangesAsync(cancellationToken);
        return await InteractionResponse(personId, interaction.Id)
            .SingleAsync(cancellationToken);
    }

    public async Task<InteractionResponse?> UpdateInteractionAsync(
        int personId,
        int interactionId,
        InteractionRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await PersonMutableAsync(personId, actorId, cancellationToken))
        {
            return null;
        }

        var interaction = await database.Set<Interaction>()
            .Where(candidate => candidate.PersonId == personId && candidate.Id == interactionId)
            .SingleOrDefaultAsync(cancellationToken);
        if (interaction is null)
        {
            return null;
        }

        interaction.Update(
            new InteractionValues(request.Date, request.Description),
            actorId,
            clock.UtcNow,
            FirebirdCivilDate.Today(clock));
        await database.SaveChangesAsync(cancellationToken);
        return await InteractionResponse(personId, interactionId)
            .SingleAsync(cancellationToken);
    }

    public async Task<bool> DeleteInteractionAsync(
        int personId,
        int interactionId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        if (!await PersonMutableAsync(personId, actorId, cancellationToken))
        {
            return false;
        }

        var interaction = await database.Set<Interaction>()
            .Where(candidate => candidate.PersonId == personId && candidate.Id == interactionId)
            .SingleOrDefaultAsync(cancellationToken);
        if (interaction is null)
        {
            return false;
        }

        database.Remove(interaction);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ValidatePlatformAsync(int platformId, CancellationToken cancellationToken)
    {
        var exists = await database.Set<UsernamePlatform>()
            .AnyAsync(platform => platform.Id == platformId, cancellationToken);
        if (!exists)
        {
            throw new FirebirdValidationException(
                "One or more Firebird catalog references do not exist.",
                FirebirdValidationReason.UnknownCatalogReference);
        }
    }

    private async Task<bool> PersonAccessibleAsync(int personId, UserId actorId, CancellationToken cancellationToken) =>
        await database.Set<Person>()
            .AsNoTracking()
            .Where(PersonPolicies.AccessibleTo(actorId))
            .AnyAsync(person => person.Id == personId, cancellationToken);

    private async Task<bool> PersonMutableAsync(int personId, UserId actorId, CancellationToken cancellationToken) =>
        await database.Set<Person>()
            .AsNoTracking()
            .Where(PersonPolicies.MutableBy(actorId))
            .AnyAsync(person => person.Id == personId, cancellationToken);

    private IQueryable<Username> Usernames(int personId) =>
        database.Set<Username>()
            .AsNoTracking()
            .Where(username => username.PersonId == personId)
            .OrderBy(username => username.Id);

    private IQueryable<UsernameResponse> UsernameResponses(int personId) =>
        Usernames(personId)
            .Select(username => new UsernameResponse(
                username.Id,
                username.PlatformId,
                database.Set<UsernamePlatform>()
                    .Where(platform => platform.Id == username.PlatformId)
                    .Select(platform => platform.Name)
                    .First(),
                username.Handle,
                username.Notes));

    private IQueryable<UsernameResponse> UsernameResponse(int personId, int usernameId) =>
        Usernames(personId)
            .Where(username => username.Id == usernameId)
            .Select(username => new UsernameResponse(
                username.Id,
                username.PlatformId,
                database.Set<UsernamePlatform>()
                    .Where(platform => platform.Id == username.PlatformId)
                    .Select(platform => platform.Name)
                    .First(),
                username.Handle,
                username.Notes));

    private IQueryable<Interaction> Interactions(int personId) =>
        database.Set<Interaction>()
            .AsNoTracking()
            .Where(interaction => interaction.PersonId == personId)
            .OrderByDescending(interaction => interaction.Date)
            .ThenByDescending(interaction => interaction.Id);

    private IQueryable<InteractionResponse> InteractionResponses(int personId) =>
        Interactions(personId)
            .Select(interaction => new InteractionResponse(
                interaction.Id,
                interaction.Date,
                interaction.Description));

    private IQueryable<InteractionResponse> InteractionResponse(int personId, int interactionId) =>
        Interactions(personId)
            .Where(interaction => interaction.Id == interactionId)
            .Select(interaction => new InteractionResponse(
                interaction.Id,
                interaction.Date,
                interaction.Description));
}
