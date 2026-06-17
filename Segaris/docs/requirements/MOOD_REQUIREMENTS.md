# Mood Requirements

## Status

Phase 2 functional definition is complete. This document is the functional
source of truth for the initial Mood implementation plan.

## Purpose

Mood records private emotional check-ins for the current user so they can review
short-term and long-term trends in their own emotional state.

The module is intentionally privacy-first. Mood does not model shared household
records, does not expose entries to administrators, and does not publish data to
Analytics or other business modules. It focuses on fast structured logging,
weekly review, and lightweight trend visualisation.

## Initial Scope

- Record multiple private mood entries per user and civil date.
- Classify each entry through one score and four fixed criteria: Energy,
  Alignment, Direction, and Source.
- Derive the target emotion in read models from the criteria combination.
- Show the current user's entries in a weekly log view.
- Provide a basic weekly score chart in the log view.
- Provide a trend dashboard with calendar-period navigation.
- Keep Mood independent from Analytics, Calendar, Attachments, and other
  business modules in the initial release.

## Excluded Scope

The initial Mood implementation excludes:

- Public or shared mood entries.
- Administrator access to another user's mood entries.
- Configurable criteria, emotion catalogs, or administrator-managed Mood
  reference data.
- Storing the concrete emotion on `MoodEntry`.
- Time-of-day tracking.
- Entry attachments.
- Launcher attention.
- Cross-module Analytics integration.
- Audit history, revision history, soft deletion, or undo.
- Spanish translations as a complete interface language.

## Privacy And Authorization

Mood entries are always private to their creator. The module does not use the
platform-standard public/private `RecordVisibility` toggle because shared mood
records are outside the initial scope.

These rules apply:

- A user can create, read, update, and delete only their own Mood entries.
- Administrators do not receive any privacy bypass.
- Requests for another user's entries return the platform not-found behavior
  where an identifier could otherwise disclose that a private entry exists.
- Collection, dashboard, and aggregate endpoints operate only on the current
  user's entries.
- API responses, logs, diagnostics, and frontend error reports must not expose
  another user's Mood content.

## Mood Entry Model

A Mood entry contains at least:

- A required civil `EntryDate`.
- A required integer score from `1` to `5`, inclusive.
- Required Energy.
- Required Alignment.
- Required Direction.
- Required Source.
- Optional notes.

Mood entries also store standard creation and modification metadata:

- `CreatedAt`
- `CreatedBy`
- `UpdatedAt`
- `UpdatedBy`

The entry does not store a time of day. Multiple entries on the same date are
valid and are ordered by insertion order.

## Score

`Score` is a simple integer between `1` and `5`, inclusive. It is interpreted by
the user as the overall subjective score for that entry.

The initial module uses simple arithmetic averages for score summaries. Scores
are not weighted by Energy, Alignment, Direction, Source, or derived emotion.

## Energy

Energy is a fixed enum with these values:

- `Low`
- `Medium`
- `High`

Energy defines the emotional intensity or mental energy of the entry.

## Alignment

Alignment is a fixed enum with these values:

- `Negative`
- `Medium`
- `Positive`

Alignment defines how habitually good or bad the emotion is for the user.

## Direction

Direction is a fixed enum with these values:

- `Harmony`
- `Defensive`
- `Offensive`
- `Stability`

Direction defines the objective or purpose of the emotion.

The value is spelled `Offensive` in code, API contracts, and documentation.

## Source

Source is a fixed enum with these values:

- `Internal`
- `External`

Source defines the origin or motive of the emotion.

## Notes

Notes are optional and intended for a short description of the entry. They are
not a long-form journal.

Notes are at most 1,000 characters.

The weekly log view does not display notes inline. Notes are visible only when
opening the entry details or editor.

## Derived Emotion Matrix

Mood does not persist the concrete emotion selected by a combination of Energy,
Alignment, Direction, and Source. The backend derives it in read models from a
static matrix owned by the Mood module.

The matrix covers every possible combination:

- 3 Energy values.
- 3 Alignment values.
- 4 Direction values.
- 2 Source values.

This produces exactly 72 mappings. Every combination must have one and only one
derived emotion code.

The initial product source for the matrix is the CSV file supplied outside the
repository at:

```text
D:\Proyectos Locales\SegarisMood.csv
```

The implementation should translate this CSV into module-owned code rather than
loading an administrator-configurable catalog. Tests must validate that the
code-backed matrix has exactly 72 mappings, contains no duplicate combinations,
and has no missing combinations.

