/* global React */
// Recipes — the shared entity selectors, reached through thin adapters:
//   • RcInventorySelector — links a recipe ingredient to one Inventory item
//     (cross-module reference; a public recipe may link only public items).
//   • RcRecipeSelector — adds a recipe to a weekly-menu slot (intra-module).
// Both reuse the seg-selector popup shell from segaris.css and float over the
// dimmed editor. Sortable, filterable, server-paginated. Exposed on window.
(() => {
const A = window.ProjectArmaliDesignSystem_ce5329;
const { Badge, Button, IconButton, Tooltip, Input, Select } = A;
const Icon = window.SegIcon;
const R = window.SegRec;
const { CAT, REC_CATEGORIES, REC_DIFFICULTY, DIFFICULTY_ORDER } = R;
const ALL_INV = window.SEG_INVENTORY;
const ALL_RCP = window.SEG_RECIPES;
const PAGE_SIZE = 8;

// ── Numbered pager ─────────────────────────────────────────────
function pageItems(page, pages) {
  if (pages <= 7) return Array.from({ length: pages }, (_, i) => i + 1);
  const out = [1];
  const lo = Math.max(2, page - 1), hi = Math.min(pages - 1, page + 1);
  if (lo > 2) out.push("…");
  for (let p = lo; p <= hi; p++) out.push(p);
  if (hi < pages - 1) out.push("…");
  out.push(pages);
  return out;
}
function Pager({ page, pages, onPage }) {
  return (
    <div className="seg-pager">
      <button className="seg-pager__btn" disabled={page <= 1} onClick={() => onPage(page - 1)} aria-label="Previous page"><Icon n="chevron-left" size={16} /></button>
      {pageItems(page, pages).map((p, i) =>
        p === "…" ? <span key={"g" + i} className="seg-pager__gap">…</span>
          : <button key={p} className={"seg-pager__btn" + (p === page ? " is-active" : "")} onClick={() => onPage(p)}>{p}</button>)}
      <button className="seg-pager__btn" disabled={page >= pages} onClick={() => onPage(page + 1)} aria-label="Next page"><Icon n="chevron-right" size={16} /></button>
    </div>
  );
}
function SortHeader({ label, sortKey, sort, onSort }) {
  const active = sort.key === sortKey;
  return (
    <span className="seg-th">
      <button className={"seg-sorth" + (active ? " is-active" : "") + (active && sort.dir === "desc" ? " is-desc" : "")} onClick={() => onSort(sortKey)}>
        {label}<span className="seg-sorth__chev"><Icon n="chevron-up" size={13} /></span>
      </button>
    </span>
  );
}

// ── Ingredient → Inventory item selector ───────────────────────
function InventorySelector({ recipeVisibility = "Public", currentId, onClose, onSelect }) {
  const [search, setSearch] = React.useState("");
  const [cat, setCat] = React.useState("all");
  const [sort, setSort] = React.useState({ key: "name", dir: "asc" });
  const [page, setPage] = React.useState(1);
  const cats = React.useMemo(() => Array.from(new Set(ALL_INV.map((i) => i.category))).sort(), []);
  const onSort = (key) => { setSort((s) => s.key === key ? { key, dir: s.dir === "asc" ? "desc" : "asc" } : { key, dir: "asc" }); setPage(1); };
  const setF = (fn) => (v) => { fn(v); setPage(1); };

  const filtered = React.useMemo(() => {
    const q = search.trim().toLowerCase();
    let rows = ALL_INV.filter((i) => {
      if (recipeVisibility === "Public" && i.visibility !== "Public") return false;
      if (q && !(i.name.toLowerCase().includes(q) || i.id.toLowerCase().includes(q))) return false;
      if (cat !== "all" && i.category !== cat) return false;
      return true;
    });
    const dir = sort.dir === "asc" ? 1 : -1;
    rows = rows.slice().sort((a, b) => {
      let av = a[sort.key], bv = b[sort.key];
      if (typeof av === "string") { av = av.toLowerCase(); bv = bv.toLowerCase(); }
      return av < bv ? -dir : av > bv ? dir : 0;
    });
    return rows;
  }, [search, cat, sort, recipeVisibility]);

  const total = filtered.length;
  const pages = Math.max(1, Math.ceil(total / PAGE_SIZE));
  const safePage = Math.min(page, pages);
  const start = (safePage - 1) * PAGE_SIZE;
  const rows = filtered.slice(start, start + PAGE_SIZE);

  const activeFilters = [
    cat !== "all" && { key: "cat", label: cat, clear: () => setF(setCat)("all") },
    search.trim() && { key: "q", label: `“${search.trim()}”`, clear: () => setF(setSearch)("") },
  ].filter(Boolean);
  const clearAll = () => { setSearch(""); setCat("all"); setPage(1); };

  return (
    <div className="seg-selector" onClick={onClose}>
      <div className="seg-selector__card" onClick={(e) => e.stopPropagation()} style={{ maxWidth: 880 }}>
        <div className="seg-selector__head">
          <div className="seg-selector__head-txt">
            <div className="armali-eyebrow">Link · Inventory</div>
            <h3>Link a pantry item</h3>
            <p>Optionally point this ingredient at a stocked item. {recipeVisibility === "Public" ? "A public recipe can link only public items." : "A private recipe can link any item you can access."}</p>
          </div>
          <IconButton label="Close" variant="ghost" icon={<Icon n="x" size={18} />} onClick={onClose} />
        </div>

        <div className="seg-selector__filters">
          <div className="seg-selector__search">
            <Input placeholder="Search by name or code" value={search}
              onChange={(e) => setF(setSearch)(e.target.value)} iconLeft={<Icon n="search" size={16} />} />
          </div>
          <Select value={cat} onChange={(e) => setF(setCat)(e.target.value)}
            options={[{ value: "all", label: "All locations" }, ...cats.map((c) => ({ value: c, label: c }))]} />
        </div>

        <div className="seg-selector__strip">
          <span className="seg-selector__count"><b>{total}</b> {total === 1 ? "item" : "items"} match</span>
          <div className="seg-chips">
            {activeFilters.map((f) => (
              <span key={f.key} className="seg-chip">{f.label}
                <button className="seg-chip__x" onClick={f.clear} aria-label="Remove filter"><Icon n="x" size={11} /></button>
              </span>
            ))}
          </div>
          {activeFilters.length > 0 && <button className="seg-linkbtn" onClick={clearAll}><Icon n="rotate-ccw" size={14} /> Clear all</button>}
        </div>

        <div className="seg-selector__scroll">
          <div className="seg-seltable">
            <div className="seg-selhead rc-invsel-head">
              <SortHeader label="Item" sortKey="name" sort={sort} onSort={onSort} />
              <SortHeader label="Location" sortKey="category" sort={sort} onSort={onSort} />
              <span className="seg-th">Unit</span>
              <SortHeader label="In stock" sortKey="stock" sort={sort} onSort={onSort} />
              <span></span>
            </div>
            {rows.length === 0 ? (
              <div className="seg-selempty">
                <span className="seg-selempty__icon"><Icon n="package-search" size={26} /></span>
                <p>No items match these filters. Try a broader search — or leave the ingredient as free text.</p>
                <Button variant="outline" size="sm" iconLeft={<Icon n="rotate-ccw" size={15} />} onClick={clearAll}>Clear filters</Button>
              </div>
            ) : rows.map((i) => {
              const current = i.id === currentId;
              const lvl = i.stock === 0 ? "is-out" : i.stock <= 1 ? "is-low" : "";
              return (
                <div key={i.id} className={"seg-selrow rc-invsel-row" + (current ? " is-current" : "")}>
                  <div className="seg-seln"><strong>{i.name}</strong><em>{i.id}</em></div>
                  <span className="seg-selcell">{i.category}</span>
                  <span className="seg-selcell">{i.unit}</span>
                  <span className={"rc-stock " + lvl}><Icon n="boxes" size={14} />{i.stock}</span>
                  <div className="seg-selrow__act">
                    {current
                      ? <span className="seg-current-tag"><Icon n="check" size={14} /> Linked</span>
                      : <Button variant="primary" size="sm" onClick={() => onSelect(i)}>Select</Button>}
                  </div>
                </div>
              );
            })}
          </div>
        </div>

        <div className="seg-selector__foot">
          <span className="seg-pageinfo">{total === 0 ? "No results" : <React.Fragment>Showing <b>{start + 1}–{Math.min(start + PAGE_SIZE, total)}</b> of <b>{total}</b></React.Fragment>}</span>
          <Pager page={safePage} pages={pages} onPage={setPage} />
          {currentId
            ? <Button variant="ghost" onClick={() => onSelect(null)}>Clear link</Button>
            : <Button variant="ghost" onClick={onClose}>Cancel</Button>}
        </div>
      </div>
    </div>
  );
}

// ── Menu slot → Recipe selector ────────────────────────────────
function RecipeSelector({ menuVisibility = "Public", slotLabel, chosen = [], onClose, onSelect }) {
  const [search, setSearch] = React.useState("");
  const [cat, setCat] = React.useState("all");
  const [diff, setDiff] = React.useState("all");
  const [sort, setSort] = React.useState({ key: "name", dir: "asc" });
  const [page, setPage] = React.useState(1);
  const onSort = (key) => { setSort((s) => s.key === key ? { key, dir: s.dir === "asc" ? "desc" : "asc" } : { key, dir: "asc" }); setPage(1); };
  const setF = (fn) => (v) => { fn(v); setPage(1); };

  const filtered = React.useMemo(() => {
    const q = search.trim().toLowerCase();
    let rows = ALL_RCP.filter((r) => {
      if (menuVisibility === "Public" && r.visibility !== "Public") return false;
      if (q && !(r.name.toLowerCase().includes(q) || (r.notes || "").toLowerCase().includes(q))) return false;
      if (cat !== "all" && r.category !== cat) return false;
      if (diff !== "all" && (r.difficulty || "") !== diff) return false;
      return true;
    });
    const dir = sort.dir === "asc" ? 1 : -1;
    rows = rows.slice().sort((a, b) => {
      let av = sort.key === "time" ? (R.totalTime(a) || 0) : a[sort.key];
      let bv = sort.key === "time" ? (R.totalTime(b) || 0) : b[sort.key];
      if (typeof av === "string") { av = av.toLowerCase(); bv = bv.toLowerCase(); }
      return av < bv ? -dir : av > bv ? dir : 0;
    });
    return rows;
  }, [search, cat, diff, sort, menuVisibility]);

  const total = filtered.length;
  const pages = Math.max(1, Math.ceil(total / PAGE_SIZE));
  const safePage = Math.min(page, pages);
  const start = (safePage - 1) * PAGE_SIZE;
  const rows = filtered.slice(start, start + PAGE_SIZE);

  const activeFilters = [
    cat !== "all" && { key: "cat", label: cat, clear: () => setF(setCat)("all") },
    diff !== "all" && { key: "diff", label: diff, clear: () => setF(setDiff)("all") },
    search.trim() && { key: "q", label: `“${search.trim()}”`, clear: () => setF(setSearch)("") },
  ].filter(Boolean);
  const clearAll = () => { setSearch(""); setCat("all"); setDiff("all"); setPage(1); };

  return (
    <div className="seg-selector" onClick={onClose}>
      <div className="seg-selector__card" onClick={(e) => e.stopPropagation()} style={{ maxWidth: 920 }}>
        <div className="seg-selector__head">
          <div className="seg-selector__head-txt">
            <div className="armali-eyebrow">Add recipe · {slotLabel}</div>
            <h3>Choose a recipe</h3>
            <p>Pick a dish for this slot. {menuVisibility === "Public" ? "A public menu can reference only public recipes." : "A private menu can reference any recipe you can access."} The same recipe may appear in many slots.</p>
          </div>
          <IconButton label="Close" variant="ghost" icon={<Icon n="x" size={18} />} onClick={onClose} />
        </div>

        <div className="seg-selector__filters">
          <div className="seg-selector__search">
            <Input placeholder="Search recipes" value={search}
              onChange={(e) => setF(setSearch)(e.target.value)} iconLeft={<Icon n="search" size={16} />} />
          </div>
          <Select value={cat} onChange={(e) => setF(setCat)(e.target.value)}
            options={[{ value: "all", label: "All categories" }, ...REC_CATEGORIES.map((c) => ({ value: c.value, label: c.value }))]} />
          <Select value={diff} onChange={(e) => setF(setDiff)(e.target.value)}
            options={[{ value: "all", label: "Any difficulty" }, ...DIFFICULTY_ORDER.map((d) => ({ value: d, label: d }))]} />
        </div>

        <div className="seg-selector__strip">
          <span className="seg-selector__count"><b>{total}</b> {total === 1 ? "recipe" : "recipes"} match</span>
          <div className="seg-chips">
            {activeFilters.map((f) => (
              <span key={f.key} className="seg-chip">{f.label}
                <button className="seg-chip__x" onClick={f.clear} aria-label="Remove filter"><Icon n="x" size={11} /></button>
              </span>
            ))}
          </div>
          {activeFilters.length > 0 && <button className="seg-linkbtn" onClick={clearAll}><Icon n="rotate-ccw" size={14} /> Clear all</button>}
        </div>

        <div className="seg-selector__scroll">
          <div className="seg-seltable">
            <div className="seg-selhead rc-recsel-head">
              <SortHeader label="Recipe" sortKey="name" sort={sort} onSort={onSort} />
              <SortHeader label="Category" sortKey="category" sort={sort} onSort={onSort} />
              <span className="seg-th">Difficulty</span>
              <SortHeader label="Total time" sortKey="time" sort={sort} onSort={onSort} />
              <span></span>
            </div>
            {rows.length === 0 ? (
              <div className="seg-selempty">
                <span className="seg-selempty__icon"><Icon n="search-x" size={26} /></span>
                <p>No recipes match these filters. Try a broader search or clear a filter.</p>
                <Button variant="outline" size="sm" iconLeft={<Icon n="rotate-ccw" size={15} />} onClick={clearAll}>Clear filters</Button>
              </div>
            ) : rows.map((r) => {
              const c = CAT[r.category];
              const here = chosen.includes(r.id);
              const t = R.totalTime(r);
              return (
                <div key={r.id} className={"seg-selrow rc-recsel-row" + (here ? " is-current" : "")}>
                  <div className="rc-seln-flex">
                    <span className={"rc-selthumb rc-tone--" + (c ? c.tone : "neutral") + (r.hasImage ? " has-image" : " is-placeholder")}>
                      <Icon n={r.hasImage ? (c ? c.icon : "utensils") : "utensils-crossed"} size={16} />
                    </span>
                    <div className="seg-seln"><strong>{r.name}</strong><em>{r.visibility === "Private" ? "Private" : "Public"}</em></div>
                  </div>
                  <span className="seg-selcell">{r.category}</span>
                  <span><window.RcDiff difficulty={r.difficulty} /></span>
                  <span className="seg-selcell">{t != null ? R.fmtMins(t) : "—"}</span>
                  <div className="seg-selrow__act">
                    {here
                      ? <span className="seg-current-tag"><Icon n="check" size={14} /> Added</span>
                      : <Button variant="primary" size="sm" onClick={() => onSelect(r)}>Add</Button>}
                  </div>
                </div>
              );
            })}
          </div>
        </div>

        <div className="seg-selector__foot">
          <span className="seg-pageinfo">{total === 0 ? "No results" : <React.Fragment>Showing <b>{start + 1}–{Math.min(start + PAGE_SIZE, total)}</b> of <b>{total}</b></React.Fragment>}</span>
          <Pager page={safePage} pages={pages} onPage={setPage} />
          <Button variant="ghost" onClick={onClose}>Done</Button>
        </div>
      </div>
    </div>
  );
}

Object.assign(window, {
  RcInventorySelector: InventorySelector,
  RcRecipeSelector: RecipeSelector,
});
})();
