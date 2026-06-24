# Calendar Requirements

## Status

Phase 2 functional definition is in discussion. This document records the
accepted initial decisions for the Calendar module and should become the
functional source of truth before an implementation plan is created.

## Purpose

Calendar provides one shared time-oriented view of household events, due dates,
and date-bound information published by other Segaris modules. It lets users see
what is relevant on a given day without moving through each source module first.

Calendar is a cross-domain read module for projected entries from other modules.
Each source module remains authoritative for its records, visibility rules,
status rules, deletion behavior, and the action opened from a calendar entry.
Calendar does not become a generic owner of every domain date.

Calendar also owns a small manual daily-note capability for private or shared
free-form notes attached to a civil date.

## Initial Scope

- Present a month-oriented calendar view using Spain household conventions:
  `Europe/Madrid`, Monday-first weeks, and civil dates.
- Query and display accessible calendar projections from participating modules.
- Show Firebird birthdays.
- Show Travel trips that are not `Cancelled` as continuous all-day events
  spanning the trip start date through the trip end date.
- Show Inventory expected receipt dates for orders that are `Planning` or
  `Active`.
- Show Assets expected end-of-life dates for assets that are not `Retired`.
- Show Maintenance due dates for tasks that are `Pending` or `InProgress`.
- Show Processes step due dates for pending steps.
- Let users create, edit, and delete manual daily notes owned by Calendar.
- Let users mark daily notes as `Private` or `Public`, with `Private` as the
  creation default.
- Provide day-level highlighting that distinguishes birthdays, travel, notes, and
  other event types.
- Let users open the source module record from projected entries where a source
  detail action is available.

## Excluded Scope

The initial Calendar implementation excludes:

- Capex entries and Opex contracts or occurrences.
- Travel itinerary entries, reservations, or expense dates.
- Process global due dates.
- Inventory orders that are `Received` or `Cancelled`.
- Assets in the `Retired` status.
- Completed or cancelled Maintenance tasks.
- Completed, skipped, or otherwise resolved Process steps.
- External calendar synchronization with Google Calendar, Outlook, CalDAV, ICS
  subscriptions, or any outbound provider integration.
- Generic recurrence rules for manual Calendar notes.
- Alarm-style reminders, push notifications, email notifications, or a
  notification inbox.
- Drag-and-drop rescheduling of source-module entries.
- Editing source-module records directly inside Calendar.
- A generic persisted event table that duplicates source-module records.
- Attachments on daily notes.
- Spanish translations.

## Calendar Projection Model

Calendar projections are read models published by source modules through narrow
contracts. Calendar consumes those contracts and does not query another module's
internal entities, tables, or EF Core types.

An indicative projected calendar entry contains:

- A stable source module code.
- A stable source entry type.
- A source record identifier or opaque source reference.
- A title safe to display to the current user.
- Optional short supporting text safe to display to the current user.
- A date range represented by a required start civil date and an optional end
  civil date.
- An optional all-day/time classification when a source later needs it; the
  accepted initial projections are civil-date based.
- A visual family used by the month grid indicator.
- Optional source status text or status code when useful for the day detail.
- An optional target route or action descriptor for opening the source record.

Projection contracts return only entries the current user is authorized to see.
Calendar does not receive inaccessible record identifiers and does not infer
authorization from source identifiers.

Projected entries are not editable through Calendar. The source module owns any
mutation and all domain validation.

## Source Modules

### Firebird

Firebird publishes birthdays for accessible people with a stored birthday.

Birthdays are recurring calendar projections derived from the person's stored
month and day. The source module owns leap-day behavior. Calendar displays the
next occurrence or occurrences that fall within the requested date range.

Birthday entries use the `Birthday` visual family. People without birthdays and
private people owned by another user never appear.

### Travel

Travel publishes each accessible trip whose date range intersects the requested
Calendar range.

A trip is displayed as one continuous all-day event spanning `StartDate` through
`EndDate`, inclusive. This continuous rendering is a Calendar UI behavior; it
does not require Travel to create separate per-day records or multiple backend
events.

Trips with status `Cancelled` are not published. Completed trips may appear when
their date range intersects the requested Calendar range, because Calendar is a
queryable time view and not only an upcoming-attention surface.

Travel itinerary entries are not published in the initial Calendar scope.

Travel entries use the `Travel` visual family.

### Inventory

