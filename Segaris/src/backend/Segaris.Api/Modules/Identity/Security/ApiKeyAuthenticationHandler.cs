using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Segaris.Api.Modules.Identity.ApiKeys;

namespace Segaris.Api.Modules.Identity.Security;

/// <summary>
/// Authenticates a request from an <c>Authorization: Bearer segaris_...</c> token.
/// </summary>
/// <remarks>
/// The produced principal is built by the same claims factory the cookie sign-in
/// uses, so <c>ICurrentUser</c>, <c>VisibilityPolicy</c>, and every module's
/// authorization policy apply to a key exactly as they apply to a session. A key
/// therefore never grants a permission its user lacks.
/// </remarks>
internal sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    ApiKeyService apiKeys,
    UserManager<SegarisUser> userManager,
    IUserClaimsPrincipalFactory<SegarisUser> claimsPrincipalFactory)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers[HeaderNames.Authorization].ToString();
        if (string.IsNullOrEmpty(header))
        {
            return AuthenticateResult.NoResult();
        }

        if (!header.StartsWith(ApiKeyAuthenticationDefaults.TokenPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Rejected();
        }

        var token = header[ApiKeyAuthenticationDefaults.TokenPrefix.Length..].Trim();
        var key = await apiKeys.FindVerifiedAsync(token, Context.RequestAborted);
        if (key is null)
        {
            return Rejected();
        }

        var user = await userManager.FindByIdAsync(
            key.UserId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (user is null || !user.IsActive)
        {
            return Rejected();
        }

        // Deactivation, password changes, and administrative security changes rotate
        // the stamp. Comparing it here puts keys behind the same invalidation
        // mechanism that SecurityStampValidator applies to cookie sessions.
        if (!string.Equals(key.SecurityStamp, user.SecurityStamp, StringComparison.Ordinal))
        {
            return Rejected();
        }

        await apiKeys.TouchAsync(key, Context.RequestAborted);

        var principal = await claimsPrincipalFactory.CreateAsync(user);
        var source = principal.Identities.First();
        var identity = new ClaimsIdentity(
            source.Claims,
            ApiKeyAuthenticationDefaults.Scheme,
            source.NameClaimType,
            source.RoleClaimType);

        return AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }

    /// <summary>
    /// Expired, revoked, unknown, and malformed tokens are indistinguishable to the
    /// caller, consistent with the generic login failure.
    /// </summary>
    private static AuthenticateResult Rejected() =>
        AuthenticateResult.Fail("Authentication failed.");
}
