namespace Segaris.Api.Modules.Wellness.Domain;

/// <summary>
/// Fixed category vocabulary for a catalogue <c>WellnessTask</c>. The category set
/// is not administrator-configurable in the initial release. These are domain
/// values, persisted as bounded strings using the member names and exchanged on the
/// wire using the same names; the frontend translates them to their display labels
/// ("Health &amp; Body", "Mind &amp; Sleep", "People &amp; Work").
/// </summary>
internal enum WellnessCategory
{
    HealthAndBody,
    MindAndSleep,
    PeopleAndWork,
}
