using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Firebird.Contracts;
using Segaris.Api.Modules.Firebird.Domain;
using Segaris.Api.Modules.Identity;
using Segaris.Persistence;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Firebird.Queries;

internal sealed class FirebirdPersonReadService(SegarisDbContext database)
{
    public async Task<PaginatedResponse<PersonSummaryResponse>> ListPeopleAsync(
        FirebirdPersonFilter filter,
        PaginationRequest pagination,
        SortRequest sort,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var people = ApplyFilters(
            database.Set<Person>().AsNoTracking().Where(PersonPolicies.AccessibleTo(userId)),
            filter);

        var totalCount = await people.CountAsync(cancellationToken);
        var page = await ApplySort(people, sort)
            .Skip(pagination.Offset)
            .Take(pagination.PageSize)
            .Select(person => new PersonSummaryResponse(
                person.Id,
                person.Name,
                person.CategoryId,
                database.Set<PersonCategory>()
                    .Where(category => category.Id == person.CategoryId).Select(category => category.Name).First(),
                person.Status.ToString(),
                person.BirthdayMonth,
                person.BirthdayDay,
                person.Visibility.ToString(),
                Avatar(person.Id, person.AvatarAttachmentId),
                person.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == person.CreatedBy).Select(user => user.DisplayName).First()))
            .ToArrayAsync(cancellationToken);

        return PaginatedResponse<PersonSummaryResponse>.Create(page, pagination, totalCount);
    }

    public async Task<PersonResponse?> GetPersonAsync(
        int personId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var row = await database.Set<Person>()
            .AsNoTracking()
            .Where(PersonPolicies.AccessibleTo(userId))
            .Where(person => person.Id == personId)
            .Select(person => new PersonDetailRow(
                person.Id,
                person.Name,
                person.CategoryId,
                database.Set<PersonCategory>()
                    .Where(category => category.Id == person.CategoryId).Select(category => category.Name).First(),
                person.Status,
                person.BirthdayMonth,
                person.BirthdayDay,
                person.Notes,
                person.Visibility,
                person.AvatarAttachmentId,
                person.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == person.CreatedBy).Select(user => user.DisplayName).First(),
                person.CreatedAt,
                person.UpdatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == person.UpdatedBy).Select(user => user.DisplayName).First(),
                person.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var usernames = await UsernameResponses(row.Id).ToArrayAsync(cancellationToken);
        var interactions = await InteractionResponses(row.Id).ToArrayAsync(cancellationToken);

        return new PersonResponse(
            row.Id,
            row.Name,
            row.CategoryId,
            row.CategoryName,
            row.Status.ToString(),
            row.BirthdayMonth,
            row.BirthdayDay,
            row.Notes,
            row.Visibility.ToString(),
            Avatar(row.Id, row.AvatarAttachmentId),
            usernames,
            interactions,
            row.CreatedById,
            row.CreatedByName,
            row.CreatedAt,
            row.UpdatedById,
            row.UpdatedByName,
            row.UpdatedAt);
    }

    private static IQueryable<Person> ApplyFilters(
        IQueryable<Person> people,
        FirebirdPersonFilter filter)
    {
        if (filter.Search is { } search)
        {
            var pattern = $"%{Escape(search.ToLowerInvariant())}%";
            people = people.Where(person =>
                EF.Functions.Like(person.Name.ToLower(), pattern, "\\")
                || (person.Notes != null && EF.Functions.Like(person.Notes.ToLower(), pattern, "\\")));
        }

        if (filter.CategoryId is { } categoryId)
        {
            people = people.Where(person => person.CategoryId == categoryId);
        }

        if (filter.Status is { } status)
        {
            people = people.Where(person => person.Status == status);
        }

        if (filter.CreatorId is { } creatorId)
        {
            people = people.Where(person => person.CreatedBy == creatorId);
        }

        if (filter.Visibility is { } visibility)
        {
            people = people.Where(person => person.Visibility == visibility);
        }

        return people;
    }

    private IQueryable<Person> ApplySort(IQueryable<Person> people, SortRequest sort)
    {
        var ascending = sort.Direction == SortDirection.Ascending;

        IOrderedQueryable<Person> ordered = sort.Field switch
        {
            FirebirdPeopleQuery.SortFields.Category => ascending
                ? people.OrderBy(person => database.Set<PersonCategory>()
                    .Where(category => category.Id == person.CategoryId).Select(category => category.Name).First())
                : people.OrderByDescending(person => database.Set<PersonCategory>()
                    .Where(category => category.Id == person.CategoryId).Select(category => category.Name).First()),
            FirebirdPeopleQuery.SortFields.Status => ascending
                ? people.OrderBy(person => person.Status)
                : people.OrderByDescending(person => person.Status),
            FirebirdPeopleQuery.SortFields.Birthday => ascending
                ? people.OrderBy(person => person.BirthdayMonth == null)
                    .ThenBy(person => person.BirthdayMonth)
                    .ThenBy(person => person.BirthdayDay)
                : people.OrderBy(person => person.BirthdayMonth == null)
                    .ThenByDescending(person => person.BirthdayMonth)
                    .ThenByDescending(person => person.BirthdayDay),
            FirebirdPeopleQuery.SortFields.Visibility => ascending
                ? people.OrderBy(person => person.Visibility)
                : people.OrderByDescending(person => person.Visibility),
            FirebirdPeopleQuery.SortFields.TieBreaker => ascending
                ? people.OrderBy(person => person.Id)
                : people.OrderByDescending(person => person.Id),
            _ => ascending
                ? people.OrderBy(person => person.Name)
                : people.OrderByDescending(person => person.Name),
        };

        return ascending ? ordered.ThenBy(person => person.Id) : ordered.ThenByDescending(person => person.Id);
    }

    private static PersonAvatarResponse Avatar(int personId, int? attachmentId) =>
        attachmentId is null
            ? FirebirdAvatarResponseFactory.Placeholder()
            : FirebirdAvatarResponseFactory.Avatar(personId, attachmentId.Value);

    private IQueryable<UsernameResponse> UsernameResponses(int personId) =>
        database.Set<Username>()
            .AsNoTracking()
            .Where(username => username.PersonId == personId)
            .OrderBy(username => username.Id)
            .Select(username => new UsernameResponse(
                username.Id,
                username.PlatformId,
                database.Set<UsernamePlatform>()
                    .Where(platform => platform.Id == username.PlatformId)
                    .Select(platform => platform.Name)
                    .First(),
                username.Handle,
                username.Notes));

    private IQueryable<InteractionResponse> InteractionResponses(int personId) =>
        database.Set<Interaction>()
            .AsNoTracking()
            .Where(interaction => interaction.PersonId == personId)
            .OrderByDescending(interaction => interaction.Date)
            .ThenByDescending(interaction => interaction.Id)
            .Select(interaction => new InteractionResponse(
                interaction.Id,
                interaction.Date,
                interaction.Description));

    private static string Escape(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);

    private sealed record PersonDetailRow(
        int Id,
        string Name,
        int CategoryId,
        string CategoryName,
        PersonStatus Status,
        int? BirthdayMonth,
        int? BirthdayDay,
        string? Notes,
        RecordVisibility Visibility,
        int? AvatarAttachmentId,
        int CreatedById,
        string CreatedByName,
        DateTimeOffset CreatedAt,
        int UpdatedById,
        string UpdatedByName,
        DateTimeOffset UpdatedAt);
}
