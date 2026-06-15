namespace Segaris.Api.Modules.Configuration.Persistence;

/// <summary>
/// Shared shape of every Configuration catalog row. Identity is the generated
/// <see cref="Id"/>; <see cref="NormalizedName"/> backs case-insensitive
/// uniqueness and <see cref="SortOrder"/> the deterministic catalog order.
/// Non-currency catalogs no longer persist a stable code.
/// </summary>
internal interface IConfigurationCatalogEntity
{
    int Id { get; set; }

    string Name { get; set; }

    string NormalizedName { get; set; }

    int SortOrder { get; set; }

    DateTimeOffset CreatedAt { get; set; }

    int? CreatedBy { get; set; }

    DateTimeOffset UpdatedAt { get; set; }

    int? UpdatedBy { get; set; }
}

internal sealed class SegarisSupplier : IConfigurationCatalogEntity
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

internal sealed class SegarisCostCenter : IConfigurationCatalogEntity
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

/// <summary>
/// Currency keeps an editable three-letter display <see cref="Code"/> in addition
/// to the shared catalog shape. <see cref="NormalizedCode"/> backs case-insensitive
/// code uniqueness alongside the normalized-name uniqueness shared with the other
/// catalogs.
/// </summary>
internal sealed class SegarisCurrency : IConfigurationCatalogEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NormalizedCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}

/// <summary>
/// Internal one-time initialization marker. A present row for a stable catalog key
/// means the catalog has been initialized and must never be automatically seeded
/// again, even if an administrator later empties it.
/// </summary>
internal sealed class SegarisCatalogInitialization
{
    public string CatalogKey { get; set; } = string.Empty;

    public DateTimeOffset InitializedAt { get; set; }
}
