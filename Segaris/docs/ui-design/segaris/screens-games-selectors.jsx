/* global React */
// Games — the game catalogue entity selector. A file-selector-style reference
// control on the playthrough editor opens this sortable / filterable / paginated
// table popup floating over the dimmed editor. It reads the admin-managed Game
// catalogue (name + platform), filtered by a platform facet and search. Reuses
// the shared seg-selector shell from segaris.css. Exposed on window.
(() => {
const A = window.ProjectArmaliDesignSystem_ce5329;
const { Button, IconButton, Input, Select, Badge } = A;
const Icon = window.SegIcon;
const G = window.SegGames;
const { GAMES, PLAT, PLATFORMS } = G;

const PAGE_SIZE = 8;

function Pager({ page, pages, onPage }) {
  if (pages <= 1) return <span className="seg-pager" />;
  return (
    <div className="seg-pager">
      <button className="seg-pager__btn" disabled={page <= 1} onClick={() => onPage(page - 1)} aria-label="Previous page"><Icon n="chevron-left" size={16} /></button>
      {Array.from({ length: pages }, (_, i) => i + 1).map((p) => (
        <button key={p} className={"seg-pager__btn" + (p === page ? " is-active" : "")} onClick={() => onPage(p)}>{p}</button>
      ))}
      <button className="seg-pager__btn" disabled={page >= pages} onClick={() => onPage(page + 1)} aria-label="Next page"><Icon n="chevron-right" size={16} /></button>
    </div>
  );
}

function GameSelector({ currentId, onClose, onSelect }) {
  const [search, setSearch] = React.useState("");
  const [plat, setPlat] = React.useState("");
  const [sort, setSort] = React.useState({ key: "name", dir: "asc" });
  const [page, setPage] = React.useState(1);

  const setF = (fn) => (v) => { fn(v); setPage(1); };

  const filtered = React.useMemo(() => {
    const q = search.trim().toLowerCase();
    let list = GAMES.filter((g) => {
      if (plat && g.platform !== plat) return false;
      if (q && !g.name.toLowerCase().includes(q)) return false;
      return true;
    });
    const dir = sort.dir === "asc" ? 1 : -1;
    list = list.slice().sort((a, b) => {
      let cmp = sort.key === "platform" ? PLAT[a.platform].label.localeCompare(PLAT[b.platform].label) : a.name.localeCompare(b.name);
      if (cmp === 0) cmp = a.name.localeCompare(b.name);
      return dir * cmp;
    });
    return list;
  }, [search, plat, sort]);

  const total = filtered.length;
  const pages = Math.max(1, Math.ceil(total / PAGE_SIZE));
  const safePage = Math.min(page, pages);
  const start = (safePage - 1) * PAGE_SIZE;
  const rows = filtered.slice(start, start + PAGE_SIZE);

  const sortBtn = (key, label, extra = "") => (
    <button className={"seg-sorth" + (sort.key === key ? " is-active" : "") + (sort.key === key && sort.dir === "desc" ? " is-desc" : "") + (extra ? " " + extra : "")}
      onClick={() => setSort((s) => ({ key, dir: s.key === key && s.dir === "asc" ? "desc" : "asc" }))}>
      {label}<span className="seg-sorth__chev"><Icon n="chevron-up" size={13} /></span>
    </button>
  );

  const platCounts = React.useMemo(() => {
    const m = {};
    GAMES.forEach((g) => { m[g.platform] = (m[g.platform] || 0) + 1; });
    return m;
  }, []);

  return (
    <div className="seg-selector" onClick={onClose}>
      <div className="seg-selector__card" style={{ maxWidth: 820 }} onClick={(e) => e.stopPropagation()}>
        <div className="seg-selector__head">
          <div className="seg-selector__head-txt">
            <div className="armali-eyebrow">Link · Game catalogue</div>
            <h3>Choose a game</h3>
            <p>Pick the game this playthrough tracks. The catalogue is managed by administrators in Configuration.</p>
          </div>
          <IconButton label="Close" variant="ghost" icon={<Icon n="x" size={18} />} onClick={onClose} />
        </div>

        <div className="seg-selector__filters">
          <div className="seg-selector__search">
            <Input placeholder="Search games by name" value={search} onChange={(e) => setF(setSearch)(e.target.value)} iconLeft={<Icon n="search" size={16} />} />
          </div>
          <Select value={plat} onChange={(e) => setF(setPlat)(e.target.value)}
            options={[{ value: "", label: "All platforms" }, ...PLATFORMS.map((p) => ({ value: p.value, label: p.label }))]} />
        </div>

        <div className="seg-selector__strip">
          <span className="seg-selector__count"><b>{total}</b> {total === 1 ? "game" : "games"} match</span>
          {plat && (
            <div className="seg-chips">
              <span className="seg-chip">{PLAT[plat].label}<button className="seg-chip__x" aria-label="Clear platform" onClick={() => setF(setPlat)("")}><Icon n="x" size={11} /></button></span>
            </div>
          )}
        </div>

        <div className="seg-selector__scroll">
          <div className="seg-seltable">
            <div className="seg-selhead gm-gamesel-head">
              <span>{sortBtn("name", "Game")}</span>
              <span>{sortBtn("platform", "Platform")}</span>
              <span className="seg-th--num" style={{ justifySelf: "start" }}>Playthroughs</span>
              <span className="seg-th--right" />
            </div>
            {rows.length === 0 ? (
              <div className="seg-selempty">
                <span className="seg-selempty__icon"><Icon n="gamepad-2" size={24} /></span>
                <p>No games match your search. The catalogue may be empty — add games in Configuration first.</p>
              </div>
            ) : rows.map((g) => {
              const p = PLAT[g.platform];
              const current = g.id === currentId;
              return (
                <div key={g.id} className={"seg-selrow gm-gamesel-row" + (current ? " is-current" : "")}>
                  <div className={"gm-selname gm-tone--" + p.tone}>
                    <span className="gm-selname__ico"><Icon n={p.icon} size={16} /></span>
                    <div className="seg-seln"><strong>{g.name}</strong></div>
                  </div>
                  <span className="seg-selcell"><span className={"gm-plat gm-tone--" + p.tone}><Icon n={p.icon} size={12} />{p.label}</span></span>
                  <span className="seg-selcell" style={{ fontVariantNumeric: "tabular-nums" }}>{g.refs}</span>
                  <div className="seg-selrow__act">
                    {current
                      ? <span className="seg-current-tag"><Icon n="check" size={13} /> Linked</span>
                      : <Button variant="outline" size="sm" onClick={() => onSelect(g)}>Select</Button>}
                  </div>
                </div>
              );
            })}
          </div>
        </div>

        <div className="seg-selector__foot">
          <span className="seg-pageinfo">{total === 0 ? "No results" : <React.Fragment>Showing <b>{start + 1}–{Math.min(start + PAGE_SIZE, total)}</b> of <b>{total}</b></React.Fragment>}</span>
          <Pager page={safePage} pages={pages} onPage={setPage} />
        </div>
      </div>
    </div>
  );
}

Object.assign(window, { GmGameSelector: GameSelector });
})();
