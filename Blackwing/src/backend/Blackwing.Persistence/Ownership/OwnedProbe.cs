using Blackwing.Shared.Ownership;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blackwing.Persistence.Ownership;

/// <summary>
/// A canary for the privacy perimeter: the smallest possible real owned entity, existing so the
/// ownership guarantee can be demonstrated end to end with two accounts before any product
/// entity exists.
/// </summary>
/// <remarks>
/// This is production code, not a test artifact: it has a migration and its own endpoints under
/// <c>/api/platform/ownership</c>. Phase 3 decides whether it is retired once <c>Image</c>
/// arrives, or kept permanently as a canary that fails loudly if the perimeter ever regresses.
/// </remarks>
public sealed class OwnedProbe : IOwnedByUser
{
    public int Id { get; set; }

    /// <inheritdoc />
    public int OwnerUserId { get; set; }

    public string Label { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}

internal sealed class OwnedProbeConfiguration : IEntityTypeConfiguration<OwnedProbe>
{
    public void Configure(EntityTypeBuilder<OwnedProbe> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ownership_probes");
        builder.HasKey(probe => probe.Id);
        builder.Property(probe => probe.Id).ValueGeneratedOnAdd();
        builder.Property(probe => probe.Label).HasMaxLength(200).IsRequired();
        builder.Property(probe => probe.CreatedAt).IsRequired();

        // Every read is already narrowed to the current owner by the global filter, so the
        // owner column is the leading predicate of every query that reaches this table.
        builder.HasIndex(probe => probe.OwnerUserId);
    }
}
