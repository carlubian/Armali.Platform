namespace Belfalas.Api.Contracts;

public sealed record WorldTemplateResponse(
    string Id,
    string Theme,
    string Name,
    WorldTemplateRenderContractResponse Render,
    IReadOnlyList<WorldTemplateDistrictResponse> Districts,
    IReadOnlyList<WorldTemplateCategoryContractResponse> Categories,
    IReadOnlyList<WorldTemplateVariantResponse> Variants);

public sealed record WorldTemplateRenderContractResponse(
    int TileWidth,
    int TileHeight,
    int MapWidth,
    int MapHeight,
    int OriginX,
    int OriginY,
    WorldTemplateCameraBoundsResponse CameraBounds,
    string AssetBasePath,
    string AtlasKey);

public sealed record WorldTemplateCameraBoundsResponse(
    int MinX,
    int MinY,
    int MaxX,
    int MaxY);

public sealed record WorldTemplateDistrictResponse(
    Guid DistrictId,
    string Name,
    int Slot,
    IReadOnlyList<WorldTemplatePlotResponse> Plots,
    IReadOnlyList<WorldTemplateDenizenSocketResponse> DenizenSockets,
    IReadOnlyList<WorldTemplateEvolutionStageResponse> EvolutionStages);

public sealed record WorldTemplatePlotResponse(
    Guid PlotId,
    string Category,
    int PositionX,
    int PositionY);

public sealed record WorldTemplateDenizenSocketResponse(
    Guid DenizenSocketId,
    int PositionX,
    int PositionY,
    double AnchorX,
    double AnchorY,
    int SortOffsetY,
    IReadOnlyList<string> CompatibleDenizenTypes);

public sealed record WorldTemplateCategoryContractResponse(
    Guid CategoryContractId,
    string Category,
    int FootprintWidth,
    int FootprintHeight,
    double AnchorX,
    double AnchorY,
    int SortOffsetY,
    bool SupportsDenizens);

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
