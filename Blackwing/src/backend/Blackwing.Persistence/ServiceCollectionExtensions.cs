using Blackwing.Persistence.Gallery;
using Blackwing.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Blackwing.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlackwingPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Blackwing");
        if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("Connection string 'Blackwing' is required.");
        services.AddDbContext<BlackwingDbContext>(options => options.UseNpgsql(connectionString));
        services.AddIdentityCore<BlackwingUser>(options =>
        {
            options.User.RequireUniqueEmail = false;
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Lockout.MaxFailedAccessAttempts = 5;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<BlackwingDbContext>();
        return services;
    }
}

public sealed class BlackwingDbContext(DbContextOptions<BlackwingDbContext> options)
    : IdentityDbContext<BlackwingUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Image> Images => Set<Image>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ImageTag> ImageTags => Set<ImageTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Image>(builder =>
        {
            builder.ToTable("images");
            builder.HasKey(image => image.Id);
            builder.Property(image => image.Sha256).HasMaxLength(64).IsFixedLength().IsRequired();
            builder.Property(image => image.ContentType).HasMaxLength(100).IsRequired();
            builder.Property(image => image.Width).IsRequired();
            builder.Property(image => image.Height).IsRequired();
            builder.Property(image => image.Bytes).IsRequired();
            builder.Property(image => image.UploadedAt).IsRequired();
            // Per-user deduplication: the same bytes re-uploaded map to one row.
            builder.HasIndex(image => new { image.OwnerUserId, image.Sha256 }).IsUnique();
            // Default gallery ordering (capture date, upload-time fallback) within an owner.
            builder.HasIndex(image => new { image.OwnerUserId, image.CapturedAt, image.Id });
            builder.HasIndex(image => new { image.OwnerUserId, image.UploadedAt, image.Id });
            // Pending-review view.
            builder.HasIndex(image => new { image.OwnerUserId, image.ReviewedAt });
            builder.HasOne<BlackwingUser>().WithMany().HasForeignKey(image => image.OwnerUserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Tag>(builder =>
        {
            builder.ToTable("tags");
            builder.HasKey(tag => tag.Id);
            builder.Property(tag => tag.Type).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(tag => tag.Value).HasMaxLength(Tag.ValueMaxLength).IsRequired();
            builder.Property(tag => tag.NormalizedValue).HasMaxLength(Tag.ValueMaxLength).IsRequired();
            // One reusable tag per label; the leftmost columns also back owner+type prefix autocomplete.
            builder.HasIndex(tag => new { tag.OwnerUserId, tag.Type, tag.NormalizedValue }).IsUnique();
            builder.ToTable(table => table.HasCheckConstraint(
                "CK_tags_type",
                $"\"{nameof(Tag.Type)}\" IN ({string.Join(", ", Enum.GetNames<TagType>().Select(value => $"'{value}'"))})"));
            builder.HasOne<BlackwingUser>().WithMany().HasForeignKey(tag => tag.OwnerUserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ImageTag>(builder =>
        {
            builder.ToTable("image_tags");
            builder.HasKey(link => new { link.ImageId, link.TagId });
            // Reverse lookup: images-of-a-tag.
            builder.HasIndex(link => new { link.TagId, link.ImageId });
            builder.HasOne<Image>().WithMany().HasForeignKey(link => link.ImageId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<Tag>().WithMany().HasForeignKey(link => link.TagId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
