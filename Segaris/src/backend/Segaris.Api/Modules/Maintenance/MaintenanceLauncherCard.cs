namespace Segaris.Api.Modules.Maintenance;

/// <summary>
/// Frozen identity used by the Maintenance launcher attention contributor (Wave 6).
/// The key is the stable module identifier reported in the aggregated launcher
/// attention response. Attention becomes true when the current user can access at
/// least one <c>Pending</c> or <c>InProgress</c> task whose <c>DueDate</c> is set and
/// is in the past or within the inclusive window from today to today plus 7 natural
/// days in <c>Europe/Madrid</c>.
/// </summary>
internal static class MaintenanceLauncherCard
{
    public const string ModuleKey = "maintenance";
}
