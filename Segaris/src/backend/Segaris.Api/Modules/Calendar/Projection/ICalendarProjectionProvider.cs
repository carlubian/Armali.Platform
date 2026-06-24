using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Calendar.Projection;

internal interface ICalendarProjectionProvider
{
    string SourceModule { get; }

    Task<IReadOnlyList<NormalizedCalendarProjection>> ListAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken);
}
