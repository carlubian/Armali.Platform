using System.Globalization;
using System.Security.Claims;
using Blackwing.Shared.Identity;

namespace Blackwing.Api.Platform.Identity;

/// <summary>
/// Reads the current identity from the cookie-authenticated principal. A missing or unparsable
/// identifier yields <c>null</c> rather than a fabricated value, which is what makes an
/// unauthenticated context see nothing at all through the ownership filter.
/// </summary>
internal sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal Principal =>
        httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();

    public bool IsAuthenticated => Principal.Identity?.IsAuthenticated == true;

    public UserId? UserId
    {
        get
        {
            var value = Principal.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var userId)
                && userId > 0
                ? new UserId(userId)
                : null;
        }
    }

    public bool IsInRole(PlatformRole role) => Principal.IsInRole(role.ToString());
}
