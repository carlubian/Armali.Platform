/* global React */
// Segaris Analytics — module data + helpers.
// Analytics is a cross-domain *read* surface: it aggregates EUR-normalized
// income/expense projections published by the financial modules. For the
// mockup we model a single base year (2026) of household figures and derive
// any selected year + its previous year from a steady index, so the year
// navigator is fully live without shipping a table per year.
(() => {

// ── Current civil year (Europe/Madrid) ─────────────────────────
const CURRENT_YEAR = 2026;          // pinned for a stable mockup
const MIN_YEAR = 2019;
const MAX_YEAR = 2026;

const MONTHS = ["Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec"];

// ── Formatting ─────────────────────────────────────────────────
const _eur0 = new Intl.NumberFormat("en-IE", { style: "currency", currency: "EUR", maximumFractionDigits: 0 });
const fmtEUR = (n) => _eur0.format(Math.round(n || 0));
function fmtCompact(n) {
  const a = Math.abs(n);
  if (a >= 1000) return (n < 0 ? "-" : "") + "\u20AC" + (a / 1000).toFixed(a >= 10000 ? 0 : 1).replace(/\.0$/, "") + "k";
  return "\u20AC" + Math.round(n);
}
const fmtPct = (frac) => (frac * 100).toFixed(frac < 0.1 ? 1 : 0) + "%";
function fmtDelta(frac) {
  const s = frac > 0 ? "+" : "";
  return s + (frac * 100).toFixed(Math.abs(frac) < 0.1 ? 1 : 0) + "%";
}

// ── Year index model ───────────────────────────────────────────
// Overall household activity grows ~6%/yr; each line item carries its own
// year-over-year growth g (= selected / previous) so categories diverge.
const yearIndex = (year) => Math.pow(1.06, year - CURRENT_YEAR);

// Scale a base item list into {label, cur, prev} for a selected year.
// Base value `v` is the 2026 figure; `g` is that item's own YoY ratio.
function scaleSeries(items, year) {
  const S = yearIndex(year);
  return items.map((it) => {
    const cur = it.v * S;
    return { label: it.label || it.m, cur, prev: cur / it.g };
  });
}
const sum = (arr, k) => arr.reduce((a, b) => a + (b[k] || 0), 0);

// ── OVERVIEW — monthly expense & income (base 2026) ────────────
const OV_EXPENSE = [
  { m: "Jan", v: 4180, g: 1.11 }, { m: "Feb", v: 3920, g: 1.10 }, { m: "Mar", v: 5240, g: 1.17 },
  { m: "Apr", v: 4760, g: 1.13 }, { m: "May", v: 6130, g: 1.17 }, { m: "Jun", v: 5080, g: 1.07 },
  { m: "Jul", v: 5520, g: 1.10 }, { m: "Aug", v: 7240, g: 1.17 }, { m: "Sep", v: 4360, g: 1.08 },
  { m: "Oct", v: 5910, g: 1.13 }, { m: "Nov", v: 5380, g: 1.10 }, { m: "Dec", v: 8470, g: 1.15 },
];
const OV_INCOME = [
  { m: "Jan", v: 5200, g: 1.08 }, { m: "Feb", v: 5200, g: 1.08 }, { m: "Mar", v: 5400, g: 1.08 },
  { m: "Apr", v: 5200, g: 1.04 }, { m: "May", v: 5650, g: 1.13 }, { m: "Jun", v: 5200, g: 1.04 },
  { m: "Jul", v: 5200, g: 1.04 }, { m: "Aug", v: 5400, g: 1.04 }, { m: "Sep", v: 6900, g: 1.11 },
  { m: "Oct", v: 5200, g: 1.04 }, { m: "Nov", v: 5200, g: 1.04 }, { m: "Dec", v: 7800, g: 1.10 },
];

// Combined monthly frame: { label, exp, expPrev, inc, incPrev, net, netPrev }
function overviewMonthly(year) {
  const e = scaleSeries(OV_EXPENSE, year);
  const i = scaleSeries(OV_INCOME, year);
  return MONTHS.map((m, k) => ({
    label: m,
    exp: e[k].cur, expPrev: e[k].prev,
    inc: i[k].cur, incPrev: i[k].prev,
    net: i[k].cur - e[k].cur, netPrev: i[k].prev - e[k].prev,
  }));
}
function overviewTotals(year) {
  const f = overviewMonthly(year);
  const exp = sum(f, "exp"), expPrev = sum(f, "expPrev");
  const inc = sum(f, "inc"), incPrev = sum(f, "incPrev");
  return {
    expense: { cur: exp, prev: expPrev, delta: (exp - expPrev) / expPrev },
    income: { cur: inc, prev: incPrev, delta: (inc - incPrev) / incPrev },
    net: { cur: inc - exp, prev: incPrev - expPrev, delta: ((inc - exp) - (incPrev - expPrev)) / Math.abs(incPrev - expPrev) },
  };
}

// ── CAPEX — completed entries only ─────────────────────────────
const CX_EXP_CAT = [
  { label: "Property", v: 14200, g: 1.22 }, { label: "Renovation", v: 6900, g: 1.45 },
  { label: "Vehicles", v: 8600, g: 0.92 }, { label: "Electronics", v: 5400, g: 1.34 },
  { label: "Appliances", v: 4300, g: 1.05 }, { label: "Furniture", v: 3800, g: 1.18 },
];
const CX_INC_CAT = [
  { label: "Asset sale", v: 3200, g: 1.60 }, { label: "Insurance", v: 1450, g: 0.85 }, { label: "Grant", v: 900, g: 1.00 },
];
const CX_EXP_SUP = [
  { label: "Leroy Merlin", v: 7600, g: 1.30 }, { label: "IKEA", v: 4200, g: 1.12 },
  { label: "MediaMarkt", v: 3900, g: 1.28 }, { label: "Apple Store", v: 3100, g: 1.40 },
  { label: "Bosch", v: 2600, g: 1.05 }, { label: "Corte Inglés", v: 2400, g: 0.95 },
];
const CX_INC_SUP = [
  { label: "Wallapop", v: 1900, g: 1.50 }, { label: "Mapfre", v: 1450, g: 0.85 }, { label: "Ajuntament", v: 900, g: 1.00 },
];
const CX_EXP_CC = [
  { label: "Home", v: 18900, g: 1.20 }, { label: "Vehicle", v: 9100, g: 0.94 },
  { label: "Office", v: 6400, g: 1.30 }, { label: "Shared", v: 4200, g: 1.10 },
];
const CX_INC_CC = [
  { label: "Home", v: 3600, g: 1.30 }, { label: "Office", v: 1200, g: 1.10 }, { label: "Shared", v: 750, g: 0.90 },
];

// ── INVENTORY — received, non-cancelled orders (expense only) ──
const IN_EXP_ITEMCAT = [
  { label: "Groceries", v: 9200, g: 1.07 }, { label: "Pantry", v: 3400, g: 1.05 },
  { label: "Pet supplies", v: 2300, g: 1.20 }, { label: "Cleaning", v: 2100, g: 1.12 },
  { label: "Toiletries", v: 1600, g: 1.09 }, { label: "Office", v: 1400, g: 1.30 },
];
const IN_EXP_SUP = [
  { label: "Mercadona", v: 7100, g: 1.06 }, { label: "Amazon", v: 4600, g: 1.28 },
  { label: "Carrefour", v: 3900, g: 1.04 }, { label: "Costco", v: 3100, g: 1.15 },
  { label: "Lidl", v: 2400, g: 1.10 }, { label: "Corte Inglés", v: 1500, g: 0.98 },
];
// Average order value (EUR / order) by supplier.
const IN_AVG_ORDER = [
  { label: "Mercadona", v: 78, g: 1.04 }, { label: "Amazon", v: 64, g: 0.95 },
  { label: "Carrefour", v: 112, g: 1.06 }, { label: "Costco", v: 184, g: 1.10 },
  { label: "Lidl", v: 58, g: 1.02 }, { label: "Corte Inglés", v: 96, g: 1.00 },
];
const IN_TOP_ITEMS = [
  { label: "Coffee beans", v: 1240, g: 1.18 }, { label: "Olive oil", v: 1080, g: 1.07 },
  { label: "Dog food", v: 980, g: 1.22 }, { label: "Dishwasher tabs", v: 760, g: 1.10 },
  { label: "Paper towels", v: 620, g: 1.05 },
];
const IN_TOP_SUP = [
  { label: "Mercadona", v: 7100, g: 1.06 }, { label: "Amazon", v: 4600, g: 1.28 },
  { label: "Carrefour", v: 3900, g: 1.04 }, { label: "Costco", v: 3100, g: 1.15 },
  { label: "Lidl", v: 2400, g: 1.10 },
];
const inventoryTotalExpense = (year) => sum(scaleSeries(IN_EXP_SUP, year), "cur");

// ── CROSS-MODULE — totals across Capex, Opex, Inventory, Travel ─
const CM_EXP_SUP = [
  { label: "Amazon", v: 11200, g: 1.24 }, { label: "Mercadona", v: 7400, g: 1.06 },
  { label: "Iberia", v: 6800, g: 1.32 }, { label: "Endesa", v: 5100, g: 1.08 },
  { label: "IKEA", v: 4200, g: 1.12 }, { label: "Corte Inglés", v: 3900, g: 0.97 },
];
const CM_EXP_CAT = [
  { label: "Property", v: 14200, g: 1.22 }, { label: "Groceries", v: 9600, g: 1.07 },
  { label: "Travel", v: 9200, g: 1.30 }, { label: "Utilities", v: 7800, g: 1.06 },
  { label: "Electronics", v: 5400, g: 1.34 }, { label: "Cleaning", v: 2100, g: 1.12 },
];
const CM_EXP_CC = [
  { label: "Home", v: 24300, g: 1.14 }, { label: "Vehicle", v: 9100, g: 0.94 },
  { label: "Travel", v: 9200, g: 1.30 }, { label: "Office", v: 8600, g: 1.22 },
  { label: "Shared", v: 5200, g: 1.10 },
];

// ── Chart palette (hex — Recharts paints SVG, var(--*) is unreliable) ──
const PALETTE = {
  expense: "#3A7CA5",   // azure-500
  expensePrev: "#5E99BD",
  income: "#519B79",    // sea-500
  incomePrev: "#79B797",
  netPos: "#519B79",
  netNeg: "#CB5742",    // terracotta-500
  grid: "rgba(124,110,86,0.18)",
  axis: "#9A9081",      // ink-400
  ink: "#2C2823",
};

window.AN = {
  CURRENT_YEAR, MIN_YEAR, MAX_YEAR, MONTHS,
  fmtEUR, fmtCompact, fmtPct, fmtDelta,
  yearIndex, scaleSeries, sum, PALETTE,
  overviewMonthly, overviewTotals,
  CX_EXP_CAT, CX_INC_CAT, CX_EXP_SUP, CX_INC_SUP, CX_EXP_CC, CX_INC_CC,
  IN_EXP_ITEMCAT, IN_EXP_SUP, IN_AVG_ORDER, IN_TOP_ITEMS, IN_TOP_SUP, inventoryTotalExpense,
  CM_EXP_SUP, CM_EXP_CAT, CM_EXP_CC,
};
})();
