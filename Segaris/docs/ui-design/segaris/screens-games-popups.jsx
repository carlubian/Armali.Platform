/* global React */
// Games — shared presentational bits + the URL-aware popups: the playthrough
// editor (create / edit, with the game→catalogue reference, start month/year,
// manual status, free-text tags, visibility), the manage-sections popup
// (reorder + colour), and the game catalogue's replace-and-delete popup.
// The playthrough editor floats the game selector over the dimmed editor
// (the entity-selector pattern). Exposed on window for the other babel scripts.
(() => {
const A = window.ProjectArmaliDesignSystem_ce5329;
const { Badge, Button, IconButton, Tooltip, Input, Select } = A;
const Icon = window.SegIcon;
const G = window.SegGames;
const { PLAT, STATUS, STATUSES, PLATFORMS, SECTION_COLORS, GM } = G;

// ── Shared presentational bits ──────────────────────────────────
function GmPlat({ platform }) {
  const p = PLAT[platform] || PLAT.Other;
  return <span className={"gm-plat gm-tone--" + p.tone}><Icon n={p.icon} size={12} />{p.label}</span>;
}

function GmStatus({ status }) {
  const s = STATUS[status] || STATUSES[0];
  const pulse = status === "Active";
  return <Badge tone={s.tone} dot pulse={pulse}>{s.label}</Badge>;
}

function GmVis({ visibility }) {
  const priv = visibility === "Private";
  return <span className={"gm-vis" + (priv ? " is-private" : "")}><Icon n={priv ? "lock" : "globe"} size={13} />{priv ? "Private" : "Public"}</span>;
}

function GmTags({ tags, max = 99 }) {
  if (!tags || tags.length === 0) return null;
  const shown = tags.slice(0, max);
  const extra = tags.length - shown.length;
  return (
    <span className="gm-tags">
      {shown.map((t) => <span key={t} className="gm-tag"><Icon n="tag" size={11} />{t}</span>)}
      {extra > 0 && <span className="gm-tag">+{extra}</span>}
    </span>
  );
}

// Linear progress bar with count + percentage.
function GmProgBar({ done, total }) {
  const p = G.pct(done, total);
  const empty = total === 0;
  return (
    <div className={"gm-progbar" + (empty ? " is-empty" : "")}>
      <div className="gm-progbar__top">
        <span className="gm-progbar__count">{empty ? "No goals yet" : <React.Fragment><b>{done}</b> of <b>{total}</b> goals</React.Fragment>}</span>
        <span className="gm-progbar__pct">{empty ? "—" : p + "%"}</span>
      </div>
      <div className="gm-progbar__track"><div className="gm-progbar__fill" style={{ width: (empty ? 0 : p) + "%" }} /></div>
    </div>
  );
}

// Circular progress ring.
function GmRing({ done, total, size = 60, stroke = 6 }) {
  const p = G.pct(done, total);
  const empty = total === 0;
  const r = (size - stroke) / 2;
  const c = 2 * Math.PI * r;
  const off = c * (1 - (empty ? 0 : p) / 100);
  return (
    <div className={"gm-ring" + (empty ? " is-empty" : "")} style={{ width: size, height: size }}>
      <svg width={size} height={size}>
        <circle className="gm-ring__track" cx={size / 2} cy={size / 2} r={r} fill="none" strokeWidth={stroke} />
        <circle className="gm-ring__val" cx={size / 2} cy={size / 2} r={r} fill="none" strokeWidth={stroke}
          strokeDasharray={c} strokeDashoffset={empty ? c : off} />
      </svg>
      <span className="gm-ring__label">
        <span className="gm-ring__pct">{empty ? "—" : <React.Fragment>{p}<small>%</small></React.Fragment>}</span>
      </span>
    </div>
  );
}

// ── Playthrough editor (create / edit) ──────────────────────────
function PlaythroughEditDialog({ mode, playthrough, onClose, startPicking = false }) {
  const editing = mode === "edit";
  const pt = editing ? playthrough : null;
  const [gameId, setGameId] = React.useState(editing ? pt.gameId : null);
  const [status, setStatus] = React.useState(editing ? pt.status : "Planning");
  const [vis, setVis] = React.useState(editing ? pt.visibility : "Public");
  const [tags, setTags] = React.useState(editing ? pt.tags.slice() : []);
  const [draft, setDraft] = React.useState("");
  const [picking, setPicking] = React.useState(startPicking);

  const game = gameId ? GM[gameId] : null;
  const plat = game ? PLAT[game.platform] : null;

  const addTag = () => {
    const v = draft.trim();
    if (v && !tags.some((t) => t.toLowerCase() === v.toLowerCase())) setTags((xs) => [...xs, v]);
    setDraft("");
  };
  const onTagKey = (e) => {
    if (e.key === "Enter" || e.key === ",") { e.preventDefault(); addTag(); }
    else if (e.key === "Backspace" && !draft && tags.length) setTags((xs) => xs.slice(0, -1));
  };

  const now = G.TODAY;
  const years = [];
  for (let y = now.getFullYear() + 1; y >= 2005; y--) years.push(y);

  return (
    <div className={"seg-modal" + (picking ? " is-under" : "")} onClick={picking ? undefined : onClose}>
      <div className={"seg-modal__card gm-editdlg" + (picking ? " is-behind" : "")} onClick={(e) => e.stopPropagation()}>
        <div className="seg-modal__head">
          <h3>{editing ? "Edit playthrough" : "New playthrough"}</h3>
          <p>{editing
            ? "Update this run — its game, start date, status and tags. Progress lives on the progress page."
            : "Track a new run of a game. New playthroughs start Public, Planning, with no sections."}</p>
        </div>

        <div className="gm-editdlg__body">
          {/* Name */}
          <div className="seg-field">
            <span className="seg-field-label">Name <span style={{ color: "var(--terracotta-500)" }}>*</span></span>
            <Input defaultValue={editing ? pt.name : ""} placeholder="e.g. First run, Honour mode, Table campaign" iconLeft={<Icon n="bookmark" size={16} />} />
          </div>

          {/* Game reference → opens the selector */}
          <div className="seg-field">
            <span className="seg-field-label">Game <span style={{ color: "var(--terracotta-500)" }}>*</span></span>
            <button type="button" className={"gm-gameref" + (game ? " is-filled gm-tone--" + plat.tone : " is-empty")} onClick={() => setPicking(true)}>
              <span className="gm-gameref__icon"><Icon n={game ? plat.icon : "gamepad-2"} size={19} /></span>
              <span className="gm-gameref__body">
                {game
                  ? <React.Fragment><span className="gm-gameref__name">{game.name}</span><span className="gm-gameref__meta">{plat.label}</span></React.Fragment>
                  : <span className="gm-gameref__placeholder">Choose a game from the catalogue</span>}
              </span>
              <span className="gm-gameref__chev"><Icon n="chevrons-up-down" size={16} /></span>
            </button>
            <span className="seg-field-hint">A playthrough cannot exist without a game. The catalogue is managed in Configuration.</span>
          </div>

          {/* Start month + year */}
          <div className="gm-grid2">
            <div>
              <span className="seg-field-label">Start month <span style={{ color: "var(--terracotta-500)" }}>*</span></span>
              <Select defaultValue={editing ? String(pt.startMonth) : "7"}
                options={G.MONTHS.map((m, i) => ({ value: String(i + 1), label: m }))} />
            </div>
            <div>
              <span className="seg-field-label">Start year <span style={{ color: "var(--terracotta-500)" }}>*</span></span>
              <Select defaultValue={editing ? String(pt.startYear) : String(now.getFullYear())}
                options={years.map((y) => ({ value: String(y), label: String(y) }))} />
            </div>
          </div>

          {/* Status */}
          <div className="seg-field">
            <span className="seg-field-label">Status</span>
            <div className="mood-seg seg-typeseg">
              {STATUSES.map((s) => (
                <button key={s.value} className={"mood-seg__btn" + (status === s.value ? " is-active" : "")} onClick={() => setStatus(s.value)}>
                  <Icon n={s.icon} size={14} /> {s.label}
                </button>
              ))}
            </div>
            <span className="seg-field-hint">Manual and descriptive — never changes on its own and never checks against goals.</span>
          </div>

          {/* Tags */}
          <div className="seg-field">
            <span className="seg-field-label">Tags</span>
            <div className="gm-taginput">
              {tags.map((t) => (
                <span key={t} className="gm-tagchip">{t}
                  <span className="gm-tagchip__x" role="button" aria-label={"Remove " + t} onClick={() => setTags((xs) => xs.filter((x) => x !== t))}><Icon n="x" size={11} /></span>
                </span>
              ))}
              <input value={draft} placeholder={tags.length ? "Add another…" : "Add a tag, press Enter"} onChange={(e) => setDraft(e.target.value)} onKeyDown={onTagKey} onBlur={addTag} />
            </div>
            <span className="seg-field-hint">Free text — trimmed and de-duplicated on save. Used for filtering and card display.</span>
          </div>

          {/* Visibility */}
          <div className="seg-field">
            <span className="seg-field-label">Visibility</span>
            <div className="mood-seg seg-visseg">
              {[["Public", "globe"], ["Private", "lock"]].map(([v, ic]) => (
                <button key={v} className={"mood-seg__btn" + (vis === v ? " is-active" : "")} onClick={() => setVis(v)}>
                  <Icon n={ic} size={14} /> {v}
                </button>
              ))}
            </div>
            <span className="seg-field-hint">Public runs can be edited by anyone in the household. Only the creator can change this.</span>
          </div>
        </div>

        <div className="seg-modal__foot">
          <span className="seg-modal__foot-note">
            {editing
              ? <React.Fragment><Icon n="user-round" size={13} /> {pt.owner} · created {pt.created}</React.Fragment>
              : <React.Fragment><Icon n="sparkles" size={13} /> Public · Planning · no sections</React.Fragment>}
          </span>
          <div className="seg-modal__foot-actions">
            {editing && <Button variant="ghost" size="sm" className="seg-danger" iconLeft={<Icon n="trash-2" size={15} />}>Delete</Button>}
            <Button variant="ghost" onClick={onClose}>Cancel</Button>
            <Button variant="primary" iconLeft={<Icon n={editing ? "check" : "plus"} size={17} />} onClick={onClose}>
              {editing ? "Save changes" : "Create playthrough"}
            </Button>
          </div>
        </div>
      </div>

      {picking && window.GmGameSelector && (
        <window.GmGameSelector
          currentId={gameId}
          onClose={() => setPicking(false)}
          onSelect={(g) => { setGameId(g.id); setPicking(false); }}
        />
      )}
    </div>
  );
}

// ── Manage sections popup (reorder + colour + rename) ───────────
function ManageSectionsDialog({ playthrough, onClose }) {
  const [secs, setSecs] = React.useState(() => playthrough.sections.map((s) => ({ id: s.id, name: s.name, color: s.color, count: s.goals.length })));
  const [newName, setNewName] = React.useState("");
  const [newColor, setNewColor] = React.useState("Blue");

  let _n = React.useRef(9000);
  const setColor = (id, token) => setSecs((xs) => xs.map((s) => s.id === id ? { ...s, color: token } : s));
  const setName = (id, v) => setSecs((xs) => xs.map((s) => s.id === id ? { ...s, name: v } : s));
  const del = (id) => setSecs((xs) => xs.filter((s) => s.id !== id));
  const move = (i, dir) => setSecs((xs) => {
    const j = i + dir; if (j < 0 || j >= xs.length) return xs;
    const c = xs.slice(); [c[i], c[j]] = [c[j], c[i]]; return c;
  });
  const add = () => {
    const v = newName.trim(); if (!v) return;
    setSecs((xs) => [...xs, { id: "sc" + (++_n.current), name: v, color: newColor, count: 0 }]);
    setNewName("");
  };

  return (
    <div className="seg-modal" onClick={onClose}>
      <div className="seg-modal__card gm-secmgr" onClick={(e) => e.stopPropagation()}>
        <div className="seg-modal__head">
          <h3>Manage sections</h3>
          <p>Rename, recolour, reorder and remove sections for <b>{playthrough.name}</b>. Reordering here keeps a deterministic order on the progress page.</p>
        </div>

        <div className="gm-secmgr__body">
          {secs.length === 0 ? (
            <div className="rc-subempty">
              <span className="rc-subempty__icon"><Icon n="layers" size={22} /></span>
              <p>No sections yet — add the first below. Sections group goals by theme, like bosses, chapters or collections.</p>
            </div>
          ) : secs.map((s, i) => (
            <div key={s.id} className={"gm-secrow gm-sec--" + s.color}>
              <div style={{ display: "flex", flexDirection: "column", gap: 0 }}>
                <button className="mood-nav__btn" style={{ width: 22, height: 18 }} onClick={() => move(i, -1)} disabled={i === 0} aria-label="Move up"><Icon n="chevron-up" size={14} /></button>
                <button className="mood-nav__btn" style={{ width: 22, height: 18 }} onClick={() => move(i, 1)} disabled={i === secs.length - 1} aria-label="Move down"><Icon n="chevron-down" size={14} /></button>
              </div>
              <div className="gm-secrow__main">
                <input className="gm-secrow__name" value={s.name} onChange={(e) => setName(s.id, e.target.value)} />
                <span className="gm-secrow__n">{s.count} {s.count === 1 ? "goal" : "goals"}</span>
              </div>
              <div className="gm-swatches">
                {SECTION_COLORS.map((c) => (
                  <button key={c.token} className={"gm-sec--" + c.token + " gm-swatch" + (s.color === c.token ? " is-active" : "")} style={{ "--sw": "var(--sec)" }} title={c.label} aria-label={c.label} onClick={() => setColor(s.id, c.token)} />
                ))}
              </div>
              <Tooltip label="Delete section" side="left">
                <IconButton size="sm" variant="ghost" className="seg-danger" label="Delete section" icon={<Icon n="trash-2" size={15} />} onClick={() => del(s.id)} />
              </Tooltip>
            </div>
          ))}

          <div className={"gm-secmgr__add gm-sec--" + newColor}>
            <Input value={newName} onChange={(e) => setNewName(e.target.value)} placeholder="New section name" iconLeft={<Icon n="layers" size={15} />} />
            <div className="gm-swatches">
              {SECTION_COLORS.map((c) => (
                <button key={c.token} className={"gm-sec--" + c.token + " gm-swatch" + (newColor === c.token ? " is-active" : "")} style={{ "--sw": "var(--sec)" }} title={c.label} aria-label={c.label} onClick={() => setNewColor(c.token)} />
              ))}
            </div>
            <Button variant="outline" size="sm" iconLeft={<Icon n="plus" size={15} />} onClick={add}>Add</Button>
          </div>
        </div>

        <div className="seg-modal__foot">
          <span className="seg-modal__foot-note"><Icon n="info" size={13} /> Section names are unique within this playthrough.</span>
          <div className="seg-modal__foot-actions">
            <Button variant="ghost" onClick={onClose}>Cancel</Button>
            <Button variant="primary" iconLeft={<Icon n="check" size={17} />} onClick={onClose}>Save order</Button>
          </div>
        </div>
      </div>
    </div>
  );
}

// ── Game catalogue: replace-and-delete popup ────────────────────
function ReplaceGameDialog({ game, onClose }) {
  const others = G.GAMES.filter((g) => g.id !== game.id);
  const [target, setTarget] = React.useState(others[0] ? others[0].id : "");
  const to = target ? GM[target] : null;
  const fromPlat = PLAT[game.platform];
  const toPlat = to ? PLAT[to.platform] : null;

  return (
    <div className="seg-modal" onClick={onClose}>
      <div className="seg-modal__card gm-secmgr" style={{ maxWidth: 560 }} onClick={(e) => e.stopPropagation()}>
        <div className="seg-modal__head">
          <h3>Delete “{game.name}”</h3>
          <p>This game is referenced by playthroughs, so it can only be removed by replacing it. Every affected run points at the replacement and keeps all of its progress.</p>
        </div>

        <div className="gm-secmgr__body" style={{ gap: "var(--space-4)" }}>
          <div className="gm-replace__note">
            <Icon n="triangle-alert" size={16} />
            <p><b>{game.refs} {game.refs === 1 ? "playthrough" : "playthroughs"}</b> reference this game. To protect privacy, individual runs — including private ones — are never listed here.</p>
          </div>

          <div className="gm-replace__arrow">
            <div className={"gm-replace__from gm-tone--" + fromPlat.tone}>
              <span className="gm-replace__ico"><Icon n={fromPlat.icon} size={18} /></span>
              <span className="gm-replace__lbl"><span>Removing</span><strong>{game.name}</strong></span>
            </div>
            <span className="gm-replace__mid"><Icon n="arrow-right" size={18} /></span>
            <div className={"gm-replace__to" + (to ? " gm-tone--" + toPlat.tone : "")}>
              <span className="gm-replace__ico"><Icon n={to ? toPlat.icon : "gamepad-2"} size={18} /></span>
              <span className="gm-replace__lbl"><span>Replace with</span><strong>{to ? to.name : "—"}</strong></span>
            </div>
          </div>

          <div className="seg-field">
            <span className="seg-field-label">Replacement game</span>
            <Select value={target} onChange={(e) => setTarget(e.target.value)}
              options={others.map((g) => ({ value: g.id, label: g.name + " · " + PLAT[g.platform].label }))} />
            <span className="seg-field-hint">Both actions run in one transaction — the reference is switched and the source game is deleted together.</span>
          </div>
        </div>

        <div className="seg-modal__foot">
          <div className="seg-modal__foot-actions" style={{ marginLeft: "auto" }}>
            <Button variant="ghost" onClick={onClose}>Cancel</Button>
            <Button variant="primary" className="seg-danger" iconLeft={<Icon n="replace" size={16} />} onClick={onClose}>Replace &amp; delete</Button>
          </div>
        </div>
      </div>
    </div>
  );
}

Object.assign(window, {
  GmPlat, GmStatus, GmVis, GmTags, GmProgBar, GmRing,
  GmPlaythroughEditDialog: PlaythroughEditDialog,
  GmManageSectionsDialog: ManageSectionsDialog,
  GmReplaceGameDialog: ReplaceGameDialog,
});
})();
