using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.Modules.Firebird.Domain;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.IntegrationTests.Firebird;

internal static class FirebirdTestData
{
    private static readonly DateTimeOffset SeedNow = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

    public static async Task<int> CategoryIdAsync(IServiceProvider services, string name)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<PersonCategory>()
            .Where(category => category.Name == name)
            .Select(category => category.Id)
            .SingleAsync();
    }

    public static async Task<int> PlatformIdAsync(IServiceProvider services, string name)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<UsernamePlatform>()
            .Where(platform => platform.Name == name)
            .Select(platform => platform.Id)
            .SingleAsync();
    }

    public static async Task<bool> PersonExistsAsync(IServiceProvider services, int personId)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<Person>().AnyAsync(person => person.Id == personId);
    }

    public static async Task<bool> UsernameExistsAsync(IServiceProvider services, int usernameId)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<Username>().AnyAsync(username => username.Id == usernameId);
    }

    public static async Task<bool> InteractionExistsAsync(IServiceProvider services, int interactionId)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<Interaction>().AnyAsync(interaction => interaction.Id == interactionId);
    }

    public static async Task<int> SeedPersonAsync(
        IServiceProvider services,
        int creatorId,
        string name = "Person",
        string categoryName = "Other",
        PersonStatus status = PersonStatus.Active,
        int? birthdayMonth = null,
        int? birthdayDay = null,
        string? notes = null,
        RecordVisibility visibility = RecordVisibility.Public)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();

        var categoryId = await database.Set<PersonCategory>()
            .Where(category => category.Name == categoryName)
            .Select(category => category.Id)
            .SingleAsync();

        var person = Person.Create(
            new PersonValues(
                name,
                categoryId,
                status,
                birthdayMonth,
                birthdayDay,
                notes,
                visibility),
            new UserId(creatorId),
            SeedNow);

        database.Add(person);
        await database.SaveChangesAsync();
        return person.Id;
    }
}
