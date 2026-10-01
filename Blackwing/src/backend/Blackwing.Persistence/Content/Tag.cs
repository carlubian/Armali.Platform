using Blackwing.Shared.Content;
using Blackwing.Shared.Ownership;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blackwing.Persistence.Content;

/// <summary>
/// A label of one of the three fixed kinds. Tags are first-class entities, private to an account
/// and unique by <c>(owner, kind, normalized value)</c>.
/// </summary>
public sealed class Tag : IOwnedByUser
{
    public int Id { get; set; }

    /// <inheritdoc />
    public int OwnerUserId { get; set; }

    public TagKind Kind { get; set; }

    /// <summary>The value exactly as it was typed, accents and capitalization included.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>The key the tag is unique by. See <see cref="TagNormalizer"/>.</summary>
    public string NormalizedValue { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<ImageTag> ImageTags { get; set; } = [];
}

internal sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("tags");
        builder.HasKey(tag => tag.Id);
        builder.Property(tag => tag.Id).ValueGeneratedOnAdd();
        builder.Property(tag => tag.Kind).HasConversion<int>();
        builder.Property(tag => tag.Value).HasMaxLength(100).IsRequired();
        builder.Property(tag => tag.NormalizedValue).HasMaxLength(100).IsRequired();
        builder.Property(tag => tag.CreatedAt).IsRequired();

        // The uniqueness the milestone asks for and, with the owner and kind leading, also the
        // index the prefix autocomplete of phase 5 will walk.
        builder.HasIndex(tag => new { tag.OwnerUserId, tag.Kind, tag.NormalizedValue })
            .IsUnique()
            .HasDatabaseName("IX_tags_owner_kind_normalized_value");
    }
}
