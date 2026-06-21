using Segaris.Shared.Authorization;

namespace Segaris.Api.Modules.Firebird.Domain;

internal static class FirebirdDefaults
{
    public const int NameMaximumLength = 200;
    public const int NotesMaximumLength = 2000;
    public const int UsernameHandleMaximumLength = 200;
    public const int UsernameNotesMaximumLength = 1000;
    public const int InteractionDescriptionMaximumLength = 2000;
    public const int CatalogNameMaximumLength = 200;
    public const int AttentionWindowDays = 7;

    public static readonly PersonStatus Status = PersonStatus.Unknown;
    public static readonly RecordVisibility Visibility = RecordVisibility.Public;
}
