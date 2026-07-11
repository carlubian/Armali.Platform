using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Blackwing.Persistence.Identity;
using Blackwing.Shared.Identity;
using Blackwing.Shared.Ownership;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace Blackwing.Api.Identity;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/api/auth");
        auth.MapGet("/antiforgery", (IAntiforgery antiforgery, HttpContext context) => Results.Ok(new AntiforgeryResponse(antiforgery.GetAndStoreTokens(context).RequestToken!))).AllowAnonymous();
        auth.MapPost("/login", async (LoginRequest request, HttpContext context, UserManager<BlackwingUser> users) => await Login(request, context, users)).AllowAnonymous().RequireRateLimiting("login");
        auth.MapPost("/logout", async (HttpContext context) => await Logout(context)).RequireAuthorization();
        auth.MapGet("/me", Me).RequireAuthorization();
        var admin = endpoints.MapGroup("/api/admin/accounts").RequireAuthorization(policy => policy.RequireRole(BlackwingRoles.Admin));
        admin.MapGet("/", ListAccounts);
        admin.MapPost("/", CreateAccount);
        admin.MapPut("/{id:guid}", async (Guid id, UpdateAccountRequest request, UserManager<BlackwingUser> users) => await UpdateAccount(id, request, users));
        admin.MapDelete("/{id:guid}", async (Guid id, UserManager<BlackwingUser> users, IUserScope scope) => await DeleteAccount(id, users, scope));
        admin.MapPost("/{id:guid}/password", async (Guid id, ResetPasswordRequest request, UserManager<BlackwingUser> users) => await ResetPassword(id, request, users));
        return endpoints;
    }

    private static async Task<IResult> Login(LoginRequest request, HttpContext context, UserManager<BlackwingUser> users)
    {
        var user = await users.FindByNameAsync(request.Username);
        if (user is null) return Results.Unauthorized();
        if (!await users.CheckPasswordAsync(user, request.Password)) { await users.AccessFailedAsync(user); return Results.Unauthorized(); }
        await users.ResetAccessFailedCountAsync(user);
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.Id.ToString()), new(ClaimTypes.Name, user.UserName!) };
        claims.AddRange((await users.GetRolesAsync(user)).Select(role => new Claim(ClaimTypes.Role, role)));
        await context.SignInAsync(IdentityConstants.ApplicationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme)));
        return Results.NoContent();
    }
    private static async Task<IResult> Logout(HttpContext context) { await context.SignOutAsync(IdentityConstants.ApplicationScheme); return Results.NoContent(); }
    private static async Task<IResult> Me(ClaimsPrincipal principal, UserManager<BlackwingUser> users)
    {
        var user = await users.GetUserAsync(principal);
        return user is null ? Results.Unauthorized() : Results.Ok(new CurrentUserResponse(user.Id, user.UserName!, (await users.GetRolesAsync(user)).ToArray()));
    }
    private static async Task<IResult> ListAccounts(UserManager<BlackwingUser> users)
    {
        var all = await users.Users.OrderBy(user => user.UserName).ToListAsync();
        var accounts = new List<AccountResponse>(all.Count);
        foreach (var user in all) accounts.Add(new AccountResponse(user.Id, user.UserName!, await ResolveRoleAsync(users, user)));
        return Results.Ok(new AccountListResponse(accounts));
    }
    private static async Task<IResult> CreateAccount(CreateAccountRequest request, UserManager<BlackwingUser> users)
    {
        var user = new BlackwingUser { Id = Guid.NewGuid(), UserName = request.Username };
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded) return Results.ValidationProblem(result.Errors.ToDictionary(error => error.Code, error => new[] { error.Description }));
        await users.AddToRoleAsync(user, BlackwingRoles.User);
        return Results.Created($"/api/admin/accounts/{user.Id}", new AccountResponse(user.Id, user.UserName!, BlackwingRoles.User));
    }
    private static async Task<IResult> UpdateAccount(Guid id, UpdateAccountRequest request, UserManager<BlackwingUser> users)
    {
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null) return Results.NotFound();
        // Only the username is editable; the role stays as assigned. SetUserNameAsync
        // validates uniqueness and rolls the security stamp, ending the renamed user's sessions.
        var result = await users.SetUserNameAsync(user, request.Username);
        return result.Succeeded
            ? Results.Ok(new AccountResponse(user.Id, user.UserName!, await ResolveRoleAsync(users, user)))
            : Results.ValidationProblem(result.Errors.ToDictionary(error => error.Code, error => new[] { error.Description }));
    }
    private static async Task<IResult> DeleteAccount(Guid id, UserManager<BlackwingUser> users, IUserScope scope)
    {
        // Guard against an admin locking themselves — or every admin — out of account management.
        if (id == scope.UserId) return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "You cannot delete your own account.");
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null) return Results.NotFound();
        if (await users.IsInRoleAsync(user, BlackwingRoles.Admin) && (await users.GetUsersInRoleAsync(BlackwingRoles.Admin)).Count <= 1)
            return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "The last administrator cannot be deleted.");
        // The user row is deleted here; the database cascades the owner's image, tag and
        // upload-job rows. Stored image bytes on disk are intentionally left for a later
        // storage sweep rather than purged inline.
        var result = await users.DeleteAsync(user);
        return result.Succeeded
            ? Results.NoContent()
            : Results.ValidationProblem(result.Errors.ToDictionary(error => error.Code, error => new[] { error.Description }));
    }
    private static async Task<string> ResolveRoleAsync(UserManager<BlackwingUser> users, BlackwingUser user) =>
        await users.IsInRoleAsync(user, BlackwingRoles.Admin) ? BlackwingRoles.Admin : BlackwingRoles.User;
    private static async Task<IResult> ResetPassword(Guid id, ResetPasswordRequest request, UserManager<BlackwingUser> users)
    {
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null) return Results.NotFound();
        var removeResult = await users.RemovePasswordAsync(user);
        var result = removeResult.Succeeded ? await users.AddPasswordAsync(user, request.Password) : removeResult;
        return result.Succeeded ? Results.NoContent() : Results.ValidationProblem(result.Errors.ToDictionary(error => error.Code, error => new[] { error.Description }));
    }
}

public sealed record AntiforgeryResponse(string RequestToken);
public sealed record LoginRequest([Required, StringLength(128)] string Username, [Required, StringLength(256, MinimumLength = 12)] string Password);
public sealed record CreateAccountRequest([Required, StringLength(128)] string Username, [Required, StringLength(256, MinimumLength = 12)] string Password);
public sealed record UpdateAccountRequest([Required, StringLength(128)] string Username);
public sealed record ResetPasswordRequest([Required, StringLength(256, MinimumLength = 12)] string Password);
public sealed record CurrentUserResponse(Guid Id, string Username, string[] Roles);
public sealed record AccountResponse(Guid Id, string Username, string Role);
public sealed record AccountListResponse(IReadOnlyList<AccountResponse> Accounts);
