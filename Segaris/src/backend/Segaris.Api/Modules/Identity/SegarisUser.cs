using Microsoft.AspNetCore.Identity;

namespace Segaris.Api.Modules.Identity;

internal sealed class SegarisUser : IdentityUser<int>
{
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
}
