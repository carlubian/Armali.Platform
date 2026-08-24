using Blackwing.Api.Modules.Identity.Configuration;
using Blackwing.Shared.Identity;
using Blackwing.Shared.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Blackwing.Api.Modules.Identity.Seeding;

/// <summary>
/// Deterministic, idempotent identity seed: the <see cref="PlatformRole"/> set and, when
/// configured, the first administrator. Runs at startup in every environment so a freshly
/// migrated database is operable, and a second start adds nothing.
/// </summary>
internal sealed class IdentitySeeder(
    RoleManager<BlackwingRole> roleManager,
    UserManager<BlackwingUser> userManager,
    IOptions<IdentityBootstrapOptions> bootstrapOptions,
    IClock clock,
    ILogger<IdentitySeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await EnsureRolesAsync();
        await EnsureBootstrapAdministratorAsync(cancellationToken);
    }

    private async Task EnsureRolesAsync()
    {
        foreach (var role in Enum.GetValues<PlatformRole>())
        {
            var name = role.ToString();
            if (await roleManager.RoleExistsAsync(name))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new BlackwingRole { Name = name });
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create the '{name}' role: {Describe(result)}");
            }

            logger.LogInformation("Created platform role {Role}.", name);
        }
    }

    private async Task EnsureBootstrapAdministratorAsync(CancellationToken cancellationToken)
    {
        var options = bootstrapOptions.Value;
        if (!options.IsConfigured)
        {
            return;
        }

        // The options validator already rejects a half-configured section at host start; this
        // guard keeps the seeder honest if it is ever driven from somewhere else.
        if (string.IsNullOrWhiteSpace(options.UserName) || string.IsNullOrWhiteSpace(options.Password))
        {
            throw new InvalidOperationException(
                $"{IdentityBootstrapOptions.SectionName} requires both UserName and Password when configured.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var existing = await userManager.FindByNameAsync(options.UserName);
        if (existing is not null)
        {
            return;
        }

        var administrator = new BlackwingUser
        {
            UserName = options.UserName,
            DisplayName = options.UserName,
            IsActive = true,
            CreatedAt = clock.UtcNow,
        };

        var created = await userManager.CreateAsync(administrator, options.Password);
        if (!created.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create the bootstrap administrator: {Describe(created)}");
        }

        var assigned = await userManager.AddToRoleAsync(administrator, PlatformRole.Admin.ToString());
        if (!assigned.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to assign the administrator role: {Describe(assigned)}");
        }

        logger.LogInformation(
            "Created bootstrap administrator {UserName}.",
            administrator.UserName);
    }

    private static string Describe(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(error => error.Description));
}
