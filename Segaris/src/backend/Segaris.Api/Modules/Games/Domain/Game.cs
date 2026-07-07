namespace Segaris.Api.Modules.Games.Domain;

/// <summary>
/// Administrator-managed Games catalogue row. Games are module-owned and surfaced
/// through Configuration; platform is a fixed enum, not a configurable catalogue.
/// </summary>
internal sealed class Game
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public GamePlatform Platform { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
