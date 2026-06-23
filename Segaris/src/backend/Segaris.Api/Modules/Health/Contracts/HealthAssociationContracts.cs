namespace Segaris.Api.Modules.Health.Contracts;

internal enum HealthAssociationVisibilityRule
{
    AccessibleOnlyCreation,
    ViewerFilteredReads,
    PublishGuard,
}

internal static class HealthAssociationContracts
{
    public static readonly IReadOnlyList<HealthAssociationVisibilityRule> VisibilityRules =
    [
        HealthAssociationVisibilityRule.AccessibleOnlyCreation,
        HealthAssociationVisibilityRule.ViewerFilteredReads,
        HealthAssociationVisibilityRule.PublishGuard,
    ];
}
