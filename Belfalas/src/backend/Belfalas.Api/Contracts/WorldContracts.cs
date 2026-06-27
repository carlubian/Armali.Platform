namespace Belfalas.Api.Contracts;

public sealed record WorldTemplateResponse(
    string Id,
    string Theme,
    string Name,
    IReadOnlyList<WorldTemplateDistrictResponse> Districts,
    IReadOnlyList<WorldTemplateVariantResponse> Variants);

public sealed record WorldTemplateDistrictResponse(
    Guid DistrictId,
    string Name,
    int Slot,
    IReadOnlyList<WorldTemplatePlotResponse> Plots,
    IReadOnlyList<WorldTemplateEvolutionStageResponse> EvolutionStages);

public sealed record WorldTemplatePlotResponse(
    Guid PlotId,
    string Category,
    int PositionX,
    int PositionY);

public sealed record WorldTemplateVariantResponse(
    Guid VariantId,
    string Category,
    string SpriteKey);

public sealed record WorldTemplateEvolutionStageResponse(
    Guid EvolutionStageId,
    int Order,
    string Kind,
    string? DenizenType);

public sealed record WorldStateResponse(
    Guid EraId,
    string EraName,
    string TemplateId,
    IReadOnlyList<WorldDistrictStateResponse> Districts);

public sealed record WorldDistrictStateResponse(
    Guid DistrictId,
    string DistrictName,
    int Slot,
    Guid? AreaId,
    string? AreaName,
    int AreaLevel,
    IReadOnlyList<BuiltPlotResponse> BuiltPlots,
    IReadOnlyList<DenizenCountResponse> Denizens);

public sealed record BuiltPlotResponse(
    Guid BuiltPlotId,
    Guid PlotId,
    string Category,
    int PositionX,
    int PositionY,
    Guid VariantId,
    string SpriteKey);

public sealed record DenizenCountResponse(
    string DenizenType,
    int Count);
