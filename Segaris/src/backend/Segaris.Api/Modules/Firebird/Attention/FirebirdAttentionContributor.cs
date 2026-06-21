using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Firebird.Domain;
using Segaris.Api.Modules.Launcher.Contracts;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Firebird.Attention;

/// <summary>
/// Contributes the Firebird launcher card's attention state for birthdays due
/// from today through the next seven natural days in Europe/Madrid.
/// </summary>
internal sealed class FirebirdAttentionContributor(
    SegarisDbContext database,
    ICurrentUser currentUser,
    IClock clock) : ILauncherAttentionContributor
{
    public string Module => FirebirdLauncherCard.ModuleKey;

    public async Task<bool> RequiresAttentionAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return false;
        }

        var today = FirebirdCivilDate.Today(clock);
        var windowEnd = today.AddDays(FirebirdDefaults.AttentionWindowDays);
        var birthdays = await database.Set<Person>()
            .AsNoTracking()
            .Where(PersonPolicies.AccessibleTo(userId))
            .Where(person => person.BirthdayMonth.HasValue && person.BirthdayDay.HasValue)
            .Select(person => new { Month = person.BirthdayMonth!.Value, Day = person.BirthdayDay!.Value })
            .ToListAsync(cancellationToken);

        return birthdays.Any(birthday =>
            FirebirdBirthdayRules.NextOccurrence(new FirebirdBirthday(birthday.Month, birthday.Day), today) <= windowEnd);
    }
}
