namespace Segaris.Api.Modules.Calendar;

internal static class CalendarSourceModules
{
    public const string Calendar = "calendar";
    public const string Firebird = "firebird";
    public const string Travel = "travel";
    public const string Inventory = "inventory";
    public const string Assets = "assets";
    public const string Maintenance = "maintenance";
    public const string Processes = "processes";

    public static readonly IReadOnlySet<string> InitialProjectionSources =
        new HashSet<string>(StringComparer.Ordinal)
        {
            Firebird,
            Travel,
            Inventory,
            Assets,
            Maintenance,
            Processes,
        };

    public static readonly IReadOnlySet<string> AllowedFilters =
        new HashSet<string>(StringComparer.Ordinal)
        {
            Calendar,
            Firebird,
            Travel,
            Inventory,
            Assets,
            Maintenance,
            Processes,
        };
}

internal static class CalendarSourceTypes
{
    public const string DailyNote = "dailyNote";
    public const string Birthday = "birthday";
    public const string Trip = "trip";
    public const string InventoryOrderExpectedReceipt = "inventoryOrderExpectedReceipt";
    public const string AssetExpectedEndOfLife = "assetExpectedEndOfLife";
    public const string MaintenanceTaskDue = "maintenanceTaskDue";
    public const string ProcessStepDue = "processStepDue";

    public static readonly IReadOnlySet<string> InitialSourceTypes =
        new HashSet<string>(StringComparer.Ordinal)
        {
            DailyNote,
            Birthday,
            Trip,
            InventoryOrderExpectedReceipt,
            AssetExpectedEndOfLife,
            MaintenanceTaskDue,
            ProcessStepDue,
        };
}

internal static class CalendarVisualFamilies
{
    public const string Birthday = "Birthday";
    public const string Travel = "Travel";
    public const string Note = "Note";
    public const string Other = "Other";

    public static readonly IReadOnlySet<string> AllowedFilters =
        new HashSet<string>(StringComparer.Ordinal)
        {
            Birthday,
            Travel,
            Note,
            Other,
        };
}

internal static class CalendarModuleContract
{
    public const bool ContributesLauncherAttention = false;
}
