using Segaris.Api.Modules.Configuration.Contracts;

namespace Segaris.Api.Modules.Firebird.Contracts;

internal enum FirebirdCatalogKind
{
    PersonCategories,
    UsernamePlatforms,
}

internal sealed record FirebirdCatalogDescriptor(
    FirebirdCatalogKind Kind,
    string RouteSegment,
    bool IsRequired,
    bool SupportsClearing);

internal static class FirebirdConfigurationContracts
{
    public static readonly IReadOnlyList<ConfigurationCatalogKind> SharedReferenceKinds = [];

    public static readonly IReadOnlyList<FirebirdCatalogDescriptor> OwnedCatalogs =
    [
        new(FirebirdCatalogKind.PersonCategories, "categories", IsRequired: true, SupportsClearing: false),
        new(FirebirdCatalogKind.UsernamePlatforms, "platforms", IsRequired: true, SupportsClearing: false),
    ];
}
