using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Blackwing.Persistence.Identity;
using Blackwing.Shared.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;

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
        admin.MapPost("/", CreateAccount);
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
    private static async Task<IResult> CreateAccount(CreateAccountRequest request, UserManager<BlackwingUser> users)
    {
        var user = new BlackwingUser { Id = Guid.NewGuid(), UserName = request.Username };
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded) return Results.ValidationProblem(result.Errors.ToDictionary(error => error.Code, error => new[] { error.Description }));
        await users.AddToRoleAsync(user, BlackwingRoles.User);
        return Results.Created($"/api/admin/accounts/{user.Id}", new AccountResponse(user.Id, user.UserName!, BlackwingRoles.User));
    }
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
public sealed record ResetPasswordRequest([Required, StringLength(256, MinimumLength = 12)] string Password);
public sealed record CurrentUserResponse(Guid Id, string Username, string[] Roles);
public sealed record AccountResponse(Guid Id, string Username, string Role);
