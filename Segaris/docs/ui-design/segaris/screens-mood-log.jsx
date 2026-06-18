/* global React */
// Mood module — Log view. Two variants:
//   A · MoodLogBoard  — 7-column Monday→Sunday week board.
//   B · MoodLogList   — day-grouped vertical list.
// Both share the weekly average-score chart, week navigation, the global
// "New entry" action, and the editable entry dialog.
(() => {
const A = window.ProjectArmaliDesignSystem_ce5329;
const { Button, IconButton, Tooltip, Input, Badge } = A;
const Icon = window.SegIcon;
const M = window.MoodData;
const {
  ENERGY, ALIGNMENT, DIRECTION, SOURCE, TONE, TONE_VARS,
  deriveEmotion, WEEK_ENTRIES, DOW, MON, sameDay, dowIndex, weekDays, avg,
  CriteriaPills, ScoreChip, WeekScoreChart,
} = M;

const TODAY = new Date(2026, 5, 17); // Wed 17 Jun 2026 (Europe/Madrid)
// Monday of the current week.
function mondayOf(d) { const x = new Date(d); x.setDate(d.getDate() - dowIndex(d)); x.setHours(0,0,0,0); return x; }
const fmtDay = (d) => `${d.getDate()} ${MON[d.getMonth()]}`;
const fmtRange = (mon) => { const sun = new Date(mon); sun.setDate(mon.getDate()+6); return `${mon.getDate()} ${MON[mon.getMonth()]} – ${sun.getDate()} ${MON[sun.getMonth()]}`; };

// ── Week navigation control ────────────────────────────────────
function WeekNav({ monday, onPrev, onNext }) {
  const sun = new Date(monday); sun.setDate(monday.getDate() + 6);
  const wk = Math.ceil((((monday - new Date(monday.getFullYear(),0,1)) / 86400000) + 1) / 7);
  return (
    <div className="mood-nav">
      <button className="mood-nav__btn" type="button" onClick={onPrev} aria-label="Previous week"><Icon n="chevron-left" size={18} /></button>
      <span className="mood-nav__label">{fmtRange(monday)}<small>Week {wk} · 2026</small></span>
      <button className="mood-nav__btn" type="button" onClick={onNext} aria-label="Next week"><Icon n="chevron-right" size={18} /></button>
    </div>
  );
}

// Group the supplied entries by the 7 days of the selected week.
function useWeekModel(monday, entries) {
  return React.useMemo(() => {
    const days = weekDays(monday).map((date) => {
      const list = entries
        .filter((e) => sameDay(e.date, date))
        .sort((a, b) => a.order - b.order);
      return { date, list, avg: avg(list.map((e) => e.score)), isToday: sameDay(date, TODAY) };
    });
    return days;
  }, [monday, entries]);
}

// ── Variant A · Week board ─────────────────────────────────────
function MoodLogBoard() {
  const [monday, setMonday] = React.useState(mondayOf(TODAY));
  const [dialog, setDialog] = React.useState(null);
  const days = useWeekModel(monday, WEEK_ENTRIES);
  const weekAvg = avg(days.flatMap((d) => d.list.map((e) => e.score)));
  const count = days.reduce((n, d) => n + d.list.length, 0);

  return (
    <div className="seg-screen mood-screen">
      <window.SegShellTopBar eyebrow="Mood" title="Log" />
      <div className="seg-page">
        <div className="seg-page__inner">

          <div className="mood-bar">
            <div>
              <div className="armali-eyebrow">Your week</div>
              <h2>Weekly log</h2>
              <p>Your private check-ins, Monday to Sunday. Only you can see these.</p>
            </div>
            <div className="mood-bar__controls">
              <Tabs />
              <WeekNav monday={monday}
                onPrev={() => setMonday((m) => { const x = new Date(m); x.setDate(m.getDate()-7); return x; })}
                onNext={() => setMonday((m) => { const x = new Date(m); x.setDate(m.getDate()+7); return x; })} />
              <Button variant="primary" iconLeft={<Icon n="plus" size={17} />} onClick={() => setDialog({ mode: "new" })}>New entry</Button>
            </div>
          </div>

          <div className="mood-chartcard">
            <div className="mood-chartcard__lead">
              <div className="armali-eyebrow">Average this week</div>
              <div className="mood-chartcard__big">{weekAvg == null ? "—" : weekAvg.toFixed(1)}</div>
              <small>{count} entries · simple mean per day</small>
            </div>
            <WeekScoreChart days={days} />
          </div>

          <div className="mood-scroll">
            <div className="mood-board">
              {days.map((d, i) => (
                <div key={i} className={"mood-daycol" + (d.isToday ? " is-today" : "")}>
                  <div className="mood-daycol__head">
                    <span>
                      <span className="mood-daycol__dow">{DOW[i]}</span>{" "}
                      <span className="mood-daycol__date">{d.date.getDate()}</span>
                    </span>
                    {d.avg != null && <span className="mood-daycol__avg"><ScoreChip score={d.avg} size={26} /></span>}
                  </div>
                  <div className="mood-daycol__body">
                    {d.list.length === 0
                      ? <div className="mood-daycol__empty">No entries</div>
                      : d.list.map((e) => (
                        <button key={e.id} type="button" className="mood-entry" onClick={() => setDialog({ mode: "edit", entry: e })}>
                          <div className="mood-entry__top">
                            <ScoreChip score={e.score} size={28} />
                            <span className="mood-emotion">{deriveEmotion(e.energy, e.alignment, e.direction, e.source)}</span>
                          </div>
                          <CriteriaPills e={e} />
                        </button>
                      ))}
                  </div>
                </div>
              ))}
            </div>
          </div>

        </div>
      </div>
      {dialog && <MoodEntryDialog {...dialog} onClose={() => setDialog(null)} />}
    </div>
  );
}

// ── Variant B · Day-grouped list ───────────────────────────────
function MoodLogList() {
  const [monday, setMonday] = React.useState(mondayOf(TODAY));
  const [dialog, setDialog] = React.useState(null);
  const days = useWeekModel(monday, WEEK_ENTRIES);
  const weekAvg = avg(days.flatMap((d) => d.list.map((e) => e.score)));
  const count = days.reduce((n, d) => n + d.list.length, 0);
  // List shows the days that actually hold entries, plus today.
  const shown = days.filter((d) => d.list.length > 0 || d.isToday);

  return (
    <div className="seg-screen mood-screen">
      <window.SegShellTopBar eyebrow="Mood" title="Log" />
      <div className="seg-page">
        <div className="seg-page__inner">

          <div className="mood-bar">
            <div>
              <div className="armali-eyebrow">Your week</div>
              <h2>Weekly log</h2>
              <p>Your private check-ins, Monday to Sunday. Only you can see these.</p>
            </div>
            <div className="mood-bar__controls">
              <Tabs />
              <WeekNav monday={monday}
                onPrev={() => setMonday((m) => { const x = new Date(m); x.setDate(m.getDate()-7); return x; })}
                onNext={() => setMonday((m) => { const x = new Date(m); x.setDate(m.getDate()+7); return x; })} />
              <Button variant="primary" iconLeft={<Icon n="plus" size={17} />} onClick={() => setDialog({ mode: "new" })}>New entry</Button>
            </div>
          </div>

          <div className="mood-chartcard">
            <div className="mood-chartcard__lead">
              <div className="armali-eyebrow">Average this week</div>
              <div className="mood-chartcard__big">{weekAvg == null ? "—" : weekAvg.toFixed(1)}</div>
              <small>{count} entries · simple mean per day</small>
            </div>
            <WeekScoreChart days={days} />
          </div>

          <div className="mood-scroll">
            <div className="mood-list">
              {shown.map((d, i) => {
                const di = dowIndex(d.date);
                return (
                  <div key={i} className={"mood-daygroup" + (d.isToday ? " is-today" : "")}>
                    <div className="mood-daygroup__head">
                      <span className="mood-daygroup__dow">{["Monday","Tuesday","Wednesday","Thursday","Friday","Saturday","Sunday"][di]}</span>
                      <span className="mood-daygroup__date">{fmtDay(d.date)}</span>
                      {d.isToday && <span className="mood-daygroup__todaytag">Today</span>}
                      <div className="mood-daygroup__meta">
                        {d.avg != null
                          ? <span className="mood-daygroup__avg">Avg <ScoreChip score={d.avg} size={26} /></span>
                          : <span className="mood-daygroup__avg" style={{ color: "var(--text-muted)" }}>No entries yet</span>}
                      </div>
                    </div>
                    {d.list.length === 0
                      ? <div className="mood-daygroup__empty">Nothing logged for today yet — add a check-in when you have a moment.</div>
                      : d.list.map((e) => (
                        <button key={e.id} type="button" className="mood-row" onClick={() => setDialog({ mode: "edit", entry: e })}>
                          <ScoreChip score={e.score} size={40} />
                          <span className="mood-emotion">{deriveEmotion(e.energy, e.alignment, e.direction, e.source)}</span>
                          <CriteriaPills e={e} />
                          <span className="mood-row__chev"><Icon n="chevron-right" size={18} /></span>
                        </button>
                      ))}
                  </div>
                );
              })}
            </div>
          </div>

        </div>
      </div>
      {dialog && <MoodEntryDialog {...dialog} onClose={() => setDialog(null)} />}
    </div>
  );
}

// Small Log/Dashboard switcher shown in the header (visual only on canvas).
function Tabs({ active = "Log" }) {
  return (
    <div className="mood-seg" role="tablist" aria-label="Mood views">
      {["Log", "Dashboard"].map((t) => (
        <button key={t} type="button" role="tab" aria-selected={t === active}
          className={"mood-seg__btn" + (t === active ? " is-active" : "")}>{t}</button>
      ))}
    </div>
  );
}

// ── Entry editor dialog (create / view / edit) ─────────────────
function ChoiceGroup({ value, options, toneMap, onChange }) {
  return (
    <div className="mood-choice">
      {options.map((opt) => {
        const active = value === opt;
        const tone = toneMap ? toneMap[opt] : null;
        const vars = active && tone ? TONE_VARS[tone] : null;
        return (
          <button key={opt} type="button"
            className={"mood-choice__btn" + (active ? " is-active" : "")}
            style={vars ? { "--chip-bg": vars[0], "--chip-fg": vars[1] } : undefined}
            onClick={() => onChange(opt)}>{opt}</button>
        );
      })}
    </div>
  );
}

function MoodEntryDialog({ mode, entry, onClose }) {
  const editing = mode === "edit";
  const [score, setScore]   = React.useState(editing ? entry.score : null);
  const [energy, setEnergy] = React.useState(editing ? entry.energy : null);
  const [align, setAlign]   = React.useState(editing ? entry.alignment : null);
  const [dir, setDir]       = React.useState(editing ? entry.direction : null);
  const [src, setSrc]       = React.useState(editing ? entry.source : null);
  const [notes, setNotes]   = React.useState(editing ? (entry.notes || "") : "");
  const dateStr = editing
    ? `${entry.date.getFullYear()}-${String(entry.date.getMonth()+1).padStart(2,"0")}-${String(entry.date.getDate()).padStart(2,"0")}`
    : "2026-06-17";

  const complete = score && energy && align && dir && src;
  const emotion = complete ? deriveEmotion(energy, align, dir, src) : null;
  const scoreVars = score ? TONE_VARS[M.SCORE_TONE[score]] : null;

  return (
    <div className="seg-modal mood-dialog" onClick={onClose}>
      <div className="seg-modal__card" onClick={(e) => e.stopPropagation()}>
        <div className="mood-dialog__head">
          <div>
            <h3>{editing ? "Edit entry" : "New entry"}</h3>
            <p>{editing ? "Update this check-in. Changes save when you choose." : "Record how you feel right now. This stays private to you."}</p>
          </div>
          <IconButton label="Close" variant="ghost" icon={<Icon n="x" size={18} />} onClick={onClose} />
        </div>

        <div className="mood-dialog__body">
          <div className="mood-grid2">
            <div className="mood-field">
              <label className="mood-field__label">Entry date <span className="mood-field__req">*</span></label>
              <Input type="date" defaultValue={dateStr} iconLeft={<Icon n="calendar" size={16} />} />
            </div>
            <div className="mood-field">
              <span className="mood-field__label">Score <span className="mood-field__req">*</span></span>
              <div className="mood-scoresel">
                {[1,2,3,4,5].map((n) => {
                  const vars = TONE_VARS[M.SCORE_TONE[n]];
                  const active = score === n;
                  return (
                    <button key={n} type="button"
                      className={"mood-scoresel__btn" + (active ? " is-active" : "")}
                      style={active ? { "--chip-bg": vars[0], "--chip-fg": vars[1] } : undefined}
                      onClick={() => setScore(n)}>{n}</button>
                  );
                })}
              </div>
            </div>
          </div>

          <div className="mood-field">
            <span className="mood-field__label">Energy <span className="mood-field__req">*</span></span>
            <ChoiceGroup value={energy} options={ENERGY} toneMap={TONE.energy} onChange={setEnergy} />
          </div>
          <div className="mood-field">
            <span className="mood-field__label">Alignment <span className="mood-field__req">*</span></span>
            <ChoiceGroup value={align} options={ALIGNMENT} toneMap={TONE.alignment} onChange={setAlign} />
          </div>
          <div className="mood-grid2">
            <div className="mood-field">
              <span className="mood-field__label">Direction <span className="mood-field__req">*</span></span>
              <ChoiceGroup value={dir} options={DIRECTION} toneMap={TONE.direction} onChange={setDir} />
            </div>
            <div className="mood-field">
              <span className="mood-field__label">Source <span className="mood-field__req">*</span></span>
              <ChoiceGroup value={src} options={SOURCE} toneMap={TONE.source} onChange={setSrc} />
            </div>
          </div>

          <div className="mood-derived">
            <span className="mood-derived__icon"><Icon n={emotion ? "sparkles" : "circle-dashed"} size={18} /></span>
            <div className="mood-derived__txt">
              <span className="mood-derived__lbl">Derived emotion</span>
              <span className="mood-derived__val">{emotion || "Pick all four criteria"}</span>
            </div>
            <span className="mood-derived__hint">Calculated from your four criteria — not stored.</span>
          </div>

          <div className="mood-field">
            <label className="mood-field__label">Notes <span style={{ color: "var(--text-muted)", fontWeight: 500 }}>· optional</span></label>
            <textarea className="mood-textarea" maxLength={1000} placeholder="A short note about this check-in…"
              value={notes} onChange={(e) => setNotes(e.target.value)} />
            <span className="mood-field__count">{notes.length} / 1000</span>
          </div>
        </div>

        <div className="mood-dialog__foot">
          {editing && (
            <Button variant="danger" iconLeft={<Icon n="trash-2" size={16} />} onClick={onClose}>Delete</Button>
          )}
          <div className="mood-dialog__foot-right">
            <Button variant="ghost" onClick={onClose}>Cancel</Button>
            <Button variant="primary" disabled={!complete} iconLeft={<Icon n="check" size={17} />} onClick={onClose}>
              {editing ? "Save changes" : "Save entry"}
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}

Object.assign(window, { MoodLogBoard, MoodLogList, MoodEntryDialog });
})();
