namespace Blackwing.Shared.Identity;

/// <summary>
/// The ambient identity of the request being served. Implemented over the HTTP
/// context in <c>Blackwing.Api</c>; consumed by the persistence layer to scope every
/// owned entity without either layer knowing about the other.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    UserId? UserId { get; }

    bool IsInRole(PlatformRole role);
}