Inventory publishes expected receipt dates for accessible orders whose status is
`Planning` or `Active` and whose `ExpectedReceiptDate` is set.

Orders with status `Received` or `Cancelled` are not published. Orders without an
expected receipt date are not published.

Inventory entries use the `Other` visual family.

### Assets

Assets publishes expected end-of-life dates for accessible assets whose status is
not `Retired` and whose `ExpectedEndOfLifeDate` is set.

Past dates may appear when they fall inside the requested Calendar range. Unlike
the Assets launcher attention rule, Calendar is a queryable time view and may
show historical or overdue date-bound information.

Retired assets are not published.

Assets entries use the `Other` visual family.

### Maintenance

Maintenance publishes due dates for accessible tasks whose status is `Pending` or
`InProgress` and whose `DueDate` is set.

Completed and cancelled tasks are not published. Tasks without a due date are not
published.

Maintenance entries use the `Other` visual family.

### Processes

Processes publishes due dates for accessible pending steps whose step `DueDate`
is set.

Calendar does not publish the process global due date in the initial scope.
Resolved steps are not published. Cancelled or completed processes should not
publish step entries.

If a process has several pending steps with due dates inside the requested range,
each qualifying step may appear as its own projected entry. A future
implementation plan may narrow this to only the frontier step if the existing
Processes contract makes broader pending-step projection too expensive or
confusing.

Processes entries use the `Other` visual family.

## Manual Daily Notes

Calendar owns manual daily notes as its only persisted domain entity in the
initial scope.

A daily note contains at least:

- A required civil date.
- A required body or note text.
- Optional short title.
- Visibility.
- Standard ownership and audit metadata.

Manual daily notes are attached to one date only. They do not span ranges, repeat,
carry alarms, own attachments, or link to source-module records in the initial
scope.

Daily notes use the `Note` visual family.

## Visibility And Authorization

Projected entries inherit their visibility from the source module. Each source
module applies its own public/private, ownership, and status filtering before
returning projections to Calendar.

Calendar daily notes use the platform-standard visibility values:

- `Public`
- `Private`

New daily notes default to `Private`.

These rules apply:

- A user can view and edit their own notes and public notes.
- A private note remains creator-only, including from administrators.
- Any authenticated user may edit a public note.
- Only the creator may change a note's visibility.

Missing and inaccessible daily notes share the platform not-found behavior so
private notes are not disclosed.

## Module Entry And Navigation

Opening Calendar takes the user directly to the calendar view. Calendar does not
have an initial dashboard, chart surface, agenda-only landing page, or reporting
view.

The primary route is:

```text
/calendar
```

Month navigation, selected day, active filters, and any open note editor should
be URL-backed where practical so state survives refreshes and dialog open/close.

Projected entries can open their source module record through the source route or
action descriptor supplied by the projection. If a source route cannot be safely
provided, Calendar shows the entry as informational only.

Creating, viewing, and editing a manual note happens in a URL-aware popup or
equivalent contextual surface over the Calendar view.

## Calendar View

The initial Calendar surface is a month grid.

The month grid:

- Uses Monday-first weeks.
- Shows the selected month with enough adjacent days to fill complete weeks.
- Provides previous month, next month, and today actions.
- Highlights the current day.
- Highlights the selected day.
- Marks days that contain accessible entries.
- Opens a day detail surface when a day is selected.

The day detail surface lists every accessible entry for the selected day,
including projected entries and manual notes. Entries should be grouped or
visually labelled by source or visual family. A projected entry shows enough
context to recognize the source record and provides an open-source action when
available. A manual note opens its Calendar-owned editor.

Travel trips spanning several days appear on every day in the range in both the
month grid indicator and the day detail. The trip remains one continuous source
projection, not duplicated source data.

## Day Indicators

Calendar uses compact day indicators to show which families of entries exist on a
day without opening it.

The accepted visual families are:

- `Birthday`
- `Travel`
- `Note`
- `Other`

`Other` covers Inventory, Assets, Maintenance, Processes, and future
non-specialized projections.

The preferred behavior is to show multiple compact indicators when a day contains
multiple visual families, up to the four accepted families. The day detail shows
the complete list of entries.

If a narrow viewport or final visual design cannot present multiple indicators
cleanly, Calendar may fall back to one priority indicator plus an additional
"more" marker. The fallback priority is:

1. `Travel`
2. `Birthday`
3. `Note`
4. `Other`

