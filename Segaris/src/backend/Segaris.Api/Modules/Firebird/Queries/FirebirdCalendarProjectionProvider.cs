using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Firebird.Contracts;
using Segaris.Api.Modules.Firebird.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Firebird.Queries;

/// <summary>
/// Publishes accessible people's birthdays as Calendar projections. Because birthdays
/// are stored as a month/day pair rather than a civil date, the accessible people with
/// a stored birthday are read first and the source-owned occurrence/leap-day rule is
/// applied in memory to expand each birthday into the occurrences that fall in the
/// requested range. The set of people is household-scale, so this stays bounded.
/// </summary>
internal sealed class FirebirdCalendarProjectionProvider(SegarisDbContext database)
    : IFirebirdCalendarProjectionProvider
{
    public async Task<IReadOnlyList<FirebirdBirthdayCalendarProjection>> ListCalendarBirthdaysAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken)
    {
        var people = await database.Set<Person>()
            .AsNoTracking()
            .Where(PersonPolicies.AccessibleTo(viewer))
            .Where(person => person.BirthdayMonth != null && person.BirthdayDay != null)
            .Select(person => new BirthdayRow(person.Id, person.Name, person.BirthdayMonth!.Value, person.BirthdayDay!.Value))
            .ToArrayAsync(cancellationToken);

        var projections = new List<FirebirdBirthdayCalendarProjection>();
        foreach (var person in people)
        {
            var birthday = new FirebirdBirthday(person.Month, person.Day);
            foreach (var occurrence in FirebirdBirthdayRules.OccurrencesInRange(birthday, from, to))
            {
                projections.Add(new FirebirdBirthdayCalendarProjection(
                    person.Id,
                    person.Name,
                    occurrence,
                    $"/people?personId={person.Id}"));
            }
        }

        return projections;
    }

    private sealed record BirthdayRow(int Id, string Name, int Month, int Day);
}
