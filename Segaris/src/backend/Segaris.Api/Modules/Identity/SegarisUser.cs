using Microsoft.AspNetCore.Identity;

namespace Segaris.Api.Modules.Identity;

internal sealed class SegarisUser : IdentityUser<int>
{
    public string DisplayName { get; set; } = string.Empty;

    public string Language { get; set; } = IdentityProfilePolicy.DefaultLanguage;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
}
