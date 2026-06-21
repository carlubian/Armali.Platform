/* global React */
// Firebird module — a personal register of known people. Sample people with
// their categories, fixed statuses, optional day/month birthdays, usernames and
// chronological interaction logs, plus the birthday helpers (calendar sort and
// the next-occurrence attention window). Exposed on window for the other babel
// scripts. No invented colors — everything maps onto Armali tokens.
(() => {

// ── Reference "today" ───────────────────────────────────────────
// Europe/Madrid, framed at 21 June 2026 so the birthday attention window
// (today … +7 natural days) reads naturally in the demo.
const TODAY = new Date(2026, 5, 21);
const MS_DAY = 86400000;

const MONTHS = ["January", "February", "March", "April", "May", "June",
  "July", "August", "September", "October", "November", "December"];
const MONTHS_SHORT = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
// Days per month ignoring the year; February allows 29 (02-29 is storable).
const DAYS_IN_MONTH = [31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

const isLeap = (y) => (y % 4 === 0 && y % 100 !== 0) || y % 400 === 0;

// Birthday is { m: 1–12, d: valid for month }. Shown day + month, never a year.
const fmtBirthday = (b) => (b ? `${b.d} ${MONTHS_SHORT[b.m - 1]}` : null);
const fmtBirthdayLong = (b) => (b ? `${b.d} ${MONTHS[b.m - 1]}` : null);

// Next upcoming calendar occurrence of a birthday relative to a date.
// A 02-29 birthday is observed on 03-01 in non-leap years.
function nextBirthday(b, from = TODAY) {
  if (!b) return null;
  const mk = (year) => {
    if (b.m === 2 && b.d === 29 && !isLeap(year)) return new Date(year, 2, 1); // 01 Mar
    return new Date(year, b.m - 1, b.d);
  };
  const y = from.getFullYear();
  const base = new Date(from.getFullYear(), from.getMonth(), from.getDate());
  let occ = mk(y);
  if (occ < base) occ = mk(y + 1);
  return occ;
}
function daysUntilBirthday(b, from = TODAY) {
  const occ = nextBirthday(b, from);
  if (!occ) return null;
  return Math.round((occ - new Date(from.getFullYear(), from.getMonth(), from.getDate())) / MS_DAY);
}
// Attention: next occurrence within today … today + 7 natural days (inclusive).
function birthdaySoon(b, from = TODAY) {
  const n = daysUntilBirthday(b, from);
  return n !== null && n >= 0 && n <= 7;
}
function birthdayRelative(b, from = TODAY) {
  const n = daysUntilBirthday(b, from);
  if (n === null) return null;
  if (n === 0) return "today";
  if (n === 1) return "tomorrow";
  return `in ${n} days`;
}
// Pure calendar order Jan→Dec (month asc, then day asc); no-birthday last.
function birthdayCompare(a, b) {
  if (!a && !b) return 0;
  if (!a) return 1;
  if (!b) return -1;
  return a.m - b.m || a.d - b.d;
}

// ── Fixed status enum (not configurable) ────────────────────────
const FB_STATUS = {
  Unknown:     { label: "Unknown",     tone: "neutral", icon: "circle-help" },
  Active:      { label: "Active",      tone: "success", icon: "circle-check" },
  Unavailable: { label: "Unavailable", tone: "gold",    icon: "circle-pause" },
  Blocked:     { label: "Blocked",     tone: "danger",  icon: "circle-slash" },
};
const FB_STATUS_ORDER = ["Unknown", "Active", "Unavailable", "Blocked"];

// ── Catalogues (owned by Firebird, managed in Configuration) ─────
// User convention: Cat A … Cat F. Each carries an order; a hue is assigned
// for gentle visual rhythm only (categories are abstract here).
const FB_CATEGORIES = [
  { value: "Cat A", order: 1, tone: "aqua" },
  { value: "Cat B", order: 2, tone: "azure" },
  { value: "Cat C", order: 3, tone: "gold" },
  { value: "Cat D", order: 4, tone: "sea" },
  { value: "Cat E", order: 5, tone: "rose" },
  { value: "Cat F", order: 6, tone: "neutral" },
];
const CATEGORY_TONE = Object.fromEntries(FB_CATEGORIES.map((c) => [c.value, c.tone]));

const FB_PLATFORMS = [
  { value: "Email",     icon: "mail" },
  { value: "Phone",     icon: "phone" },
  { value: "Discord",   icon: "message-circle" },
  { value: "Twitter",   icon: "bird" },
  { value: "Instagram", icon: "camera" },
  { value: "Other",     icon: "at-sign" },
];
const PLATFORM_ICON = Object.fromEntries(FB_PLATFORMS.map((p) => [p.value, p.icon]));

// ── Builders ────────────────────────────────────────────────────
let _uid = 0, _iid = 0;
const uname = (platform, value, notes) => ({ id: "u" + (++_uid), platform, value, notes: notes || null });
const inter = (date, description) => ({ id: "i" + (++_iid), date, description });

// ── Sample register ─────────────────────────────────────────────
// Contacts the household knows. Avatars are represented by warm initials
// (`avatar: true`) or a neutral placeholder (`avatar: false`).
const PEOPLE = [
  {
    id: "p-lucia", name: "Lucía Romero", category: "Cat A", status: "Active",
    birthday: { m: 6, d: 24 }, avatar: true, visibility: "Public",
    owner: "Marina Velasco", created: "2 Feb 2026", updated: "5 days ago",
    notes: "Met at the sailing club. Knows the harbour-master — useful for the summer berth.",
    usernames: [uname("Phone", "+34 655 102 233"), uname("Instagram", "@lucia.romero", "Posts mostly regattas.")],
    interactions: [
      inter("2026-06-15", "Coffee by the harbour — she offered to introduce us to the berth office."),
      inter("2026-05-30", "Called about the August regatta calendar."),
      inter("2026-03-08", "Ran into her at the chandlery; swapped numbers."),
    ],
  },
  {
    id: "p-tomas", name: "Tomás Iglesias", category: "Cat B", status: "Active",
    birthday: { m: 3, d: 14 }, avatar: true, visibility: "Public",
    owner: "Diego Salas", created: "11 Jan 2026", updated: "Yesterday",
    notes: "Plumber — reliable, does emergency call-outs. Quoted the bathroom re-pipe.",
    usernames: [uname("Email", "tomas.iglesias@fontaneria.es"), uname("Phone", "+34 600 778 145", "Best reached mornings.")],
    interactions: [
      inter("2026-06-19", "Confirmed he can start the bathroom job the first week of July."),
      inter("2026-04-22", "Fixed the kitchen leak — €80, paid in cash."),
    ],
  },
  {
    id: "p-aitana", name: "Aitana Cruz", category: "Cat A", status: "Unknown",
    birthday: null, avatar: false, visibility: "Public",
    owner: "Nora Quintana", created: "18 May 2026", updated: "1 month ago",
    notes: "New neighbour on the third floor. Still getting to know her.",
    usernames: [],
    interactions: [inter("2026-05-18", "Said hello in the lobby; she just moved in.")],
  },
  {
    id: "p-bruno", name: "Bruno Cano", category: "Cat C", status: "Active",
    birthday: { m: 6, d: 27 }, avatar: true, visibility: "Public",
    owner: "Marina Velasco", created: "3 Mar 2026", updated: "3 days ago",
    notes: "Accountant who handles the household tax return. Office near Plaza Mayor.",
    usernames: [uname("Email", "bruno@canoasesores.com"), uname("Twitter", "@bcano_fiscal", "Rarely checks DMs.")],
    interactions: [
      inter("2026-06-18", "Sent over the Renta documents for review."),
      inter("2026-02-10", "Annual planning meeting — agreed the quarterly schedule."),
    ],
  },
  {
    id: "p-greta", name: "Greta Lindqvist", category: "Cat D", status: "Active",
    birthday: { m: 12, d: 2 }, avatar: true, visibility: "Private",
    owner: "Nora Quintana", created: "22 Nov 2025", updated: "2 weeks ago",
    notes: "Old university friend, now in Malmö. Visits every winter.",
    usernames: [uname("Instagram", "@greta.lqv"), uname("Discord", "greta#4417")],
    interactions: [
      inter("2026-06-06", "Video call — planning her December visit."),
      inter("2025-12-20", "She stayed for the holidays; lovely week."),
    ],
  },
  {
    id: "p-mateo", name: "Mateo Ferrer", category: "Cat B", status: "Unavailable",
    birthday: { m: 9, d: 9 }, avatar: false, visibility: "Public",
    owner: "Hugo Belmonte", created: "9 Apr 2026", updated: "3 weeks ago",
    notes: "Electrician. Currently on a long job abroad — back in autumn.",
    usernames: [uname("Phone", "+34 678 221 909")],
    interactions: [inter("2026-04-15", "Said he's unavailable until September — abroad on a contract.")],
  },
  {
    id: "p-olivia", name: "Olivia Park", category: "Cat E", status: "Active",
    birthday: { m: 6, d: 23 }, avatar: true, visibility: "Public",
    owner: "Diego Salas", created: "27 Feb 2026", updated: "4 days ago",
    notes: "Lucía's friend — graphic designer. Helped with the apartment signage.",
    usernames: [
      uname("Discord", "olivia.park", "Personal."),
      uname("Discord", "studio.olivia", "Work account."),
      uname("Email", "hello@oliviapark.design"),
    ],
    interactions: [
      inter("2026-06-17", "Delivered the revised door-plate design."),
      inter("2026-05-02", "Quoted the signage job."),
    ],
  },
  {
    id: "p-samuel", name: "Samuel Ortiz", category: "Cat C", status: "Blocked",
    birthday: { m: 1, d: 30 }, avatar: true, visibility: "Private",
    owner: "Hugo Belmonte", created: "15 Dec 2025", updated: "2 months ago",
    notes: "Former contractor. Disputed final invoice — no further contact.",
    usernames: [uname("Phone", "+34 612 004 558")],
    interactions: [inter("2026-03-30", "Final message about the invoice dispute. Blocked afterwards.")],
  },
  {
    id: "p-nadia", name: "Nadia Haddad", category: "Cat A", status: "Active",
    birthday: { m: 2, d: 29 }, avatar: true, visibility: "Public",
    owner: "Marina Velasco", created: "8 Mar 2024", updated: "1 week ago",
    notes: "Doctor and close friend. Leap-day birthday — celebrated on 1 March off-years.",
    usernames: [uname("Phone", "+34 699 451 028"), uname("Instagram", "@dr.nadia.h")],
    interactions: [
      inter("2026-06-12", "Dinner together — caught up properly."),
      inter("2026-03-01", "Celebrated her birthday (observed)."),
    ],
  },
  {
    id: "p-edgar", name: "Édgar Molina", category: "Cat F", status: "Unknown",
    birthday: null, avatar: false, visibility: "Public",
    owner: "Diego Salas", created: "30 May 2026", updated: "3 weeks ago",
    notes: "Met briefly at a conference. Might be a useful contact for the workshop.",
    usernames: [uname("Email", "edgar.molina@workmail.com")],
    interactions: [inter("2026-05-29", "Exchanged cards at the woodworking fair.")],
  },
  {
    id: "p-carmen", name: "Carmen Sáez", category: "Cat B", status: "Active",
    birthday: { m: 8, d: 18 }, avatar: true, visibility: "Public",
    owner: "Nora Quintana", created: "5 Jan 2026", updated: "6 days ago",
    notes: "Family doctor's receptionist — schedules appointments quickly.",
    usernames: [uname("Phone", "+34 913 220 110", "Health centre line."), uname("Email", "cs.recepcion@centrosalud.es")],
    interactions: [inter("2026-06-14", "Booked the annual check-ups for the family.")],
  },
  {
    id: "p-ivan", name: "Iván Petrov", category: "Cat D", status: "Unavailable",
    birthday: { m: 11, d: 5 }, avatar: true, visibility: "Public",
    owner: "Hugo Belmonte", created: "2 Oct 2025", updated: "1 month ago",
    notes: "Cousin's husband. Travels constantly for work; hard to reach.",
    usernames: [uname("Twitter", "@ipetrov")],
    interactions: [inter("2026-05-10", "Brief message — he's between trips, will call when settled.")],
  },
  {
    id: "p-paula", name: "Paula Ferreira", category: "Cat A", status: "Active",
    birthday: { m: 7, d: 1 }, avatar: true, visibility: "Public",
    owner: "Marina Velasco", created: "19 Feb 2026", updated: "2 days ago",
    notes: "Neighbour and friend — waters the plants when we travel.",
    usernames: [uname("Instagram", "@paula.f"), uname("Email", "paula.ferreira@mail.pt")],
    interactions: [
      inter("2026-06-20", "Gave her the spare key for the July trip."),
      inter("2026-06-01", "Lunch on the terrace."),
    ],
  },
  {
    id: "p-hugo-m", name: "Hugo Marín", category: "Cat E", status: "Active",
    birthday: { m: 6, d: 28 }, avatar: false, visibility: "Public",
    owner: "Diego Salas", created: "12 Apr 2026", updated: "1 week ago",
    notes: "Football five-a-side organiser. Sets the weekly fixtures.",
    usernames: [uname("Phone", "+34 644 870 312")],
    interactions: [inter("2026-06-16", "Confirmed for Thursday's match.")],
  },
];

window.SEG_PEOPLE = PEOPLE;
window.SegFire = {
  TODAY, MONTHS, MONTHS_SHORT, DAYS_IN_MONTH, isLeap,
  FB_STATUS, FB_STATUS_ORDER, FB_CATEGORIES, CATEGORY_TONE, FB_PLATFORMS, PLATFORM_ICON,
  fmtBirthday, fmtBirthdayLong, nextBirthday, daysUntilBirthday, birthdaySoon,
  birthdayRelative, birthdayCompare,
};
})();
