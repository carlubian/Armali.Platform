namespace Segaris.Api.Modules.Firebird.Contracts;

internal sealed record CreatePersonRequest(
    string? Name,
    int CategoryId,
    string? Status,
    int? BirthdayMonth,
    int? BirthdayDay,
    string? Notes,
    string? Visibility);

internal sealed record UpdatePersonRequest(
    string? Name,
    int CategoryId,
    string? Status,
    int? BirthdayMonth,
    int? BirthdayDay,
    string? Notes,
    string? Visibility);

internal sealed record UsernameRequest(
    int PlatformId,
    string? Handle,
    string? Notes);

internal sealed record InteractionRequest(
    DateOnly Date,
    string? Description);

internal sealed record PersonCategoryRequest(string? Name);

internal sealed record UsernamePlatformRequest(string? Name);
