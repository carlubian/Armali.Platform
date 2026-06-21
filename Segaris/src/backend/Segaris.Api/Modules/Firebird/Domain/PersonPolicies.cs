using System.Linq.Expressions;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Firebird.Domain;

internal static class PersonPolicies
{
    public static Expression<Func<Person, bool>> AccessibleTo(UserId userId) =>
        person => person.Visibility == RecordVisibility.Public || person.CreatedBy == userId.Value;

    public static Expression<Func<Person, bool>> MutableBy(UserId userId) => AccessibleTo(userId);

    public static bool CanChangeVisibility(Person person, UserId userId) =>
        person.CreatedBy == userId.Value;
}