Derived emotions are exposed as stable codes, not user-facing text. The
frontend translates those codes through the module's i18next resources.

The weekly log view may display the derived emotion for each entry. The
dashboard does not need to display derived emotions in the initial version.

## Entry Dates And Ordering

`EntryDate` is a civil date with no artificial past or future boundary. Although
the normal workflow defaults new entries to today in `Europe/Madrid`, users may
record entries for any date.

Multiple entries on the same date are valid. There is no hard maximum per day.
The expected normal use is around three to five entries per day.

Entries are ordered by insertion order within a date, using `CreatedAt`
ascending with the entry identifier as a deterministic tie-breaker.

## Deletion

Deletion is physical, immediate, and irreversible in the live application.
There is no trash, archive, soft deletion, revision history, or undo workflow.

A user may delete any of their own Mood entries. Deletion requires explicit
confirmation in the interface.

## Validation

- `EntryDate` is required and has no artificial past or future boundary.
- `Score` is required and must be an integer between `1` and `5`, inclusive.
- Energy is required and must be a known value.
- Alignment is required and must be a known value.
- Direction is required and must be a known value.
- Source is required and must be a known value.
- Notes are optional and at most 1,000 characters.
- A user cannot create, update, delete, query, or aggregate another user's
  entries.

## Creation Defaults

A new Mood entry starts with:

- `EntryDate` equal to today in `Europe/Madrid`.
- No default score until the user chooses one.
- No default Energy, Alignment, Direction, or Source until the user chooses
  them.
- No notes.

## Module Entry And Navigation

Opening Mood takes the user to an immersive Mood module with two primary
screens:

- Log.
- Dashboard.

Mood does not use a generic table-first ERP layout as its only experience. The
module should make fast private entry and review feel natural while still
following the shared Segaris shell, routing, loading, error, and dialog
conventions.

## Log View

The Log view shows the current user's entries for one week at a time.

Weeks run from Monday to Sunday using the household `Europe/Madrid` civil-date
rules. Opening the Log defaults to the week containing today. The user can
navigate to previous and next weeks.

The Log view includes:

- A week navigation control.
- A global `New entry` action.
- A weekly display covering Monday through Sunday.
- Every entry owned by the current user in the selected week.
- A compact chart showing the average score per day for the selected week.

The `New entry` action opens an entry form. The form defaults `EntryDate` to
today in `Europe/Madrid`, regardless of the currently selected week. The user
can choose any valid date in the form.

The weekly display has no filters in the initial version. It shows all entries
for the selected week.

Each entry shown in the weekly display includes at least:

- Entry date or day grouping.
- Score.
- Energy.
- Alignment.
- Direction.
- Source.
- Derived emotion.

Notes are not shown inline. Opening an entry shows its details and allows free
editing.

The weekly score chart uses the simple average of entries per day. Days with no
entries are displayed as missing data, not as zero.

## Entry Editor

Creating, viewing, and editing a Mood entry happens in a popup dialog over the
Log view.

The dialog opens from:

- The global `New entry` action.
- Selecting an existing entry in the weekly display.

The dialog is directly editable whenever the entry belongs to the current user.
There is no separate read-only mode in the initial version.

The editor uses explicit save actions and no autosave. Saving validates the
complete entry. While a request is in progress, actions that could submit the
same changes again are disabled.

After a successful create, update, or delete, the selected week and any relevant
dashboard queries are refreshed. Validation or server errors keep the dialog
open and preserve the user's input. Concurrent edits use the platform's
last-write-wins convention.

Closing a dirty editor requires confirmation before discarding changes.

## Dashboard View

The Dashboard view provides lightweight trend analysis for the current user's
entries within a selected calendar period.

The dashboard supports these time scales:

- Year.
- Semester.
- Quarter.
- Month.

The default scale is Year, and the default period is the current calendar year
in `Europe/Madrid`.

The user can navigate to the previous or next period while keeping the selected
scale. Changing scale resets the dashboard to the current period for the newly
selected scale.

Periods are strict calendar periods:

- A year runs from 1 January through 31 December.
- Semester 1 runs from 1 January through 30 June.
- Semester 2 runs from 1 July through 31 December.
- Quarter 1 runs from 1 January through 31 March.
- Quarter 2 runs from 1 April through 30 June.
- Quarter 3 runs from 1 July through 30 September.
- Quarter 4 runs from 1 October through 31 December.
- A month runs from the first through the last day of that calendar month.

