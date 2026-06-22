using Segaris.Api.Modules.Recipes.Domain;

namespace Segaris.Api.Modules.Recipes.Queries;

/// <summary>Bound query surface for <c>GET /api/recipes/menus</c>.</summary>
internal sealed class WeeklyMenuListQuery
{
    public DateOnly? Week { get; init; }

    public WeeklyMenuFilter ToFilter() => new(Week is { } week ? WeeklyMenu.NormalizeWeek(week) : null);
}

internal sealed record WeeklyMenuFilter(DateOnly? Week);
