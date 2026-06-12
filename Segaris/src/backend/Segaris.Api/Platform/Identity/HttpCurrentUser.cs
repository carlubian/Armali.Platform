using System.Globalization;
using System.Security.Claims;
using Segaris.Shared.Identity;

namespace Segaris.Api.Platform.Identity;

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
