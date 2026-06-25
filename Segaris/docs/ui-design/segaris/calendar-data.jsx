/* global React */
// Calendar module — a cross-domain read view. Calendar projects date-bound
// entries published by other Segaris modules (Firebird birthdays, Travel trips,
// Inventory receipts, Assets end-of-life, Maintenance due dates, Process step
// due dates) and owns one persisted entity of its own: manual daily notes.
//
// Each projected entry is a read model — Calendar never mutates it. The four
// accepted visual families drive the month-grid indicators:
//   Birthday · Travel · Note · Other
// where Other covers Inventory / Assets / Maintenance / Processes.
//
// Sample data + date helpers, exposed on window for the other babel scripts.
// No invented colors — everything maps onto Armali tokens (see calendar.css).
(() => {

// ── Reference "today" (Europe/Madrid) ───────────────────────────
// Wednesday 24 Jun 2026 — inside the ISO week beginning Monday 22 Jun 2026,
// matching the Recipes module so the household clock agrees across Segaris.
const TODAY = new Date(2026, 5, 24);
const MONTHS = ["January", "February", "March", "April", "May", "June",
  "July", "August", "September", "October", "November", "December"];
const MONTHS_SHORT = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
// Monday-first weekday labels (Spain household convention).
const DOW_SHORT = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];
const DOW_LONG = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

// ── Visual families ─────────────────────────────────────────────
// The four accepted families. `prio` is the fallback priority used when a
// narrow cell can only show one indicator + a "more" marker.
const FAMILIES = {
  Travel:   { key: "Travel",   label: "Travel",   icon: "plane",       cls: "cal-fam--travel",   prio: 1 },
  Birthday: { key: "Birthday", label: "Birthday", icon: "cake",        cls: "cal-fam--birthday", prio: 2 },
  Note:     { key: "Note",     label: "Note",     icon: "sticky-note", cls: "cal-fam--note",     prio: 3 },
  Other:    { key: "Other",    label: "Other",    icon: "circle-dot",  cls: "cal-fam--other",    prio: 4 },
};
const FAMILY_ORDER = ["Travel", "Birthday", "Note", "Other"];

// ── Source modules (for grouping + the open-source action) ──────
const MODULES = {
  firebird:    { key: "firebird",    label: "Firebird",    icon: "contact-round" },
  travel:      { key: "travel",      label: "Travel",      icon: "plane" },
  inventory:   { key: "inventory",   label: "Inventory",   icon: "package" },
  assets:      { key: "assets",      label: "Assets",      icon: "armchair" },
  maintenance: { key: "maintenance", label: "Maintenance", icon: "wrench" },
  processes:   { key: "processes",   label: "Processes",   icon: "list-checks" },
  calendar:    { key: "calendar",    label: "Calendar",    icon: "sticky-note" },
};
// The modules a user can filter by (source filter). Calendar's own notes count.
const SOURCE_FILTERS = ["firebird", "travel", "inventory", "assets", "maintenance", "processes", "calendar"];

// ── Date helpers (Monday-first, civil dates) ────────────────────
const d = (day) => new Date(2026, 5, day);   // June 2026
const jd = (day) => new Date(2026, 6, day);  // July 2026
function ymd(date) {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
}
function sameDay(a, b) {
  return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
}
function addDays(date, n) { const x = new Date(date); x.setDate(x.getDate() + n); return x; }
function startOfDay(date) { return new Date(date.getFullYear(), date.getMonth(), date.getDate()); }
// Monday of the week containing `date` (0 = Monday).
function mondayOf(date) {
  const x = startOfDay(date);
  const dow = (x.getDay() + 6) % 7;
  x.setDate(x.getDate() - dow);
  return x;
}
// Whether `date` is inside an inclusive [start, end] civil range.
function inRange(date, start, end) {
  const t = startOfDay(date).getTime();
  return t >= startOfDay(start).getTime() && t <= startOfDay(end || start).getTime();
}
// The 35/42 cells covering a month, padded to whole Monday-first weeks.
function monthGrid(viewYear, viewMonth) {
  const first = new Date(viewYear, viewMonth, 1);
  const start = mondayOf(first);
  const last = new Date(viewYear, viewMonth + 1, 0);
  const endMon = mondayOf(last);
  const weeks = Math.round((endMon - start) / (7 * 86400000)) + 1;
  const cells = [];
  for (let i = 0; i < weeks * 7; i++) {
    const date = addDays(start, i);
    cells.push({ date, inMonth: date.getMonth() === viewMonth });
  }
  return cells;
}
function fmtLong(date) { return `${DOW_LONG[(date.getDay() + 6) % 7]}, ${date.getDate()} ${MONTHS[date.getMonth()]} ${date.getFullYear()}`; }
function fmtRange(start, end) {
  if (!end || sameDay(start, end)) return `${start.getDate()} ${MONTHS_SHORT[start.getMonth()]}`;
  const sameMonth = start.getMonth() === end.getMonth();
  return sameMonth
    ? `${start.getDate()}–${end.getDate()} ${MONTHS_SHORT[end.getMonth()]}`
    : `${start.getDate()} ${MONTHS_SHORT[start.getMonth()]} – ${end.getDate()} ${MONTHS_SHORT[end.getMonth()]}`;
}
function tripDays(start, end) { return Math.round((startOfDay(end) - startOfDay(start)) / 86400000) + 1; }

// ── Projected entries + manual notes ────────────────────────────
// `family` drives the indicator; `module` drives grouping & the open action.
// `route` is the source action descriptor — null means informational only.
// Notes (module "calendar") are the only editable, Calendar-owned entries.
let _eid = 0;
const E = (o) => ({ id: "cal" + (++_eid), ...o });

const ENTRIES = [
  // ── Firebird · birthdays (recurring; next occurrence in range) ──
  E({ module: "firebird", family: "Birthday", type: "Birthday", start: d(6),
      title: "Diego Salas", supporting: "Turns 41", status: "Recurring", route: "Open in Firebird" }),
  E({ module: "firebird", family: "Birthday", type: "Birthday", start: d(19),
      title: "Marina Velasco", supporting: "Turns 38", status: "Recurring", route: "Open in Firebird" }),
  E({ module: "firebird", family: "Birthday", type: "Birthday", start: d(24),
      title: "Abuela Carmen", supporting: "Turns 79", status: "Recurring", route: "Open in Firebird" }),
  E({ module: "firebird", family: "Birthday", type: "Birthday", start: d(30),
      title: "Lucía Marín", supporting: "Turns 9", status: "Recurring", route: "Open in Firebird" }),
  E({ module: "firebird", family: "Birthday", type: "Birthday", start: jd(3),
      title: "Hugo Belmonte", supporting: "Turns 52", status: "Recurring", route: "Open in Firebird" }),

  // ── Travel · trips (continuous all-day spans; not Cancelled) ────
  E({ module: "travel", family: "Travel", type: "Trip", start: d(12), end: d(15),
      title: "Lisbon long weekend", supporting: "Marina & Diego", status: "Completed", route: "Open in Travel" }),
  E({ module: "travel", family: "Travel", type: "Trip", start: d(24), end: d(28),
      title: "Girona & the Costa Brava", supporting: "Whole household", status: "Confirmed", route: "Open in Travel" }),
  E({ module: "travel", family: "Travel", type: "Trip", start: jd(2), end: jd(4),
      title: "Work trip · Munich", supporting: "Diego", status: "Planned", route: "Open in Travel" }),

  // ── Inventory · expected receipt (Planning / Active) ────────────
  E({ module: "inventory", family: "Other", type: "Expected receipt", start: d(9),
      title: "Monthly pantry restock", supporting: "Order INV-ORD-118", status: "Active", route: "Open in Inventory" }),
  E({ module: "inventory", family: "Other", type: "Expected receipt", start: d(26),
      title: "New mattress delivery", supporting: "Order INV-ORD-121", status: "Planning", route: "Open in Inventory" }),

  // ── Assets · expected end-of-life (not Retired; past may show) ──
  E({ module: "assets", family: "Other", type: "Expected end of life", start: d(18),
      title: "Kitchen dishwasher", supporting: "Bosch · in service since 2017", status: "In service", route: "Open in Assets" }),
  E({ module: "assets", family: "Other", type: "Expected end of life", start: jd(3),
      title: "Road bike", supporting: "Diego's commuter", status: "In service", route: "Open in Assets" }),

  // ── Maintenance · due dates (Pending / InProgress) ──────────────
  E({ module: "maintenance", family: "Other", type: "Task due", start: d(16),
      title: "Boiler annual service", supporting: "Assigned to Marina", status: "Pending", route: "Open in Maintenance" }),
  E({ module: "maintenance", family: "Other", type: "Task due", start: d(24),
      title: "Replace smoke-alarm batteries", supporting: "Hallway & landing units", status: "In progress", route: "Open in Maintenance" }),
  E({ module: "maintenance", family: "Other", type: "Task due", start: jd(1),
      title: "Car ITV inspection", supporting: "Renault — booked 09:30", status: "Pending", route: "Open in Maintenance" }),

  // ── Processes · pending step due dates (not the global due) ─────
  E({ module: "processes", family: "Other", type: "Step due", start: d(10),
      title: "Mortgage renewal · Submit documents", supporting: "Step 2 of 5", status: "Pending", route: "Open in Processes" }),
  E({ module: "processes", family: "Other", type: "Step due", start: d(24),
      title: "Passport renewal · Book appointment", supporting: "Step 1 of 4", status: "Pending", route: "Open in Processes" }),
  E({ module: "processes", family: "Other", type: "Step due", start: d(29),
      title: "Mortgage renewal · Sign with notary", supporting: "Step 4 of 5", status: "Pending", route: "Open in Processes" }),

  // ── Calendar · manual daily notes (the only owned entity) ───────
  E({ module: "calendar", family: "Note", type: "Note", start: d(5), isNote: true,
      title: "Pay the community fees", supporting: "Transfer before the 7th to avoid the surcharge.",
      visibility: "Private", owner: "Marina Velasco", updated: "3 weeks ago" }),
  E({ module: "calendar", family: "Note", type: "Note", start: d(19), isNote: true,
      title: "Book a table for Marina's birthday", supporting: "Somewhere by the water — 6 of us, around 21:00.",
      visibility: "Public", owner: "Diego Salas", updated: "5 days ago" }),
  E({ module: "calendar", family: "Note", type: "Note", start: d(24), isNote: true,
      title: "Call the plumber about the leak", supporting: "Under the kitchen sink — getting worse. Ask about Thursday.",
      visibility: "Private", owner: "Marina Velasco", updated: "2 hours ago" }),
  E({ module: "calendar", family: "Note", type: "Note", start: d(24), isNote: true,
      title: "Bins out tonight", supporting: "Recycling week.",
      visibility: "Public", owner: "Diego Salas", updated: "Yesterday" }),
  E({ module: "calendar", family: "Note", type: "Note", start: d(27), isNote: true,
      title: "Pick up the dry cleaning", supporting: "Ticket on the fridge.",
      visibility: "Private", owner: "Nora Quintana", updated: "1 week ago" }),
];

// ── Query helpers ───────────────────────────────────────────────
// Entries intersecting a single day (trips span their whole range).
function entriesOnDay(date, { sources, families } = {}) {
  return ENTRIES.filter((e) => {
    if (sources && !sources.has(e.module)) return false;
    if (families && !families.has(e.family)) return false;
    return inRange(date, e.start, e.end);
  });
}
// The distinct families present on a day (for the compact indicators).
function familiesOnDay(date, filters) {
  const set = new Set(entriesOnDay(date, filters).map((e) => e.family));
  return FAMILY_ORDER.filter((f) => set.has(f));
}
// Final presentation order: family → date → module → title.
function orderEntries(list) {
  return [...list].sort((a, b) =>
    FAMILIES[a.family].prio - FAMILIES[b.family].prio ||
    a.start - b.start ||
    a.module.localeCompare(b.module) ||
    a.title.localeCompare(b.title));
}
// Position of `date` within a trip span → for continuous-bar rendering.
function tripSegment(e, date) {
  const isStart = sameDay(date, e.start);
  const isEnd = sameDay(date, e.end || e.start);
  const dow = (date.getDay() + 6) % 7; // 0 = Monday
  return { isStart, isEnd, weekStart: dow === 0, weekEnd: dow === 6 };
}

window.CalData = {
  TODAY, MONTHS, MONTHS_SHORT, DOW_SHORT, DOW_LONG,
  FAMILIES, FAMILY_ORDER, MODULES, SOURCE_FILTERS, ENTRIES,
  d, jd, ymd, sameDay, addDays, startOfDay, mondayOf, inRange, monthGrid,
  fmtLong, fmtRange, tripDays, entriesOnDay, familiesOnDay, orderEntries, tripSegment,
};
})();
