using Microsoft.EntityFrameworkCore;

namespace Segaris.Persistence;

public sealed class SegarisDbContext(
    DbContextOptions<SegarisDbContext> options,
    IEnumerable<ISegarisModelContributor> modelContributors)
    : DbContext(options)
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
        configurationBuilder.Properties<string>().HaveMaxLength(200);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var contributor in modelContributors)
        {
            contributor.Configure(modelBuilder);
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateUtcTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ValidateUtcTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ValidateUtcTimestamps()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            foreach (var property in entry.Properties)
            {
                if (property.Metadata.ClrType == typeof(DateTimeOffset)
                    && property.CurrentValue is DateTimeOffset timestamp
                    && timestamp.Offset != TimeSpan.Zero)
                {
                    throw new InvalidOperationException(
                        $"{entry.Metadata.ClrType.Name}.{property.Metadata.Name} must be UTC.");
                }
            }
        }
    }
}
