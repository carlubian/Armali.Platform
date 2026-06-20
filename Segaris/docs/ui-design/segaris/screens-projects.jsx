/* global React */
(() => {
const A = window.ProjectArmaliDesignSystem_ce5329;
const { Badge, Button, IconButton, Tooltip, Input, Select, Switch } = A;
const Icon = window.SegIcon;
const P = window.SegProj;
const { PROJ_STATUS, PROJ_STATUS_ORDER, RISK_BAND, FILE_ICON, riskBand, bandSummary, unifiedCode, padNum } = P;
const PROGRAMS = window.SEG_PROJECTS;

// ── Flatten helpers ────────────────────────────────────────────
// Resolve an item id to { item, axis, program } with ancestor context.
function locate(id) {
  for (const program of PROGRAMS) {
    if (program.id === id) return { kind: "program", program };
    for (const axis of program.axes) {
      if (axis.id === id) return { kind: "axis", program, axis };
      for (const item of axis.items) {
        if (item.id === id) return { kind: item.type, program, axis, item };
      }
    }
  }
  return null;
}
const AXIS_OPTIONS = PROGRAMS.flatMap((pg) =>
  pg.axes.map((ax) => ({ value: ax.id, label: `${pg.name} › ${ax.name}`, code: pg.code + ax.code }))
);

// ── Small shared bits ──────────────────────────────────────────
function StatusBadge({ status, dot = true }) {
  const s = PROJ_STATUS[status];
  return <Badge tone={s.tone} dot={dot}>{s.label}</Badge>;
}
function VisibilityBadge({ visibility }) {
  const priv = visibility === "Private";
  return (
    <span className={"seg-vis" + (priv ? " is-private" : "")}>
      <Icon n={priv ? "lock" : "globe"} size={13} />{priv ? "Private" : "Public"}
    </span>
  );
}

// Risk-band summary — pills + a stacked proportion bar.
function BandSummary({ risks, compact = false }) {
  const sum = bandSummary(risks);
  const total = (risks || []).length;
  const order = ["high", "medium", "low"];
  return (
    <div className={"seg-bands" + (compact ? " is-compact" : "")}>
      <div className="seg-bands__pills">
        {["low", "medium", "high"].map((b) => (
          <span key={b} className={"seg-bandpill is-" + b}>
            <i className="seg-bandpill__dot" /><b>{sum[b]}</b>{RISK_BAND[b].label}
          </span>
        ))}
      </div>
      <div className="seg-bands__bar" title={`${total} ${total === 1 ? "risk" : "risks"}`}>
        {total === 0
          ? <span className="seg-bands__empty" />
          : order.map((b) => sum[b] > 0 && (
              <span key={b} className={"seg-bands__seg is-" + b} style={{ flexGrow: sum[b] }} />
            ))}
      </div>
    </div>
  );
}

// Inline 1–5 selector.
function Scale15({ value, onChange, tone = "aqua" }) {
  return (
    <div className="seg-scale" data-tone={tone}>
      {[1, 2, 3, 4, 5].map((n) => (
        <button key={n} type="button"
          className={"seg-scale__btn" + (n === value ? " is-active" : "")}
          onClick={() => onChange(n)}>{n}</button>
      ))}
    </div>
  );
}

// ── Tree ───────────────────────────────────────────────────────
function Tree({ selId, onSelect, showCodes, onNew }) {
  // Expand the branch that holds the current selection by default.
  const loc = selId ? locate(selId) : null;
  const [openP, setOpenP] = React.useState(() => {
    const s = {}; PROGRAMS.forEach((pg) => { s[pg.id] = loc ? loc.program.id === pg.id : pg.id === PROGRAMS[0].id; }); return s;
  });
  const [openA, setOpenA] = React.useState(() => {
    const s = {};
    PROGRAMS.forEach((pg) => pg.axes.forEach((ax) => {
      s[ax.id] = loc && loc.axis ? loc.axis.id === ax.id : (pg.id === PROGRAMS[0].id && ax === pg.axes[0]);
    }));
    return s;
  });
  const toggleP = (id) => setOpenP((s) => ({ ...s, [id]: !s[id] }));
  const toggleA = (id) => setOpenA((s) => ({ ...s, [id]: !s[id] }));

  return (
    <aside className="seg-tree">
      <div className="seg-tree__head">
        <div>
          <div className="armali-eyebrow">Hierarchy</div>
          <h3>Programs</h3>
        </div>
        <Tooltip label="New project or activity" side="bottom">
          <IconButton variant="solid" label="New item" icon={<Icon n="plus" size={17} />} onClick={onNew} />
        </Tooltip>
      </div>

      <div className="seg-tree__scroll">
        {PROGRAMS.map((pg) => {
          const pOpen = openP[pg.id];
          const nProj = pg.axes.reduce((a, ax) => a + ax.items.filter((i) => i.type === "project").length, 0);
          const nAct = pg.axes.reduce((a, ax) => a + ax.items.filter((i) => i.type === "activity").length, 0);
          return (
            <div key={pg.id} className="seg-tbranch">
              <div className={"seg-tnode seg-tnode--program" + (selId === pg.id ? " is-sel" : "")}>
                <button className="seg-tnode__twist" onClick={() => toggleP(pg.id)} aria-label={pOpen ? "Collapse" : "Expand"}>
                  <Icon n="chevron-right" size={15} className={"seg-twist" + (pOpen ? " is-open" : "")} />
                </button>
                <button className="seg-tnode__body" onClick={() => onSelect(pg.id)}>
                  <span className="seg-tnode__icon"><Icon n={pOpen ? "folder-open" : "folder"} size={16} /></span>
                  <span className="seg-code">{pg.code}</span>
                  <span className="seg-tnode__name">{pg.name}</span>
                  <span className="seg-tnode__count">{pg.axes.length}</span>
                </button>
              </div>

              {pOpen && pg.axes.map((ax) => {
                const aOpen = openA[ax.id];
                return (
                  <div key={ax.id} className="seg-tbranch seg-tbranch--axis">
                    <div className={"seg-tnode seg-tnode--axis" + (selId === ax.id ? " is-sel" : "")}>
                      <button className="seg-tnode__twist" onClick={() => toggleA(ax.id)} aria-label={aOpen ? "Collapse" : "Expand"}>
                        <Icon n="chevron-right" size={14} className={"seg-twist" + (aOpen ? " is-open" : "")} />
                      </button>
                      <button className="seg-tnode__body" onClick={() => onSelect(ax.id)}>
                        <span className="seg-tnode__icon"><Icon n="git-branch" size={15} /></span>
                        <span className="seg-code">{ax.code}</span>
                        <span className="seg-tnode__name">{ax.name}</span>
                        <span className="seg-tnode__count">{ax.items.length}</span>
                      </button>
                    </div>

                    {aOpen && (ax.items.length === 0 ? (
                      <div className="seg-tempty">Nothing here yet</div>
                    ) : ax.items.map((it) => {
                      const isProj = it.type === "project";
                      const code = unifiedCode(pg.code, ax.code, it.number);
                      const sum = isProj ? bandSummary(it.risks) : null;
                      const topBand = sum ? (sum.high ? "high" : sum.medium ? "medium" : sum.low ? "low" : null) : null;
                      return (
                        <button key={it.id}
                          className={"seg-tnode seg-tnode--leaf seg-tleaf--" + it.type + (selId === it.id ? " is-sel" : "")}
                          onClick={() => onSelect(it.id)}>
                          <span className={"seg-leaf__type seg-leaf__type--" + it.type}>
                            <Icon n={isProj ? "folder-kanban" : "circle-dot"} size={14} />
                          </span>
                          <span className="seg-leaf__txt">
                            {showCodes && <span className="seg-leaf__code">{code}</span>}
                            <span className="seg-tnode__name">{it.name}</span>
                          </span>
                          {topBand && <span className={"seg-leaf__risk is-" + topBand} title={`${(it.risks || []).length} risks`} />}
                          <span className={"seg-leaf__status seg-leaf__status--" + PROJ_STATUS[it.status].tone} title={PROJ_STATUS[it.status].label} />
                        </button>
                      );
                    }))}
                  </div>
                );
              })}
            </div>
          );
        })}
      </div>

      <div className="seg-tree__foot">
        <Icon n="info" size={13} />
        <span>Programs &amp; axes are structure — managed in Configuration.</span>
      </div>
    </aside>
  );
}

// ── Details pane ───────────────────────────────────────────────
function Empty() {
  return (
    <div className="seg-pdetail seg-pdetail--empty">
      <div className="seg-empty">
        <span className="seg-empty__icon"><Icon n="folder-tree" size={30} /></span>
        <h3>Choose a project or activity</h3>
        <p>Pick an item from the tree to see its identifier, status and — for projects — its risks and result files.</p>
      </div>
    </div>
  );
}

function StructureDetail({ loc, onSelect }) {
  const isProgram = loc.kind === "program";
  const node = isProgram ? loc.program : loc.axis;
  const children = isProgram ? loc.program.axes : loc.axis.items;
  return (
    <div className="seg-pdetail">
      <div className="seg-pdetail__head">
        <div className="armali-eyebrow">{isProgram ? "Program" : "Axis"} · structure</div>
        <div className="seg-pdetail__titlerow">
          <h2>{node.name}</h2>
          <span className="seg-code seg-code--lg">{node.code}</span>
        </div>
        <div className="seg-pdetail__badges">
          <Badge tone="azure" dot>Always public</Badge>
          {!isProgram && <span className="seg-vis"><Icon n="git-branch" size={13} />{loc.program.name}</span>}
        </div>
      </div>
      <div className="seg-pdetail__body">
        <div className="seg-pcard seg-pcard--note">
          <span className="seg-pcard__noteicon"><Icon n="settings-2" size={18} /></span>
          <div>
            <strong>Managed in Configuration</strong>
            <p>Renaming, recoding, deleting and reassigning {isProgram ? "programs" : "axes"} happens in the Configuration experience. Deleting a non-empty {isProgram ? "program" : "axis"} requires reassigning its children first.</p>
          </div>
          <Button variant="outline" size="sm" iconLeft={<Icon n="external-link" size={15} />}>Open in Configuration</Button>
        </div>
        <div className="seg-pcard">
          <div className="seg-pcard__head"><h4>{isProgram ? "Axes" : "Projects & activities"}</h4><span className="seg-pcard__n">{children.length}</span></div>
          <div className="seg-childlist">
            {children.length === 0 ? <div className="seg-childlist__empty">Empty — valid but unproductive.</div> :
              children.map((c) => (
                <button key={c.id} className="seg-childrow" onClick={() => onSelect(c.id)}>
                  <Icon n={isProgram ? "git-branch" : (c.type === "project" ? "folder-kanban" : "circle-dot")} size={15} />
                  <span className="seg-childrow__name">{c.name}</span>
                  {isProgram ? <span className="seg-code">{c.code}</span> : <StatusBadge status={c.status} />}
                  <Icon n="chevron-right" size={15} className="seg-childrow__chev" />
                </button>
              ))}
          </div>
        </div>
      </div>
    </div>
  );
}

function ItemDetail({ loc, onEdit, onRisk }) {
  const { item, axis, program } = loc;
  const isProj = item.type === "project";
  const code = unifiedCode(program.code, axis.code, item.number);
  return (
    <div className="seg-pdetail">
      <div className="seg-pdetail__head">
        <div className="seg-pdetail__crumb">
          <span>{program.name}</span><Icon n="chevron-right" size={13} /><span>{axis.name}</span>
        </div>
        <div className="seg-pdetail__titlerow">
          <h2>{item.name}</h2>
          <Badge tone={isProj ? "azure" : "neutral"}>{isProj ? "Project" : "Activity"}</Badge>
        </div>
        <div className="seg-pdetail__uid">
          <Icon n="hash" size={14} />
          <code>{code}</code>
          <span className="seg-pdetail__uidname">{item.name}</span>
          <Tooltip label="Copy identifier" side="top"><button className="seg-copy"><Icon n="copy" size={13} /></button></Tooltip>
        </div>
        <div className="seg-pdetail__badges">
          <StatusBadge status={item.status} />
          <VisibilityBadge visibility={item.visibility} />
          <span className="seg-pdetail__spacer" />
          <Button variant="ghost" size="sm" iconLeft={<Icon n="trash-2" size={15} />} className="seg-danger">Delete</Button>
          <Button variant="primary" size="sm" iconLeft={<Icon n="pencil" size={15} />} onClick={onEdit}>Edit</Button>
        </div>
      </div>

      <div className="seg-pdetail__body">
        {isProj ? (
          <React.Fragment>
            <div className="seg-pcard">
              <div className="seg-pcard__head">
                <div>
                  <h4>Risk analysis</h4>
                  <span className="seg-pcard__sub">{(item.risks || []).length} {(item.risks || []).length === 1 ? "risk" : "risks"} · score = probability × impact × mitigation</span>
                </div>
                <Button variant="outline" size="sm" iconLeft={<Icon n="shield-alert" size={15} />} onClick={onRisk}>Open risk table</Button>
              </div>
              {(item.risks || []).length === 0
                ? <div className="seg-pcard__empty">No risks recorded yet — open the risk table to add one.</div>
                : <BandSummary risks={item.risks} />}
            </div>

            <div className="seg-pcard">
              <div className="seg-pcard__head">
                <div><h4>Result files</h4><span className="seg-pcard__sub">Shared platform attachments · no thumbnails</span></div>
                <Button variant="outline" size="sm" iconLeft={<Icon n="paperclip" size={15} />}>Add file</Button>
              </div>
              {item.files.length === 0
                ? <div className="seg-pcard__empty">Nothing attached yet — add the project's result files.</div>
                : <div className="seg-files">
                    {item.files.map((f) => (
                      <div key={f.id} className="seg-file">
                        <span className="seg-file__icon"><Icon n={FILE_ICON[f.kind] || "file"} size={17} /></span>
                        <span className="seg-file__name">{f.name}</span>
                        <span className="seg-file__size">{f.size}</span>
                        <Tooltip label="Download" side="top"><IconButton size="sm" variant="ghost" label="Download" icon={<Icon n="download" size={14} />} /></Tooltip>
                        <Tooltip label="Remove" side="top"><IconButton size="sm" variant="ghost" label="Remove" icon={<Icon n="x" size={14} />} /></Tooltip>
                      </div>
                    ))}
                  </div>}
            </div>
            <MetaRow item={item} />
          </React.Fragment>
        ) : (
          <React.Fragment>
            <div className="seg-pcard seg-pcard--note">
              <span className="seg-pcard__noteicon"><Icon n="circle-dot" size={18} /></span>
              <div>
                <strong>A lightweight unit of work</strong>
                <p>An activity carries only a name and a status. It has no risks and no result files — those belong to projects.</p>
              </div>
            </div>
            <MetaRow item={item} />
          </React.Fragment>
        )}
      </div>
    </div>
  );
}

function MetaRow({ item }) {
  return (
    <div className="seg-meta">
      <div className="seg-meta__cell"><span>Number</span><b>{padNum(item.number)}</b><em>assigned at creation</em></div>
      <div className="seg-meta__cell"><span>Owner</span><b>{item.owner}</b><em>creator</em></div>
      <div className="seg-meta__cell"><span>Created</span><b>{item.created}</b></div>
      <div className="seg-meta__cell"><span>Updated</span><b>{item.updated}</b></div>
    </div>
  );
}

// ── Create / edit popup (shared; reveals project-only fields) ───
function ItemEditDialog({ mode, loc, behind, onClose }) {
  const editing = mode === "edit";
  const it = editing ? loc.item : null;
  const [type, setType] = React.useState(editing ? it.type : "project");
  const [status, setStatus] = React.useState(editing ? it.status : "Planning");
  const [vis, setVis] = React.useState(editing ? it.visibility : "Public");
  const isProj = type === "project";
  const defaultAxis = editing ? loc.axis.id : AXIS_OPTIONS[0].value;

  return (
    <div className={"seg-modal" + (behind ? " is-under" : "")} onClick={behind ? undefined : onClose}>
      <div className={"seg-modal__card seg-itemdlg" + (behind ? " is-behind" : "")} onClick={(e) => e.stopPropagation()}>
        <div className="seg-modal__head">
          <h3>{editing ? `Edit ${type}` : "New item"}</h3>
          <p>{editing
            ? "Update this item. Moving it to another axis keeps its number — the identifier is recomputed."
            : "Add a project or activity inside an axis. A global number is assigned on save."}</p>
        </div>

        <div className="seg-field">
          <span className="seg-field-label">Type</span>
          <div className="mood-seg seg-typeseg">
            {[["project", "folder-kanban", "Project"], ["activity", "circle-dot", "Activity"]].map(([v, ic, lb]) => (
              <button key={v} className={"mood-seg__btn" + (type === v ? " is-active" : "")} onClick={() => setType(v)}>
                <Icon n={ic} size={14} /> {lb}
              </button>
            ))}
          </div>
          <span className="seg-field-hint">{isProj ? "Projects carry risks and result files." : "Activities carry only a name and a status."}</span>
        </div>

        <Input label="Name" defaultValue={editing ? it.name : ""} placeholder={isProj ? "What is this project?" : "What is this activity?"} />

        <div className="seg-modal__grid">
          <div>
            <span className="seg-field-label">Status</span>
            <Select value={status} onChange={(e) => setStatus(e.target.value)}
              options={PROJ_STATUS_ORDER.map((s) => ({ value: s, label: PROJ_STATUS[s].label }))} />
          </div>
          <div>
            <span className="seg-field-label">Parent axis</span>
            <Select defaultValue={defaultAxis} options={AXIS_OPTIONS.map((a) => ({ value: a.value, label: a.label }))} />
          </div>
        </div>

        <div className="seg-field">
          <span className="seg-field-label">Visibility</span>
          <div className="mood-seg seg-visseg">
            {[["Public", "globe"], ["Private", "lock"]].map(([v, ic]) => (
              <button key={v} className={"mood-seg__btn" + (vis === v ? " is-active" : "")} onClick={() => setVis(v)}>
                <Icon n={ic} size={14} /> {v}
              </button>
            ))}
          </div>
          <span className="seg-field-hint">Public items are editable by everyone in the household. Only the creator can change visibility.</span>
        </div>

        {/* Project-only section — revealed when type = Project */}
        <div className={"seg-projonly" + (isProj ? " is-open" : "")}>
          <div className="seg-projonly__inner">
            <div className="seg-subhead"><Icon n="folder-kanban" size={14} /> Project extras</div>
            <div className="seg-projonly__row">
              <div className="seg-projonly__block">
                <span className="seg-field-label">Risk analysis</span>
                {editing && it.type === "project" && (it.risks || []).length
                  ? <BandSummary risks={it.risks} compact />
                  : <span className="seg-field-hint">No risks yet — open the risk table after saving.</span>}
              </div>
              <div className="seg-projonly__block">
                <span className="seg-field-label">Result files</span>
                <span className="seg-field-hint">
                  {editing && it.type === "project" && it.files.length
                    ? `${it.files.length} file${it.files.length === 1 ? "" : "s"} attached`
                    : "Attach result files after saving."}
                </span>
              </div>
            </div>
          </div>
        </div>

        <div className="seg-modal__foot">
          <span className="seg-modal__foot-note">
            {editing ? <React.Fragment><Icon n="hash" size={13} /> Number {padNum(it.number)} · won't change</React.Fragment>
                     : <React.Fragment><Icon n="sparkles" size={13} /> A number is assigned on save</React.Fragment>}
          </span>
          <div className="seg-modal__foot-actions">
            <Button variant="ghost" onClick={onClose}>Cancel</Button>
            <Button variant="primary" iconLeft={<Icon n={editing ? "check" : "plus"} size={17} />} onClick={onClose}>
              {editing ? "Save changes" : `Create ${type}`}
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}

// ── Risk analysis popup (live scoring) ─────────────────────────
let _rid = 1000;
function RiskDialog({ loc, onClose }) {
  const { item, axis, program } = loc;
  const code = unifiedCode(program.code, axis.code, item.number);
  const [risks, setRisks] = React.useState(() => (item.risks || []).map((r) => ({ ...r })));

  const recompute = (r) => { const score = r.p * r.i * r.m; return { ...r, score, band: riskBand(score) }; };
  const setField = (id, key, val) => setRisks((rs) => rs.map((r) => r.id === id ? recompute({ ...r, [key]: val }) : r));
  const setDesc = (id, val) => setRisks((rs) => rs.map((r) => r.id === id ? { ...r, description: val } : r));
  const remove = (id) => setRisks((rs) => rs.filter((r) => r.id !== id));
  const add = () => setRisks((rs) => [...rs, recompute({ id: "r" + (++_rid), description: "", p: 3, i: 3, m: 3 })]);

  const sum = bandSummary(risks);
  const highest = risks.reduce((m, r) => Math.max(m, r.score), 0);

  return (
    <div className="seg-selector seg-riskwrap" onClick={onClose}>
      <div className="seg-selector__card seg-riskcard" onClick={(e) => e.stopPropagation()}>
        <div className="seg-selector__head">
          <div className="seg-selector__head-txt">
            <div className="armali-eyebrow">Risk analysis</div>
            <h3>{item.name}</h3>
            <p className="seg-risk__uid"><code>{code}</code> · score = probability × impact × mitigation (1–125)</p>
          </div>
          <IconButton label="Close" variant="ghost" icon={<Icon n="x" size={18} />} onClick={onClose} />
        </div>

        {/* Band summary dashboard */}
        <div className="seg-riskdash">
          {["low", "medium", "high"].map((b) => (
            <div key={b} className={"seg-riskstat is-" + b}>
              <span className="seg-riskstat__val">{sum[b]}</span>
              <span className="seg-riskstat__lbl">{RISK_BAND[b].label} risk</span>
              <span className="seg-riskstat__rng">{b === "low" ? "score < 60" : b === "medium" ? "60 – 99" : "≥ 100"}</span>
            </div>
          ))}
          <div className="seg-riskstat seg-riskstat--total">
            <span className="seg-riskstat__val">{risks.length}</span>
            <span className="seg-riskstat__lbl">Total risks</span>
            <span className="seg-riskstat__rng">highest score {highest || "—"}</span>
          </div>
          <div className="seg-riskbar">
            <BandSummary risks={risks} />
          </div>
        </div>

        {/* Editable table */}
        <div className="seg-risktable">
          <div className="seg-riskhead">
            <span>Risk description</span>
            <span className="seg-riskhead__c">Probability</span>
            <span className="seg-riskhead__c">Impact</span>
            <span className="seg-riskhead__c">Mitigation</span>
            <span className="seg-riskhead__c seg-riskhead__score">Score</span>
            <span className="seg-riskhead__c">Band</span>
            <span></span>
          </div>
          <div className="seg-risktable__scroll">
            {risks.length === 0 ? (
              <div className="seg-pcard__empty seg-risk__empty">Nothing here yet — add the first risk to start scoring.</div>
            ) : risks.map((r) => (
              <div key={r.id} className="seg-riskrow">
                <input className="seg-riskdesc" value={r.description}
                  placeholder="Describe the risk…" onChange={(e) => setDesc(r.id, e.target.value)} />
                <div className="seg-riskrow__c"><Scale15 value={r.p} onChange={(v) => setField(r.id, "p", v)} /></div>
                <div className="seg-riskrow__c"><Scale15 value={r.i} onChange={(v) => setField(r.id, "i", v)} /></div>
                <div className="seg-riskrow__c"><Scale15 value={r.m} onChange={(v) => setField(r.id, "m", v)} /></div>
                <div className="seg-riskrow__c seg-riskrow__score" style={{ "--band": RISK_BAND[r.band].color }}>{r.score}</div>
                <div className="seg-riskrow__c"><Badge tone={RISK_BAND[r.band].tone} dot>{RISK_BAND[r.band].label}</Badge></div>
                <div className="seg-riskrow__c">
                  <Tooltip label="Delete risk" side="top"><IconButton size="sm" variant="ghost" label="Delete risk" icon={<Icon n="trash-2" size={15} />} onClick={() => remove(r.id)} /></Tooltip>
                </div>
              </div>
            ))}
          </div>
          <button className="seg-riskadd" onClick={add}><Icon n="plus" size={16} /> Add risk</button>
        </div>

        <div className="seg-selector__foot seg-riskfoot">
          <span className="seg-pageinfo"><Icon n="info" size={14} /> The score is computed by the backend — never entered by hand.</span>
          <div className="seg-modal__foot-actions">
            <Button variant="ghost" onClick={onClose}>Cancel</Button>
            <Button variant="primary" iconLeft={<Icon n="check" size={17} />} onClick={onClose}>Save risks</Button>
          </div>
        </div>
      </div>
    </div>
  );
}

// ── Orchestrator ───────────────────────────────────────────────
function ProjectsScreen({ initialSel, dialog: initialDialog, showCodes = true }) {
  const [selId, setSelId] = React.useState(initialSel || null);
  const [dialog, setDialog] = React.useState(initialDialog || null); // null | {mode:'new'} | {mode:'edit'} | {mode:'risk'}
  const loc = selId ? locate(selId) : null;

  const openNew = () => setDialog({ mode: "new" });
  const openEdit = () => setDialog({ mode: "edit" });
  const openRisk = () => setDialog({ mode: "risk" });
  const close = () => setDialog(null);

  let detail;
  if (!loc) detail = <Empty />;
  else if (loc.kind === "program" || loc.kind === "axis") detail = <StructureDetail loc={loc} onSelect={setSelId} />;
  else detail = <ItemDetail loc={loc} onEdit={openEdit} onRisk={openRisk} />;

  const popupBehind = dialog && dialog.mode === "risk";

  return (
    <div className="seg-screen">
      {window.SegShellTopBar ? <window.SegShellTopBar eyebrow="Projects" title="Projects" /> : null}
      <div className="seg-page seg-page--proj">
        <div className="seg-proj">
          <Tree selId={selId} onSelect={setSelId} showCodes={showCodes} onNew={openNew} />
          <main className="seg-proj__detail">{detail}</main>
        </div>
      </div>

      {dialog && (dialog.mode === "new" || dialog.mode === "edit") && (
        <ItemEditDialog mode={dialog.mode} loc={loc} behind={false} onClose={close} />
      )}
      {dialog && dialog.mode === "risk" && loc && loc.item && (
        <RiskDialog loc={loc} onClose={close} />
      )}
    </div>
  );
}

// ── Variants for the canvas ────────────────────────────────────
const ProjectsMain     = ({ showCodes }) => <ProjectsScreen initialSel="it-142" showCodes={showCodes} />;
const ProjectsActivity = ({ showCodes }) => <ProjectsScreen initialSel="it-099" showCodes={showCodes} />;
const ProjectsEdit     = ({ showCodes }) => <ProjectsScreen initialSel="it-142" dialog={{ mode: "edit" }} showCodes={showCodes} />;
const ProjectsRisk     = ({ showCodes }) => <ProjectsScreen initialSel="it-142" dialog={{ mode: "risk" }} showCodes={showCodes} />;

Object.assign(window, { ProjectsMain, ProjectsActivity, ProjectsEdit, ProjectsRisk });
})();
