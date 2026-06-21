/* global React */
// Processes module — sample procedures + derived-state helpers. Exposed on
// window for the other babel scripts. A Process owns an ordered list of Steps
// completed strictly in sequence (the "frontier" model). Status is derived,
// except a manual terminal Cancelled override.
(() => {

// ── Reference date ──────────────────────────────────────────────
// The whole demo is framed around "today" so due dates read naturally
// (overdue / due-soon / later). Europe/Madrid, mid-June 2026.
const TODAY = new Date(2026, 5, 20);
const MS_DAY = 86400000;

const MONTHS = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
const parseDate = (s) => { if (!s) return null; const [y, m, d] = s.split("-").map(Number); return new Date(y, m - 1, d); };
const fmtDate = (s) => { const d = parseDate(s); return d ? `${d.getDate()} ${MONTHS[d.getMonth()]} ${d.getFullYear()}` : null; };
const fmtShort = (s) => { const d = parseDate(s); return d ? `${d.getDate()} ${MONTHS[d.getMonth()]}` : null; };
const daysUntil = (s) => { const d = parseDate(s); return d ? Math.round((d - TODAY) / MS_DAY) : null; };

// Urgency band of a civil date against today (inclusive 7-day window).
function dueUrgency(s) {
  const n = daysUntil(s);
  if (n === null) return "none";
  if (n < 0) return "overdue";
  if (n <= 7) return "soon";
  return "later";
}
// Human relative phrase ("4 days overdue", "in 6 days", "today").
function dueRelative(s) {
  const n = daysUntil(s);
  if (n === null) return null;
  if (n === 0) return "due today";
  if (n === 1) return "due tomorrow";
  if (n === -1) return "1 day overdue";
  if (n < 0) return `${-n} days overdue`;
  return `in ${n} days`;
}

// ── Step / status helpers ──────────────────────────────────────
const isResolved = (st) => st === "completed" || st === "skipped";

// The frontier: index of the first step that is neither completed nor skipped.
// Returns steps.length when every step is resolved (process complete).
function frontierIndex(steps) {
  const i = steps.findIndex((s) => !isResolved(s.state));
  return i === -1 ? steps.length : i;
}

// Index of the most-recently resolved step (the only one that may be undone).
function lastResolvedIndex(steps) {
  const f = frontierIndex(steps);
  return f - 1; // -1 when nothing resolved
}

// Derived status — Cancelled overrides everything.
function deriveStatus(p) {
  if (p.cancelled) return "Cancelled";
  const steps = p.steps || [];
  if (steps.length === 0) return "NotStarted";
  const resolved = steps.filter((s) => isResolved(s.state)).length;
  const allDone = steps.every((s) => (s.optional ? isResolved(s.state) : s.state === "completed"));
  if (allDone) return "Completed";
  if (resolved === 0) return "NotStarted";
  return "InProgress";
}

const PROC_STATUS = {
  NotStarted: { label: "Not started", tone: "neutral", icon: "circle-dashed" },
  InProgress: { label: "In progress", tone: "aqua",    icon: "loader" },
  Completed:  { label: "Completed",   tone: "success", icon: "circle-check-big" },
  Cancelled:  { label: "Cancelled",   tone: "danger",  icon: "ban" },
};
const PROC_STATUS_ORDER = ["NotStarted", "InProgress", "Completed", "Cancelled"];

function progress(p) {
  const steps = p.steps || [];
  const resolved = steps.filter((s) => isResolved(s.state)).length;
  return { resolved, total: steps.length };
}

// Effective due date = global due date, else the frontier step's due date.
function effectiveDue(p) {
  if (p.due) return { date: p.due, source: "process" };
  const steps = p.steps || [];
  const f = frontierIndex(steps);
  const fs = steps[f];
  if (fs && fs.due) return { date: fs.due, source: "step" };
  return { date: null, source: null };
}

// Launcher attention: open process whose effective due date is overdue or
// within the next 7 days. Completed / Cancelled never qualify.
function needsAttention(p) {
  const status = deriveStatus(p);
  if (status === "Completed" || status === "Cancelled") return false;
  const checks = [p.due];
  const steps = p.steps || [];
  const fs = steps[frontierIndex(steps)];
  if (fs) checks.push(fs.due);
  return checks.some((d) => { const u = dueUrgency(d); return u === "overdue" || u === "soon"; });
}

const PROC_CATEGORIES = [
  { value: "Administrative", icon: "stamp" },
  { value: "Legal",          icon: "scale" },
  { value: "Tax",            icon: "landmark" },
  { value: "Health",         icon: "heart-pulse" },
  { value: "Education",      icon: "graduation-cap" },
  { value: "Vehicle",        icon: "car" },
  { value: "Housing",        icon: "house" },
  { value: "Other",          icon: "folder" },
];
const CATEGORY_ICON = Object.fromEntries(PROC_CATEGORIES.map((c) => [c.value, c.icon]));

// ── Step + process builders ────────────────────────────────────
let _sid = 0;
const step = (description, state, due, opts = {}) => ({
  id: "s" + (++_sid), description, state, due: due || null,
  optional: !!opts.optional, notes: opts.notes || null,
});

// ── Sample procedures (real-world bureaucratic / household) ─────
const PROCESSES = [
  {
    id: "pc-pass", name: "Renew Spanish passport", category: "Administrative",
    due: null, visibility: "Public", cancelled: false,
    owner: "Marina Velasco", created: "2 May 2026", updated: "3 days ago",
    notes: "Current passport expires 14 Aug 2026 — renew before the summer trip.",
    attachments: [
      { id: "a1", name: "Old passport scan.pdf", kind: "pdf", size: "1.2 MB" },
      { id: "a2", name: "Padrón certificate.pdf", kind: "pdf", size: "240 KB" },
      { id: "a3", name: "Passport photo.jpg", kind: "image", size: "820 KB" },
    ],
    steps: [
      step("Gather required documents (DNI, padrón, photo)", "completed", "2026-05-12"),
      step("Book an appointment at the Comisaría online", "completed", "2026-05-28"),
      step("Request expedited processing", "skipped", null, { optional: true, notes: "Not needed — standard timeline is fine." }),
      step("Attend appointment and submit the application", "pending", "2026-06-24", { notes: "Bring originals and the fee receipt." }),
      step("Pay the issuance fee (€30)", "pending", "2026-06-26"),
      step("Collect the new passport", "pending", "2026-07-10"),
    ],
  },
  {
    id: "pc-mort", name: "Mortgage application — Calle Robles flat", category: "Housing",
    due: "2026-07-15", visibility: "Private", cancelled: false,
    owner: "Diego Salas", created: "11 Apr 2026", updated: "Yesterday",
    notes: "Pre-approval valid for 60 days. Notary slot tentatively held for mid-July.",
    attachments: [
      { id: "a1", name: "Payslips — last 6 months.zip", kind: "zip", size: "4.1 MB" },
      { id: "a2", name: "Pre-approval letter.pdf", kind: "pdf", size: "180 KB" },
    ],
    steps: [
      step("Collect financial paperwork (payslips, tax return)", "completed", "2026-04-20"),
      step("Obtain bank pre-approval", "completed", "2026-05-05"),
      step("Commission the property valuation", "completed", "2026-05-30"),
      step("Submit the full mortgage application", "pending", "2026-06-22", { notes: "Upload the signed valuation report with the form." }),
      step("Negotiate conditions and interest rate", "pending", "2026-07-02", { optional: true }),
      step("Sign the deed before the notary", "pending", "2026-07-15"),
    ],
  },
  {
    id: "pc-resid", name: "Residency permit (TIE) renewal", category: "Legal",
    due: "2026-06-18", visibility: "Public", cancelled: false,
    owner: "Nora Quintana", created: "20 Mar 2026", updated: "6 days ago",
    notes: "Renewal window opened 60 days before expiry. Frontier step is now overdue.",
    attachments: [{ id: "a1", name: "Form EX-17.pdf", kind: "pdf", size: "96 KB" }],
    steps: [
      step("Complete form EX-17", "completed", "2026-04-10"),
      step("Pay the administrative fee (Tasa 790-012)", "completed", "2026-04-18"),
      step("Submit renewal at the extranjería office", "pending", "2026-06-12", { notes: "Office requires a prior cita — book early." }),
      step("Provide fingerprints for the new card", "pending", "2026-07-01"),
      step("Collect the renewed TIE card", "pending", "2026-07-20"),
    ],
  },
  {
    id: "pc-tax", name: "Annual income tax return (Renta 2025)", category: "Tax",
    due: "2026-06-30", visibility: "Private", cancelled: false,
    owner: "Marina Velasco", created: "8 May 2026", updated: "2 days ago",
    notes: "Draft (borrador) accepted. Confirm before the 30 June deadline.",
    attachments: [{ id: "a1", name: "Borrador Renta 2025.pdf", kind: "pdf", size: "640 KB" }],
    steps: [
      step("Gather income certificates and deductions", "completed", "2026-05-18"),
      step("Review the draft return (borrador)", "completed", "2026-06-05"),
      step("Confirm and file the return", "pending", "2026-06-28", { notes: "Result is a small refund — no payment due." }),
    ],
  },
  {
    id: "pc-licence", name: "Renew driving licence", category: "Vehicle",
    due: null, visibility: "Public", cancelled: false,
    owner: "Hugo Belmonte", created: "3 Feb 2026", updated: "14 Mar 2026",
    notes: "Completed ahead of expiry. Keeping as history.",
    attachments: [{ id: "a1", name: "Medical certificate.pdf", kind: "pdf", size: "210 KB" }],
    steps: [
      step("Pass the psychotechnical medical exam", "completed", "2026-02-10"),
      step("Pay the renewal fee at the DGT", "completed", "2026-02-18"),
      step("Receive the provisional licence", "completed", "2026-02-19"),
      step("Receive the definitive card by post", "completed", "2026-03-12"),
    ],
  },
  {
    id: "pc-school", name: "Enrol Lucía in secondary school", category: "Education",
    due: "2026-09-01", visibility: "Public", cancelled: false,
    owner: "Diego Salas", created: "12 Jun 2026", updated: "Today",
    notes: "Enrolment period opens in September. Container created early to plan.",
    attachments: [],
    steps: [
      step("Request the school place application form", "pending", "2026-09-01"),
      step("Submit the application with documents", "pending", "2026-09-08"),
      step("Confirm the assigned place", "pending", "2026-09-20"),
    ],
  },
  {
    id: "pc-health", name: "Transfer health card to new region", category: "Health",
    due: null, visibility: "Public", cancelled: false,
    owner: "Nora Quintana", created: "1 Jun 2026", updated: "5 days ago",
    notes: "Moved to a new autonomous community — re-register with the local health centre.",
    attachments: [],
    steps: [
      step("Register at the new health centre", "completed", "2026-06-09"),
      step("Request the new health card (tarjeta sanitaria)", "pending", "2026-06-23"),
      step("Assign a family doctor", "pending", "2026-07-05"),
    ],
  },
  {
    id: "pc-padron", name: "Update padrón after moving house", category: "Administrative",
    due: null, visibility: "Public", cancelled: false,
    owner: "Marina Velasco", created: "28 May 2026", updated: "1 week ago",
    notes: "",
    attachments: [{ id: "a1", name: "Rental contract.pdf", kind: "pdf", size: "1.4 MB" }],
    steps: [
      step("Book a town-hall appointment", "pending", "2026-06-19"),
      step("Bring the rental contract and ID", "pending", "2026-06-19"),
      step("Collect the new padrón certificate", "pending", "2026-07-02"),
    ],
  },
  {
    id: "pc-inherit", name: "Inheritance acceptance and tax", category: "Legal",
    due: null, visibility: "Private", cancelled: true,
    owner: "Hugo Belmonte", created: "15 Jan 2026", updated: "2 Apr 2026",
    notes: "Procedure abandoned — handled jointly by another family member instead.",
    attachments: [],
    steps: [
      step("Obtain the death and last-will certificates", "completed", "2026-01-25"),
      step("Inventory the estate assets", "completed", "2026-02-12"),
      step("Sign acceptance before the notary", "pending", null),
      step("Pay inheritance tax", "pending", null),
    ],
  },
];

window.SEG_PROCESSES = PROCESSES;
window.SegProc = {
  TODAY, PROC_STATUS, PROC_STATUS_ORDER, PROC_CATEGORIES, CATEGORY_ICON,
  parseDate, fmtDate, fmtShort, daysUntil, dueUrgency, dueRelative,
  isResolved, frontierIndex, lastResolvedIndex, deriveStatus, progress,
  effectiveDue, needsAttention,
};
})();
