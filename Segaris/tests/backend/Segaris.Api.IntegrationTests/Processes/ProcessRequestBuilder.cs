using Segaris.Api.Modules.Processes.Contracts;

namespace Segaris.Api.IntegrationTests.Processes;

/// <summary>
/// Shared builder for the frozen Processes write contracts, used by the Wave 2+ process
/// and step endpoint tests. The status is system-derived and the execution state is
/// preserved by step identity, so neither is expressible here.
/// </summary>
internal sealed class ProcessRequestBuilder
{
    private string? name = "Renew passport";
    private int categoryId;
    private DateOnly? dueDate;
    private string? notes;
    private string? visibility = "Public";

    public static ProcessRequestBuilder Default() => new();

    public ProcessRequestBuilder WithName(string? value) { name = value; return this; }
    public ProcessRequestBuilder WithCategory(int value) { categoryId = value; return this; }
    public ProcessRequestBuilder WithDueDate(DateOnly? value) { dueDate = value; return this; }
    public ProcessRequestBuilder WithNotes(string? value) { notes = value; return this; }
    public ProcessRequestBuilder WithVisibility(string? value) { visibility = value; return this; }

    public CreateProcessRequest BuildCreate() => new(
        name,
        categoryId,
        dueDate,
        notes,
        visibility);

    public UpdateProcessRequest BuildUpdate() => new(
        name,
        categoryId,
        dueDate,
        notes,
        visibility);
}
