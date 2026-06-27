namespace Belfalas.Domain;

/// <summary>The kind of a single stage in a district's evolution sequence.</summary>
public enum EvolutionStageKind
{
    Building,
    Denizen,
    Upgrade,
}

/// <summary>
/// A themed, reusable definition of a world: its districts (one per area slot), the
/// plots and their categories, the sprite variants available per category, and the
/// ordered evolution sequence of each district. Catalogue/reference data shared
/// across eras; an era instances exactly one template.
/// </summary>
public sealed class WorldTemplate
{
    /// <summary>Human-readable slug, e.g. <c>tropical-v1</c>.</summary>
    public required string Id { get; set; }
    public required string Theme { get; set; }
    public required string Name { get; set; }

    public ICollection<District> Districts { get; set; } = [];
    public ICollection<Variant> Variants { get; set; } = [];
}

/// <summary>One district of a world template, occupying a single area slot.</summary>
public sealed class District
{
    public Guid Id { get; set; }
    public required string WorldTemplateId { get; set; }
    public required string Name { get; set; }

    /// <summary>Zero-based area slot this district fills within the template.</summary>
    public int Slot { get; set; }

    public WorldTemplate? WorldTemplate { get; set; }
    public ICollection<Plot> Plots { get; set; } = [];
    public ICollection<EvolutionStage> EvolutionStages { get; set; } = [];
}

/// <summary>
/// A buildable cell within a district at a fixed isometric position. Adjacency is
/// derived from the grid coordinates, so organic growth can pick a free plot next to
/// an already-built one.
/// </summary>
public sealed class Plot
{
    public Guid Id { get; set; }
    public Guid DistrictId { get; set; }
    public required string Category { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }

    public District? District { get; set; }
}

/// <summary>
/// A sprite variant belonging to a category's variant set within a template. The set
/// of variants sharing a (template, category) is drawn from at random when a plot of
/// that category is built.
/// </summary>
public sealed class Variant
{
    public Guid Id { get; set; }
    public required string WorldTemplateId { get; set; }
    public required string Category { get; set; }
    public required string SpriteKey { get; set; }

    public WorldTemplate? WorldTemplate { get; set; }
}

/// <summary>
/// One ordered stage in a district's evolution sequence. Each area level reached
/// advances one stage; a stage adds a building, a denizen, or an upgrade.
/// </summary>
public sealed class EvolutionStage
{
    public Guid Id { get; set; }
    public Guid DistrictId { get; set; }

    /// <summary>One-based position within the district's sequence (the area level it represents).</summary>
    public int Order { get; set; }
    public EvolutionStageKind Kind { get; set; }

    /// <summary>For <see cref="EvolutionStageKind.Denizen"/> stages, the denizen identity added; otherwise null.</summary>
    public string? DenizenType { get; set; }

    public District? District { get; set; }
}
