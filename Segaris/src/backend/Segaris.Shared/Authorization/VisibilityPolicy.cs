using Segaris.Shared.Identity;

namespace Segaris.Shared.Authorization;

public static class VisibilityPolicy
{
    public static bool CanAccess(
        RecordVisibility visibility,
        UserId creatorId,
        ICurrentUser currentUser)
    {
        ArgumentNullException.ThrowIfNull(currentUser);

        if (!currentUser.IsAuthenticated || currentUser.UserId is not { } currentUserId)
        {
            return false;
        }

        return visibility == RecordVisibility.Public || currentUserId == creatorId;
    }
}
