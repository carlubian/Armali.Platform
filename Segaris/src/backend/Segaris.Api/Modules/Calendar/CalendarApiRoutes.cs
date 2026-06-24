namespace Segaris.Api.Modules.Calendar;

/// <summary>Frozen route shapes for the Calendar HTTP surface.</summary>
internal static class CalendarApiRoutes
{
    public const string Tag = "Calendar";
    public const string Calendar = "calendar";

    public const string Entries = "/entries";
    public const string Notes = "/notes";
    public const string NoteById = "/notes/{noteId:int}";

    public static class QueryParameters
    {
        public const string From = "from";
        public const string To = "to";
        public const string SourceModule = "sourceModule";
        public const string VisualFamily = "visualFamily";
    }
}
