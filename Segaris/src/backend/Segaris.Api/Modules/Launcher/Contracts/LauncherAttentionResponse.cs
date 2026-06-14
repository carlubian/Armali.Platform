namespace Segaris.Api.Modules.Launcher.Contracts;

/// <summary>
/// Frozen response contract for <c>GET /api/launcher/attention</c>. The launcher
/// exposes only the platform-standard boolean attention state per module, never
/// a count or item details. The list shape accepts later modules without
/// changing the existing entries.
/// </summary>
internal sealed record LauncherAttentionResponse(IReadOnlyList<ModuleAttention> Modules);

/// <summary>
/// A single module's attention state. <see cref="Module"/> is the stable module
/// key; <see cref="RequiresAttention"/> is the boolean attention flag.
/// </summary>
internal sealed record ModuleAttention(string Module, bool RequiresAttention);