The fallback is a UI adaptation only; it must not affect the day detail list or
the backend projection results.

## Filters

Calendar should let users filter visible entries by at least:

- Source module.
- Visual family.

Filters affect the month indicators and the day detail list together. The
selected month and day remain stable when filters change.

Filtering must not bypass source-module authorization. Hidden entries are only
client-visible entries removed from the current presentation; inaccessible entries
are never returned by the backend.

## Query Range

Calendar queries projections by inclusive civil-date range. The frontend may query
slightly beyond the visible month to cover adjacent week cells.

The backend must bound the maximum query range to prevent unbounded cross-module
reads. A practical initial maximum is one calendar year per request, unless the
implementation plan chooses a smaller bound for performance.

Source modules should return deterministic ordering for entries on the same day.
Calendar may apply a final presentation order by visual family, date, source
module, title, and source identifier.

## Validation

Manual daily-note validation includes:

- Date is required and is a valid civil date.
- Body is required, trimmed, not whitespace-only, and at most 4,000 characters.
- Title is optional, trimmed when supplied, not whitespace-only when present, and
  at most 200 characters.
- Visibility is a known value.

Projection query validation includes:

- Start date and end date are required civil dates.
- End date must be greater than or equal to start date.
- The requested range must not exceed the backend maximum.
- Source-module and visual-family filters must use known allow-listed values.

## Creation Defaults

A new manual daily note starts with:

- Date equal to the currently selected day, or today in `Europe/Madrid` when no day
  is selected.
- Visibility `Private`.
- Empty title.
- Empty body until supplied by the user.

Calendar itself creates no projected entries.

## Attention

Calendar may have a launcher card, but the initial Calendar module does not need
to request attention through the launcher.

Calendar displays date-bound information when opened. Existing source modules may
continue to contribute their own launcher attention under their already accepted
rules.

## Acceptance Criteria

The initial Calendar definition is satisfied when:

1. Authenticated users can open Calendar directly on a month grid using
   `Europe/Madrid`, Monday-first weeks, current-day highlighting, selected-day
   highlighting, month navigation, and a day detail surface.
2. Calendar consumes source-module projection contracts rather than querying source
   module entities, tables, or implementation services directly.
3. Projection results are filtered by each source module for the current user, and
   Calendar never receives or displays inaccessible private records.
4. Firebird birthdays appear for accessible people with birthdays, using the
   source-owned birthday occurrence rules.
5. Travel trips that are not `Cancelled` appear as continuous all-day events
   spanning trip start through trip end, without publishing itinerary entries.
6. Inventory publishes only accessible `Planning` and `Active` orders with an
   expected receipt date.
7. Assets publishes only accessible non-`Retired` assets with an expected
   end-of-life date, including past dates when they fall in the requested range.
8. Maintenance publishes only accessible `Pending` or `InProgress` tasks with due
   dates.
9. Processes publishes pending step due dates, not process global due dates, and
   excludes completed or cancelled processes and resolved steps.
10. Capex and Opex do not publish Calendar entries in the initial scope.
11. Users can create, query, edit, and irreversibly delete Calendar-owned manual
    daily notes with the documented fields, validation, privacy rules, and
    `Private` creation default.
12. Month-grid indicators distinguish birthday, travel, note, and other event
    families, showing multiple compact indicators where practical and preserving
    complete day detail even if a priority fallback is used.
13. Calendar filters by source module and visual family affect indicators and day
    details without changing authorization behavior.
14. Projected entries can open their source module record when the source supplies
    a safe route or action descriptor, while manual notes open a Calendar-owned
    editor.
15. SQLite and PostgreSQL migrations for Calendar daily notes, backend
    unit/integration/architecture tests, frontend component tests, and a
    representative Playwright journey verify the supported behavior and privacy
    boundaries.

## Deferred Decisions

- Whether Travel itinerary entries should later appear in Calendar.
- Whether Capex or Opex should later publish optional financial calendar
  projections.
- Whether Recipes weekly menus should later appear in Calendar.
- Whether Calendar should eventually provide agenda, week, or year views in
  addition to the month grid.
- Whether manual notes should gain recurrence, attachments, tags, reminders, or
  links to source-module records.
- Whether Calendar should later expose launcher attention.
- Whether external calendar synchronization, import, export, or ICS feed support
  should ever be introduced.
- Whether projected entries should be cached or materialized for performance after
  representative data volumes exist.
