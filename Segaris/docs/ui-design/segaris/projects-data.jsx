/* global React */
// Projects module — sample hierarchy + helpers. Exposed on window for the
// other babel scripts. Program → Axis → (Project | Activity). Numbers are
// global and unique; the unified identifier is computed on demand.
(() => {

// Fixed status enum (descriptive only). aqua is the Badge default (no class).
const PROJ_STATUS = {
  Planning:  { label: "Planning",  tone: "neutral" },
  Active:    { label: "Active",    tone: "aqua"    },
  Completed: { label: "Completed", tone: "success" },
  OnHold:    { label: "On hold",   tone: "gold"    },
  Cancelled: { label: "Cancelled", tone: "danger"  },
};
const PROJ_STATUS_ORDER = ["Planning", "Active", "Completed", "OnHold", "Cancelled"];

// Risk band from a computed score (score = probability × impact × mitigation).
function riskBand(score) {
  if (score >= 100) return "high";
  if (score >= 60)  return "medium";
  return "low";
}
const RISK_BAND = {
  low:    { label: "Low",    tone: "success", color: "var(--sea-500)",        soft: "var(--sea-100)"    },
  medium: { label: "Medium", tone: "gold",    color: "var(--gold-500)",       soft: "var(--gold-100)"   },
  high:   { label: "High",   tone: "danger",  color: "var(--terracotta-500)", soft: "var(--danger-soft)" },
};

// Compute the six-digit, zero-padded number string.
const padNum = (n) => String(n).padStart(6, "0");

// Unified identifier: PPPPAAAA-123456  (the code half — name appended by caller)
const unifiedCode = (programCode, axisCode, number) =>
  `${programCode}${axisCode}-${padNum(number)}`;

let _r = 0;
const risk = (description, p, i, m) => {
  const score = p * i * m;
  return { id: "r" + (++_r), description, p, i, m, score, band: riskBand(score) };
};

// File attachments (result files). No thumbnails — text + type only.
const file = (name, kind, size) => ({ id: name, name, kind, size });

// ── The tree ───────────────────────────────────────────────────
// Programs & axes are read-only structure (managed in Configuration).
const PROGRAMS = [
  {
    id: "pg-home", code: "HOME", name: "Home & property",
    axes: [
      {
        id: "ax-ktch", code: "KTCH", name: "Kitchen renovation",
        items: [
          {
            id: "it-142", type: "project", number: 142, name: "Kitchen refit",
            status: "Active", visibility: "Public",
            owner: "Marina Velasco", created: "8 Jan 2026", updated: "4 minutes ago",
            risks: [
              risk("Water damage found under the sink during demolition", 4, 5, 5),
              risk("Asbestos behind old units forces a stop-work order", 3, 5, 4),
              risk("Worktop delivery slips past the install date", 3, 3, 3),
            ],
            files: [
              file("Final layout plan.pdf", "pdf", "2.4 MB"),
              file("Cabinet quote — Howdens.pdf", "pdf", "480 KB"),
              file("Before & after photos.zip", "zip", "18.2 MB"),
              file("Worktop invoice.pdf", "pdf", "96 KB"),
            ],
          },
          {
            id: "it-143", type: "activity", number: 143, name: "Replace tap washers",
            status: "Completed", visibility: "Public",
            owner: "Diego Salas", created: "10 Jan 2026", updated: "12 Jan 2026",
          },
          {
            id: "it-151", type: "project", number: 151, name: "Install backsplash tiles",
            status: "Planning", visibility: "Public",
            owner: "Marina Velasco", created: "2 Feb 2026", updated: "Yesterday",
            risks: [
              risk("Tiles arrive cracked and need reordering", 3, 3, 3),
              risk("Wall is uneven and needs prep work", 3, 3, 4),
            ],
            files: [ file("Tile sample board.jpg", "image", "3.1 MB") ],
          },
        ],
      },
      {
        id: "ax-grdn", code: "GRDN", name: "Garden & outdoors",
        items: [
          {
            id: "it-118", type: "project", number: 118, name: "Build raised vegetable beds",
            status: "Active", visibility: "Public",
            owner: "Diego Salas", created: "21 Nov 2025", updated: "3 days ago",
            risks: [ risk("Poor soil quality stunts the first harvest", 3, 3, 4) ],
            files: [ file("Bed dimensions.pdf", "pdf", "210 KB") ],
          },
          {
            id: "it-124", type: "activity", number: 124, name: "Prune the fruit trees",
            status: "OnHold", visibility: "Public",
            owner: "Nora Quintana", created: "4 Dec 2025", updated: "9 Dec 2025",
          },
        ],
      },
    ],
  },
  {
    id: "pg-well", code: "WELL", name: "Health & wellbeing",
    axes: [
      {
        id: "ax-fitn", code: "FITN", name: "Fitness",
        items: [
          {
            id: "it-087", type: "project", number: 87, name: "Run a half marathon",
            status: "Active", visibility: "Private",
            owner: "Marina Velasco", created: "5 Oct 2025", updated: "Today",
            risks: [
              risk("Knee injury flares during peak mileage weeks", 4, 4, 4),
              risk("Race cancelled by extreme weather", 2, 5, 3),
            ],
            files: [
              file("Training plan — 16 weeks.pdf", "pdf", "640 KB"),
              file("Race registration.pdf", "pdf", "88 KB"),
            ],
          },
          {
            id: "it-090", type: "activity", number: 90, name: "Buy running shoes",
            status: "Completed", visibility: "Public",
            owner: "Marina Velasco", created: "8 Oct 2025", updated: "9 Oct 2025",
          },
        ],
      },
      {
        id: "ax-nutr", code: "NUTR", name: "Nutrition",
        items: [
          {
            id: "it-099", type: "activity", number: 99, name: "Meal-prep Sundays",
            status: "Active", visibility: "Public",
            owner: "Nora Quintana", created: "1 Nov 2025", updated: "2 days ago",
          },
          {
            id: "it-101", type: "project", number: 101, name: "Cut sugar for 90 days",
            status: "OnHold", visibility: "Private",
            owner: "Diego Salas", created: "12 Nov 2025", updated: "1 week ago",
            risks: [ risk("Social events derail the streak", 4, 3, 3) ],
            files: [],
          },
        ],
      },
    ],
  },
  {
    id: "pg-grow", code: "GROW", name: "Learning & career",
    axes: [
      {
        id: "ax-skil", code: "SKIL", name: "Skills & study",
        items: [
          {
            id: "it-063", type: "project", number: 63, name: "Learn Spanish to B2",
            status: "Active", visibility: "Public",
            owner: "Marina Velasco", created: "3 Sep 2025", updated: "5 hours ago",
            risks: [
              risk("Motivation drops without a study partner", 4, 3, 4),
              risk("Travel plans interrupt the study streak", 3, 3, 3),
            ],
            files: [
              file("B2 mock exam results.pdf", "pdf", "1.1 MB"),
              file("Vocabulary deck.csv", "csv", "42 KB"),
              file("Tutor agreement.pdf", "pdf", "120 KB"),
            ],
          },
          {
            id: "it-071", type: "activity", number: 71, name: "Finish typing course",
            status: "Cancelled", visibility: "Public",
            owner: "Hugo Belmonte", created: "16 Sep 2025", updated: "1 Oct 2025",
          },
        ],
      },
      {
        id: "ax-crer", code: "CRER", name: "Career moves",
        items: [
          {
            id: "it-110", type: "project", number: 110, name: "Switch to a product role",
            status: "Planning", visibility: "Private",
            owner: "Diego Salas", created: "19 Nov 2025", updated: "Yesterday",
            risks: [
              risk("Internal opening freezes during the reorg", 4, 5, 5),
              risk("Skills gap in analytics shows in interviews", 4, 4, 4),
              risk("Current manager blocks the transfer", 3, 4, 4),
            ],
            files: [ file("Updated CV.pdf", "pdf", "180 KB") ],
          },
        ],
      },
    ],
  },
  {
    id: "pg-fnce", code: "FNCE", name: "Finances",
    axes: [
      {
        id: "ax-save", code: "SAVE", name: "Savings & investing",
        items: [
          {
            id: "it-045", type: "project", number: 45, name: "Build a 6-month emergency fund",
            status: "Active", visibility: "Private",
            owner: "Marina Velasco", created: "2 Sep 2025", updated: "Today",
            risks: [ risk("Unexpected car repair drains the fund", 4, 4, 3) ],
            files: [
              file("Savings tracker.xlsx", "sheet", "64 KB"),
              file("Bank statement Q1.pdf", "pdf", "320 KB"),
            ],
          },
          {
            id: "it-052", type: "activity", number: 52, name: "Cancel unused subscriptions",
            status: "Completed", visibility: "Public",
            owner: "Nora Quintana", created: "9 Sep 2025", updated: "14 Sep 2025",
          },
        ],
      },
    ],
  },
];

// File-kind → lucide icon
const FILE_ICON = {
  pdf: "file-text", image: "file-image", zip: "file-archive",
  csv: "file-spreadsheet", sheet: "file-spreadsheet", doc: "file-text",
};

// Risk-band summary for a project: counts of low / medium / high.
function bandSummary(risks) {
  const out = { low: 0, medium: 0, high: 0 };
  (risks || []).forEach((r) => { out[r.band]++; });
  return out;
}

window.SEG_PROJECTS = PROGRAMS;
window.SegProj = {
  PROJ_STATUS, PROJ_STATUS_ORDER, RISK_BAND, FILE_ICON,
  riskBand, bandSummary, unifiedCode, padNum,
};
})();
