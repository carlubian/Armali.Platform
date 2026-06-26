/* global React */
// Segaris Analytics — screens.
// The module shell (top bar + tab row + year navigator) and the four tab
// surfaces selected for this mock: Overview, Capex, Inventory, Cross-module.
// Tabs and the year navigator are live; previous/next re-derive every chart
// from the year index in analytics-data, mirroring the spec's "recompute on
// open" model. Lazy loading is represented per-tab (only the active tab's
// charts mount).
(() => {
const AN = window.AN;
const { fmtEUR, fmtDelta, scaleSeries, sum, PALETTE } = AN;
const Icon = window.SegIcon;
const { ANChartCard, ANYoYLegend, ANBarsYoY, ANLineYoY, ANNetBars, ANRankBars, anMonthlyPair } = window;
const SegShellTopBar = window.SegShellTopBar;

// ── Table + summary builders (accessible equivalents) ──────────
function makeTable(dimension, scaled, year) {
  return {
    caption: `${dimension} — ${year} versus ${year - 1}, EUR.`,
    columns: [dimension, String(year), String(year - 1), "YoY"],
    rows: scaled.map((r) => [r.label, fmtEUR(r.cur), fmtEUR(r.prev), fmtDelta((r.cur - r.prev) / r.prev)]),
  };
}
function rankTable(dimension, scaled, year, total) {
  return {
    caption: `${dimension} — top ${scaled.length} for ${year}, with share of total and ${year - 1} comparison.`,
    columns: [dimension, String(year), "% of total", String(year - 1)],
    rows: scaled.map((r) => [r.label, fmtEUR(r.cur), AN.fmtPct(r.cur / total), fmtEUR(r.prev)]),
  };
}
function makeSummary(title, scaled, year) {
  const top = scaled.slice(0, 3).map((r) => `${r.label} ${fmtEUR(r.cur)}`).join(", ");
  const tc = sum(scaled, "cur"), tp = sum(scaled, "prev");
  return `${title}. In ${year}: ${top}${scaled.length > 3 ? ", and more" : ""}. Total ${fmtEUR(tc)} versus ${fmtEUR(tp)} in ${year - 1} (${fmtDelta((tc - tp) / tp)}).`;
}

// ── Reusable categorical bar card ──────────────────────────────
function BarCard({ eyebrow, icon, title, sub, base, year, color, prevStyle, barSize, unit, height = "an-chart--md", className }) {
  const scaled = scaleSeries(base, year);
  return (
    <ANChartCard
      eyebrow={eyebrow} icon={icon} title={title} sub={sub} height={height} className={className}
      legend={<ANYoYLegend year={year} color={color} />}
      summary={makeSummary(title, scaled, year)}
      table={makeTable(eyebrow || title, scaled, year)}>
      <ANBarsYoY data={scaled} year={year} color={color} prevStyle={prevStyle} barSize={barSize} unit={unit} />
    </ANChartCard>
  );
}

// ── Reusable top-5 rank card ───────────────────────────────────
function RankCard({ eyebrow, icon, title, sub, base, year, total, color, prevStyle }) {
  const scaled = scaleSeries(base, year);
  return (
    <ANChartCard
      eyebrow={eyebrow} icon={icon} title={title} sub={sub} height="an-chart--lg"
      legend={<ANYoYLegend year={year} color={color} />}
      summary={makeSummary(title, scaled, year)}
      note={`Bars labelled with EUR and share of total Inventory expense (${fmtEUR(total)}).`}
      table={rankTable(eyebrow || title, scaled, year, total)}>
      <ANRankBars data={scaled} year={year} total={total} color={color} prevStyle={prevStyle} />
    </ANChartCard>
  );
}

// ── Overview stat card ─────────────────────────────────────────
const STAT_TONE = {
  expense: { c: "var(--azure-500)", s: "var(--azure-100)" },
  income: { c: "var(--sea-500)", s: "var(--sea-100)" },
  netPos: { c: "var(--sea-500)", s: "var(--sea-100)" },
  netNeg: { c: "var(--terracotta-500)", s: "var(--danger-soft)" },
};
function Stat({ label, icon, tone, value, delta, goodWhenUp, year }) {
  const up = delta > 0;
  const flat = Math.abs(delta) < 0.005;
  const good = up === goodWhenUp;
  const cls = flat ? "is-flat" : good ? "is-up" : "is-down";
  return (
    <div className="an-stat" style={{ "--stat": tone.c, "--stat-soft": tone.s }}>
      <div className="an-stat__top">
        <span className="an-stat__icon"><Icon n={icon} size={18} /></span>
        <span className="an-stat__label">{label}</span>
      </div>
      <div className="an-stat__val">{value}</div>
      <div className="an-stat__foot">
        <span className={"an-delta " + cls}>
          {!flat && <Icon n={up ? "trending-up" : "trending-down"} size={14} />}
          {fmtDelta(delta)}
        </span>
        <span>vs {year - 1}</span>
      </div>
    </div>
  );
}

// ── Tab head ───────────────────────────────────────────────────
function TabHead({ eyebrow, title, desc, scope }) {
  return (
    <div className="an-tabhead">
      <div className="an-tabhead__txt">
        <div className="armali-eyebrow">{eyebrow}</div>
        <h2>{title}</h2>
        <p>{desc}</p>
      </div>
      {scope && <span className="an-tabhead__scope"><Icon n="filter" size={13} />{scope}</span>}
    </div>
  );
}

// ── TABS ───────────────────────────────────────────────────────
function OverviewTab({ year }) {
  const frame = AN.overviewMonthly(year);
  const t = AN.overviewTotals(year);
  const netTone = t.net.cur >= 0 ? STAT_TONE.netPos : STAT_TONE.netNeg;
  const expData = anMonthlyPair(frame, "expense");
  const incData = anMonthlyPair(frame, "income");
  return (
    <React.Fragment>
      <TabHead eyebrow="Overview" title={`Year ${year} at a glance`}
        desc="A high-level read of the year across every participating module — totals, monthly trend and net balance, each compared with the previous year."
        scope="All participating modules" />
      <div className="an-stats">
        <Stat label="Total expenses" icon="arrow-down-circle" tone={STAT_TONE.expense}
          value={fmtEUR(t.expense.cur)} delta={t.expense.delta} goodWhenUp={false} year={year} />
        <Stat label="Total income" icon="arrow-up-circle" tone={STAT_TONE.income}
          value={fmtEUR(t.income.cur)} delta={t.income.delta} goodWhenUp={true} year={year} />
        <Stat label="Net balance" icon="scale" tone={netTone}
          value={fmtEUR(t.net.cur)} delta={t.net.delta} goodWhenUp={true} year={year} />
      </div>
      <div className="an-grid an-grid--2">
        <ANChartCard eyebrow="Expenses" icon="trending-down" title="Total expenses by month" height="an-chart--lg"
          legend={<ANYoYLegend year={year} color={PALETTE.expense} />}
          summary={`Total monthly expenses for ${year} and ${year - 1}. ${year} totals ${fmtEUR(t.expense.cur)}.`}
          table={makeTable("Month", expData, year)}>
          <ANLineYoY data={expData} curKey="cur" prevKey="prev" year={year} color={PALETTE.expense} />
        </ANChartCard>
        <ANChartCard eyebrow="Income" icon="trending-up" title="Total income by month" height="an-chart--lg"
          legend={<ANYoYLegend year={year} color={PALETTE.income} />}
          summary={`Total monthly income for ${year} and ${year - 1}. ${year} totals ${fmtEUR(t.income.cur)}.`}
          table={makeTable("Month", incData, year)}>
          <ANLineYoY data={incData} curKey="cur" prevKey="prev" year={year} color={PALETTE.income} />
        </ANChartCard>
        <ANChartCard className="an-span-2" eyebrow="Net balance" icon="scale" title="Net balance by month"
          sub="Income minus expenses · current year as bars, previous year as a dashed line" height="an-chart--lg"
          legend={<ANYoYLegend year={year} color={PALETTE.netPos} prevLabel={`${year - 1} net`} />}
          summary={`Net balance per month for ${year}; negative months shown in terracotta. ${year} net ${fmtEUR(t.net.cur)} versus ${fmtEUR(t.net.prev)} in ${year - 1}.`}
          table={makeTable("Month", frame.map((r) => ({ label: r.label, cur: r.net, prev: r.netPrev })), year)}>
          <ANNetBars frame={frame} year={year} />
        </ANChartCard>
      </div>
    </React.Fragment>
  );
}

function CapexTab({ year, prevStyle }) {
  const E = PALETTE.expense, I = PALETTE.income;
  return (
    <React.Fragment>
      <TabHead eyebrow="Capex" title="Capital income & expense"
        desc="Completed Capex entries grouped by category, supplier and cost centre — income and expense shown side by side."
        scope="Completed entries only" />
      <div className="an-grid an-grid--2">
        <BarCard eyebrow="Expenses by category" icon="trending-down" title="Expenses by category" base={AN.CX_EXP_CAT} year={year} color={E} prevStyle={prevStyle} />
        <BarCard eyebrow="Income by category" icon="trending-up" title="Income by category" base={AN.CX_INC_CAT} year={year} color={I} prevStyle={prevStyle} />
        <BarCard eyebrow="Expenses by supplier" icon="trending-down" title="Expenses by supplier" base={AN.CX_EXP_SUP} year={year} color={E} prevStyle={prevStyle} />
        <BarCard eyebrow="Income by supplier" icon="trending-up" title="Income by supplier" base={AN.CX_INC_SUP} year={year} color={I} prevStyle={prevStyle} />
        <BarCard eyebrow="Expenses by cost centre" icon="trending-down" title="Expenses by cost centre" base={AN.CX_EXP_CC} year={year} color={E} prevStyle={prevStyle} />
        <BarCard eyebrow="Income by cost centre" icon="trending-up" title="Income by cost centre" base={AN.CX_INC_CC} year={year} color={I} prevStyle={prevStyle} />
      </div>
    </React.Fragment>
  );
}

function InventoryTab({ year, prevStyle }) {
  const E = PALETTE.expense;
  const total = AN.inventoryTotalExpense(year);
  return (
    <React.Fragment>
      <TabHead eyebrow="Inventory" title="Order spending"
        desc="Received order spending by item category and supplier, average order value, and the year's top items and suppliers."
        scope="Planning & cancelled excluded" />
      <div className="an-grid an-grid--2">
        <BarCard eyebrow="Expenses by item category" icon="shapes" title="Expenses by item category" base={AN.IN_EXP_ITEMCAT} year={year} color={E} prevStyle={prevStyle} />
        <BarCard eyebrow="Expenses by supplier" icon="store" title="Expenses by supplier" base={AN.IN_EXP_SUP} year={year} color={E} prevStyle={prevStyle} />
      </div>
      <div className="an-grid an-grid--2">
        <RankCard eyebrow="Top items" icon="award" title="Top 5 items by spend" base={AN.IN_TOP_ITEMS} year={year} total={total} color={E} prevStyle={prevStyle} />
        <RankCard eyebrow="Top suppliers" icon="award" title="Top 5 suppliers by spend" base={AN.IN_TOP_SUP} year={year} total={total} color={E} prevStyle={prevStyle} />
      </div>
      <div className="an-grid an-grid--1">
        <BarCard className="an-span-2" eyebrow="Average order amount by supplier" icon="receipt" title="Average order amount by supplier"
          sub="Mean EUR per received order" base={AN.IN_AVG_ORDER} year={year} color={E} prevStyle={prevStyle} barSize={22} unit="eur" />
      </div>
    </React.Fragment>
  );
}

function CrossTab({ year, prevStyle }) {
  const E = PALETTE.expense;
  return (
    <React.Fragment>
      <TabHead eyebrow="Cross-module" title="Pooled expenses"
        desc="Total expenses pooled across Capex, Opex, Inventory and Travel — grouped by supplier, category and cost-centre label."
        scope="Capex · Opex · Inventory · Travel" />
      <div className="an-grid an-grid--2">
        <BarCard eyebrow="Total expenses by supplier" icon="store" title="Expenses by supplier" base={AN.CM_EXP_SUP} year={year} color={E} prevStyle={prevStyle} />
        <BarCard eyebrow="Total expenses by category" icon="shapes" title="Expenses by category" base={AN.CM_EXP_CAT} year={year} color={E} prevStyle={prevStyle} />
        <BarCard className="an-span-2" eyebrow="Total expenses by cost centre" icon="building-2" title="Expenses by cost centre" base={AN.CM_EXP_CC} year={year} color={E} prevStyle={prevStyle} height="an-chart--lg" />
      </div>
      <div className="an-card__note" style={{ marginTop: 0 }}>
        <Icon n="info" size={13} />Categories are matched across modules by normalized display label — this does not create shared category ownership.
      </div>
    </React.Fragment>
  );
}

// ── Year navigator ─────────────────────────────────────────────
function YearNav({ year, setYear }) {
  const atMin = year <= AN.MIN_YEAR, atMax = year >= AN.MAX_YEAR;
  const isCurrent = year === AN.CURRENT_YEAR;
  return (
    <div className="an-yearnav" role="group" aria-label="Select year">
      <button type="button" className="an-yearnav__btn" disabled={atMin} aria-label="Previous year" onClick={() => setYear((y) => Math.max(AN.MIN_YEAR, y - 1))}><Icon n="chevron-left" size={18} /></button>
      <div className="an-yearnav__label">{year}<small>vs {year - 1}</small></div>
      <button type="button" className="an-yearnav__btn" disabled={atMax} aria-label="Next year" onClick={() => setYear((y) => Math.min(AN.MAX_YEAR, y + 1))}><Icon n="chevron-right" size={18} /></button>
      <button type="button" className="an-thisyear" disabled={isCurrent} onClick={() => setYear(AN.CURRENT_YEAR)}><Icon n="calendar-check" size={14} />This year</button>
    </div>
  );
}

// ── Module shell ───────────────────────────────────────────────
const TABS = [
  { key: "overview", name: "Overview", icon: "layout-dashboard" },
  { key: "capex", name: "Capex", icon: "receipt-text" },
  { key: "inventory", name: "Inventory", icon: "package" },
  { key: "cross", name: "Cross-module", icon: "shuffle" },
];

function AnalyticsScreen({ initialTab = "overview", prevStyle = "faded" }) {
  const [tab, setTab] = React.useState(initialTab);
  const [year, setYear] = React.useState(AN.CURRENT_YEAR);
  return (
    <div className="seg-screen an-screen armali-aurora">
      <SegShellTopBar eyebrow="Read view · Financial trends" title="Analytics" />
      <div className="an-subbar">
        <div className="an-tabs" role="tablist">
          {TABS.map((t) => (
            <button key={t.key} type="button" role="tab" aria-selected={tab === t.key}
              className={"an-tab" + (tab === t.key ? " is-active" : "")} onClick={() => setTab(t.key)}>
              <Icon n={t.icon} size={15} />{t.name}
            </button>
          ))}
        </div>
        <div className="an-subbar__right">
          <span className="an-eur"><Icon n="euro" size={12} />Amounts in EUR</span>
          <YearNav year={year} setYear={setYear} />
        </div>
      </div>
      <div className="an-scroll">
        <div className="an-page">
          {tab === "overview" && <OverviewTab year={year} />}
          {tab === "capex" && <CapexTab year={year} prevStyle={prevStyle} />}
          {tab === "inventory" && <InventoryTab year={year} prevStyle={prevStyle} />}
          {tab === "cross" && <CrossTab year={year} prevStyle={prevStyle} />}
        </div>
      </div>
    </div>
  );
}

Object.assign(window, { AnalyticsScreen });
})();
