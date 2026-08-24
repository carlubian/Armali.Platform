using Microsoft.AspNetCore.Identity;

namespace Blackwing.Api.Modules.Identity;

/// <summary>
/// A Blackwing account. The profile is deliberately minimal: a display name and nothing else.
/// There is no language preference (the interface is Spanish only), no avatar, and no mandatory
/// email address.
/// </summary>
internal sealed class BlackwingUser : IdentityUser<int>
{
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// A deactivated account is refused at sign-in exactly as a non-existent one is, and its
    /// live sessions die on their next request through the security stamp.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
}
