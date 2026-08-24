using System.Reflection;
using Blackwing.Persistence.Ownership;
using Blackwing.Shared.Identity;
using Blackwing.Shared.Ownership;
using Microsoft.EntityFrameworkCore;

namespace Blackwing.Persistence;

/// <summary>
/// The single application database context, and the place where the privacy perimeter of
/// Blackwing is enforced rather than merely intended.
/// </summary>
/// <remarks>
/// It does three things, each of them load-bearing: it assembles its model from the modules that
/// contribute to it, it scopes every owned entity to the current account through a global query
/// filter, and it stamps the owner on insert so forgetting to do so is impossible.
/// </remarks>
public sealed class BlackwingDbContext(
    DbContextOptions<BlackwingDbContext> options,
    IEnumerable<IBlackwingModelContributor> modelContributors,
    ICurrentUser currentUser)
    : DbContext(options)
{
    private static readonly MethodInfo ApplyOwnershipFilterMethod =
        typeof(BlackwingDbContext).GetMethod(
            nameof(ApplyOwnershipFilter),
            BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("The ownership filter helper could not be located.");

    /// <summary>Ownership perimeter canary. See <see cref="OwnedProbe"/>.</summary>
    public DbSet<OwnedProbe> OwnershipProbes => Set<OwnedProbe>();

    /// <summary>
    /// The account every owned query is narrowed to. Zero is never a valid identifier
    /// (<see cref="UserId"/> rejects zero and negatives), so a context without an authenticated
    /// user sees absolutely nothing instead of seeing everything.
    /// </summary>
    private int CurrentOwnerId => currentUser.UserId?.Value ?? 0;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // 1. Model contributors. This project maps nothing of its own beyond the ownership
        // canary; each module maps its own entities from its own folder. That is what keeps
        // Blackwing.Persistence free of any dependency on ASP.NET Core Identity, and what makes
        // swapping the local identity module for an Armali SSO a change confined to one module.
        modelBuilder.ApplyConfiguration(new OwnedProbeConfiguration());

        foreach (var contributor in modelContributors)
        {
            contributor.Configure(modelBuilder);
        }

        // 2. Global ownership filter, applied once the model is complete so it also covers
        // whatever a module contributed. The safe path is the one that comes out by default:
        // bypassing it requires an explicit IgnoreQueryFilters at the call site, which is
        // visible in review. The identity tables carry no filter on purpose; they are global.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToArray())
        {
            if (!typeof(IOwnedByUser).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            ApplyOwnershipFilterMethod
                .MakeGenericMethod(entityType.ClrType)
                .Invoke(this, [modelBuilder]);
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampOwnership();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        StampOwnership();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Registers the query filter for one owned entity type. The comparison reads
    /// <see cref="CurrentOwnerId"/> off this context instance, and EF Core turns an instance
    /// member of the context into a query parameter: the value is evaluated once per query while
    /// the model itself is still built and cached exactly once.
    /// </summary>
    private void ApplyOwnershipFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, IOwnedByUser
    {
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(entity => entity.OwnerUserId == CurrentOwnerId);
    }

    /// <summary>
    /// 3. Owner stamping on insert. Every new owned row takes its owner from the request
    /// identity, and an insert attempted without one fails hard. Assigning the owner is
    /// therefore not something an endpoint can forget, and no request body can ever choose it.
    /// </summary>
    private void StampOwnership()
    {
        foreach (var entry in ChangeTracker.Entries<IOwnedByUser>())
        {
            if (entry.State != EntityState.Added)
            {
                continue;
            }

            var ownerId = currentUser.UserId
                ?? throw new InvalidOperationException(
                    $"{entry.Metadata.ClrType.Name} is owned content and cannot be created "
                    + "without an authenticated user.");

            entry.Entity.OwnerUserId = ownerId.Value;
        }
    }
}
