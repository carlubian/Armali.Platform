namespace Segaris.Api.Modules.Projects.Contracts;

/// <summary>Frozen tree projection of a program node, ordered by code ascending.</summary>
internal sealed record ProgramNodeResponse(int Id, string Code, string Name);

/// <summary>Frozen tree projection of an axis node, ordered by code ascending.</summary>
internal sealed record AxisNodeResponse(int Id, string Code, string Name, int ProgramId);

/// <summary>
/// Frozen risk-band summary surfaced on a project so the counts of low, medium, and
/// high risks can be shown without opening the full risk table.
/// </summary>
internal sealed record ProjectRiskBandSummaryResponse(int Low, int Medium, int High);

/// <summary>
/// Frozen tree projection of a project or activity, ordered by number ascending.
/// <see cref="Kind"/> is <c>Project</c> or <c>Activity</c>; <see cref="RiskSummary"/> is
/// populated only for projects. <see cref="Identifier"/> is the computed unified
/// identifier.
/// </summary>
internal sealed record ProjectTreeItemResponse(
    int Id,
    string Kind,
    int Number,
    string Identifier,
    string Name,
    string Status,
    string Visibility,
    ProjectRiskBandSummaryResponse? RiskSummary);

/// <summary>
/// Frozen project detail projection. <see cref="Identifier"/> is computed on demand and
/// never persisted. Project attachments have no primary image.
/// </summary>
internal sealed record ProjectResponse(
    int Id,
    int Number,
    string Identifier,
    string Name,
    string Status,
    string Visibility,
    int AxisId,
    ProjectRiskBandSummaryResponse RiskSummary,
    IReadOnlyList<ProjectAttachmentResponse> Attachments,
    int CreatedById,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    int? UpdatedById,
    string? UpdatedByName,
    DateTimeOffset? UpdatedAt);

/// <summary>Frozen activity detail projection. An activity has no risks and no attachments.</summary>
internal sealed record ActivityResponse(
    int Id,
    int Number,
    string Identifier,
    string Name,
    string Status,
    string Visibility,
    int AxisId,
    int CreatedById,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    int? UpdatedById,
    string? UpdatedByName,
    DateTimeOffset? UpdatedAt);

/// <summary>
/// Frozen project-risk projection. <see cref="Score"/> is the system-computed product of
/// the three factors and <see cref="Band"/> is its <c>RiskBand</c> name.
/// </summary>
internal sealed record ProjectRiskResponse(
    int Id,
    string Description,
    int Probability,
    int Impact,
    int Mitigation,
    int Score,
    string Band);

/// <summary>Frozen project attachment projection; there is no primary image.</summary>
internal sealed record ProjectAttachmentResponse(
    string Id,
    string FileName,
    string ContentType,
    long Size,
    int CreatedById,
    DateTimeOffset CreatedAt);

/// <summary>
/// Frozen management projection of a program, ordered by code ascending for
/// Configuration and the tree.
/// </summary>
internal sealed record ProgramResponse(int Id, string Code, string Name);

/// <summary>Frozen management projection of an axis, ordered by code ascending.</summary>
internal sealed record AxisResponse(int Id, string Code, string Name, int ProgramId);

/// <summary>
/// Frozen privacy-neutral deletion-impact projection for a program or axis.
/// <see cref="ChildCount"/> is the number of children that would be reassigned (counted
/// without disclosing other users' private items) and <see cref="HasCompatibleTarget"/>
/// reports whether any compatible target node exists, so the UI can block deletion when
/// none does.
/// </summary>
internal sealed record StructuralNodeDeletionImpactResponse(int ChildCount, bool HasCompatibleTarget);
