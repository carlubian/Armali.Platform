namespace Segaris.Api.Modules.Assets.Domain;

/// <summary>
/// The fixed, descriptive asset lifecycle status. It blocks no operation by
/// itself and is not managed through Configuration. Only non-<see cref="Retired"/>
/// assets participate in launcher attention.
/// </summary>
internal enum AssetStatus
{
    Active,
    Stored,
    Retired,
}
