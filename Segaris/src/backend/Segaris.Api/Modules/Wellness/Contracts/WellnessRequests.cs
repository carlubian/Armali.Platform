namespace Segaris.Api.Modules.Wellness.Contracts;

/// <summary>
/// Create payload for a catalogue task. Name and category are both required; the
/// category is carried as a nullable string so a missing or unknown value surfaces as
/// a stable <c>wellness.task.validation</c> failure rather than a deserialization
/// error. Tasks are created or deleted only, so there is no update payload.
/// </summary>
internal sealed record CreateWellnessTaskRequest(
    string? Name,
    string? Category);
