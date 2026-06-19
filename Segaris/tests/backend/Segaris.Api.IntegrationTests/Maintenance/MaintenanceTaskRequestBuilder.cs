using Segaris.Api.Modules.Maintenance.Contracts;

namespace Segaris.Api.IntegrationTests.Maintenance;

internal sealed class MaintenanceTaskRequestBuilder
{
    private string? title = "Replace filter";
    private int maintenanceTypeId;
    private string? status = "Pending";
    private string? priority = "Medium";
    private DateOnly? dueDate;
    private string? notes;
    private int? assetId;
    private string? visibility = "Public";

    public static MaintenanceTaskRequestBuilder Default() => new();

    public MaintenanceTaskRequestBuilder WithTitle(string? value) { title = value; return this; }
    public MaintenanceTaskRequestBuilder WithType(int value) { maintenanceTypeId = value; return this; }
    public MaintenanceTaskRequestBuilder WithStatus(string? value) { status = value; return this; }
    public MaintenanceTaskRequestBuilder WithPriority(string? value) { priority = value; return this; }
    public MaintenanceTaskRequestBuilder WithDueDate(DateOnly? value) { dueDate = value; return this; }
    public MaintenanceTaskRequestBuilder WithNotes(string? value) { notes = value; return this; }
    public MaintenanceTaskRequestBuilder WithAsset(int? value) { assetId = value; return this; }
    public MaintenanceTaskRequestBuilder WithVisibility(string? value) { visibility = value; return this; }

    public CreateMaintenanceTaskRequest BuildCreate() => new(
        title,
        maintenanceTypeId,
        status,
        priority,
        dueDate,
        notes,
        assetId,
        visibility);

    public UpdateMaintenanceTaskRequest BuildUpdate() => new(
        title,
        maintenanceTypeId,
        status,
        priority,
        dueDate,
        notes,
        assetId,
        visibility);
}
