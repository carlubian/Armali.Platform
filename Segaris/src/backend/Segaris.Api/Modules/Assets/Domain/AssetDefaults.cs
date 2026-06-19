using Segaris.Shared.Authorization;

namespace Segaris.Api.Modules.Assets.Domain;

internal static class AssetDefaults
{
    public const int NameMaximumLength = 200;
    public const int CodeMaximumLength = 50;
    public const int BrandModelMaximumLength = 200;
    public const int SerialNumberMaximumLength = 200;
    public const int NotesMaximumLength = 4000;

    public static readonly AssetStatus Status = AssetStatus.Active;
    public static readonly RecordVisibility Visibility = RecordVisibility.Public;
}
