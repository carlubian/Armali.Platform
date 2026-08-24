using Microsoft.EntityFrameworkCore;

namespace Blackwing.Persistence;

/// <summary>
/// Lets a module contribute its own mapping to the single <see cref="BlackwingDbContext"/>
/// without the persistence project having to reference the module.
/// </summary>
/// <remarks>
/// This exists for exactly one reason: <c>Blackwing.Persistence</c> must not depend on ASP.NET
/// Core Identity. The identity tables are mapped by a contributor that lives in
/// <c>Blackwing.Api</c>, so replacing the local identity module with an Armali SSO one day is a
/// change confined to that module and its contributor. An architecture test pins the rule.
/// </remarks>
public interface IBlackwingModelContributor
{
    void Configure(ModelBuilder modelBuilder);
}
