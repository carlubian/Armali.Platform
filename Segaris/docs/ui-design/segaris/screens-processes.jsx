/* global React */
// Processes — the step "path" visual language + the step-timeline popup
// (two variants) and the restructure mode. Shared orb components are exposed
// on window so the table script can reuse the mini progress indicator.
(() => {
const A = window.ProjectArmaliDesignSystem_ce5329;
const { Badge, Button, IconButton, Tooltip } = A;
const Icon = window.SegIcon;
const P = window.SegProc;
const { PROC_STATUS, CATEGORY_ICON, isResolved, frontierIndex, lastResolvedIndex,
        deriveStatus, progress, fmtShort, fmtDate, dueUrgency, dueRelative } = P;

// ── Orb ─────────────────────────────────────────────────────────
function orbClass(step, isFrontier) {
  if (step.state === "completed") return "is-done";
  if (step.state === "skipped") return "is-skipped";
  if (isFrontier) return "is-frontier";
  return "is-pending";
}
function ProcOrb({ step, index, frontier, size }) {
  const cls = orbClass(step, frontier);
  let content;
  if (step.state === "completed") content = <Icon n="check" size={size ? size * 0.5 : 22} />;
  else if (step.state === "skipped") content = <Icon n="minus" size={size ? size * 0.5 : 20} />;
  else content = index + 1;
  return (
    <span className={"proc-orb " + cls} style={size ? { "--orb-size": size + "px" } : undefined}>
      {content}
    </span>
  );
}

// ── Mini progress (table) ───────────────────────────────────────
function ProcProgress({ p }) {
  const steps = p.steps || [];
  if (steps.length === 0) return <span className="proc-prog__empty">No steps yet</span>;
  const f = frontierIndex(steps);
  const { resolved, total } = progress(p);
  const cancelled = p.cancelled;
  // Cap the dot strip so very long processes stay tidy.
  const shown = steps.slice(0, 9);
  return (
    <div className="proc-prog">
      <span className="proc-prog__dots">
        {shown.map((s, i) => {
          const isF = !cancelled && i === f;
          const cls = s.state === "completed" ? "is-done"
            : s.state === "skipped" ? "is-skipped"
            : isF ? "is-frontier" : "is-pending";
          return <span key={i} className={"proc-prog__dot " + cls} />;
        })}
        {steps.length > 9 && <span className="proc-prog__frac" style={{ fontSize: 11 }}>+{steps.length - 9}</span>}
      </span>
      <span className="proc-prog__frac">{resolved}/{total}</span>
    </div>
  );
}

// ── Track (horizontal orb path) ─────────────────────────────────
function NodeLabel({ step, index, frontier }) {
  const u = dueUrgency(step.due);
  const numLabel = step.state === "completed" ? "Done"
    : step.state === "skipped" ? "Skipped"
    : frontier ? "Current step" : "Step " + String(index + 1).padStart(2, "0");
  return (
    <React.Fragment>
      <span className="proc-node__num">{numLabel}</span>
      <span className="proc-node__desc">{step.description}</span>
      {step.due && (
        <span className={"proc-node__due is-" + u}><Icon n="calendar" size={11} />{fmtShort(step.due)}</span>
      )}
      {step.optional && step.state !== "skipped" && <span className="proc-node__flag">Optional</span>}
    </React.Fragment>
  );
}

function ProcTrack({ steps, frontier, compact }) {
  return (
    <div className="proc-track">
      {steps.map((s, i) => {
        const isF = !compact ? i === frontier : i === frontier;
        const cls = orbClass(s, i === frontier);
        const parts = [];
        if (i > 0) {
          const linkDone = isResolved(steps[i - 1].state);
          parts.push(<div key={"l" + i} className={"proc-link " + (linkDone ? "is-done" : "is-pending")} />);
        }
        parts.push(
          <div key={"n" + i} className={"proc-node " + cls} style={compact ? { width: 56 } : undefined}>
            <ProcOrb step={s} index={i} frontier={i === frontier} />
            {compact
              ? <span className="proc-node__num">{String(i + 1).padStart(2, "0")}</span>
              : <NodeLabel step={s} index={i} frontier={i === frontier} />}
          </div>
        );
        return <React.Fragment key={i}>{parts}</React.Fragment>;
      })}
    </div>
  );
}

// ── Shared header for the timeline popup ────────────────────────
function StatusBadge({ status }) {
  const s = PROC_STATUS[status];
  return <Badge tone={s.tone} dot pulse={status === "InProgress"}>{s.label}</Badge>;
}
function TimelineHead({ p, onClose }) {
  const status = deriveStatus(p);
  const { resolved, total } = progress(p);
  return (
    <div className="seg-selector__head">
      <span className="proc-name__icon" style={{ width: 44, height: 44 }}>
        <Icon n={CATEGORY_ICON[p.category] || "list-checks"} size={20} />
      </span>
      <div className="seg-selector__head-txt">
        <div className="armali-eyebrow">{p.category} · step timeline</div>
        <h3>{p.name}</h3>
        <div className="proc-tl__sub">
          <StatusBadge status={status} />
          <span className="proc-tl__metng">{total === 0 ? "No steps yet" : `${resolved} of ${total} steps resolved`}</span>
          {p.due && <span className="proc-tl__metng">· Due {fmtDate(p.due)}</span>}
        </div>
      </div>
      <IconButton label="Close" variant="ghost" icon={<Icon n="x" size={18} />} onClick={onClose} />
    </div>
  );
}

// ── The timeline popup (variant: "track" | "list") ──────────────
function ProcTimeline({ process, variant = "track", onClose }) {
  const [steps, setSteps] = React.useState(() => (process.steps || []).map((s) => ({ ...s })));
  const live = { ...process, steps };
  const f = frontierIndex(steps);
  const lastRes = lastResolvedIndex(steps);
  const status = deriveStatus(live);
  const complete = steps.length > 0 && f >= steps.length;

  const setState = (idx, state) => setSteps((ss) => ss.map((s, i) => (i === idx ? { ...s, state } : s)));
  const doComplete = () => f < steps.length && setState(f, "completed");
  const doSkip = () => f < steps.length && steps[f].optional && setState(f, "skipped");
  const doUndo = () => lastRes >= 0 && setState(lastRes, "pending");

  return (
    <div className="seg-selector" onClick={onClose}>
      <div className="seg-selector__card proc-tlcard" onClick={(e) => e.stopPropagation()}>
        <TimelineHead p={live} onClose={onClose} />

        {variant === "track" ? (
          <React.Fragment>
            <div className="proc-trackwrap">
              <ProcTrack steps={steps} frontier={f} />
            </div>

            {complete ? (
              <div className="proc-frontier proc-frontier--done">
                <span className="proc-frontier__icon"><Icon n="party-popper" size={20} /></span>
                <div className="proc-frontier__txt">
                  <div className="armali-eyebrow">Procedure complete</div>
                  <h4>Every required step is done</h4>
                  <p>Nothing left to do — this process no longer needs attention.</p>
                </div>
                <div className="proc-frontier__act">
                  {lastRes >= 0 && <Button variant="outline" size="sm" iconLeft={<Icon n="undo-2" size={15} />} onClick={doUndo}>Undo last</Button>}
                </div>
              </div>
            ) : steps.length === 0 ? (
              <div className="proc-frontier">
                <span className="proc-frontier__icon"><Icon n="list-plus" size={20} /></span>
                <div className="proc-frontier__txt">
                  <div className="armali-eyebrow">Empty procedure</div>
                  <h4>No steps yet</h4>
                  <p>Add steps to start tracking this procedure.</p>
                </div>
                <div className="proc-frontier__act"><Button variant="outline" size="sm" iconLeft={<Icon n="pencil" size={15} />}>Restructure</Button></div>
              </div>
            ) : (
              <div className="proc-frontier">
                <span className="proc-frontier__icon"><Icon n="flag" size={20} /></span>
                <div className="proc-frontier__txt">
                  <div className="armali-eyebrow">Next pending step · the frontier</div>
                  <h4>{steps[f].description}</h4>
                  <p>
                    {steps[f].due
                      ? <React.Fragment>Due {fmtDate(steps[f].due)} — {dueRelative(steps[f].due)}.</React.Fragment>
                      : "No due date set."}
                    {steps[f].notes ? " " + steps[f].notes : ""}
                  </p>
                </div>
                <div className="proc-frontier__act">
                  {lastRes >= 0 && (
                    <Tooltip label="Undo the last resolved step" side="top">
                      <IconButton variant="ghost" label="Undo" icon={<Icon n="undo-2" size={16} />} onClick={doUndo} />
                    </Tooltip>
                  )}
                  {steps[f].optional && <Button variant="outline" size="sm" iconLeft={<Icon n="chevrons-right" size={15} />} onClick={doSkip}>Skip</Button>}
                  <Button variant="primary" size="sm" iconLeft={<Icon n="check" size={16} />} onClick={doComplete}>Complete step</Button>
                </div>
              </div>
            )}
          </React.Fragment>
        ) : (
          <React.Fragment>
            {steps.length > 0 && (
              <div className="proc-trackwrap proc-spine">
                <ProcTrack steps={steps} frontier={f} compact />
              </div>
            )}
            <div className="proc-steplist">
              {steps.length === 0 && (
                <div className="proc-empty" style={{ padding: "var(--space-7) 0" }}>
                  <span className="proc-empty__icon"><Icon n="list-plus" size={26} /></span>
                  <h3>No steps yet</h3>
                  <p>This procedure is an empty container — add steps to begin.</p>
                </div>
              )}
              {steps.map((s, i) => {
                const isF = i === f;
                const cls = orbClass(s, isF);
                const u = dueUrgency(s.due);
                return (
                  <div key={s.id} className={"proc-step " + cls}>
                    <div className="proc-step__orbcol"><ProcOrb step={s} index={i} frontier={isF} /></div>
                    <div className="proc-step__main">
                      <div className="proc-step__desc">{s.description}</div>
                      <div className="proc-step__meta">
                        {s.due && <span className={"proc-step__due is-" + u}><Icon n="calendar" size={12} />{fmtDate(s.due)} · {dueRelative(s.due)}</span>}
                        {s.optional && <span className="proc-state-pill is-skipped">Optional</span>}
                        {s.notes && <span className="proc-step__note"><Icon n="sticky-note" size={12} />{s.notes}</span>}
                      </div>
                    </div>
                    <div className="proc-step__act">
                      {isF ? (
                        <React.Fragment>
                          {s.optional && <Button variant="outline" size="sm" iconLeft={<Icon n="chevrons-right" size={14} />} onClick={doSkip}>Skip</Button>}
                          <Button variant="primary" size="sm" iconLeft={<Icon n="check" size={15} />} onClick={doComplete}>Complete</Button>
                        </React.Fragment>
                      ) : i === lastRes ? (
                        <React.Fragment>
                          <span className={"proc-state-pill " + (s.state === "skipped" ? "is-skipped" : "is-done")}>
                            {s.state === "skipped" ? "Skipped" : "Completed"}
                          </span>
                          <Tooltip label="Undo — returns this step to pending" side="top">
                            <IconButton variant="ghost" size="sm" label="Undo" icon={<Icon n="undo-2" size={15} />} onClick={doUndo} />
                          </Tooltip>
                        </React.Fragment>
                      ) : (
                        <span className={"proc-state-pill " + cls}>
                          {s.state === "completed" ? "Completed" : s.state === "skipped" ? "Skipped" : "Pending"}
                        </span>
                      )}
                    </div>
                  </div>
                );
              })}
            </div>
          </React.Fragment>
        )}

        <div className="seg-selector__foot">
          <span className="seg-pageinfo"><Icon n="info" size={14} /> Steps run in strict order — only the frontier can be completed or skipped, and only the last resolved step undone.</span>
          <div className="seg-modal__foot-actions" style={{ display: "flex", gap: "var(--space-3)" }}>
            <Button variant="outline" iconLeft={<Icon n="list-restart" size={16} />}>Restructure steps</Button>
            <Button variant="ghost" onClick={onClose}>Close</Button>
          </div>
        </div>
      </div>
    </div>
  );
}

// ── Restructure mode ────────────────────────────────────────────
let _newId = 5000;
function ProcRestructure({ process, onClose }) {
  const [steps, setSteps] = React.useState(() => (process.steps || []).map((s) => ({ ...s })));
  const f = frontierIndex(steps);

  const setDesc = (id, v) => setSteps((ss) => ss.map((s) => (s.id === id ? { ...s, description: v } : s)));
  const toggleOpt = (id) => setSteps((ss) => ss.map((s) => (s.id === id ? { ...s, optional: !s.optional } : s)));
  const remove = (id) => setSteps((ss) => ss.filter((s) => s.id !== id));
  const move = (idx, dir) => setSteps((ss) => {
    const j = idx + dir;
    if (j < f || j >= ss.length) return ss; // can't move into the resolved prefix
    const copy = ss.slice();
    [copy[idx], copy[j]] = [copy[j], copy[idx]];
    return copy;
  });
  const add = () => setSteps((ss) => [...ss, { id: "s" + (++_newId), description: "", state: "pending", due: null, optional: false, notes: null }]);

  const pendingStart = f; // first non-resolved index
  return (
    <div className="seg-selector" onClick={onClose}>
      <div className="seg-selector__card proc-tlcard" onClick={(e) => e.stopPropagation()}>
        <div className="seg-selector__head">
          <span className="proc-name__icon" style={{ width: 44, height: 44 }}>
            <Icon n="list-restart" size={20} />
          </span>
          <div className="seg-selector__head-txt">
            <div className="armali-eyebrow">Restructure · {process.name}</div>
            <h3>Edit the step list</h3>
            <div className="proc-tl__sub"><span className="proc-tl__metng">Add, rename, reorder and re-date steps — even while the process is in progress.</span></div>
          </div>
          <IconButton label="Close" variant="ghost" icon={<Icon n="x" size={18} />} onClick={onClose} />
        </div>

        <div className="proc-rs__banner">
          <Icon n="shield-check" size={16} />
          <p><strong>Contiguity is protected.</strong> Resolved steps (completed or skipped) stay locked as a contiguous prefix at the front. New and pending steps can only be placed at or after the frontier; the backend rejects anything that would leave a resolved step after a pending one.</p>
        </div>

        <div className="proc-rs__list">
          {steps.map((s, i) => {
            const resolved = isResolved(s.state);
            const isF = i === f;
            const firstPending = i === pendingStart;
            return (
              <React.Fragment key={s.id}>
                {firstPending && i > 0 && (
                  <div className="proc-rs__div">
                    <span className="proc-rs__divline" />
                    <span className="proc-rs__divlabel">Frontier · editable from here</span>
                    <span className="proc-rs__divline" />
                  </div>
                )}
                <div className={"proc-rsrow" + (resolved ? " is-locked" : "")}>
                  {resolved ? (
                    <span className="proc-rsrow__grip" title="Locked — resolved"><Icon n="lock" size={14} /></span>
                  ) : (
                    <span className="proc-rsrow__grip" title="Drag to reorder"><Icon n="grip-vertical" size={16} /></span>
                  )}
                  <span className="proc-rsrow__orb"><ProcOrb step={s} index={i} frontier={isF} /></span>
                  <input className="proc-rsrow__desc" value={s.description} disabled={resolved}
                    placeholder="Describe this step…" onChange={(e) => setDesc(s.id, e.target.value)} />
                  <span className={"proc-rsrow__date" + (s.due ? "" : " is-empty")}>
                    <Icon n="calendar" size={13} />{s.due ? fmtShort(s.due) : "No date"}
                  </span>
                  {resolved ? (
                    <span className="proc-rslocktag"><Icon n={s.state === "skipped" ? "minus" : "check"} size={12} />{s.state === "skipped" ? "Skipped" : "Completed"}</span>
                  ) : (
                    <button className={"proc-rsrow__opt" + (s.optional ? " is-on" : "")} onClick={() => toggleOpt(s.id)}>
                      <Icon n={s.optional ? "split" : "minus"} size={13} />{s.optional ? "Optional" : "Required"}
                    </button>
                  )}
                  {resolved ? (
                    <span />
                  ) : (
                    <Tooltip label="Remove step" side="left">
                      <IconButton variant="ghost" size="sm" label="Remove step" icon={<Icon n="trash-2" size={15} />} onClick={() => remove(s.id)} />
                    </Tooltip>
                  )}
                </div>
              </React.Fragment>
            );
          })}
          <button className="proc-rsadd" onClick={add}><Icon n="plus" size={16} /> Add step</button>
        </div>

        <div className="seg-selector__foot">
          <span className="seg-pageinfo"><Icon n="info" size={14} /> Reordering is allowed within the pending tail; resolved steps keep their state by identity.</span>
          <div className="seg-modal__foot-actions" style={{ display: "flex", gap: "var(--space-3)" }}>
            <Button variant="ghost" onClick={onClose}>Cancel</Button>
            <Button variant="primary" iconLeft={<Icon n="check" size={16} />} onClick={onClose}>Save step list</Button>
          </div>
        </div>
      </div>
    </div>
  );
}

Object.assign(window, { ProcOrb, ProcProgress, ProcTrack, ProcTimeline, ProcRestructure });
})();
