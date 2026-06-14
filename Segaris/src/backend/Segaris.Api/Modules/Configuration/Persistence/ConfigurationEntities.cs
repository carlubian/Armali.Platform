namespace Segaris.Api.Modules.Configuration.Persistence;

internal interface IConfigurationCatalogEntity
{
    int Id { get; set; }

    string Code { get; set; }

    string Name { get; set; }

    DateTimeOffset CreatedAt { get; set; }

    int? CreatedBy { get; set; }

    DateTimeOffset UpdatedAt { get; set; }

    int? UpdatedBy { get; set; }
}

internal sealed class SegarisSupplier : IConfigurationCatalogEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}

internal sealed class SegarisCostCenter : IConfigurationCatalogEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}

internal sealed class SegarisCurrency : IConfigurationCatalogEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
