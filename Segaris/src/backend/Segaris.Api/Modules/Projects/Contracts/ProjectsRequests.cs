namespace Segaris.Api.Modules.Projects.Contracts;

/// <summary>
/// Frozen request contract for <c>POST /api/projects/projects</c>. <see cref="Status"/>
/// and <see cref="Visibility"/> are the fixed string vocabularies (the
/// <c>ProjectStatus</c> member names and the platform visibility names) and default to
/// <c>Planning</c> and <c>Public</c> when omitted. The global number is system-assigned
/// and is never accepted from the client.
/// </summary>
internal sealed record CreateProjectRequest(
    int AxisId,
    string? Name,
    string? Status,
    string? Visibility);

/// <summary>
/// Frozen request contract for <c>PUT /api/projects/projects/{projectId}</c>. Supplying
/// a different <see cref="AxisId"/> reparents the project; its global number is
/// preserved and only the creator may change <see cref="Visibility"/>.
/// </summary>
internal sealed record UpdateProjectRequest(
    int AxisId,
    string? Name,
    string? Status,
    string? Visibility);

/// <summary>Frozen request contract for <c>POST /api/projects/activities</c>.</summary>
internal sealed record CreateActivityRequest(
    int AxisId,
    string? Name,
    string? Status,
    string? Visibility);

/// <summary>
/// Frozen request contract for <c>PUT /api/projects/activities/{activityId}</c>.
/// Supplying a different <see cref="AxisId"/> reparents the activity; its global number
/// is preserved.
/// </summary>
internal sealed record UpdateActivityRequest(
    int AxisId,
    string? Name,
    string? Status,
    string? Visibility);

/// <summary>
/// Frozen request contract for creating and updating a project risk. The three factors
/// are integers in the inclusive range 1-5; the score is system-computed as their
/// product and is never accepted from the client.
/// </summary>
internal sealed record ProjectRiskRequest(
    string? Description,
    int Probability,
    int Impact,
    int Mitigation);

/// <summary>
/// Frozen request contract for creating and updating a <c>Program</c> through the
/// Configuration presentation boundary. The code is exactly four uppercase ASCII
/// letters and is globally unique across programs.
/// </summary>
internal sealed record ProgramRequest(string? Name, string? Code);

/// <summary>
/// Frozen request contract for creating and updating an <c>Axis</c> through the
/// Configuration presentation boundary. The code is exactly four uppercase ASCII
/// letters and is globally unique across axes; <see cref="ProgramId"/> is the parent
/// program.
/// </summary>
internal sealed record AxisRequest(string? Name, string? Code, int ProgramId);

/// <summary>
/// Frozen request contract for deleting a non-empty program or axis. Every child is
/// atomically reassigned to the single compatible <see cref="TargetNodeId"/> (an axis's
/// projects and activities to another axis, a program's axes to another program) before
/// the node is deleted; the operation rolls back on any failure.
/// </summary>
internal sealed record StructuralNodeReassignmentRequest(int TargetNodeId);
