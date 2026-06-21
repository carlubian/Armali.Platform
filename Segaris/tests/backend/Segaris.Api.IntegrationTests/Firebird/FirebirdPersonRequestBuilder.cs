using Segaris.Api.Modules.Firebird.Contracts;

namespace Segaris.Api.IntegrationTests.Firebird;

internal sealed class FirebirdPersonRequestBuilder
{
    private string? name = "Ada Lovelace";
    private int categoryId;
    private string? status = "Active";
    private int? birthdayMonth;
    private int? birthdayDay;
    private string? notes;
    private string? visibility = "Public";

    public static FirebirdPersonRequestBuilder Default() => new();

    public FirebirdPersonRequestBuilder WithName(string? value) { name = value; return this; }
    public FirebirdPersonRequestBuilder WithCategory(int value) { categoryId = value; return this; }
    public FirebirdPersonRequestBuilder WithStatus(string? value) { status = value; return this; }
    public FirebirdPersonRequestBuilder WithBirthday(int? month, int? day) { birthdayMonth = month; birthdayDay = day; return this; }
    public FirebirdPersonRequestBuilder WithNotes(string? value) { notes = value; return this; }
    public FirebirdPersonRequestBuilder WithVisibility(string? value) { visibility = value; return this; }

    public CreatePersonRequest BuildCreate() => new(
        name,
        categoryId,
        status,
        birthdayMonth,
        birthdayDay,
        notes,
        visibility);

    public UpdatePersonRequest BuildUpdate() => new(
        name,
        categoryId,
        status,
        birthdayMonth,
        birthdayDay,
        notes,
        visibility);
}
