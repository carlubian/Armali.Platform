namespace Segaris.Shared.Identity;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    UserId? UserId { get; }

    bool IsInRole(PlatformRole role);
}
