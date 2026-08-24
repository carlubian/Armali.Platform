namespace Blackwing.Shared.Identity;

/// <summary>
/// The complete role set. Roles are seeded from this enum at startup, so adding a
/// member here is the only step needed to make a new role exist in the database.
/// </summary>
public enum PlatformRole
{
    User,
    Admin,
}
