/* global React */
// Mood module — Dashboard view. Two variants:
//   A · MoodDashScore    — score min/avg/max emphasis + score-by-weekday.
//   B · MoodDashCriteria — criteria distribution emphasis.
// Both share the calendar-period controls (Year/Semester/Quarter/Month),
// previous/next navigation, and operate only on the current user's entries.
(() => {
const A = window.ProjectArmaliDesignSystem_ce5329;
const { Button } = A;
const Icon = window.SegIcon;
const M = window.MoodData;
const {
  ENERGY, ALIGNMENT, DIRECTION, SOURCE, TONE, TONE_VARS,
  YEAR_ENTRIES, DOW, MON, dowIndex, avg, scoreColor,
} = M;

const TODAY = new Date(2026, 5, 17);
const SCALES = ["Year", "Semester", "Quarter", "Month"];

// ── Strict calendar-period maths (Europe/Madrid) ───────────────
// state = { scale, year, idx }  — idx meaning depends on scale.
function currentPeriod(scale, ref = TODAY) {
  const year = ref.getFullYear(), m = ref.getMonth();
  if (scale === "Year")     return { scale, year, idx: 0 };
  if (scale === "Semester") return { scale, year, idx: m < 6 ? 0 : 1 };
  if (scale === "Quarter")  return { scale, year, idx: Math.floor(m / 3) };
  return { scale, year, idx: m }; // Month
}
function periodRange(p) {
  const { scale, year, idx } = p;
  if (scale === "Year")     return { start: new Date(year,0,1),       end: new Date(year,11,31) };
  if (scale === "Semester") return { start: new Date(year,idx*6,1),   end: new Date(year,idx*6+6,0) };
  if (scale === "Quarter")  return { start: new Date(year,idx*3,1),   end: new Date(year,idx*3+3,0) };
  return { start: new Date(year,idx,1), end: new Date(year,idx+1,0) };
}
function periodLabel(p) {
  const { scale, year, idx } = p;
  if (scale === "Year")     return { main: String(year), sub: "Full calendar year" };
  if (scale === "Semester") return { main: `H${idx+1} ${year}`, sub: idx === 0 ? "Jan – Jun" : "Jul – Dec" };
  if (scale === "Quarter")  return { main: `Q${idx+1} ${year}`, sub: ["Jan – Mar","Apr – Jun","Jul – Sep","Oct – Dec"][idx] };
  return { main: `${MON[idx]} ${year}`, sub: "Calendar month" };
}
function stepPeriod(p, dir) {
  const counts = { Year: 1, Semester: 2, Quarter: 4, Month: 12 };
  if (p.scale === "Year") return { ...p, year: p.year + dir };
  const n = counts[p.scale];
  let idx = p.idx + dir, year = p.year;
  if (idx < 0)  { idx = n - 1; year -= 1; }
  if (idx >= n) { idx = 0;     year += 1; }
  return { ...p, idx, year };
}

// ── Aggregations (current user's entries within the period) ────
function useAggregates(period) {
  return React.useMemo(() => {
    const { start, end } = periodRange(period);
    const rows = YEAR_ENTRIES.filter((e) => e.date >= start && e.date <= end);
    const scores = rows.map((e) => e.score);
    const overall = scores.length ? {
      min: Math.min(...scores), max: Math.max(...scores),
      avg: scores.reduce((a, b) => a + b, 0) / scores.length, n: scores.length,
    } : null;
    // score by weekday
    const byDow = DOW.map((_, i) => {
      const s = rows.filter((e) => dowIndex(e.date) === i).map((e) => e.score);
      return s.length ? { min: Math.min(...s), max: Math.max(...s), avg: avg(s), n: s.length } : null;
    });
    // criteria distributions
    const dist = (key, values, toneMap) => {
      const total = rows.length || 1;
      return values.map((v) => {
        const n = rows.filter((e) => e[key] === v).length;
        return { value: v, n, pct: n / total, tone: toneMap[v] };
      });
    };
    return {
      overall, byDow, n: rows.length,
      energy:    dist("energy",    ENERGY,    TONE.energy),
      alignment: dist("alignment", ALIGNMENT, TONE.alignment),
      direction: dist("direction", DIRECTION, TONE.direction),
      source:    dist("source",    SOURCE,    TONE.source),
    };
  }, [period.scale, period.year, period.idx]);
}

// ── Shared chrome ──────────────────────────────────────────────
function PeriodNav({ period, setPeriod }) {
  const lab = periodLabel(period);
  return (
    <div className="mood-nav">
      <button className="mood-nav__btn" type="button" aria-label="Previous period" onClick={() => setPeriod((p) => stepPeriod(p, -1))}><Icon n="chevron-left" size={18} /></button>
      <span className="mood-nav__label">{lab.main}<small>{lab.sub}</small></span>
      <button className="mood-nav__btn" type="button" aria-label="Next period" onClick={() => setPeriod((p) => stepPeriod(p, +1))}><Icon n="chevron-right" size={18} /></button>
    </div>
  );
}
function ScaleSeg({ scale, onChange }) {
  return (
    <div className="mood-seg" role="tablist" aria-label="Time scale">
      {SCALES.map((s) => (
        <button key={s} type="button" role="tab" aria-selected={s === scale}
          className={"mood-seg__btn" + (s === scale ? " is-active" : "")}
          onClick={() => onChange(s)}>{s}</button>
      ))}
    </div>
  );
}
function ViewTabs() {
  return (
    <div className="mood-seg" role="tablist" aria-label="Mood views">
      {["Log", "Dashboard"].map((t) => (
        <button key={t} type="button" role="tab" aria-selected={t === "Dashboard"}
          className={"mood-seg__btn" + (t === "Dashboard" ? " is-active" : "")}>{t}</button>
      ))}
    </div>
  );
}

// Score-by-day-of-week range chart (min–max bar + avg marker).
function DowChart({ byDow }) {
  const lo = 1, hi = 5, span = hi - lo;
  const pos = (v) => ((v - lo) / span) * 100;
  return (
    <div style={{ display: "flex", gap: "var(--space-4)" }}>
      <div className="mood-dow__scale"><span>5</span><span>4</span><span>3</span><span>2</span><span>1</span></div>
      <div className="mood-dow" style={{ flex: 1 }}>
        {byDow.map((d, i) => (
          <div key={i} className="mood-dow__col">
            {d ? (
              <div className="mood-dow__track">
                <span className="mood-dow__range" style={{
                  bottom: pos(d.min) + "%", top: (100 - pos(d.max)) + "%",
                  "--range": scoreColor(d.avg),
                }} />
                <span className="mood-dow__avg" style={{ bottom: pos(d.avg) + "%" }} />
              </div>
            ) : <div className="mood-dow__track" style={{ background: "transparent" }} />}
            <div className="mood-dow__cap">
              <span className="mood-dow__avgval">{d ? d.avg.toFixed(1) : "·"}</span>
              <span className="mood-dow__lbl">{DOW[i]}</span>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

// Criteria distribution (horizontal bars).
function Distribution({ rows }) {
  const max = Math.max(...rows.map((r) => r.pct), 0.0001);
  return (
    <div className="mood-dist">
      {rows.map((r) => {
        const [bg, fg] = TONE_VARS[r.tone] || TONE_VARS.neutral;
        return (
          <div key={r.value} className="mood-dist__row">
            <span className="mood-dist__lbl">{r.value}</span>
            <div className="mood-dist__track">
              <div className="mood-dist__fill" style={{ width: (r.pct / max * 100) + "%", background: fg, opacity: 0.85 }} />
            </div>
            <span className="mood-dist__pct">{Math.round(r.pct * 100)}%</span>
          </div>
        );
      })}
    </div>
  );
}

function DashShell({ period, setPeriod, children }) {
  return (
    <div className="seg-screen mood-screen">
      <window.SegShellTopBar eyebrow="Mood" title="Dashboard" />
      <div className="seg-page">
        <div className="seg-page__inner">
          <div className="mood-bar">
            <div>
              <div className="armali-eyebrow">Your trends</div>
              <h2>Dashboard</h2>
              <p>How your check-ins move across a calendar period. Only your own entries.</p>
            </div>
            <div className="mood-bar__controls">
              <ViewTabs />
              <ScaleSeg scale={period.scale} onChange={(s) => setPeriod(currentPeriod(s))} />
              <PeriodNav period={period} setPeriod={setPeriod} />
            </div>
          </div>
          <div className="mood-scroll">
            {children}
          </div>
        </div>
      </div>
    </div>
  );
}

const EmptyPeriod = () => (
  <div className="mood-card"><div className="mood-emptynote">No entries in this period — nothing to chart yet. Pick another period, or add check-ins in the Log.</div></div>
);

// ── Variant A · Score emphasis ─────────────────────────────────
function MoodDashScore() {
  const [period, setPeriod] = React.useState(currentPeriod("Year"));
  const g = useAggregates(period);
  const lab = periodLabel(period);
  return (
    <DashShell period={period} setPeriod={setPeriod}>
      <div className="mood-dash">
        {g.n === 0 ? <EmptyPeriod /> : (
          <React.Fragment>
            <div className="mood-scorecards">
              <div className="mood-scorecard" style={{ "--bar": "var(--terracotta-500)" }}>
                <span className="mood-scorecard__lbl">Lowest</span>
                <span className="mood-scorecard__val">{g.overall.min}</span>
                <span className="mood-scorecard__note">Hardest check-in this period</span>
              </div>
              <div className="mood-scorecard" style={{ "--bar": "var(--aqua-500)" }}>
                <span className="mood-scorecard__lbl">Average</span>
                <span className="mood-scorecard__val">{g.overall.avg.toFixed(1)}</span>
                <span className="mood-scorecard__note">Across {g.overall.n} entries · {lab.main}</span>
              </div>
              <div className="mood-scorecard" style={{ "--bar": "var(--sea-500)" }}>
                <span className="mood-scorecard__lbl">Highest</span>
                <span className="mood-scorecard__val">{g.overall.max}</span>
                <span className="mood-scorecard__note">Best check-in this period</span>
              </div>
            </div>

            <div className="mood-card">
              <div className="mood-card__head">
                <span className="mood-card__title">Score by day of week</span>
                <span className="mood-card__sub">Min – max range · marker shows the average</span>
              </div>
              <DowChart byDow={g.byDow} />
            </div>

            <div className="mood-grid" style={{ gridTemplateColumns: "1fr 1fr" }}>
              <div className="mood-card">
                <div className="mood-card__head"><span className="mood-card__title">Alignment</span><span className="mood-card__sub">{g.n} entries</span></div>
                <Distribution rows={g.alignment} />
              </div>
              <div className="mood-card">
                <div className="mood-card__head"><span className="mood-card__title">Energy</span><span className="mood-card__sub">{g.n} entries</span></div>
                <Distribution rows={g.energy} />
              </div>
            </div>
          </React.Fragment>
        )}
      </div>
    </DashShell>
  );
}

// ── Variant B · Criteria emphasis ──────────────────────────────
function MoodDashCriteria() {
  const [period, setPeriod] = React.useState(currentPeriod("Year"));
  const g = useAggregates(period);
  return (
    <DashShell period={period} setPeriod={setPeriod}>
      <div className="mood-dash">
        {g.n === 0 ? <EmptyPeriod /> : (
          <React.Fragment>
            <div className="mood-card" style={{ flexDirection: "row", alignItems: "center", gap: "var(--space-7)", flexWrap: "wrap" }}>
              <div className="mood-chartcard__lead" style={{ maxWidth: "none" }}>
                <div className="armali-eyebrow" style={{ color: "var(--accent)" }}>Period average</div>
                <div className="mood-chartcard__big">{g.overall.avg.toFixed(1)}</div>
                <small>{g.overall.n} entries · low {g.overall.min} · high {g.overall.max}</small>
              </div>
              <div style={{ flex: 1, minWidth: 360 }}><DowChart byDow={g.byDow} /></div>
            </div>

            <div className="mood-distgrid">
              <div className="mood-card">
                <div className="mood-card__head"><span className="mood-card__title">Energy</span><span className="mood-card__sub">Intensity</span></div>
                <Distribution rows={g.energy} />
              </div>
              <div className="mood-card">
                <div className="mood-card__head"><span className="mood-card__title">Alignment</span><span className="mood-card__sub">Habitually good / bad</span></div>
                <Distribution rows={g.alignment} />
              </div>
              <div className="mood-card">
                <div className="mood-card__head"><span className="mood-card__title">Direction</span><span className="mood-card__sub">Purpose</span></div>
                <Distribution rows={g.direction} />
              </div>
              <div className="mood-card">
                <div className="mood-card__head"><span className="mood-card__title">Source</span><span className="mood-card__sub">Origin</span></div>
                <Distribution rows={g.source} />
              </div>
            </div>
          </React.Fragment>
        )}
      </div>
    </DashShell>
  );
}

Object.assign(window, { MoodDashScore, MoodDashCriteria });
})();
