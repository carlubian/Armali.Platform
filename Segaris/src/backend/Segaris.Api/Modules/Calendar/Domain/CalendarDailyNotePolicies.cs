using System.Linq.Expressions;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Calendar.Domain;

internal static class CalendarDailyNotePolicies
{
    public static Expression<Func<CalendarDailyNote, bool>> AccessibleTo(UserId userId) =>
        note => note.Visibility == RecordVisibility.Public || note.CreatedBy == userId.Value;

    public static Expression<Func<CalendarDailyNote, bool>> MutableBy(UserId userId) => AccessibleTo(userId);

    public static bool CanChangeVisibility(CalendarDailyNote note, UserId userId) =>
        note.CreatedBy == userId.Value;
}
