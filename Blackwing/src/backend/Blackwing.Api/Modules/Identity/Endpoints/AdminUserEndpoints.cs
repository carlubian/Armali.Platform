using System.Globalization;
using System.Security.Claims;
using Blackwing.Api.Modules.Identity.Security;
using Blackwing.Api.Platform.Api;
using Blackwing.Shared.Identity;
using Blackwing.Shared.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Blackwing.Api.Modules.Identity.Endpoints;

/// <summary>
/// The complete administrative surface over accounts: list, create, reset a password, and
/// activate or deactivate. There is deliberately nothing else.
/// </summary>
/// <remarks>
/// An administrator has no route of any kind to another account's content. If an administrative
/// endpoint ever appears here returning images, tags, or any other <c>IOwnedByUser</c> entity, it
/// is wrong by definition, not merely questionable.
/// </remarks>
internal static class AdminUserEndpoints
{
    /// <summary>Matches the <c>DisplayName</c> column width in the identity mapping.</summary>
    private const int MaximumDisplayNameLength = 200;

    public static void MapAdminUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapBlackwingApiGroup("admin/users", "Administrative users")
            .RequireAuthorization(IdentityPolicies.Admin);

        group.MapGet("", ListAsync)
            .WithSummary("Lists every account");

        group.MapPost("", CreateAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithSummary("Creates an account with a name, a password, and a role");

        group.MapPost("/{id:int}/password", ResetPasswordAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithSummary("Sets a new password for an account and invalidates its sessions");

        group.MapPost("/{id:int}/activate", (
            int id,
            ClaimsPrincipal principal,
            UserManager<BlackwingUser> userManager,
            CancellationToken cancellationToken) =>
            SetActiveAsync(id, isActive: true, principal, userManager, cancellationToken))
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithSummary("Reactivates an account");

        group.MapPost("/{id:int}/deactivate", (
            int id,
            ClaimsPrincipal principal,
            UserManager<BlackwingUser> userManager,
            CancellationToken cancellationToken) =>
            SetActiveAsync(id, isActive: false, principal, userManager, cancellationToken))
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithSummary("Deactivates an account and invalidates its sessions");
    }

    /// <summary>
    /// Returns the whole list, ordered by identifier. There is no pagination on purpose: the
    /// account corpus of a household fits comfortably in one response, and the cursor-based
    /// contract that the gallery will need is a different shape from an offset one.
    /// </summary>
    private static async Task<IResult> ListAsync(
        UserManager<BlackwingUser> userManager,
        CancellationToken cancellationToken)
    {
        var users = await userManager.Users
            .AsNoTracking()
            .OrderBy(user => user.Id)
            .ToListAsync(cancellationToken);

        var items = new List<AdminUserResponse>(users.Count);
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            items.Add(ToResponse(user, roles));
        }

        return TypedResults.Ok(items);
    }

    private static async Task<IResult> CreateAsync(
        CreateUserRequest request,
        UserManager<BlackwingUser> userManager,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var (userName, role) = ValidateCreate(request);
        cancellationToken.ThrowIfCancellationRequested();

        var user = new BlackwingUser
        {
            UserName = userName,
            DisplayName = userName,
            IsActive = true,
            CreatedAt = clock.UtcNow,
        };

        var created = await userManager.CreateAsync(user, request.Password!);
        if (!created.Succeeded)
        {
            throw IdentityProblem.FromResult(created, "password");
        }

        var assigned = await userManager.AddToRoleAsync(user, role.ToString());
        if (!assigned.Succeeded)
        {
            throw IdentityProblem.FromResult(assigned, "role");
        }

        var response = ToResponse(user, [role.ToString()]);
        return TypedResults.Created($"/api/admin/users/{user.Id}", response);
    }

    private static async Task<IResult> ResetPasswordAsync(
        int id,
        SetPasswordRequest request,
        UserManager<BlackwingUser> userManager,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await FindAsync(id, userManager);

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword ?? string.Empty);
        if (!result.Succeeded)
        {
            throw IdentityProblem.FromResult(result, "newPassword");
        }

