using Blackwing.Persistence.Identity;
using Blackwing.Shared.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Blackwing.Api.Identity;

public sealed class IdentitySeeder(UserManager<BlackwingUser> users, RoleManager<IdentityRole<Guid>> roles, IOptions<InitialAdminOptions> options)
{
    public async Task SeedAsync()
    {
        foreach (var name in new[] { BlackwingRoles.User, BlackwingRoles.Admin })
            if (!await roles.RoleExistsAsync(name)) await roles.CreateAsync(new IdentityRole<Guid>(name));
        var admin = options.Value;
        if (string.IsNullOrWhiteSpace(admin.Username) || string.IsNullOrWhiteSpace(admin.Password)) return;
        var user = await users.FindByNameAsync(admin.Username);
        if (user is null)
        {
            user = new BlackwingUser { Id = Guid.NewGuid(), UserName = admin.Username };
            var result = await users.CreateAsync(user, admin.Password);
            if (!result.Succeeded) throw new InvalidOperationException($"Initial admin could not be created: {string.Join(", ", result.Errors.Select(error => error.Description))}");
        }
        if (!await users.IsInRoleAsync(user, BlackwingRoles.Admin)) await users.AddToRoleAsync(user, BlackwingRoles.Admin);
    }
}

public sealed class InitialAdminOptions
{
    public const string SectionName = "InitialAdmin";
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
