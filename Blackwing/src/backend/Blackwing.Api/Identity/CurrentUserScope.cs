using System.Security.Claims;
using Blackwing.Shared.Ownership;

namespace Blackwing.Api.Identity;

public sealed class CurrentUserScope(IHttpContextAccessor httpContextAccessor) : IUserScope
{
    public Guid UserId => Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
        ? userId
        : throw new UnauthorizedAccessException("An authenticated user scope is required.");
}
