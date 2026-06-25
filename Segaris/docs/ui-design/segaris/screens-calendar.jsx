/* global React */
// Calendar — month view. The module's only surface: a Monday-first month grid
// of projected cross-module entries + Calendar-owned daily notes, with a
// day-detail surface and source/family filters.
//
//   A · MonthBoard   — rich cells (continuous travel bars + family chips) with
//                      a persistent day-detail rail.
//   B · MonthCompact — compact family-dot indicators (the priority fallback)
//                      with the day detail in a floating popover.
//
// Both share the header (month nav · today), the filter bar, and the note editor.
(() => {
const A = window.ProjectArmaliDesignSystem_ce5329;
const { Button, IconButton, Tooltip, Badge } = A;
const Icon = window.SegIcon;
const C = window.CalData;
const { FAMILIES, FAMILY_ORDER, MODULES, SOURCE_FILTERS, TODAY } = C;

// ── Header: month navigation + today + view tabs ───────────────
function MonthNav({ year, month }) {
  return (
    <div className="cal-nav">
      <button className="cal-nav__btn" type="button" aria-label="Previous month"><Icon n="chevron-left" size={18} /></button>
      <span className="cal-nav__label">{C.MONTHS[month]} {year}</span>
      <button className="cal-nav__btn" type="button" aria-label="Next month"><Icon n="chevron-right" size={18} /></button>
    </div>
  );
}
function ViewTabs({ active }) {
  return (
    <div className="mood-seg" role="tablist" aria-label="Calendar views">
      {[["board", "Rich"], ["compact", "Compact"]].map(([k, lbl]) => (
        <button key={k} type="button" role="tab" aria-selected={k === active}
          className={"mood-seg__btn" + (k === active ? " is-active" : "")}>{lbl}</button>
      ))}
    </div>
  );
}

// ── Filter bar: source module + visual family ──────────────────
function FilterBar({ sources, families, onToggleSource, onToggleFamily, onReset }) {
  const allOn = sources.size === SOURCE_FILTERS.length && families.size === FAMILY_ORDER.length;
  return (
    <div className="cal-filters">
      <div className="cal-filters__group">
        <span className="cal-filters__lbl"><Icon n="layers" size={13} /> Family</span>
        <div className="cal-filters__chips">
          {FAMILY_ORDER.map((f) => {
            const fam = FAMILIES[f]; const on = families.has(f);
            return (
              <button key={f} type="button"
                className={"cal-toggle " + fam.cls + (on ? " is-on" : " is-off")}
                onClick={() => onToggleFamily(f)}>
                <span className="cal-toggle__dot" /> {fam.label}
              </button>
            );
          })}
        </div>
      </div>
      <span className="cal-filters__divider" />
      <div className="cal-filters__group">
        <span className="cal-filters__lbl"><Icon n="filter" size={13} /> Source</span>
        <div className="cal-filters__chips">
          {SOURCE_FILTERS.map((s) => {
            const m = MODULES[s]; const on = sources.has(s);
            return (
              <button key={s} type="button"
                className={"cal-toggle" + (on ? " is-on" : " is-off")}
                onClick={() => onToggleSource(s)}>
                <Icon n={m.icon} size={14} /> {m.label}
              </button>
            );
          })}
        </div>
      </div>
      {!allOn && (
        <button type="button" className="cal-filters__clear" onClick={onReset}>
          <Icon n="rotate-ccw" size={14} /> Reset
        </button>
      )}
    </div>
  );
}

// ── Travel continuous bar inside a cell ────────────────────────
function TravelBar({ e, date }) {
  const seg = C.tripSegment(e, date);
  const showLabel = seg.isStart || seg.weekStart;
  const cls = "cal-bar-travel" + (seg.isStart ? " is-start" : "") + (seg.isEnd ? " is-end" : "");
  return (
    <div className={cls} title={e.title}>
      {seg.isStart && <Icon n="plane" size={12} />}
      <span className={"cal-bar-travel__txt" + (showLabel ? "" : " is-ghost")}>{e.title}</span>
    </div>
  );
}

// ── A single rich day cell ─────────────────────────────────────
function RichCell({ cell, selected, filters, onSelect }) {
  const { date, inMonth } = cell;
  const isToday = C.sameDay(date, TODAY);
  const isSel = C.sameDay(date, selected);
  const list = C.orderEntries(C.entriesOnDay(date, filters));
  const trips = list.filter((e) => e.family === "Travel");
  const rest = list.filter((e) => e.family !== "Travel");
  const MAX = 3;
  const shownRest = rest.slice(0, Math.max(0, MAX - trips.length));
  const more = list.length - trips.length - shownRest.length;

  return (
    <button type="button"
      className={"cal-cell" + (inMonth ? "" : " is-out") + (isToday ? " is-today" : "") + (isSel ? " is-selected" : "")}
      onClick={() => onSelect(date)}>
      <div className="cal-cell__head">
        <span className="cal-cell__num">{date.getDate()}</span>
        {more > 0 && <span className="cal-cell__more">+{more}</span>}
      </div>
      <div className="cal-cell__events">
        {trips.map((e) => <TravelBar key={e.id} e={e} date={date} />)}
        {shownRest.map((e) => {
          const fam = FAMILIES[e.family];
          return (
            <div key={e.id} className={"cal-chip " + fam.cls}>
              <span className="cal-chip__dot" />
              <span className="cal-chip__txt">{e.title}</span>
            </div>
          );
        })}
      </div>
    </button>
  );
}

// ── A compact (dots) day cell ──────────────────────────────────
function CompactCell({ cell, selected, filters, onSelect }) {
  const { date, inMonth } = cell;
  const isToday = C.sameDay(date, TODAY);
  const isSel = C.sameDay(date, selected);
  const fams = C.familiesOnDay(date, filters); // already in priority order
  const shown = fams.slice(0, 4);
  return (
    <button type="button"
      className={"cal-cell cal-cell--compact" + (inMonth ? "" : " is-out") + (isToday ? " is-today" : "") + (isSel ? " is-selected" : "")}
      onClick={() => onSelect(date)}>
      <div className="cal-cell__head">
        <span className="cal-cell__num">{date.getDate()}</span>
      </div>
      {shown.length > 0 && (
        <div className="cal-dots">
          {shown.map((f) => <span key={f} className={"cal-dot " + FAMILIES[f].cls} />)}
        </div>
      )}
    </button>
  );
}

// ── The grid (shared shell, swappable cell) ────────────────────
function Grid({ year, month, selected, filters, onSelect, Cell }) {
  const cells = C.monthGrid(year, month);
  const weeks = [];
  for (let i = 0; i < cells.length; i += 7) weeks.push(cells.slice(i, i + 7));
  return (
    <div className="cal-gridcard">
      <div className="cal-dow">
        {C.DOW_SHORT.map((d, i) => (
          <div key={d} className={"cal-dow__cell" + (i >= 5 ? " is-weekend" : "")}>{d}</div>
        ))}
      </div>
      <div className="cal-grid">
        {weeks.map((wk, wi) => (
          <div key={wi} className="cal-week">
            {wk.map((cell) => (
              <Cell key={C.ymd(cell.date)} cell={cell} selected={selected} filters={filters} onSelect={onSelect} />
            ))}
          </div>
        ))}
      </div>
    </div>
  );
}

// ── Day-detail content (grouped by family) ─────────────────────
function DayItem({ e, date, onEditNote }) {
  const fam = FAMILIES[e.family];
  const mod = MODULES[e.module];
  const isNote = !!e.isNote;
  const isTrip = e.family === "Travel";
  const tripLabel = isTrip ? `Day ${C.tripDays(e.start, date)} of ${C.tripDays(e.start, e.end)}` : null;
  return (
    <button type="button" className={"cal-item " + fam.cls} onClick={isNote ? () => onEditNote(e) : undefined}>
      <span className="cal-item__icon"><Icon n={isNote ? "sticky-note" : (isTrip ? "plane" : (e.family === "Birthday" ? "cake" : mod.icon))} size={16} /></span>
      <span className="cal-item__body">
        <span className="cal-item__title">{e.title}</span>
        {e.supporting && <span className="cal-item__sub">{e.supporting}</span>}
        <span className="cal-item__meta">
          {isNote ? (
            <span className={"cal-vis" + (e.visibility === "Private" ? " is-private" : "")}>
              <Icon n={e.visibility === "Private" ? "lock" : "users"} size={11} /> {e.visibility}
            </span>
          ) : (
            <span className="cal-item__src"><Icon n={mod.icon} size={11} /> {mod.label}</span>
          )}
          {tripLabel && <><span className="cal-item__sep" /><span className="cal-item__span">{tripLabel}</span></>}
          {e.status && <><span className="cal-item__sep" /><span className="cal-item__status">{e.status}</span></>}
        </span>
      </span>
      <span className="cal-item__chev">
        {isNote
          ? <Icon n="pencil" size={15} />
          : <Tooltip label={e.route || "Informational"} side="left"><Icon n={e.route ? "arrow-up-right" : "info"} size={15} /></Tooltip>}
      </span>
    </button>
  );
}

function DayDetailBody({ date, filters, onNewNote, onEditNote }) {
  const list = C.orderEntries(C.entriesOnDay(date, filters));
  if (list.length === 0) {
    return (
      <div className="cal-empty">
        <span className="cal-empty__icon"><Icon n="calendar-days" size={26} /></span>
        <p>Nothing on this day yet — add a note to remember something.</p>
        <Button variant="outline" size="sm" iconLeft={<Icon n="plus" size={15} />} onClick={() => onNewNote(date)}>Add a note</Button>
      </div>
    );
  }
  const groups = FAMILY_ORDER
    .map((f) => ({ fam: FAMILIES[f], items: list.filter((e) => e.family === f) }))
    .filter((g) => g.items.length > 0);
  return (
    <React.Fragment>
      {groups.map((g) => (
        <div key={g.fam.key} className={"cal-group " + g.fam.cls}>
          <div className="cal-group__head">
            <span className="cal-group__icon"><Icon n={g.fam.icon} size={14} /></span>
            <span className="cal-group__lbl">{g.fam.label}</span>
            <span className="cal-group__n">{g.items.length}</span>
          </div>
          <div className="cal-group__items">
            {g.items.map((e) => <DayItem key={e.id} e={e} date={date} onEditNote={onEditNote} />)}
          </div>
        </div>
      ))}
    </React.Fragment>
  );
}

function DayHeader({ date }) {
  const isToday = C.sameDay(date, TODAY);
  return (
    <div className="cal-detail__head-txt">
      <div className="cal-detail__dow">{C.DOW_LONG[(date.getDay() + 6) % 7]}</div>
      <div className="cal-detail__date">
        <h3>{date.getDate()}</h3>
        <span>{C.MONTHS[date.getMonth()]} {date.getFullYear()}</span>
        {isToday && <span className="cal-detail__todaytag">Today</span>}
      </div>
    </div>
  );
}

// ── Shared header + filter state hook ──────────────────────────
function useCalState() {
  const [selected, setSelected] = React.useState(TODAY);
  const [sources, setSources] = React.useState(new Set(SOURCE_FILTERS));
  const [families, setFamilies] = React.useState(new Set(FAMILY_ORDER));
  const [dialog, setDialog] = React.useState(null);
  const toggle = (set, setter) => (k) => {
    const next = new Set(set); next.has(k) ? next.delete(k) : next.add(k); setter(next);
  };
  const reset = () => { setSources(new Set(SOURCE_FILTERS)); setFamilies(new Set(FAMILY_ORDER)); };
  const filters = { sources, families };
  return { selected, setSelected, filters, dialog, setDialog,
    onToggleSource: toggle(sources, setSources), onToggleFamily: toggle(families, setFamilies), reset };
}

function Header({ activeView, onNewNote }) {
  return (
    <div className="cal-bar">
      <div>
        <div className="armali-eyebrow">June · 2026 · Europe/Madrid</div>
        <h2>Calendar</h2>
        <p>One view of everything date-bound across your home — projected from each module, plus your own notes.</p>
      </div>
      <div className="cal-bar__controls">
        <ViewTabs active={activeView} />
        <MonthNav year={2026} month={5} />
        <button type="button" className="cal-today"><Icon n="dot" size={16} /> Today</button>
        <Button variant="primary" iconLeft={<Icon n="plus" size={17} />} onClick={() => onNewNote()}>New note</Button>
      </div>
    </div>
  );
}

// ── Variant A · rich board + day-detail rail ───────────────────
function MonthBoard() {
  const s = useCalState();
  const openNew = (date) => s.setDialog({ mode: "new", date: date || s.selected });
  const openEdit = (note) => s.setDialog({ mode: "edit", note });
  return (
    <div className="seg-screen cal-screen">
      <window.SegShellTopBar eyebrow="Calendar" title="Month" />
      <div className="seg-page">
        <div className="cal-page__inner">
          <Header activeView="board" onNewNote={openNew} />
          <FilterBar {...s.filters} onToggleSource={s.onToggleSource} onToggleFamily={s.onToggleFamily} onReset={s.reset} />
          <div className="cal-layout">
            <Grid year={2026} month={5} selected={s.selected} filters={s.filters} onSelect={s.setSelected} Cell={RichCell} />
            <aside className="cal-detail">
              <div className="cal-detail__head"><DayHeader date={s.selected} /></div>
              <div className="cal-detail__body"><DayDetailBody date={s.selected} filters={s.filters} onNewNote={openNew} onEditNote={openEdit} /></div>
              <div className="cal-detail__foot">
                <Button variant="outline" block iconLeft={<Icon n="plus" size={16} />} onClick={() => openNew(s.selected)}>Add note to this day</Button>
              </div>
            </aside>
          </div>
        </div>
      </div>
      {s.dialog && <window.CalNoteEditor {...s.dialog} onClose={() => s.setDialog(null)} />}
    </div>
  );
}

// ── Variant B · compact dots + floating day-detail popover ─────
function MonthCompact() {
  const s = useCalState();
  const openNew = (date) => s.setDialog({ mode: "new", date: date || s.selected });
  const openEdit = (note) => s.setDialog({ mode: "edit", note });
  return (
    <div className="seg-screen cal-screen">
      <window.SegShellTopBar eyebrow="Calendar" title="Month" />
      <div className="seg-page">
        <div className="cal-page__inner">
          <Header activeView="compact" onNewNote={openNew} />
          <FilterBar {...s.filters} onToggleSource={s.onToggleSource} onToggleFamily={s.onToggleFamily} onReset={s.reset} />
          <div className="cal-layout cal-layout--full" style={{ position: "relative" }}>
            <Grid year={2026} month={5} selected={s.selected} filters={s.filters} onSelect={s.setSelected} Cell={CompactCell} />
            <div className="cal-popover" style={{ top: 8, right: 8 }}>
              <div className="cal-detail__head">
                <DayHeader date={s.selected} />
                <span className="cal-popover__close">
                  <IconButton label="Close" variant="ghost" icon={<Icon n="x" size={16} />} />
                </span>
              </div>
              <div className="cal-detail__body"><DayDetailBody date={s.selected} filters={s.filters} onNewNote={openNew} onEditNote={openEdit} /></div>
              <div className="cal-detail__foot">
                <Button variant="outline" block size="sm" iconLeft={<Icon n="plus" size={15} />} onClick={() => openNew(s.selected)}>Add note</Button>
              </div>
            </div>
          </div>
        </div>
      </div>
      {s.dialog && <window.CalNoteEditor {...s.dialog} onClose={() => s.setDialog(null)} />}
    </div>
  );
}

Object.assign(window, { CalMonthBoard: MonthBoard, CalMonthCompact: MonthCompact });
})();
