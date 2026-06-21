/* global React */
// Processes — the primary table view (search · filters · sort · pagination),
// the create/edit popup, and the orchestrator that wires the step-timeline and
// restructure popups over the table. Canvas variants are exported at the end.
(() => {
const A = window.ProjectArmaliDesignSystem_ce5329;
const { Badge, Button, IconButton, Tooltip, Input, Select } = A;
const Icon = window.SegIcon;
const P = window.SegProc;
const { PROC_STATUS, PROC_STATUS_ORDER, PROC_CATEGORIES, CATEGORY_ICON,
        deriveStatus, progress, effectiveDue, needsAttention,
        fmtDate, dueRelative, dueUrgency } = P;
const ALL = window.SEG_PROCESSES;

const ATTACH_ICON = { pdf: "file-text", image: "file-image", zip: "file-archive", sheet: "file-spreadsheet", doc: "file-text" };

function StatusBadge({ status }) {
  const s = PROC_STATUS[status];
  return <Badge tone={s.tone} dot pulse={status === "InProgress"}>{s.label}</Badge>;
}
function VisibilityCell({ visibility }) {
  const priv = visibility === "Private";
  return (
    <span className={"proc-vis" + (priv ? " is-private" : "")}>
      <Icon n={priv ? "lock" : "globe"} size={13} />{priv ? "Private" : "Public"}
    </span>
  );
}
function DueCell({ p }) {
  const eff = effectiveDue(p);
  const status = deriveStatus(p);
  if (!eff.date) return <span className="proc-due--none">— no date</span>;
  const closed = status === "Completed" || status === "Cancelled";
  const u = closed ? "later" : dueUrgency(eff.date);
  return (
    <div className="proc-due">
      <span className="proc-due__date">{fmtDate(eff.date)}</span>
      {!closed && <span className={"proc-due__rel is-" + u}>{dueRelative(eff.date)}</span>}
      <span className="proc-due__src">{eff.source === "step" ? "next step" : "process date"}</span>
    </div>
  );
}

// ── Create / edit process popup ─────────────────────────────────
function ProcessEditDialog({ mode, process, onClose }) {
  const editing = mode === "edit";
  const p = editing ? process : null;
  const [vis, setVis] = React.useState(editing ? p.visibility : "Public");
  const [cat, setCat] = React.useState(editing ? p.category : PROC_CATEGORIES[0].value);
  const attachments = editing ? (p.attachments || []) : [];

  return (
    <div className="seg-modal" onClick={onClose}>
      <div className="seg-modal__card proc-editdlg" onClick={(e) => e.stopPropagation()}>
        <div className="seg-modal__head">
          <h3>{editing ? "Edit process" : "New process"}</h3>
          <p>{editing
            ? "Update this procedure's own fields. Steps are managed from the step timeline."
            : "Create a procedure container. You can add its steps afterwards from the timeline."}</p>
        </div>

        <Input label="Name" defaultValue={editing ? p.name : ""} placeholder="What procedure is this?" iconLeft={<Icon n="list-checks" size={16} />} />

        <div className="proc-fieldrow">
          <div>
            <span className="seg-field-label">Category</span>
            <Select value={cat} onChange={(e) => setCat(e.target.value)}
              options={PROC_CATEGORIES.map((c) => ({ value: c.value, label: c.value }))} />
          </div>
          <Input label="Global due date" type="date" defaultValue={editing && p.due ? p.due : ""} hint="Optional" />
        </div>

        <div className="seg-field">
          <span className="seg-field-label">Notes</span>
          <textarea className="proc-textarea" defaultValue={editing ? (p.notes || "") : ""}
            placeholder="Context, reference numbers, reminders… (optional)" />
          <span className="seg-field-hint">Up to 4,000 characters.</span>
        </div>

        <div className="seg-field">
          <span className="seg-field-label">Visibility</span>
          <div className="mood-seg proc-visseg">
            {[["Public", "globe"], ["Private", "lock"]].map(([v, ic]) => (
              <button key={v} className={"mood-seg__btn" + (vis === v ? " is-active" : "")} onClick={() => setVis(v)}>
                <Icon n={ic} size={14} /> {v}
              </button>
            ))}
          </div>
          <span className="seg-field-hint">New processes default to public — any household member can edit a public process. Only the creator can change visibility.</span>
        </div>

        <div className="seg-field">
          <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
            <span className="seg-field-label" style={{ marginBottom: 0 }}>Attachments</span>
            <Button variant="outline" size="sm" iconLeft={<Icon n="paperclip" size={14} />}>Add file</Button>
          </div>
          <div className="proc-attach">
            {attachments.length === 0
              ? <span className="proc-attach__empty">No attachments — add forms, receipts or official letters.</span>
              : attachments.map((a) => (
                  <div key={a.id} className="proc-attach__row">
                    <span className="proc-attach__icon"><Icon n={ATTACH_ICON[a.kind] || "file"} size={16} /></span>
                    <span className="proc-attach__name">{a.name}</span>
                    <span className="proc-attach__size">{a.size}</span>
                    <IconButton variant="ghost" size="sm" label="Remove" icon={<Icon n="x" size={14} />} />
                  </div>
                ))}
          </div>
        </div>

        <div className="seg-modal__foot">
          <span className="seg-modal__foot-note" style={{ display: "inline-flex", alignItems: "center", gap: "0.4em", marginRight: "auto", color: "var(--text-muted)", fontFamily: "var(--font-body)", fontSize: "var(--text-sm)" }}>
            {editing
              ? <React.Fragment><Icon n="circle-dashed" size={13} /> Status is derived from steps</React.Fragment>
              : <React.Fragment><Icon n="sparkles" size={13} /> Starts public, not started, no steps</React.Fragment>}
          </span>
          <Button variant="ghost" onClick={onClose}>Cancel</Button>
          <Button variant="primary" iconLeft={<Icon n={editing ? "check" : "plus"} size={17} />} onClick={onClose}>
            {editing ? "Save changes" : "Create process"}
          </Button>
        </div>
      </div>
    </div>
  );
}

// ── Pager ───────────────────────────────────────────────────────
function Pager({ page, pages, onPage }) {
  const items = Array.from({ length: pages }, (_, i) => i + 1);
  return (
    <div className="seg-pager">
      <button className="seg-pager__btn" disabled={page <= 1} onClick={() => onPage(page - 1)} aria-label="Previous page"><Icon n="chevron-left" size={16} /></button>
      {items.map((p) => (
        <button key={p} className={"seg-pager__btn" + (p === page ? " is-active" : "")} onClick={() => onPage(p)}>{p}</button>
      ))}
      <button className="seg-pager__btn" disabled={page >= pages} onClick={() => onPage(page + 1)} aria-label="Next page"><Icon n="chevron-right" size={16} /></button>
    </div>
  );
}

// ── Sorting ─────────────────────────────────────────────────────
function effDueValue(p) {
  const eff = effectiveDue(p);
  if (!eff.date) return Infinity; // no date sorts last
  return P.parseDate(eff.date).getTime();
}
function sortProcesses(list, sort) {
  const arr = list.slice();
  arr.sort((a, b) => {
    let cmp = 0;
    if (sort.key === "name") cmp = a.name.localeCompare(b.name);
    else cmp = effDueValue(a) - effDueValue(b); // default: effective due asc, none last
    if (cmp === 0) cmp = a.id.localeCompare(b.id);
    return sort.dir === "desc" ? -cmp : cmp;
  });
  return arr;
}

// ── Orchestrator ────────────────────────────────────────────────
function ProcessesScreen({ initialDialog }) {
  const [search, setSearch] = React.useState("");
  const [cat, setCat] = React.useState("");
  const [status, setStatus] = React.useState("");
  const [sort, setSort] = React.useState({ key: "due", dir: "asc" });
  const [dialog, setDialog] = React.useState(initialDialog || null);

  const filtered = React.useMemo(() => {
    let list = ALL.filter((p) => {
      if (cat && p.category !== cat) return false;
      if (status && deriveStatus(p) !== status) return false;
      if (search.trim()) {
        const q = search.trim().toLowerCase();
        if (!(p.name.toLowerCase().includes(q) || (p.notes || "").toLowerCase().includes(q))) return false;
      }
      return true;
    });
    return sortProcesses(list, sort);
  }, [search, cat, status, sort]);

  const total = ALL.length;
  const openAttn = ALL.filter(needsAttention).length;
  const completed = ALL.filter((p) => deriveStatus(p) === "Completed").length;

  const toggleSort = (key) => setSort((s) => (s.key === key ? { key, dir: s.dir === "asc" ? "desc" : "asc" } : { key, dir: "asc" }));
  const sortIcon = (key) => (sort.key !== key ? null : <span className="proc-th__sort"><Icon n={sort.dir === "asc" ? "arrow-up" : "arrow-down"} size={13} /></span>);

  const close = () => setDialog(null);
  const openTimeline = (process, variant) => setDialog({ mode: "timeline", process, variant });
  const openEdit = (process) => setDialog({ mode: "edit", process });

  return (
    <div className="seg-screen">
      {window.SegShellTopBar ? <window.SegShellTopBar eyebrow="Processes" title="Processes" /> : null}
      <div className="seg-page">
        <div className="seg-page__inner">
          <div className="proc-bar">
            <div>
              <div className="armali-eyebrow">Step-by-step procedures</div>
              <h2>Processes</h2>
              <p>Sequential bureaucratic, legal and administrative procedures — tracked step by step toward a due date.</p>
            </div>
            <div className="proc-bar__stats">
              <div className="seg-stat-pill"><strong>{total}</strong><span>Processes</span></div>
              <div className="seg-stat-pill"><strong>{openAttn}</strong><span>Need attention</span></div>
              <div className="seg-stat-pill"><strong>{completed}</strong><span>Completed</span></div>
              <Button variant="primary" iconLeft={<Icon n="plus" size={17} />} onClick={() => setDialog({ mode: "new" })}>New process</Button>
            </div>
          </div>

          <div className="proc-toolbar">
            <div className="proc-toolbar__search">
              <Input placeholder="Search name and notes" value={search}
                onChange={(e) => setSearch(e.target.value)} iconLeft={<Icon n="search" size={16} />} />
            </div>
            <Select value={cat} onChange={(e) => setCat(e.target.value)}
              options={[{ value: "", label: "All categories" }, ...PROC_CATEGORIES.map((c) => ({ value: c.value, label: c.value }))]} />
            <Select value={status} onChange={(e) => setStatus(e.target.value)}
              options={[{ value: "", label: "All statuses" }, ...PROC_STATUS_ORDER.map((s) => ({ value: s, label: PROC_STATUS[s].label }))]} />
          </div>

          <div className="proc-tablecard">
            <div className="proc-table">
              <div className="proc-thead">
                <button className="proc-th" onClick={() => toggleSort("name")}>Process {sortIcon("name")}</button>
                <span>Category</span>
                <span>Status</span>
                <span>Progress</span>
                <button className="proc-th" onClick={() => toggleSort("due")}>Effective due {sortIcon("due")}</button>
                <span>Visibility</span>
                <span className="proc-th--right" style={{ justifySelf: "end" }}>Manage</span>
              </div>
              <div className="proc-tbody">
                {filtered.length === 0 ? (
                  <div className="proc-empty">
                    <span className="proc-empty__icon"><Icon n="search-x" size={26} /></span>
                    <h3>Nothing matches</h3>
                    <p>No processes match your search and filters — try clearing them.</p>
                  </div>
                ) : filtered.map((p) => {
                  const st = deriveStatus(p);
                  const closed = st === "Completed" || st === "Cancelled";
                  return (
                    <div key={p.id} role="button" tabIndex={0} className={"proc-trow" + (closed ? " is-closed" : "")} onClick={() => openTimeline(p, "track")}>
                      <div className="proc-name">
                        <span className="proc-name__icon"><Icon n={CATEGORY_ICON[p.category] || "list-checks"} size={17} /></span>
                        <div className="proc-name__txt">
                          <strong>{p.name}</strong>
                          <em>{p.owner}</em>
                        </div>
                      </div>
                      <span className="proc-cat"><Icon n={CATEGORY_ICON[p.category]} size={14} />{p.category}</span>
                      <span><StatusBadge status={st} /></span>
                      <span><window.ProcProgress p={p} /></span>
                      <DueCell p={p} />
                      <VisibilityCell visibility={p.visibility} />
                      <div className="proc-trow__act" onClick={(e) => e.stopPropagation()}>
                        <Tooltip label="Step timeline" side="top">
                          <IconButton size="sm" label="Step timeline" icon={<Icon n="git-commit-vertical" size={15} />} onClick={() => openTimeline(p, "list")} />
                        </Tooltip>
                        <Tooltip label="Edit process" side="top">
                          <IconButton size="sm" label="Edit process" icon={<Icon n="pencil" size={15} />} onClick={() => openEdit(p)} />
                        </Tooltip>
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          </div>

          <div className="seg-selector__foot" style={{ borderTop: "none", padding: "0 var(--space-2)", background: "transparent" }}>
            <span className="seg-pageinfo">{filtered.length === 0 ? "No results" : <React.Fragment>Showing <b>1–{filtered.length}</b> of <b>{filtered.length}</b></React.Fragment>}</span>
            <div style={{ display: "flex", alignItems: "center", gap: "var(--space-4)" }}>
              <Select value="25" options={[{ value: "25", label: "25 per page" }, { value: "50", label: "50 per page" }]} onChange={() => {}} />
              <Pager page={1} pages={1} onPage={() => {}} />
            </div>
          </div>
        </div>
      </div>

      {dialog && (dialog.mode === "new" || dialog.mode === "edit") && (
        <ProcessEditDialog mode={dialog.mode} process={dialog.process} onClose={close} />
      )}
      {dialog && dialog.mode === "timeline" && (
        <window.ProcTimeline process={dialog.process} variant={dialog.variant} onClose={close} />
      )}
      {dialog && dialog.mode === "restructure" && (
        <window.ProcRestructure process={dialog.process} onClose={close} />
      )}
    </div>
  );
}

const featured = ALL.find((p) => p.id === "pc-pass");
const mortgage = ALL.find((p) => p.id === "pc-mort");

const ProcessesTable      = () => <ProcessesScreen />;
const ProcessesTimelineA  = () => <ProcessesScreen initialDialog={{ mode: "timeline", process: featured, variant: "track" }} />;
const ProcessesTimelineB  = () => <ProcessesScreen initialDialog={{ mode: "timeline", process: featured, variant: "list" }} />;
const ProcessesRestructure = () => <ProcessesScreen initialDialog={{ mode: "restructure", process: mortgage }} />;
const ProcessesEdit       = () => <ProcessesScreen initialDialog={{ mode: "edit", process: featured }} />;

Object.assign(window, { ProcessesTable, ProcessesTimelineA, ProcessesTimelineB, ProcessesRestructure, ProcessesEdit });
})();
