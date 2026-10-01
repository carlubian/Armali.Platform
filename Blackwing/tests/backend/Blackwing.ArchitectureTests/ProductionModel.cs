using System.Reflection;
using Blackwing.Persistence;
using Blackwing.Shared.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Blackwing.ArchitectureTests;

/// <summary>
/// Builds the EF Core model exactly as the real host does, so architecture tests assert against
/// the production mapping rather than a reconstruction of it.
/// </summary>
internal static class ProductionModel
{
    /// <summary>
    /// The production model, built once. No database is touched: building a model requires a
    /// provider but never a connection. The identity module is loaded through the very seam the
    /// real host uses, so the model under test includes the module's tables.
    /// </summary>
    private static readonly Lazy<IModel> LazyModel = new(Build, isThreadSafe: true);

    public static IModel Value => LazyModel.Value;

    public static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }

    private static IModel Build()
    {
        var options = new DbContextOptionsBuilder<BlackwingDbContext>()
            .UseNpgsql("Host=localhost;Database=blackwing;Username=blackwing;Password=blackwing")
            .Options;

        using var context = new BlackwingDbContext(options, DiscoverContributors(), new NoCurrentUser());
        return context.Model;
    }

    /// <summary>
    /// Discovers the model contributors the API registers, so the test never has to name them. A
    /// module that starts contributing tables is covered from the moment it exists.
    /// </summary>
    private static IEnumerable<IBlackwingModelContributor> DiscoverContributors() =>
        GetLoadableTypes(typeof(Program).Assembly)
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(typeof(IBlackwingModelContributor).IsAssignableFrom)
            .Select(type => (IBlackwingModelContributor)Activator.CreateInstance(type)!)
            .ToArray();

    private sealed class NoCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated => false;

        public UserId? UserId => null;

        public bool IsInRole(PlatformRole role) => false;
    }
}
