using Blackwing.Persistence;
using Blackwing.Shared.Ownership;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Blackwing.ArchitectureTests;

/// <summary>
/// The privacy perimeter as a structural rule rather than as a habit. Adding an owned entity and
/// forgetting to scope it must fail here, because by the time such a mistake shows up as a bug it
/// has already leaked somebody's content.
/// </summary>
public sealed class OwnershipTests
{
    /// <summary>The production model. See <see cref="ProductionModel"/>.</summary>
    private static readonly Lazy<IModel> Model = new(() => ProductionModel.Value, isThreadSafe: true);

    [Fact]
    public void Every_owned_entity_type_in_the_model_has_a_query_filter()
    {
        var unscoped = OwnedEntityTypes()
            .Where(entityType => !entityType.GetDeclaredQueryFilters().Any())
            .Select(entityType => entityType.ClrType.FullName)
            .ToArray();

        Assert.NotEmpty(OwnedEntityTypes());
        Assert.Empty(unscoped);
    }

    /// <summary>
    /// The filter is only worth something on entities that reach the model. An owned type that is
    /// declared but never mapped would sail past the test above without ever being scoped.
    /// </summary>
    [Fact]
    public void Every_owned_type_in_the_solution_is_mapped_by_the_model()
    {
        var mapped = Model.Value.GetEntityTypes()
            .Select(entityType => entityType.ClrType)
            .ToHashSet();

        var declared = new[] { PersistenceAssembly.Assembly, typeof(Program).Assembly }
            .SelectMany(ProductionModel.GetLoadableTypes)
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(typeof(IOwnedByUser).IsAssignableFrom)
            .ToArray();

        Assert.NotEmpty(declared);
        Assert.Empty(declared
            .Where(type => !mapped.Contains(type))
            .Select(type => type.FullName));
    }

    /// <summary>
    /// The converse rule. Accounts and roles are global by nature — an account is not owned by an
    /// account — and scoping them would leave the login unable to find the user it authenticates.
    /// A filter on anything that is not <see cref="IOwnedByUser"/> is therefore a mistake.
    /// </summary>
    [Fact]
    public void Nothing_that_is_not_owned_carries_a_query_filter()
    {
        var tables = Model.Value.GetEntityTypes()
            .Select(entityType => entityType.GetTableName())
            .ToArray();

        var filtered = Model.Value.GetEntityTypes()
            .Where(entityType => !typeof(IOwnedByUser).IsAssignableFrom(entityType.ClrType))
            .Where(entityType => entityType.GetDeclaredQueryFilters().Any())
            .Select(entityType => entityType.ClrType.FullName)
            .ToArray();

        // Also proves the module seam was exercised: without the identity contributor the model
        // would hold nothing but the content entities and this test would pass vacuously.
        Assert.Contains("identity_users", tables);
        Assert.Contains("identity_roles", tables);
        Assert.Empty(filtered);
    }

    /// <summary>
    /// The persistence project must stay free of ASP.NET Core Identity: that is what makes
    /// replacing the local identity module with an Armali SSO a change confined to one module.
    /// </summary>
    [Fact]
    public void Persistence_does_not_depend_on_aspnet_core_identity()
    {
        var identityReferences = PersistenceAssembly.Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null
                && (name.StartsWith("Microsoft.AspNetCore.Identity", StringComparison.Ordinal)
                    || name.StartsWith("Microsoft.Extensions.Identity", StringComparison.Ordinal)))
            .ToArray();

        Assert.Empty(identityReferences);
    }

    private static IEntityType[] OwnedEntityTypes() =>
        Model.Value.GetEntityTypes()
            .Where(entityType => typeof(IOwnedByUser).IsAssignableFrom(entityType.ClrType))
            .ToArray();

}
