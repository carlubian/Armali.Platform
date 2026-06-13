using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Platform.Api;
using Segaris.Api.Platform.Observability;
using Segaris.Shared.Attachments;

namespace Segaris.Api.Modules.Identity.Endpoints;

internal static class SessionEndpoints
{
    public static void MapSessionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup("session", "Session");

        group.MapGet("/antiforgery", (HttpContext httpContext, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(httpContext);
            return TypedResults.Ok(new AntiforgeryResponse(tokens.RequestToken!));
        })
        .AllowAnonymous()
        .WithSummary("Issues an antiforgery token for subsequent state-changing requests");

        group.MapPost("", LoginAsync)
            .AllowAnonymous()
            .RequireRateLimiting(ObservabilityServiceCollectionExtensions.AuthenticationRateLimitPolicy)
            .WithSummary("Authenticates a user and establishes a session cookie");

        group.MapGet("", GetSessionAsync)
        .RequireAuthorization()
        .WithSummary("Returns the current authenticated session");

        group.MapDelete("", async (SignInManager<SegarisUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return TypedResults.NoContent();
        })
        .RequireAuthorization()
        .AddEndpointFilter<AntiforgeryEndpointFilter>()
        .WithSummary("Signs out and removes the session cookie");

        group.MapPost("/password", ChangePasswordAsync)
            .RequireAuthorization()
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithSummary("Changes the current user's password");
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        UserManager<SegarisUser> userManager,
        SignInManager<SegarisUser> signInManager,
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
        UserManager<SegarisUser> userManager,
        IAttachmentService attachments,
        CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            throw ApiProblemException.NotFound();
        }

        var roles = principal.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var avatar = await attachments.FindByOwnerAsync(
            IdentityProfilePolicy.AvatarOwner(user.Id),
            cancellationToken);

        return TypedResults.Ok(new SessionResponse(
            user.Id,
            user.UserName!,
            user.DisplayName,
            user.Language,
            roles,
            avatar is null ? null : IdentityProfilePolicy.AvatarUrl(user.Id)));
    }

    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        UserManager<SegarisUser> userManager,
        SignInManager<SegarisUser> signInManager,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            throw ApiProblemException.NotFound();
        }

        var result = await userManager.ChangePasswordAsync(
            user,
            request.CurrentPassword ?? string.Empty,
            request.NewPassword ?? string.Empty);

        if (!result.Succeeded)
        {
            throw IdentityProblem.FromResult(result, "newPassword");
        }

        // ChangePasswordAsync rotates the security stamp; refresh the current cookie so
        // the active session is not invalidated by the user's own password change.
        await signInManager.RefreshSignInAsync(user);
        return TypedResults.NoContent();
    }

    private static ApiProblemException InvalidCredentials() => new(
        StatusCodes.Status401Unauthorized,
        ApiErrorCodes.Unauthorized,
        "Authentication failed.");

    internal sealed record LoginRequest(string? UserName, string? Password);

    internal sealed record ChangePasswordRequest(string? CurrentPassword, string? NewPassword);

    internal sealed record SessionResponse(
        int UserId,
        string UserName,
        string DisplayName,
        string Language,
        IReadOnlyList<string> Roles,
        string? AvatarUrl);

    internal sealed record AntiforgeryResponse(string CsrfToken);
}