The selected period filters entries exclusively to dates inside that period.
Future periods and periods with no entries are valid selections and show empty
or no-data states rather than special errors.

## Dashboard Charts

The initial Dashboard may include:

- Minimum, average, and maximum score by day of week for the selected period.
- Minimum, average, and maximum score by month for Year, Semester, and Quarter
  scales.
- Minimum, average, and maximum score by week for Month scale.
- Distribution of Energy values in the selected period.
- Distribution of Alignment values in the selected period.
- Distribution of Direction values in the selected period.
- Distribution of Source values in the selected period.
- Evolution of Energy, Alignment, Direction, and Source by month for Year,
  Semester, and Quarter scales.
- Evolution of Energy, Alignment, Direction, and Source by week for Month scale.

All dashboard calculations use only the current user's entries.

Criteria distributions and evolution should count entries by enum value. They do
not need to infer or display derived emotions in the initial dashboard.

## Attention

Mood does not contribute launcher attention in the initial version. Missing
entries, negative scores, or any other Mood trend must not activate the launcher
attention indicator.

## Integration Boundaries

Mood is independent from the other business modules in the initial release.

- Analytics does not consume Mood data.
- Mood does not publish Calendar events.
- Mood does not use Attachments.
- Mood does not consume Configuration catalogs.
- Mood owns its fixed criteria values and derived-emotion matrix.

## API And Query Shape

The exact route and DTO names are implementation-plan details, but the API must
support:

- Creating the current user's Mood entry.
- Listing the current user's entries for a selected inclusive date range.
- Reading one current-user Mood entry.
- Updating one current-user Mood entry.
- Deleting one current-user Mood entry.
- Reading dashboard aggregates for the current user and selected calendar
  period.

Read models that represent entries include the derived emotion code. Mutation
requests do not accept a derived emotion because it is calculated by the module.

Log range queries are bounded by the weekly view. Dashboard aggregate queries
are bounded by the selected calendar period. The initial module does not require
unbounded all-history collection reads.

## Acceptance Criteria

The initial Mood definition is satisfied when:

1. Authenticated users can create, query, edit, and irreversibly delete only
   their own Mood entries with the documented fields, defaults, validation, and
   date behavior.
2. Mood entries are always owner-only; administrators cannot view or mutate
   another user's entries, and private existence is not disclosed through detail
   endpoints.
3. Each entry stores `EntryDate`, `Score`, Energy, Alignment, Direction, Source,
   optional notes, and standard metadata, but does not store a time of day,
   visibility value, attachment reference, or concrete emotion.
4. The fixed criteria enums expose exactly the documented values, including
   `Offensive` for Direction.
5. The derived-emotion matrix is code-backed, covers exactly the 72 possible
   criteria combinations, contains no duplicate or missing combinations, and
   returns a stable translatable emotion code.
6. Users can create multiple entries for the same date with no hard per-day
   limit, and entries within a day are returned in insertion order.
7. The Log view defaults to the week containing today in `Europe/Madrid`,
   navigates by Monday-to-Sunday weeks, shows every owned entry in the selected
   week without filters, and opens entries in an editable dialog.
8. The Log view provides a global new-entry action whose form defaults
   `EntryDate` to today, and the weekly chart shows simple average score per day
   while treating days without entries as missing data rather than zero.
9. Notes are available in the entry details/editor but are not shown inline in
   the weekly log display.
10. The Dashboard defaults to the current year, supports Year, Semester,
    Quarter, and Month scales, navigates previous and next periods, and resets
    to the current period when scale changes.
11. Dashboard periods are strict calendar periods in `Europe/Madrid`, and all
    aggregates include only entries whose `EntryDate` falls inside the selected
    period.
12. Dashboard charts provide score min/average/max summaries and criteria
    distribution or evolution views for the current user's selected period.
13. SQLite and PostgreSQL migrations, backend unit/integration/architecture
    tests, frontend component tests, and a representative Playwright journey
    verify the supported behavior, derived-emotion matrix, period calculations,
    and privacy boundaries.

## Deferred Decisions

- Whether a future version should add opt-in reminders or launcher attention
  for missing entries.
- Whether future dashboards should include correlation matrices between
  criteria and score.
- Whether the weekly Log should later support filters, search, or richer
  calendar visualisations.
- Whether Mood should provide export or portability features for personal data.
- Whether derived-emotion labels should be editable by the product owner through
  a future non-administrator tooling workflow.
