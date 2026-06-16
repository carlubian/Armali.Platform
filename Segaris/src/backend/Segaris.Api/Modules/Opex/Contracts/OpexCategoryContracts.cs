namespace Segaris.Api.Modules.Opex.Contracts;

internal sealed record OpexCategoryRequest(string? Name);

internal sealed record OpexCategoryResponse(int Id, string Name, int SortOrder);
