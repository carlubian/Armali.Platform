using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Api;
using Segaris.Shared.Attachments;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Identity.Endpoints;

internal static class AdminUserEndpoints
{
    private static readonly HashSet<string> AllowedSortFields =
        new(StringComparer.Ordinal) { "id", "userName", "createdAt" };

    public static void MapAdminUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup("admin/users", "Administrative users")
            .RequireAuthorization(IdentityPolicies.Admin);

        group.MapGet("", ListAsync)
            .WithSummary("Lists household accounts");

        group.MapPost("", CreateAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithSummary("Creates a household account");

        group.MapPut("/{id:int}", UpdateAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithSummary("Updates an account's display name and role");

        group.MapPost("/{id:int}/activate", (int id, UserManager<SegarisUser> userManager) =>
            SetActiveAsync(id, true, userManager))
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithSummary("Activates an account");

        group.MapPost("/{id:int}/deactivate", (int id, UserManager<SegarisUser> userManager) =>
            SetActiveAsync(id, false, userManager))
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithSummary("Deactivates an account and invalidates its sessions");

        group.MapPost("/{id:int}/password", RecoverCredentialAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithSummary("Sets a new password for an account and invalidates its sessions");
    }

    private static async Task<IResult> ListAsync(
        [AsParameters] PaginationQuery query,
        HttpRequest request,
        UserManager<SegarisUser> userManager,
        IAttachmentService attachments,
        CancellationToken cancellationToken)
    {
        var pagination = query.ToRequest();
        var sort = ParseSort(
            request.Query["sort"].FirstOrDefault(),
            request.Query["sortDirection"].FirstOrDefault());

        var users = userManager.Users.AsNoTracking();
        var ordered = ApplySort(users, sort);

        var totalCount = await users.CountAsync(cancellationToken);
        var page = await ordered
            .Skip(pagination.Offset)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        var items = new List<AdminUserResponse>(page.Count);
        foreach (var user in page)
        {
            var roles = await userManager.GetRolesAsync(user);
            var avatar = await attachments.FindByOwnerAsync(
                IdentityProfilePolicy.AvatarOwner(user.Id),
                cancellationToken);
            items.Add(ToResponse(user, roles, avatar is not null));
        }

        return TypedResults.Ok(PaginatedResponse<AdminUserResponse>.Create(items, pagination, totalCount));
    }

    private static async Task<IResult> CreateAsync(
        CreateUserRequest request,
        UserManager<SegarisUser> userManager,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var role = ValidateCreate(request);
        cancellationToken.ThrowIfCancellationRequested();

        var user = new SegarisUser
        {
            UserName = request.UserName,
            DisplayName = request.UserName!.Trim(),
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

        var response = ToResponse(user, [role.ToString()], hasAvatar: false);
        return TypedResults.Created($"/api/admin/users/{user.Id}", response);
    }

    private static async Task<IResult> UpdateAsync(
        int id,
        UpdateUserRequest request,
        ClaimsPrincipal principal,
        UserManager<SegarisUser> userManager,
        IAttachmentService attachments,
        CancellationToken cancellationToken)
    {
        var (displayName, role) = ValidateUpdate(request);

        var user = await userManager.FindByIdAsync(id.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (user is null)
        {
            throw ApiProblemException.NotFound();
        }

        var roles = await userManager.GetRolesAsync(user);
        var currentRole = roles.Contains(PlatformRole.Admin.ToString())
            ? PlatformRole.Admin
            : PlatformRole.User;
        var roleChanged = role != currentRole;

        if (roleChanged)
        {
            // Administrators cannot change their own role; a different administrator
            // must do it. This also prevents an administrator from accidentally
            // demoting themselves out of administration.
            if (string.Equals(
                    userManager.GetUserId(principal),
                    user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    StringComparison.Ordinal))
            {
                throw RoleProblem("Administrators cannot change their own role.");
            }

            // Keep at least one administrator so the household cannot lock itself out.
            if (currentRole == PlatformRole.Admin
                && await IsLastAdministratorAsync(userManager, cancellationToken))
            {
                throw RoleProblem("At least one administrator must remain.");
            }
        }

        user.DisplayName = displayName;
        var updated = await userManager.UpdateAsync(user);
        if (!updated.Succeeded)
        {
            throw IdentityProblem.FromResult(updated, "displayName");
        }

        if (roleChanged)
        {
            var removed = await userManager.RemoveFromRolesAsync(user, roles);
            if (!removed.Succeeded)
            {
                throw IdentityProblem.FromResult(removed, "role");
            }

            var assigned = await userManager.AddToRoleAsync(user, role.ToString());
            if (!assigned.Succeeded)
            {
                throw IdentityProblem.FromResult(assigned, "role");
            }
        }

        var avatar = await attachments.FindByOwnerAsync(
            IdentityProfilePolicy.AvatarOwner(user.Id),
            cancellationToken);
        return TypedResults.Ok(ToResponse(user, [role.ToString()], avatar is not null));
    }

    private static async Task<bool> IsLastAdministratorAsync(
        UserManager<SegarisUser> userManager,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var admins = await userManager.GetUsersInRoleAsync(PlatformRole.Admin.ToString());
        return admins.Count <= 1;
    }

    private static async Task<IResult> SetActiveAsync(
        int id,
        bool isActive,
        UserManager<SegarisUser> userManager)
    {
        var user = await userManager.FindByIdAsync(id.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (user is null)
        {
            throw ApiProblemException.NotFound();
        }

        if (user.IsActive == isActive)
        {
            return TypedResults.NoContent();
        }

        user.IsActive = isActive;
        var updated = await userManager.UpdateAsync(user);
        if (!updated.Succeeded)
        {
            throw IdentityProblem.FromResult(updated, "isActive");
        }

        if (!isActive)
        {
            // Invalidate active sessions immediately when an account is deactivated.
            await userManager.UpdateSecurityStampAsync(user);
        }

        return TypedResults.NoContent();
    }

    private static async Task<IResult> RecoverCredentialAsync(
        int id,
        SetPasswordRequest request,
        UserManager<SegarisUser> userManager,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByIdAsync(id.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (user is null)
        {
            throw ApiProblemException.NotFound();
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword ?? string.Empty);
        if (!result.Succeeded)
        {
            throw IdentityProblem.FromResult(result, "newPassword");
        }

        return TypedResults.NoContent();
    }

    private static PlatformRole ValidateCreate(CreateUserRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            errors["userName"] = ["User name is required."];
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

        return role;
    }

    private static (string DisplayName, PlatformRole Role) ValidateUpdate(UpdateUserRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (!IdentityProfilePolicy.TryNormalizeDisplayName(request.DisplayName, out var displayName))
        {
            errors["displayName"] =
                [$"Display name must contain between 1 and {IdentityProfilePolicy.MaximumDisplayNameLength} characters."];
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

        return (displayName, role);
    }

    private static ApiProblemException RoleProblem(string message) => new(
        StatusCodes.Status400BadRequest,
        ApiErrorCodes.BadRequest,
        "One or more request values are invalid.",
        errors: new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["role"] = [message],
        });

    private static SortRequest ParseSort(string? sort, string? direction)
    {
        try
        {
            return SortRequest.Create(sort, direction, AllowedSortFields, "createdAt", "id");
        }
        catch (ArgumentException exception)
        {
            throw new ApiProblemException(
                StatusCodes.Status400BadRequest,
                ApiErrorCodes.BadRequest,
                "One or more request values are invalid.",
                errors: new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    [exception.ParamName == "direction" ? "sortDirection" : "sort"] = [exception.Message],
                });
        }
    }

    private static IQueryable<SegarisUser> ApplySort(IQueryable<SegarisUser> users, SortRequest sort)
    {
        var ascending = sort.Direction == SortDirection.Ascending;
        return sort.Field switch
        {
            "userName" => ascending
                ? users.OrderBy(user => user.NormalizedUserName).ThenBy(user => user.Id)
                : users.OrderByDescending(user => user.NormalizedUserName).ThenBy(user => user.Id),
            // Auto-increment integer keys are monotonic with creation, so ordering by Id
            // reproduces creation order. This also avoids SQLite's lack of ORDER BY support
            // for DateTimeOffset while keeping identical behavior on PostgreSQL.
            "createdAt" => ascending
                ? users.OrderBy(user => user.Id)
                : users.OrderByDescending(user => user.Id),
            _ => ascending
                ? users.OrderBy(user => user.Id)
                : users.OrderByDescending(user => user.Id),
        };
    }

    private static AdminUserResponse ToResponse(
        SegarisUser user,
        IEnumerable<string> roles,
        bool hasAvatar) => new(
        user.Id,
        user.UserName!,
        user.DisplayName,
        roles.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
        user.IsActive,
        user.CreatedAt,
        hasAvatar ? IdentityProfilePolicy.AvatarUrl(user.Id) : null);

    internal sealed record CreateUserRequest(string? UserName, string? Password, string? Role);

    internal sealed record UpdateUserRequest(string? DisplayName, string? Role);

    internal sealed record SetPasswordRequest(string? NewPassword);

    internal sealed record AdminUserResponse(
        int Id,
        string UserName,
        string DisplayName,
        IReadOnlyList<string> Roles,
        bool IsActive,
        DateTimeOffset CreatedAt,
        string? AvatarUrl);
}
