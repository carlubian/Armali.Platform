using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Firebird.Contracts;
using Segaris.Api.Modules.Firebird.Domain;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Firebird.Mutations;

internal sealed class FirebirdPersonWriteService(SegarisDbContext database, IClock clock)
{
    public async Task<int> CreateAsync(
        CreatePersonRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var values = Map(
            request.Name,
            request.CategoryId,
            request.Status,
            request.BirthdayMonth,
            request.BirthdayDay,
            request.Notes,
            request.Visibility);
        await ValidateReferencesAsync(values, cancellationToken);

        var person = Person.Create(values, actorId, clock.UtcNow);
        database.Add(person);
        await database.SaveChangesAsync(cancellationToken);
        return person.Id;
    }

    public async Task<bool> UpdateAsync(
        int personId,
        UpdatePersonRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var person = await database.Set<Person>()
            .Where(PersonPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == personId)
            .FirstOrDefaultAsync(cancellationToken);
        if (person is null)
        {
            return false;
        }

        var values = Map(
            request.Name,
            request.CategoryId,
            request.Status,
            request.BirthdayMonth,
            request.BirthdayDay,
            request.Notes,
            request.Visibility);
        ValidateVisibilityChange(person, values.Visibility, actorId);
        await ValidateReferencesAsync(values, cancellationToken);

        person.Update(values, actorId, clock.UtcNow);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(
        int personId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var person = await database.Set<Person>()
            .Where(PersonPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == personId)
            .FirstOrDefaultAsync(cancellationToken);
        if (person is null)
        {
            return false;
        }

        database.Remove(person);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ValidateReferencesAsync(PersonValues values, CancellationToken cancellationToken)
    {
        var categoryExists = await database.Set<PersonCategory>()
            .AnyAsync(category => category.Id == values.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new FirebirdValidationException(
                "One or more Firebird catalog references do not exist.",
                FirebirdValidationReason.UnknownCatalogReference);
        }
    }

    private static void ValidateVisibilityChange(Person person, RecordVisibility requestedVisibility, UserId actorId)
    {
        if (requestedVisibility != person.Visibility && !PersonPolicies.CanChangeVisibility(person, actorId))
        {
            throw new FirebirdValidationException(
                "Only the creator may change person visibility.",
                FirebirdValidationReason.VisibilityForbidden);
        }
    }

    private static PersonValues Map(
        string? name,
        int categoryId,
        string? status,
        int? birthdayMonth,
        int? birthdayDay,
        string? notes,
        string? visibility)
    {
        ValidateBirthday(birthdayMonth, birthdayDay);
        return new(
            name,
            categoryId,
            ParseEnum(status, FirebirdDefaults.Status, "status"),
            birthdayMonth,
            birthdayDay,
            notes,
            ParseEnum(visibility, FirebirdDefaults.Visibility, "visibility"));
    }

    private static void ValidateBirthday(int? month, int? day)
    {
        try
        {
            _ = FirebirdBirthdayRules.Create(month, day);
        }
        catch (ArgumentException exception)
        {
            throw new FirebirdValidationException(exception.Message);
        }
    }

    private static TEnum ParseEnum<TEnum>(string? value, TEnum defaultValue, string field)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new FirebirdValidationException($"The {field} is not a recognized value.");
    }
}
