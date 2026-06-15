namespace Segaris.Api.Modules.Configuration.Contracts;

/// <summary>
/// The two allowed reordering directions. A move that would cross a catalog
/// boundary (moving the first row up or the last row down) is a validation error
/// rather than a silent no-op.
/// </summary>
internal enum CatalogMoveDirection
{
    Up,
    Down,
}

/// <summary>
/// Parses the frozen wire vocabulary (<c>up</c>, <c>down</c>) into a
/// <see cref="CatalogMoveDirection"/>. Matching is case-insensitive; any other
/// value is rejected so the management endpoints can return a validation error.
/// </summary>
internal static class CatalogMoveDirections
{
    public const string Up = "up";

    public const string Down = "down";

    public static bool TryParse(string? value, out CatalogMoveDirection direction)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case Up:
                direction = CatalogMoveDirection.Up;
                return true;
            case Down:
                direction = CatalogMoveDirection.Down;
                return true;
            default:
                direction = default;
                return false;
        }
    }

    public static string ToWireValue(CatalogMoveDirection direction) => direction switch
    {
        CatalogMoveDirection.Up => Up,
        CatalogMoveDirection.Down => Down,
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
    };
}
