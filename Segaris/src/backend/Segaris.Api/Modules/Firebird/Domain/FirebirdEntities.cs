using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Firebird.Domain;

internal sealed class PersonCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}

internal sealed class UsernamePlatform
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}

internal sealed record PersonValues(
    string? Name,
    int CategoryId,
    PersonStatus Status,
    int? BirthdayMonth,
    int? BirthdayDay,
    string? Notes,
    RecordVisibility Visibility);

internal sealed class Person
{
    private Person()
    {
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int CategoryId { get; private set; }
    public PersonStatus Status { get; private set; }
    public int? BirthdayMonth { get; private set; }
    public int? BirthdayDay { get; private set; }
    public string? Notes { get; private set; }
    public int? AvatarAttachmentId { get; private set; }
    public RecordVisibility Visibility { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int UpdatedBy { get; private set; }

    public static Person Create(PersonValues values, UserId creatorId, DateTimeOffset now)
    {
        FirebirdValidation.EnsureUtc(now);
        var person = new Person
        {
            CreatedAt = now,
            CreatedBy = creatorId.Value,
            UpdatedAt = now,
            UpdatedBy = creatorId.Value,
        };
        person.Apply(values, creatorId, now, isCreation: true);
        return person;
    }

    public void Update(PersonValues values, UserId actorId, DateTimeOffset now) =>
        Apply(values, actorId, now, isCreation: false);

    internal void ReplaceCategory(int categoryId, UserId actorId, DateTimeOffset now)
    {
        FirebirdValidation.EnsureUtc(now);
        FirebirdValidation.EnsurePositiveIdentifier(categoryId, "Category identifier");
        CategoryId = categoryId;
        StampModification(actorId, now);
    }

    internal void SetAvatarAttachment(int? attachmentId, UserId actorId, DateTimeOffset now)
    {
        FirebirdValidation.EnsureUtc(now);
        AvatarAttachmentId = attachmentId;
        StampModification(actorId, now);
    }

    private void Apply(PersonValues values, UserId actorId, DateTimeOffset now, bool isCreation)
    {
        ArgumentNullException.ThrowIfNull(values);
        FirebirdValidation.EnsureUtc(now);
        var name = FirebirdValidation.ValidateName(values.Name);
        var notes = FirebirdValidation.ValidateNotes(values.Notes);
        FirebirdValidation.EnsureKnownStatusAndVisibility(values.Status, values.Visibility);
        FirebirdValidation.EnsurePositiveIdentifier(values.CategoryId, "Category identifier");
        var birthday = FirebirdBirthdayRules.Create(values.BirthdayMonth, values.BirthdayDay);

        if (!isCreation && values.Visibility != Visibility && actorId.Value != CreatedBy)
        {
            throw new FirebirdValidationException(
                "Only the creator may change person visibility.",
                FirebirdValidationReason.VisibilityForbidden);
        }

        Name = name;
        CategoryId = values.CategoryId;
        Status = values.Status;
        BirthdayMonth = birthday?.Month;
        BirthdayDay = birthday?.Day;
        Notes = notes;
        Visibility = values.Visibility;
        StampModification(actorId, now);
    }

    private void StampModification(UserId actorId, DateTimeOffset now)
    {
        UpdatedAt = now;
        UpdatedBy = actorId.Value;
    }
}

internal sealed record UsernameValues(int PlatformId, string? Handle, string? Notes);

internal sealed class Username
{
    private Username()
    {
    }

    public int Id { get; private set; }
    public int PersonId { get; private set; }
    public int PlatformId { get; private set; }
    public string Handle { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int UpdatedBy { get; private set; }

    public static Username Create(int personId, UsernameValues values, UserId creatorId, DateTimeOffset now)
    {
        FirebirdValidation.EnsureUtc(now);
        FirebirdValidation.EnsurePositiveIdentifier(personId, "Person identifier", FirebirdValidationReason.UsernameValidation);
        var username = new Username
        {
            PersonId = personId,
            CreatedAt = now,
            CreatedBy = creatorId.Value,
            UpdatedAt = now,
            UpdatedBy = creatorId.Value,
        };
        username.Update(values, creatorId, now);
        return username;
    }

    public void Update(UsernameValues values, UserId actorId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(values);
        FirebirdValidation.EnsureUtc(now);
        FirebirdValidation.EnsurePositiveIdentifier(values.PlatformId, "Platform identifier", FirebirdValidationReason.UsernameValidation);
        PlatformId = values.PlatformId;
        Handle = FirebirdValidation.ValidateUsernameHandle(values.Handle);
        Notes = FirebirdValidation.ValidateUsernameNotes(values.Notes);
        UpdatedAt = now;
        UpdatedBy = actorId.Value;
    }

    internal void ReplacePlatform(int platformId, UserId actorId, DateTimeOffset now)
    {
        FirebirdValidation.EnsureUtc(now);
        FirebirdValidation.EnsurePositiveIdentifier(platformId, "Platform identifier", FirebirdValidationReason.UsernameValidation);
        PlatformId = platformId;
        UpdatedAt = now;
        UpdatedBy = actorId.Value;
    }
}

internal sealed record InteractionValues(DateOnly Date, string? Description);

internal sealed class Interaction
{
    private Interaction()
    {
    }

    public int Id { get; private set; }
    public int PersonId { get; private set; }
    public DateOnly Date { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int UpdatedBy { get; private set; }

    public static Interaction Create(
        int personId,
        InteractionValues values,
        UserId creatorId,
        DateTimeOffset now,
        DateOnly today)
    {
        FirebirdValidation.EnsureUtc(now);
        FirebirdValidation.EnsurePositiveIdentifier(personId, "Person identifier", FirebirdValidationReason.InteractionValidation);
        var interaction = new Interaction
        {
            PersonId = personId,
            CreatedAt = now,
            CreatedBy = creatorId.Value,
            UpdatedAt = now,
            UpdatedBy = creatorId.Value,
        };
        interaction.Update(values, creatorId, now, today);
        return interaction;
    }

    public void Update(InteractionValues values, UserId actorId, DateTimeOffset now, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(values);
        FirebirdValidation.EnsureUtc(now);
        FirebirdValidation.EnsureNotFuture(values.Date, today);
        Date = values.Date;
        Description = FirebirdValidation.ValidateInteractionDescription(values.Description);
        UpdatedAt = now;
        UpdatedBy = actorId.Value;
    }
}

