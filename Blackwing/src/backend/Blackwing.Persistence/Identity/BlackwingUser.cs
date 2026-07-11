using Microsoft.AspNetCore.Identity;

namespace Blackwing.Persistence.Identity;

/// <summary>Local Blackwing identity, intentionally isolated from other Armali products.</summary>
public sealed class BlackwingUser : IdentityUser<Guid>;
