namespace Belfalas.Api.Contracts;

public sealed record CreateEraRequest(
    string Name,
    DateOnly StartDate,
    int Weeks,
    string TemplateId,
    IReadOnlyList<CreateAreaRequest> Areas);

public sealed record CreateAreaRequest(string Name, int Order);
