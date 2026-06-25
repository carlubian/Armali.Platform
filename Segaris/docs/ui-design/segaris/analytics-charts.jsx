/* global React */
// Segaris Analytics — chart primitives.
// Module-owned wrappers around Recharts so a later chart-library swap stays
// contained here. Every chart is paired with a YoY legend and an accessible
// table-equivalent (toggled per card) — tooltips are never the only way to
// read a value, per the Analytics spec.
(() => {
const R = window.Recharts;
const {
  ResponsiveContainer, BarChart, Bar, LineChart, Line, ComposedChart,
  XAxis, YAxis, CartesianGrid, Tooltip, Cell, LabelList, ReferenceLine,
} = R;
const AN = window.AN;
const { fmtEUR, fmtCompact, fmtPct, fmtDelta, PALETTE } = AN;
const Icon = window.SegIcon;

const BODY = "Nunito";
const DISP = "League Spartan";
const axisTick = { fill: PALETTE.axis, fontSize: 11, fontFamily: BODY };

// ── Custom tooltip ─────────────────────────────────────────────
function TipBox({ active, payload, label, unit }) {
  if (!active || !payload || !payload.length) return null;
  const f = unit === "eur" ? fmtEUR : (v) => fmtEUR(v);
  const cur = payload.find((p) => p.dataKey === "cur");
  const prev = payload.find((p) => p.dataKey === "prev");
  let delta = null;
  if (cur && prev && prev.value) delta = (cur.value - prev.value) / prev.value;
  return (
    <div className="an-tip">
      <div className="an-tip__label">{label}</div>
      {payload.map((p, i) => (
        <div className="an-tip__row" key={i}>
          <span className="an-tip__dot" style={{ background: p.color, opacity: p.dataKey === "prev" ? 0.5 : 1 }} />
          <span className="an-tip__k">{p.name}</span>
          <span className="an-tip__v">{f(p.value)}</span>
        </div>
      ))}
      {delta != null && (
        <div className="an-tip__delta">
          Year over year {fmtDelta(delta)}
        </div>
      )}
    </div>
  );
}

// Previous-year bar appearance from the cur hue + tweak style.
function prevBar(color, prevStyle) {
  if (prevStyle === "outline") return { fill: color, fillOpacity: 0.08, stroke: color, strokeWidth: 1.5, strokeDasharray: "3 3" };
  return { fill: color, fillOpacity: 0.30 };
}

// ── YoY legend ─────────────────────────────────────────────────
function YoYLegend({ year, color, curLabel, prevLabel }) {
  return (
    <div className="an-legend">
      <span className="an-legend__item">
        <span className="an-legend__swatch" style={{ background: color }} />
        {curLabel || year}
      </span>
      <span className="an-legend__item">
        <span className="an-legend__swatch is-prev" style={{ background: color, opacity: 0.3, boxShadow: "inset 0 0 0 1.5px " + color }} />
        {prevLabel || (year - 1)}
      </span>
    </div>
  );
}

// ── Accessible table ───────────────────────────────────────────
function DataTable({ columns, rows, caption }) {
  return (
    <div className="an-tablewrap">
      <table className="an-table">
        {caption && <caption>{caption}</caption>}
        <thead><tr>{columns.map((c, i) => <th key={i} scope="col">{c}</th>)}</tr></thead>
        <tbody>
          {rows.map((r, i) => (
            <tr key={i}>{r.map((cell, j) => j === 0 ? <th key={j} scope="row">{cell}</th> : <td key={j}>{cell}</td>)}</tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

// ── Chart card shell ───────────────────────────────────────────
function ChartCard({ eyebrow, icon, title, sub, legend, summary, table, height = "an-chart--md", note, children, className }) {
  const [showTable, setShowTable] = React.useState(false);
  return (
    <section className={"an-card" + (className ? " " + className : "")}>
      <div className="an-card__head">
        <div className="an-card__titles">
          {eyebrow && <div className="an-card__eyebrow">{icon && <Icon n={icon} size={12} />}{eyebrow}</div>}
          <h3 className="an-card__title">{title}</h3>
          {sub && <div className="an-card__sub">{sub}</div>}
        </div>
        <div className="an-card__tools">
          {legend}
          {table && (
            <button type="button" className={"an-tbtn" + (showTable ? " is-active" : "")}
              aria-pressed={showTable} title={showTable ? "Show chart" : "Show data table"}
              onClick={() => setShowTable((s) => !s)}>
              <Icon n={showTable ? "chart-column" : "table"} size={16} />
            </button>
          )}
        </div>
      </div>
      {showTable && table
        ? <DataTable {...table} />
        : <div className={"an-chart " + height} role="img" aria-label={summary}>{children}</div>}
      {note && !showTable && <div className="an-card__note"><Icon n="info" size={13} />{note}</div>}
      {summary && <p className="an-sr">{summary}</p>}
    </section>
  );
}

// ── Grouped vertical bars (categorical YoY) ────────────────────
function BarsYoY({ data, year, color = PALETTE.expense, prevStyle = "faded", barSize = 17, unit = "eur" }) {
  const fmtY = unit === "eur" ? fmtCompact : (v) => "\u20AC" + v;
  return (
    <ResponsiveContainer width="100%" height="100%">
      <BarChart data={data} margin={{ top: 8, right: 6, left: 2, bottom: 2 }} barGap={3} barCategoryGap="22%">
        <CartesianGrid vertical={false} stroke={PALETTE.grid} />
        <XAxis dataKey="label" tick={axisTick} tickLine={false} axisLine={{ stroke: PALETTE.grid }} interval={0} tickMargin={8} />
        <YAxis tick={axisTick} tickLine={false} axisLine={false} width={46} tickFormatter={fmtY} />
        <Tooltip cursor={{ fill: "rgba(124,110,86,0.07)" }} content={<TipBox unit={unit} />} />
        <Bar dataKey="prev" name={String(year - 1)} radius={[5, 5, 0, 0]} maxBarSize={barSize} {...prevBar(color, prevStyle)} isAnimationActive={false} />
        <Bar dataKey="cur" name={String(year)} fill={color} radius={[5, 5, 0, 0]} maxBarSize={barSize} isAnimationActive={false} />
      </BarChart>
    </ResponsiveContainer>
  );
}

// ── Monthly trend line (YoY) ───────────────────────────────────
function LineYoY({ data, curKey, prevKey, year, color }) {
  return (
    <ResponsiveContainer width="100%" height="100%">
      <LineChart data={data} margin={{ top: 8, right: 10, left: 2, bottom: 2 }}>
        <CartesianGrid vertical={false} stroke={PALETTE.grid} />
        <XAxis dataKey="label" tick={axisTick} tickLine={false} axisLine={{ stroke: PALETTE.grid }} interval={0} tickMargin={8} />
        <YAxis tick={axisTick} tickLine={false} axisLine={false} width={46} tickFormatter={fmtCompact} />
        <Tooltip content={<TipBox unit="eur" />} />
        <Line type="monotone" dataKey={prevKey} name={String(year - 1)} stroke={color} strokeWidth={2}
          strokeDasharray="4 4" strokeOpacity={0.45} dot={false} isAnimationActive={false} />
        <Line type="monotone" dataKey={curKey} name={String(year)} stroke={color} strokeWidth={2.75}
          dot={{ r: 2.5, fill: color, strokeWidth: 0 }} activeDot={{ r: 4 }} isAnimationActive={false} />
      </LineChart>
    </ResponsiveContainer>
  );
}

// Adapt the overview monthly frame to a {label, cur, prev} pair for the
// custom tooltip (so YoY delta shows), keyed by metric.
function monthlyPair(frame, metric) {
  const map = { expense: ["exp", "expPrev"], income: ["inc", "incPrev"] };
  const [c, p] = map[metric];
  return frame.map((r) => ({ label: r.label, cur: r[c], prev: r[p] }));
}

// ── Monthly net balance (diverging bars + prev-year line) ──────
function NetBars({ frame, year }) {
  const data = frame.map((r) => ({ label: r.label, cur: r.net, prev: r.netPrev }));
  return (
    <ResponsiveContainer width="100%" height="100%">
      <ComposedChart data={data} margin={{ top: 8, right: 10, left: 2, bottom: 2 }} barCategoryGap="26%">
        <CartesianGrid vertical={false} stroke={PALETTE.grid} />
        <XAxis dataKey="label" tick={axisTick} tickLine={false} axisLine={{ stroke: PALETTE.grid }} interval={0} tickMargin={8} />
        <YAxis tick={axisTick} tickLine={false} axisLine={false} width={46} tickFormatter={fmtCompact} />
        <Tooltip cursor={{ fill: "rgba(124,110,86,0.07)" }} content={<TipBox unit="eur" />} />
        <ReferenceLine y={0} stroke={PALETTE.axis} strokeOpacity={0.5} />
        <Bar dataKey="cur" name={String(year)} radius={[4, 4, 0, 0]} maxBarSize={26} isAnimationActive={false}>
          {data.map((d, i) => <Cell key={i} fill={d.cur >= 0 ? PALETTE.netPos : PALETTE.netNeg} />)}
        </Bar>
        <Line type="monotone" dataKey="prev" name={String(year - 1)} stroke={PALETTE.ink} strokeWidth={2}
          strokeDasharray="4 4" strokeOpacity={0.4} dot={false} isAnimationActive={false} />
      </ComposedChart>
    </ResponsiveContainer>
  );
}

// ── Horizontal rank bars (top-5 with % of total) ───────────────
function RankBars({ data, year, total, color = PALETTE.expense, prevStyle = "faded" }) {
  const renderPct = (props) => {
    const { x, y, width, height, value } = props;
    return (
      <text x={x + width + 8} y={y + height / 2} dominantBaseline="central"
        fontFamily={DISP} fontWeight={600} fontSize={11.5} fill={PALETTE.ink}>
        {fmtEUR(value)} · {fmtPct(value / total)}
      </text>
    );
  };
  return (
    <ResponsiveContainer width="100%" height="100%">
      <BarChart data={data} layout="vertical" margin={{ top: 4, right: 96, left: 6, bottom: 2 }} barGap={2} barCategoryGap="30%">
        <CartesianGrid horizontal={false} stroke={PALETTE.grid} />
        <XAxis type="number" hide />
        <YAxis type="category" dataKey="label" tick={{ ...axisTick, fontFamily: DISP, fontWeight: 600, fill: PALETTE.ink }}
          tickLine={false} axisLine={false} width={104} />
        <Tooltip cursor={{ fill: "rgba(124,110,86,0.07)" }} content={<TipBox unit="eur" />} />
        <Bar dataKey="prev" name={String(year - 1)} radius={[0, 4, 4, 0]} maxBarSize={11} {...prevBar(color, prevStyle)} isAnimationActive={false} />
        <Bar dataKey="cur" name={String(year)} fill={color} radius={[0, 4, 4, 0]} maxBarSize={11} isAnimationActive={false}>
          <LabelList dataKey="cur" content={renderPct} />
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  );
}

Object.assign(window, { ANChartCard: ChartCard, ANYoYLegend: YoYLegend, ANDataTable: DataTable, ANBarsYoY: BarsYoY, ANLineYoY: LineYoY, ANNetBars: NetBars, ANRankBars: RankBars, anMonthlyPair: monthlyPair });
})();
