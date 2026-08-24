using System.Security.Claims;
using Blackwing.Api.Modules.Identity.Security;
using Blackwing.Api.Platform.Api;
using Blackwing.Api.Platform.Observability;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;

namespace Blackwing.Api.Modules.Identity.Endpoints;

internal static class SessionEndpoints
{
    public static void MapSessionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapBlackwingApiGroup("session", "Session");

        group.MapGet("/antiforgery", (HttpContext httpContext, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(httpContext);
            return TypedResults.Ok(new AntiforgeryResponse(tokens.RequestToken!));
        })
        .AllowAnonymous()
        .WithSummary("Issues an antiforgery token for subsequent state-changing requests");

        group.MapPost("", LoginAsync)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingExtensions.AuthenticationRateLimitPolicy)
            .WithSummary("Authenticates a user and establishes a session cookie");

        group.MapGet("", GetSessionAsync)
            .RequireAuthorization()
            .WithSummary("Returns the current authenticated session");

        group.MapDelete("", async (SignInManager<BlackwingUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return TypedResults.NoContent();
        })
        .RequireAuthorization()
        .AddEndpointFilter<AntiforgeryEndpointFilter>()
        .WithSummary("Signs out and removes the session cookie");
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        UserManager<BlackwingUser> userManager,
        SignInManager<BlackwingUser> signInManager,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ApiProblemException(
                StatusCodes.Status400BadRequest,
                ApiErrorCodes.BadRequest,
                "One or more request values are invalid.",
                errors: new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    ["userName"] = string.IsNullOrWhiteSpace(request.UserName)
                        ? ["User name is required."]
                        : [],
                    ["password"] = string.IsNullOrWhiteSpace(request.Password)
                        ? ["Password is required."]
                        : [],
                });
        }

        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByNameAsync(request.UserName);

        // A deactivated account is refused exactly as a non-existent one is. All three failure
        // modes - unknown user, wrong password, deactivated account - leave through the same
        // exception, so the response body can never be used to prove an account exists.
        if (user is null || !user.IsActive)
        {
            throw InvalidCredentials();
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            throw InvalidCredentials();
        }

        return TypedResults.NoContent();
    }

    private static async Task<IResult> GetSessionAsync(
        ClaimsPrincipal principal,
        UserManager<BlackwingUser> userManager,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            throw ApiProblemException.NotFound();
        }

        var roles = principal.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        return TypedResults.Ok(new SessionResponse(
            user.Id,
            user.UserName!,
            user.DisplayName,
            roles));
    }

    private static ApiProblemException InvalidCredentials() => new(
        StatusCodes.Status401Unauthorized,
        ApiErrorCodes.Unauthorized,
        "Authentication failed.");

    internal sealed record LoginRequest(string? UserName, string? Password);

    internal sealed record SessionResponse(
        int UserId,
        string UserName,
        string DisplayName,
        IReadOnlyList<string> Roles);

    internal sealed record AntiforgeryResponse(string CsrfToken);
}
