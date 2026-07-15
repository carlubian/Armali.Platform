using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Identity.ApiKeys;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Identity.Persistence;

/// <summary>
/// Maps the ASP.NET Core Identity model into the single <see cref="SegarisDbContext"/>
/// without deriving the context from <c>IdentityDbContext</c>, so the persistence
/// project remains provider-neutral and free of Identity dependencies.
/// </summary>
internal sealed class IdentityModelContributor : ISegarisModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<SegarisUser>(user =>
        {
            user.ToTable("identity_users");
            user.HasKey(entity => entity.Id);
            user.HasIndex(entity => entity.NormalizedUserName)
                .HasDatabaseName("UserNameIndex")
                .IsUnique();
            user.HasIndex(entity => entity.NormalizedEmail).HasDatabaseName("EmailIndex");

            user.Property(entity => entity.ConcurrencyStamp).IsConcurrencyToken();
            user.Property(entity => entity.UserName).HasMaxLength(256);
            user.Property(entity => entity.NormalizedUserName).HasMaxLength(256);
            user.Property(entity => entity.Email).HasMaxLength(256);
            user.Property(entity => entity.NormalizedEmail).HasMaxLength(256);
            user.Property(entity => entity.DisplayName).HasMaxLength(200).IsRequired();
            user.Property(entity => entity.Language).HasMaxLength(10).IsRequired();
            user.Property(entity => entity.CreatedAt).IsRequired();

            user.HasMany<IdentityUserClaim<int>>()
                .WithOne()
                .HasForeignKey(claim => claim.UserId)
                .IsRequired();
            user.HasMany<IdentityUserLogin<int>>()
                .WithOne()
                .HasForeignKey(login => login.UserId)
                .IsRequired();
            user.HasMany<IdentityUserToken<int>>()
                .WithOne()
                .HasForeignKey(token => token.UserId)
                .IsRequired();
            user.HasMany<IdentityUserRole<int>>()
                .WithOne()
                .HasForeignKey(userRole => userRole.UserId)
                .IsRequired();
        });

        modelBuilder.Entity<SegarisRole>(role =>
        {
            role.ToTable("identity_roles");
            role.HasKey(entity => entity.Id);
            role.HasIndex(entity => entity.NormalizedName)
                .HasDatabaseName("RoleNameIndex")
                .IsUnique();

            role.Property(entity => entity.ConcurrencyStamp).IsConcurrencyToken();
            role.Property(entity => entity.Name).HasMaxLength(256);
            role.Property(entity => entity.NormalizedName).HasMaxLength(256);

            role.HasMany<IdentityUserRole<int>>()
                .WithOne()
                .HasForeignKey(userRole => userRole.RoleId)
                .IsRequired();
            role.HasMany<IdentityRoleClaim<int>>()
                .WithOne()
                .HasForeignKey(claim => claim.RoleId)
                .IsRequired();
        });

        modelBuilder.Entity<IdentityUserClaim<int>>(claim =>
        {
            claim.ToTable("identity_user_claims");
            claim.HasKey(entity => entity.Id);
        });

        modelBuilder.Entity<IdentityUserRole<int>>(userRole =>
        {
            userRole.ToTable("identity_user_roles");
            userRole.HasKey(entity => new { entity.UserId, entity.RoleId });
        });

        modelBuilder.Entity<IdentityUserLogin<int>>(login =>
        {
            login.ToTable("identity_user_logins");
            login.HasKey(entity => new { entity.LoginProvider, entity.ProviderKey });
            login.Property(entity => entity.LoginProvider).HasMaxLength(128);
            login.Property(entity => entity.ProviderKey).HasMaxLength(128);
        });

        modelBuilder.Entity<IdentityUserToken<int>>(token =>
        {
            token.ToTable("identity_user_tokens");
            token.HasKey(entity => new { entity.UserId, entity.LoginProvider, entity.Name });
            token.Property(entity => entity.LoginProvider).HasMaxLength(128);
            token.Property(entity => entity.Name).HasMaxLength(128);
        });

        modelBuilder.Entity<IdentityRoleClaim<int>>(claim =>
        {
            claim.ToTable("identity_role_claims");
            claim.HasKey(entity => entity.Id);
        });

        modelBuilder.Entity<SegarisApiKey>(apiKey =>
        {
            apiKey.ToTable("identity_api_keys");
            apiKey.HasKey(entity => entity.Id);
            apiKey.Property(entity => entity.Id).ValueGeneratedOnAdd();
            apiKey.Property(entity => entity.Name)
                .HasMaxLength(ApiKeyPolicy.MaximumNameLength).IsRequired();
            apiKey.Property(entity => entity.KeyId)
                .HasMaxLength(ApiKeyToken.KeyIdLength).IsRequired();
            apiKey.Property(entity => entity.SecretHash).HasMaxLength(100).IsRequired();
            apiKey.Property(entity => entity.SecurityStamp).HasMaxLength(256).IsRequired();
            apiKey.Property(entity => entity.CreatedAt).IsRequired();

            apiKey.HasOne<SegarisUser>()
                .WithMany()
                .HasForeignKey(entity => entity.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // The lookup index for authentication: every request resolves a key by
            // its public identifier before the secret is verified.
            apiKey.HasIndex(entity => entity.KeyId).IsUnique();
            apiKey.HasIndex(entity => entity.UserId);
        });
    }
}
