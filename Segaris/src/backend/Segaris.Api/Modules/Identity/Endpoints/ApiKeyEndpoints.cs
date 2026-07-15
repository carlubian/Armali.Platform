using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Segaris.Api.Modules.Identity.ApiKeys;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Identity.Endpoints;

/// <summary>
/// Self-service API-key management. Keys are a profile concern, so they hang from
/// the profile group rather than the session lifecycle.
/// </summary>
/// <remarks>
/// Creation is self-service only. A user can therefore only ever expose their own
/// records: no user, including an administrator, can mint a key that reaches
/// another user's private data.
/// </remarks>
internal static class ApiKeyEndpoints
{
    public static void MapApiKeyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var keys = endpoints.MapSegarisApiGroup("session/profile/api-keys", "API keys")
            .RequireAuthorization();

        keys.MapPost("", CreateAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithSummary("Creates an API key and returns the complete token exactly once");
        keys.MapGet("", ListAsync)
            .WithSummary("Lists the current user's API keys without their secrets");
        keys.MapDelete("/{id:int}", RevokeAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithSummary("Revokes one of the current user's API keys");
    }

    private static async Task<IResult> CreateAsync(
        CreateApiKeyRequest request,
        ClaimsPrincipal principal,
        UserManager<SegarisUser> userManager,
        ApiKeyService apiKeys,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(principal, userManager);
        var issued = await apiKeys.IssueAsync(user, request.Name, request.ExpiresAt, cancellationToken);

        return TypedResults.Created(
            $"/api/session/profile/api-keys/{issued.Record.Id}",
            new CreatedApiKeyResponse(ToResponse(issued.Record), issued.Token));
    }

    private static async Task<IResult> ListAsync(
        ClaimsPrincipal principal,
        UserManager<SegarisUser> userManager,
        ApiKeyService apiKeys,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(principal, userManager);
        var records = await apiKeys.ListAsync(user.Id, cancellationToken);
        return TypedResults.Ok(records.Select(ToResponse).ToArray());
    }

    private static async Task<IResult> RevokeAsync(
        int id,
        ClaimsPrincipal principal,
        UserManager<SegarisUser> userManager,
        ApiKeyService apiKeys,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(principal, userManager);
        return await apiKeys.RevokeAsync(user.Id, id, cancellationToken)
            ? TypedResults.NoContent()
            : throw ApiProblemException.NotFound();
    }

    private static async Task<SegarisUser> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        UserManager<SegarisUser> userManager) =>
        await userManager.GetUserAsync(principal) ?? throw ApiProblemException.NotFound();

    private static ApiKeyResponse ToResponse(SegarisApiKey key) => new(
        key.Id,
        key.Name,
        key.KeyId,
        key.CreatedAt,
        key.ExpiresAt,
        key.LastUsedAt,
        key.RevokedAt);

    internal sealed record CreateApiKeyRequest(string? Name, DateTimeOffset? ExpiresAt);

    internal sealed record ApiKeyResponse(
        int Id,
        string Name,
        string KeyId,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ExpiresAt,
        DateTimeOffset? LastUsedAt,
        DateTimeOffset? RevokedAt);

    /// <summary>
    /// The only response that carries <paramref name="Token"/>. It is not
    /// recoverable afterwards.
    /// </summary>
    internal sealed record CreatedApiKeyResponse(ApiKeyResponse Key, string Token);
}
