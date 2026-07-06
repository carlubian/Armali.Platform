/* global React */
// Games — the module entry. Opens on a server-paginated playthrough card
// collection (search · game / platform / status / visibility filters · sort by
// name, game, start date, status & derived progress). Each playthrough opens a
// dedicated progress page (section list on the left, the selected section's
// goals on the right). Administrators manage the Game catalogue through a
// Configuration surface with replace-on-delete. The orchestrator wires the
// URL-aware playthrough editor, the manage-sections popup, and the game
// selector over the views. Canvas variants are exported at the end.
(() => {
const A = window.ProjectArmaliDesignSystem_ce5329;
const { Button, IconButton, Tooltip, Input, Select, Badge, Checkbox } = A;
const Icon = window.SegIcon;
const G = window.SegGames;
const { PLATFORMS, PLAT, STATUSES, STATUS, SECTION_COLORS, GAMES, GM, PLAYTHROUGHS } = G;

const PAGE_SIZE = 9;

// ── Pager ───────────────────────────────────────────────────────
function Pager({ page, pages, onPage }) {
  return (
    <div className="seg-pager">
      <button className="seg-pager__btn" disabled={page <= 1} onClick={() => onPage && onPage(page - 1)} aria-label="Previous page"><Icon n="chevron-left" size={16} /></button>
      {Array.from({ length: pages }, (_, i) => i + 1).map((p) => (
        <button key={p} className={"seg-pager__btn" + (p === page ? " is-active" : "")} onClick={() => onPage && onPage(p)}>{p}</button>
      ))}
      <button className="seg-pager__btn" disabled={page >= pages} onClick={() => onPage && onPage(page + 1)} aria-label="Next page"><Icon n="chevron-right" size={16} /></button>
    </div>
  );
}

// ── Collection cards ────────────────────────────────────────────
function PtCard({ pt, onOpen, onEdit }) {
  const game = GM[pt.gameId];
  const plat = PLAT[game.platform];
  const prog = G.playthroughProgress(pt);
  return (
    <div role="button" tabIndex={0} className={"gm-card gm-tone--" + plat.tone} onClick={() => onOpen(pt)}>
      <div className="gm-card__top">
        <span className="gm-card__cover"><Icon n={plat.icon} size={24} /></span>
        <div className="gm-card__head">
          <div className="gm-card__name">{pt.name}</div>
          <div className="gm-card__game"><Icon n="gamepad-2" size={13} />{game.name}</div>
        </div>
        <span className="gm-card__vis"><window.GmVis visibility={pt.visibility} /></span>
      </div>
      <div className="gm-card__meta">
        <window.GmStatus status={pt.status} />
        <window.GmPlat platform={game.platform} />
      </div>
      <window.GmProgBar done={prog.done} total={prog.total} />
      {pt.tags.length > 0 && <window.GmTags tags={pt.tags} max={3} />}
      <div className="gm-card__foot">
        <span className="gm-card__date"><Icon n="calendar" size={13} />Started {G.fmtStart(pt.startMonth, pt.startYear)}</span>
        <span className="gm-card__open">Open progress <Icon n="arrow-right" size={13} /></span>
      </div>
    </div>
  );
}

function PtRow({ pt, onOpen }) {
  const game = GM[pt.gameId];
  const plat = PLAT[game.platform];
  const prog = G.playthroughProgress(pt);
  const p = G.pct(prog.done, prog.total);
  return (
    <div role="button" tabIndex={0} className={"gm-row gm-tone--" + plat.tone} onClick={() => onOpen(pt)}>
      <span className="gm-row__cover"><Icon n={plat.icon} size={21} /></span>
      <div className="gm-row__id">
        <div className="gm-row__name">{pt.name}</div>
        <div className="gm-row__game"><Icon n="gamepad-2" size={12} />{game.name} · {plat.label}</div>
      </div>
      <span><window.GmStatus status={pt.status} /></span>
      <window.GmTags tags={pt.tags} max={2} />
      <div className="gm-row__prog">
        <div className="gm-rowbar"><div className="gm-rowbar__fill" style={{ width: (prog.total ? p : 0) + "%" }} /></div>
        <span className="gm-row__progtxt">{prog.total ? <React.Fragment><b>{prog.done}/{prog.total}</b> goals · {p}%</React.Fragment> : "No goals yet"}</span>
      </div>
      <div className="gm-row__act"><Icon n="chevron-right" size={18} style={{ color: "var(--text-muted)" }} /></div>
    </div>
  );
}

// ── Collection view ─────────────────────────────────────────────
const SORTS = [
  { value: "name", label: "Sort: name" },
  { value: "game", label: "Sort: game" },
  { value: "start", label: "Sort: start date" },
  { value: "status", label: "Sort: status" },
  { value: "progress", label: "Sort: progress" },
];
const STATUS_ORDER = { Planning: 0, Active: 1, Completed: 2 };

function CollectionView({ variant, onOpen, onNew, onEdit, onConfig }) {
  const [search, setSearch] = React.useState("");
  const [plat, setPlat] = React.useState("");
  const [status, setStatus] = React.useState("");
  const [vis, setVis] = React.useState("");
  const [sort, setSort] = React.useState({ key: "start", dir: "desc" });

  const filtered = React.useMemo(() => {
    const q = search.trim().toLowerCase();
    let list = PLAYTHROUGHS.filter((pt) => {
      const game = GM[pt.gameId];
      if (plat && game.platform !== plat) return false;
      if (status && pt.status !== status) return false;
      if (vis && pt.visibility !== vis) return false;
      if (q && !(pt.name.toLowerCase().includes(q) || game.name.toLowerCase().includes(q))) return false;
      return true;
    });
    const dir = sort.dir === "asc" ? 1 : -1;
    list = list.slice().sort((a, b) => {
      let cmp = 0;
      if (sort.key === "name") cmp = a.name.localeCompare(b.name);
      else if (sort.key === "game") cmp = GM[a.gameId].name.localeCompare(GM[b.gameId].name);
      else if (sort.key === "start") cmp = (a.startYear * 12 + a.startMonth) - (b.startYear * 12 + b.startMonth);
      else if (sort.key === "status") cmp = STATUS_ORDER[a.status] - STATUS_ORDER[b.status];
      else if (sort.key === "progress") cmp = G.pct(...Object.values(G.playthroughProgress(a))) - G.pct(...Object.values(G.playthroughProgress(b)));
      if (cmp === 0) cmp = a.name.localeCompare(b.name);
      return dir * cmp;
    });
    return list;
  }, [search, plat, status, vis, sort]);

  const activeCount = PLAYTHROUGHS.filter((p) => p.status === "Active").length;
  const Card = variant === "row" ? PtRow : PtCard;

  return (
    <React.Fragment>
      <div className="gm-bar">
        <div>
          <div className="armali-eyebrow">Progress tracker</div>
          <h2>Games</h2>
          <p>Every run, campaign and save the household is playing — grouped into sections of goals you tick off as you go.</p>
        </div>
        <div className="gm-bar__stats">
          <Tooltip label="Manage the game catalogue" side="bottom">
            <Button variant="ghost" iconLeft={<Icon n="settings-2" size={16} />} onClick={onConfig}>Catalogue</Button>
          </Tooltip>
          <div className="seg-stat-pill"><strong>{PLAYTHROUGHS.length}</strong><span>Playthroughs</span></div>
          <div className="seg-stat-pill"><strong>{activeCount}</strong><span>Active</span></div>
          <Button variant="primary" iconLeft={<Icon n="plus" size={17} />} onClick={onNew}>New playthrough</Button>
        </div>
      </div>

      <div className="gm-toolbar">
        <div className="gm-toolbar__search">
          <Input placeholder="Search by playthrough or game name" value={search} onChange={(e) => setSearch(e.target.value)} iconLeft={<Icon n="search" size={16} />} />
        </div>
        <Select value={plat} onChange={(e) => setPlat(e.target.value)}
          options={[{ value: "", label: "All platforms" }, ...PLATFORMS.map((p) => ({ value: p.value, label: p.label }))]} />
        <Select value={status} onChange={(e) => setStatus(e.target.value)}
          options={[{ value: "", label: "Any status" }, ...STATUSES.map((s) => ({ value: s.value, label: s.label }))]} />
        <Select value={vis} onChange={(e) => setVis(e.target.value)}
          options={[{ value: "", label: "Any visibility" }, { value: "Public", label: "Public" }, { value: "Private", label: "Private" }]} />
        <span className="gm-toolbar__spacer" />
        <div className="gm-sort">
          <Select value={sort.key} onChange={(e) => setSort((s) => ({ ...s, key: e.target.value }))} options={SORTS} />
          <Tooltip label={sort.dir === "asc" ? "Ascending" : "Descending"} side="bottom">
            <button className="gm-sort__dir" onClick={() => setSort((s) => ({ ...s, dir: s.dir === "asc" ? "desc" : "asc" }))} aria-label="Toggle sort direction">
              <Icon n={sort.dir === "asc" ? "arrow-up" : "arrow-down"} size={16} />
            </button>
          </Tooltip>
        </div>
      </div>

      <div className="gm-galleryscroll">
        {filtered.length === 0 ? (
          <div className="gm-gallery">
            <div className="gm-empty">
              <span className="gm-empty__icon"><Icon n="gamepad-2" size={26} /></span>
              <h3>Nothing matches</h3>
              <p>No playthroughs match your search and filters — try clearing them, or start a new one.</p>
            </div>
          </div>
        ) : (
          <div className={"gm-gallery gm-gallery--" + (variant === "row" ? "row" : "card")}>
            {filtered.map((pt) => <Card key={pt.id} pt={pt} onOpen={onOpen} onEdit={onEdit} />)}
          </div>
        )}
      </div>

      <div className="seg-selector__foot" style={{ borderTop: "none", padding: "0 var(--space-2)", background: "transparent" }}>
        <span className="seg-pageinfo">{filtered.length === 0 ? "No results" : <React.Fragment>Showing <b>1–{filtered.length}</b> of <b>{filtered.length}</b> playthroughs</React.Fragment>}</span>
        <div style={{ display: "flex", alignItems: "center", gap: "var(--space-4)" }}>
          <Select value="25" options={[{ value: "25", label: "25 per page" }, { value: "50", label: "50 per page" }]} onChange={() => {}} />
          <Pager page={1} pages={1} />
        </div>
      </div>
    </React.Fragment>
  );
}

// ── Progress page (section list + goals) ────────────────────────
function ProgressPage({ playthrough, onBack, onEdit, onManage, initialSectionId }) {
  const game = GM[playthrough.gameId];
  const plat = PLAT[game.platform];
  // Local, editable copy so goals can be ticked and added.
  const [secs, setSecs] = React.useState(() => playthrough.sections.map((s) => ({ ...s, goals: s.goals.map((g) => ({ ...g })) })));
  const [selId, setSelId] = React.useState(initialSectionId || (playthrough.sections[0] && playthrough.sections[0].id) || null);
  const [draft, setDraft] = React.useState("");

  const sel = secs.find((s) => s.id === selId) || secs[0] || null;
  const overall = secs.reduce((a, s) => { a.total += s.goals.length; a.done += s.goals.filter((g) => g.done).length; return a; }, { done: 0, total: 0 });

  const toggle = (gid) => setSecs((xs) => xs.map((s) => s.id !== sel.id ? s : { ...s, goals: s.goals.map((g) => g.id === gid ? { ...g, done: !g.done } : g) }));
  const addGoal = () => {
    const v = draft.trim(); if (!v || !sel) return;
    setSecs((xs) => xs.map((s) => s.id !== sel.id ? s : { ...s, goals: [...s.goals, { id: "gl-new-" + Date.now(), content: v, done: false }] }));
    setDraft("");
  };
  const delGoal = (gid) => setSecs((xs) => xs.map((s) => s.id !== sel.id ? s : { ...s, goals: s.goals.filter((g) => g.id !== gid) }));

  const selProg = sel ? { done: sel.goals.filter((g) => g.done).length, total: sel.goals.length } : { done: 0, total: 0 };
  const selPct = G.pct(selProg.done, selProg.total);

  return (
    <div className="gm-prog">
      <div className={"gm-proghead gm-tone--" + plat.tone}>
        <Button variant="outline" size="sm" iconLeft={<Icon n="arrow-left" size={15} />} onClick={onBack}>All playthroughs</Button>
        <span className="gm-proghead__cover"><Icon n={plat.icon} size={26} /></span>
        <div className="gm-proghead__id">
          <div className="gm-proghead__crumb"><Icon n="gamepad-2" size={12} />{game.name}<Icon n="chevron-right" size={12} />{plat.label}</div>
          <div className="gm-proghead__name">{playthrough.name}</div>
          <div className="gm-proghead__meta">
            <window.GmStatus status={playthrough.status} />
            <window.GmVis visibility={playthrough.visibility} />
            <span className="gm-metadot" />
            <span className="gm-card__date"><Icon n="calendar" size={13} />Started {G.fmtStartLong(playthrough.startMonth, playthrough.startYear)}</span>
            {playthrough.tags.length > 0 && <React.Fragment><span className="gm-metadot" /><window.GmTags tags={playthrough.tags} max={4} /></React.Fragment>}
          </div>
        </div>
        <div className="gm-proghead__ring"><window.GmRing done={overall.done} total={overall.total} size={68} stroke={7} /></div>
        <div className="gm-proghead__act">
          <Tooltip label="Rename, recolour & reorder sections" side="bottom">
            <Button variant="outline" iconLeft={<Icon n="layers" size={16} />} onClick={onManage}>Sections</Button>
          </Tooltip>
          <Button variant="primary" iconLeft={<Icon n="pencil" size={15} />} onClick={onEdit}>Edit</Button>
        </div>
      </div>

      {secs.length === 0 ? (
        <div className="gm-goals" style={{ flex: 1 }}>
          <div className="gm-goals__none">
            <span className="gm-empty__icon"><Icon n="layers" size={28} /></span>
            <h3>No sections yet</h3>
            <p>Sections group goals by theme — bosses, chapters, collections, quests. Add the first to start tracking progress for this run.</p>
            <Button variant="primary" iconLeft={<Icon n="plus" size={17} />} onClick={onManage}>Add a section</Button>
          </div>
        </div>
      ) : (
        <div className="gm-progbody">
          {/* Section rail */}
          <div className="gm-seclist">
            <div className="gm-seclist__head">
              <h3>Sections</h3>
              <span className="rc-section__n">{secs.length}</span>
            </div>
            <div className="gm-seclist__scroll">
              {secs.map((s) => {
                const sp = { done: s.goals.filter((g) => g.done).length, total: s.goals.length };
                const p = G.pct(sp.done, sp.total);
                return (
                  <button key={s.id} className={"gm-secitem gm-sec--" + s.color + (s.id === (sel && sel.id) ? " is-active" : "")} onClick={() => setSelId(s.id)}>
                    <span className="gm-secitem__swatch" />
                    <span className="gm-secitem__txt">
                      <span className="gm-secitem__name">{s.name}</span>
                      <span className="gm-secitem__sub">{sp.total ? `${sp.done} / ${sp.total} · ${p}%` : "No goals"}</span>
                    </span>
                    <span className="gm-secitem__mini"><span className="gm-secitem__minifill" style={{ width: (sp.total ? p : 0) + "%" }} /></span>
                  </button>
                );
              })}
            </div>
            <div className="gm-seclist__foot">
              <Button variant="ghost" size="sm" iconLeft={<Icon n="settings-2" size={15} />} onClick={onManage} style={{ width: "100%" }}>Manage sections</Button>
            </div>
          </div>

          {/* Goals pane */}
          <div className={"gm-goals gm-sec--" + (sel ? sel.color : "Blue")}>
            <div className="gm-goals__head">
              <span className="gm-goals__swatch"><Icon n="target" size={17} /></span>
              <div className="gm-goals__id">
                <div className="gm-goals__name">{sel ? sel.name : "—"}</div>
                <div className="gm-goals__sub">{selProg.total ? `${selProg.done} of ${selProg.total} complete` : "No goals in this section yet"}</div>
              </div>
              <div className="gm-goals__prog">
                <div className="gm-goals__progbar"><div className="gm-goals__progfill" style={{ width: (selProg.total ? selPct : 0) + "%" }} /></div>
                <span className="gm-goals__pct">{selProg.total ? selPct + "%" : "—"}</span>
              </div>
            </div>
            <div className="gm-goals__scroll">
              {sel && sel.goals.length === 0 ? (
                <div className="gm-goals__empty">
                  <span className="gm-empty__icon"><Icon n="list-checks" size={26} /></span>
                  <h3>Nothing here yet</h3>
                  <p>Add the first goal below — a boss to beat, a quest to finish, a collectible to find. Tick it off when it's done.</p>
                </div>
              ) : sel && sel.goals.map((g) => (
                <div key={g.id} className={"gm-goal" + (g.done ? " is-done" : "")}>
                  <Checkbox checked={g.done} onChange={() => toggle(g.id)} />
                  <span className="gm-goal__txt">{g.content}</span>
                  <span className="gm-goal__act">
                    <Tooltip label="Delete goal" side="left">
                      <IconButton size="sm" variant="ghost" className="seg-danger" label="Delete goal" icon={<Icon n="trash-2" size={14} />} onClick={() => delGoal(g.id)} />
                    </Tooltip>
                  </span>
                </div>
              ))}
              {sel && (
                <div className="gm-goaladd">
                  <span className="gm-goaladd__icon"><Icon n="plus" size={16} /></span>
                  <input value={draft} placeholder="Add a goal and press Enter" onChange={(e) => setDraft(e.target.value)}
                    onKeyDown={(e) => { if (e.key === "Enter") { e.preventDefault(); addGoal(); } }} />
                  {draft.trim() && <Button variant="primary" size="sm" onClick={addGoal}>Add</Button>}
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

// ── Game catalogue create / edit dialog (admin) ─────────────────
function GameEditDialog({ mode, game, onClose }) {
  const editing = mode === "edit";
  const [platform, setPlatform] = React.useState(editing ? game.platform : "PC");
  return (
    <div className="seg-modal" onClick={onClose}>
      <div className="seg-modal__card seg-itemdlg" onClick={(e) => e.stopPropagation()}>
        <div className="seg-modal__head">
          <h3>{editing ? "Edit game" : "New game"}</h3>
          <p>{editing ? "Update this catalogue entry. Playthroughs keep their link." : "Add a game to the catalogue. A playthrough can then track a run of it."}</p>
        </div>
        <div className="seg-field">
          <span className="seg-field-label">Name <span style={{ color: "var(--terracotta-500)" }}>*</span></span>
          <Input defaultValue={editing ? game.name : ""} placeholder="Game title" iconLeft={<Icon n="gamepad-2" size={16} />} />
          <span className="seg-field-hint">Required, at most 200 characters, unique across the catalogue (case-insensitive).</span>
        </div>
        <div className="seg-field">
          <span className="seg-field-label">Platform <span style={{ color: "var(--terracotta-500)" }}>*</span></span>
          <Select value={platform} onChange={(e) => setPlatform(e.target.value)} options={PLATFORMS.map((p) => ({ value: p.value, label: p.label }))} />
          <span className="seg-field-hint">A fixed set — not configurable in this release.</span>
        </div>
        <div className="seg-modal__foot">
          <div className="seg-modal__foot-actions" style={{ marginLeft: "auto" }}>
            <Button variant="ghost" onClick={onClose}>Cancel</Button>
            <Button variant="primary" iconLeft={<Icon n={editing ? "check" : "plus"} size={17} />} onClick={onClose}>{editing ? "Save game" : "Add game"}</Button>
          </div>
        </div>
      </div>
    </div>
  );
}

// ── Configuration · game catalogue ──────────────────────────────
function ConfigCatalogue({ onBack, onNewGame, onEditGame, onDeleteGame }) {
  const referenced = GAMES.filter((g) => g.refs > 0).length;
  return (
    <React.Fragment>
      <div className="gm-bar">
        <div>
          <Button variant="ghost" size="sm" iconLeft={<Icon n="arrow-left" size={15} />} onClick={onBack} style={{ marginBottom: 6 }}>Back to Games</Button>
          <div className="armali-eyebrow">Configuration · Games</div>
          <h2>Game catalogue</h2>
          <p>Administrators manage the shared list of games here. Playthroughs reference these entries; a referenced game can only be removed by replacing it.</p>
        </div>
        <div className="gm-bar__stats">
          <div className="seg-stat-pill"><strong>{GAMES.length}</strong><span>Games</span></div>
          <div className="seg-stat-pill"><strong>{referenced}</strong><span>Referenced</span></div>
          <Button variant="primary" iconLeft={<Icon n="plus" size={17} />} onClick={onNewGame}>New game</Button>
        </div>
      </div>

      <div className="gm-galleryscroll">
        <div className="seg-tablecard">
          <div className="gm-cathead">
            <span>Order</span>
            <span>Game</span>
            <span>Platform</span>
            <span>Playthroughs</span>
            <span style={{ textAlign: "right" }}>Actions</span>
          </div>
          {GAMES.map((g, i) => {
            const p = PLAT[g.platform];
            return (
              <div key={g.id} className={"gm-catrow gm-tone--" + p.tone}>
                <span className="gm-catrow__ord">
                  <span className="gm-catrow__handle"><Icon n="grip-vertical" size={15} /></span>
                  <span className="seg-code">{g.order}</span>
                </span>
                <span className="gm-catrow__name">
                  <span className="gm-catrow__nameicon"><Icon n={p.icon} size={16} /></span>
                  <strong>{g.name}</strong>
                </span>
                <span><span className={"gm-plat gm-tone--" + p.tone}><Icon n={p.icon} size={12} />{p.label}</span></span>
                <span className={"gm-catrow__refs" + (g.refs === 0 ? " is-zero" : "")}>
                  <Icon n="users-round" size={14} /><b>{g.refs}</b>{g.refs === 0 ? " · unreferenced" : ""}
                </span>
                <span className="gm-catrow__act">
                  <Tooltip label="Edit" side="top"><IconButton size="sm" variant="ghost" label="Edit game" icon={<Icon n="pencil" size={15} />} onClick={() => onEditGame(g)} /></Tooltip>
                  <Tooltip label={g.refs > 0 ? "Replace & delete" : "Delete"} side="top">
                    <IconButton size="sm" variant="ghost" className="seg-danger" label="Delete game" icon={<Icon n="trash-2" size={15} />} onClick={() => onDeleteGame(g)} />
                  </Tooltip>
                </span>
              </div>
            );
          })}
        </div>
      </div>

      <div className="seg-selector__foot" style={{ borderTop: "none", padding: "0 var(--space-2)", background: "transparent" }}>
        <span className="seg-pageinfo"><Icon n="info" size={14} /> Deleting an unreferenced game is immediate. Deleting a referenced one requires a replacement.</span>
      </div>
    </React.Fragment>
  );
}

// ── Orchestrator ────────────────────────────────────────────────
function GamesScreen({ view: initialView = "collection", variant = "card", initialDialog }) {
  const [view, setView] = React.useState(initialView);
  const [current, setCurrent] = React.useState(initialView === "progress" ? (initialDialog && initialDialog.pt) : null);
  const [dialog, setDialog] = React.useState(initialDialog && initialDialog.mode ? initialDialog : null);
  const close = () => setDialog(null);

  const eyebrow = view === "config" ? "Configuration" : view === "progress" ? "Games" : "Games";
  const title = view === "config" ? "Game catalogue" : view === "progress" && current ? current.name : "Games";

  return (
    <div className="seg-screen">
      {window.SegShellTopBar ? <window.SegShellTopBar eyebrow={eyebrow} title={title} /> : null}
      <div className="seg-page">
        <div className="seg-page__inner">
          {view === "collection" && (
            <CollectionView variant={variant}
              onOpen={(pt) => { setCurrent(pt); setView("progress"); }}
              onEdit={(pt) => setDialog({ mode: "edit", pt })}
              onNew={() => setDialog({ mode: "new" })}
              onConfig={() => setView("config")} />
          )}
          {view === "progress" && current && (
            <ProgressPage playthrough={current}
              initialSectionId={initialDialog && initialDialog.sectionId}
              onBack={() => setView("collection")}
              onEdit={() => setDialog({ mode: "edit", pt: current })}
              onManage={() => setDialog({ mode: "manage", pt: current })} />
          )}
          {view === "config" && (
            <ConfigCatalogue
              onBack={() => setView("collection")}
              onNewGame={() => setDialog({ mode: "game-new" })}
              onEditGame={(g) => setDialog({ mode: "game-edit", game: g })}
              onDeleteGame={(g) => g.refs > 0 ? setDialog({ mode: "replace", game: g }) : setDialog({ mode: "game-del", game: g })} />
          )}
        </div>
      </div>

      {dialog && (dialog.mode === "edit" || dialog.mode === "new") && (
        <window.GmPlaythroughEditDialog mode={dialog.mode === "new" ? "new" : "edit"} playthrough={dialog.pt} startPicking={!!dialog.picking} onClose={close} />
      )}
      {dialog && dialog.mode === "manage" && (
        <window.GmManageSectionsDialog playthrough={dialog.pt} onClose={close} />
      )}
      {dialog && dialog.mode === "replace" && (
        <window.GmReplaceGameDialog game={dialog.game} onClose={close} />
      )}
      {dialog && (dialog.mode === "game-new" || dialog.mode === "game-edit") && (
        <GameEditDialog mode={dialog.mode === "game-new" ? "new" : "edit"} game={dialog.game} onClose={close} />
      )}
    </div>
  );
}

// ── Canvas variants ─────────────────────────────────────────────
const featured = PLAYTHROUGHS.find((p) => p.id === "pt-elden-first");
const emptyPt = PLAYTHROUGHS.find((p) => p.id === "pt-hollow");
const refGame = GM["gm-eldenring"];

const GamesCollectionCards = () => <GamesScreen view="collection" variant="card" />;
const GamesCollectionRows  = () => <GamesScreen view="collection" variant="row" />;
const GamesPlaythroughEdit = () => <GamesScreen view="collection" variant="card" initialDialog={{ mode: "edit", pt: featured }} />;
const GamesPlaythroughNew  = () => <GamesScreen view="collection" variant="card" initialDialog={{ mode: "new" }} />;
const GamesGamePick        = () => <GamesScreen view="collection" variant="card" initialDialog={{ mode: "new", picking: true }} />;
const GamesProgress        = () => <GamesScreen view="progress" initialDialog={{ pt: featured }} />;
const GamesProgressEmpty   = () => <GamesScreen view="progress" initialDialog={{ pt: emptyPt }} />;
const GamesManageSections  = () => <GamesScreen view="progress" initialDialog={{ pt: featured, mode: "manage" }} />;
const GamesConfig          = () => <GamesScreen view="config" />;
const GamesConfigReplace   = () => <GamesScreen view="config" initialDialog={{ mode: "replace", game: refGame }} />;

Object.assign(window, {
  GamesCollectionCards, GamesCollectionRows,
  GamesPlaythroughEdit, GamesPlaythroughNew, GamesGamePick,
  GamesProgress, GamesProgressEmpty, GamesManageSections,
  GamesConfig, GamesConfigReplace,
});
})();
