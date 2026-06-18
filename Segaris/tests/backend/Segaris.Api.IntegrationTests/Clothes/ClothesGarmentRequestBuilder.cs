using Segaris.Api.Modules.Clothes.Contracts;

namespace Segaris.Api.IntegrationTests.Clothes;

internal sealed class ClothesGarmentRequestBuilder
{
    private string? name = "Blue sweater";
    private int categoryId;
    private string? status = "Active";
    private string? size;
    private IReadOnlyList<int> colorIds = [];
    private string? washingCare;
    private string? dryingCare;
    private string? ironingCare;
    private string? dryCleaningCare;
    private string? notes;
    private string? visibility = "Public";

    public static ClothesGarmentRequestBuilder Default() => new();

    public ClothesGarmentRequestBuilder WithName(string? value) { name = value; return this; }
    public ClothesGarmentRequestBuilder WithCategory(int value) { categoryId = value; return this; }
    public ClothesGarmentRequestBuilder WithStatus(string? value) { status = value; return this; }
    public ClothesGarmentRequestBuilder WithSize(string? value) { size = value; return this; }
    public ClothesGarmentRequestBuilder WithColors(params int[] values) { colorIds = values; return this; }
    public ClothesGarmentRequestBuilder WithCare(string? washing = null, string? drying = null, string? ironing = null, string? dryCleaning = null)
    {
        washingCare = washing;
        dryingCare = drying;
        ironingCare = ironing;
        dryCleaningCare = dryCleaning;
        return this;
    }

    public ClothesGarmentRequestBuilder WithNotes(string? value) { notes = value; return this; }
    public ClothesGarmentRequestBuilder WithVisibility(string? value) { visibility = value; return this; }

    public CreateClothesGarmentRequest BuildCreate() => new(
        name,
        categoryId,
        status,
        size,
        colorIds,
        washingCare,
        dryingCare,
        ironingCare,
        dryCleaningCare,
        notes,
        visibility);

    public UpdateClothesGarmentRequest BuildUpdate() => new(
        name,
        categoryId,
        status,
        size,
        colorIds,
        washingCare,
        dryingCare,
        ironingCare,
        dryCleaningCare,
        notes,
        visibility);
}
