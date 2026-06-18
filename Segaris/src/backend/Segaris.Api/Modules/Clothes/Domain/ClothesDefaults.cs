using Segaris.Shared.Authorization;

namespace Segaris.Api.Modules.Clothes.Domain;

internal static class ClothesDefaults
{
    public const int NameMaximumLength = 200;
    public const int SizeMaximumLength = 50;
    public const int NotesMaximumLength = 4000;

    public static readonly ClothesGarmentStatus GarmentStatus = ClothesGarmentStatus.Active;
    public static readonly RecordVisibility Visibility = RecordVisibility.Public;
}
