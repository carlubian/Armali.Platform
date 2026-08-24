using Microsoft.AspNetCore.Identity;

namespace Blackwing.Api.Modules.Identity;

/// <summary>
/// A platform role. It adds nothing to the framework type: the role set is closed and lives in
/// <see cref="Blackwing.Shared.Identity.PlatformRole"/>, from which the rows are seeded.
/// </summary>
internal sealed class BlackwingRole : IdentityRole<int>
{
}
