using Segaris.Shared.Api;

namespace Segaris.Api.Modules.Calendar;

/// <summary>Stable machine-readable Calendar failures.</summary>
internal static class CalendarErrorCodes
{
    public static readonly ErrorCode EntryRangeInvalid = new("calendar.entries.range_invalid");
    public static readonly ErrorCode EntrySourceModuleUnsupported = new("calendar.entries.source_module_unsupported");
    public static readonly ErrorCode EntryVisualFamilyUnsupported = new("calendar.entries.visual_family_unsupported");

    public static readonly ErrorCode NoteNotFound = new("calendar.note.not_found");
    public static readonly ErrorCode NoteValidation = new("calendar.note.validation");
    public static readonly ErrorCode NoteVisibilityForbidden = new("calendar.note.visibility_forbidden");
}
