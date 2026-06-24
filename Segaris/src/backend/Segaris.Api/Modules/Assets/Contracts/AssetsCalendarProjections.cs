using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Assets.Contracts;

internal sealed record AssetExpectedEndOfLifeCalendarProjection(
    int AssetId,
    string Name,
    string Status,
    DateOnly ExpectedEndOfLifeDate,
    string? TargetRoute);

internal interface IAssetsCalendarProjectionProvider
{
    Task<IReadOnlyList<AssetExpectedEndOfLifeCalendarProjection>> ListCalendarExpectedEndOfLifeAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken);
}
