using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Firebird.Contracts;

internal sealed record FirebirdBirthdayCalendarProjection(
    int PersonId,
    string PersonName,
    DateOnly OccurrenceDate,
    string? TargetRoute);

internal interface IFirebirdCalendarProjectionProvider
{
    Task<IReadOnlyList<FirebirdBirthdayCalendarProjection>> ListCalendarBirthdaysAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken);
}