        // ResetPasswordAsync rotates the security stamp on its own, and the stamp validation
        // interval is zero, so any live session of that account dies on its next request.
        return TypedResults.NoContent();
    }

    private static async Task<IResult> SetActiveAsync(
        int id,
        bool isActive,
        ClaimsPrincipal principal,
        UserManager<BlackwingUser> userManager,
        CancellationToken cancellationToken)
    {
        var user = await FindAsync(id, userManager);

        if (user.IsActive == isActive)
        {
            return TypedResults.NoContent();
        }

        if (!isActive)
        {
            // An administrator locking themselves out mid-operation is a real failure, not a
            // hypothetical one.
            if (string.Equals(
                    userManager.GetUserId(principal),
                    user.Id.ToString(CultureInfo.InvariantCulture),
                    StringComparison.Ordinal))
            {
                throw ActiveStateProblem("Administrators cannot deactivate their own account.");
            }

            // Keep at least one active administrator, or the house is left with no way to manage
            // accounts at all.
            if (await IsLastActiveAdministratorAsync(user, userManager, cancellationToken))
            {
                throw ActiveStateProblem("At least one active administrator must remain.");
            }
        }

        user.IsActive = isActive;
        var updated = await userManager.UpdateAsync(user);
        if (!updated.Succeeded)
        {
            throw IdentityProblem.FromResult(updated, "isActive");
        }

        if (!isActive)
        {
            // Invalidate the account's live sessions immediately.
            await userManager.UpdateSecurityStampAsync(user);
        }

        return TypedResults.NoContent();
    }

    private static async Task<bool> IsLastActiveAdministratorAsync(
        BlackwingUser candidate,
        UserManager<BlackwingUser> userManager,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var administrators = await userManager.GetUsersInRoleAsync(PlatformRole.Admin.ToString());
        if (!administrators.Any(administrator => administrator.Id == candidate.Id))
        {
            return false;
        }

        return administrators.Count(administrator => administrator.IsActive) <= 1;
    }

    private static async Task<BlackwingUser> FindAsync(int id, UserManager<BlackwingUser> userManager) =>
        await userManager.FindByIdAsync(id.ToString(CultureInfo.InvariantCulture))
        ?? throw ApiProblemException.NotFound();

    private static (string UserName, PlatformRole Role) ValidateCreate(CreateUserRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        var userName = request.UserName?.Trim() ?? string.Empty;
        if (userName.Length is 0 or > MaximumDisplayNameLength)
        {
            errors["userName"] =
                [$"User name must contain between 1 and {MaximumDisplayNameLength} characters."];
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors["password"] = ["Password is required."];
        }

        PlatformRole role = default;
        if (string.IsNullOrWhiteSpace(request.Role)
            || !Enum.TryParse(request.Role, ignoreCase: true, out role)
            || !Enum.IsDefined(role))
        {
            errors["role"] = ["Role must be 'User' or 'Admin'."];
        }

        if (errors.Count > 0)
        {
            throw new ApiProblemException(
                StatusCodes.Status400BadRequest,
                ApiErrorCodes.BadRequest,
                "One or more request values are invalid.",
                errors: errors);
        }

        return (userName, role);
    }

    private static ApiProblemException ActiveStateProblem(string message) => new(
        StatusCodes.Status400BadRequest,
        ApiErrorCodes.BadRequest,
        "One or more request values are invalid.",
        errors: new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["isActive"] = [message],
        });

    private static AdminUserResponse ToResponse(BlackwingUser user, IEnumerable<string> roles) => new(
        user.Id,
        user.UserName!,
        user.DisplayName,
        roles.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
        user.IsActive,
        user.CreatedAt);

    internal sealed record CreateUserRequest(string? UserName, string? Password, string? Role);

    internal sealed record SetPasswordRequest(string? NewPassword);

    internal sealed record AdminUserResponse(
        int Id,
        string UserName,
        string DisplayName,
        IReadOnlyList<string> Roles,
        bool IsActive,
        DateTimeOffset CreatedAt);
}
