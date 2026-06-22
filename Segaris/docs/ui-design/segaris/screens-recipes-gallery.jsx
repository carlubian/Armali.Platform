/* global React */
// Recipes — the module entry. A server-paginated recipe thumbnail gallery
// (search · category & difficulty filters · name/category sort) and a weekly
// menu planner (a 7-day × 4-slot grid with Monday-anchored week navigation),
// switched through internal navigation. The orchestrator wires the URL-aware
// recipe editor, menu editor, and their entity selectors over the views.
// Canvas variants are exported at the end.
(() => {
const A = window.ProjectArmaliDesignSystem_ce5329;
const { Button, IconButton, Tooltip, Input, Select, Badge } = A;
const Icon = window.SegIcon;
const R = window.SegRec;
const { CAT, REC_CATEGORIES, DIFFICULTY_ORDER, MEAL_SLOTS, RCP } = R;
const ALL = window.SEG_RECIPES;
const MENU = window.SEG_MENU;

// ── Gallery cards ───────────────────────────────────────────────
function VisDot({ visibility }) {
  const priv = visibility === "Private";
  return (
    <Tooltip label={priv ? "Private — only you" : "Public"} side="left">
      <span className={"rc-thumb__vis" + (priv ? " is-private" : "")}><Icon n={priv ? "lock" : "globe"} size={13} /></span>
    </Tooltip>
  );
}

function TileCard({ recipe, onOpen }) {
  return (
    <div role="button" tabIndex={0} className="rc-tile" onClick={() => onOpen(recipe)}>
      <div style={{ position: "relative" }}>
        <window.RcThumb recipe={recipe} glyph={40} />
        <VisDot visibility={recipe.visibility} />
      </div>
      <div className="rc-tile__body">
        <div className="rc-tile__name">{recipe.name}</div>
        <div className="rc-tile__row">
          <window.RcCatChip category={recipe.category} />
          <window.RcMeta recipe={recipe} />
        </div>
      </div>
    </div>
  );
}

function DetailCard({ recipe, onOpen }) {
  return (
    <div role="button" tabIndex={0} className="rc-dcard" onClick={() => onOpen(recipe)}>
      <div className="rc-dcard__thumb" style={{ position: "relative" }}>
        <window.RcThumb recipe={recipe} glyph={30} />
      </div>
      <div className="rc-dcard__body">
        <div className="rc-dcard__top">
          <div className="rc-dcard__name">{recipe.name}</div>
          <VisDot visibility={recipe.visibility} />
        </div>
        <div className="rc-dcard__meta">
          <window.RcCatChip category={recipe.category} />
          <window.RcDiff difficulty={recipe.difficulty} />
        </div>
        <div className="rc-dcard__counts">
          <span className="rc-dcard__count"><Icon n="clock" size={14} />{R.totalTime(recipe) != null ? R.fmtMins(R.totalTime(recipe)) : "—"}</span>
          <span className="rc-dcard__count"><Icon n="users" size={14} />{recipe.servings || "—"}</span>
          <span className="rc-dcard__count"><Icon n="list" size={14} />{recipe.ingredients.length} ingr</span>
          <span className="rc-dcard__count"><Icon n="list-ordered" size={14} />{recipe.steps.length} steps</span>
        </div>
      </div>
    </div>
  );
}

// ── Pager (demo: single page) ───────────────────────────────────
function Pager({ page, pages }) {
  return (
    <div className="seg-pager">
      <button className="seg-pager__btn" disabled={page <= 1} aria-label="Previous page"><Icon n="chevron-left" size={16} /></button>
      {Array.from({ length: pages }, (_, i) => i + 1).map((p) => <button key={p} className={"seg-pager__btn" + (p === page ? " is-active" : "")}>{p}</button>)}
      <button className="seg-pager__btn" disabled={page >= pages} aria-label="Next page"><Icon n="chevron-right" size={16} /></button>
    </div>
  );
}

// ── Gallery view ────────────────────────────────────────────────
function GalleryView({ variant, viewSwitch, onOpen, onNew }) {
  const [search, setSearch] = React.useState("");
  const [cat, setCat] = React.useState("");
  const [diff, setDiff] = React.useState("");
  const [sort, setSort] = React.useState({ key: "name", dir: "asc" });

  const filtered = React.useMemo(() => {
    const q = search.trim().toLowerCase();
    let list = ALL.filter((r) => {
      if (cat && r.category !== cat) return false;
      if (diff && (r.difficulty || "") !== diff) return false;
      if (q && !(r.name.toLowerCase().includes(q) || (r.notes || "").toLowerCase().includes(q))) return false;
      return true;
    });
    const dir = sort.dir === "asc" ? 1 : -1;
    list = list.slice().sort((a, b) => {
      let cmp = sort.key === "category" ? a.category.localeCompare(b.category) : a.name.localeCompare(b.name);
      if (cmp === 0) cmp = a.name.localeCompare(b.name);
      return dir * cmp;
    });
    return list;
  }, [search, cat, diff, sort]);

  const inMenu = React.useMemo(() => {
    const set = new Set();
    Object.values(MENU.grid).forEach((day) => Object.values(day).forEach((arr) => arr.forEach((id) => set.add(id))));
    return set.size;
  }, []);

  const Card = variant === "detail" ? DetailCard : TileCard;

  return (
    <React.Fragment>
      <div className="rc-bar">
        <div>
          <div className="armali-eyebrow">The household cookbook</div>
          <h2>Recipes</h2>
          <p>Your collection of dishes — ingredients, method and a photo. Plan them into weekly menus from the planner.</p>
        </div>
        <div className="rc-bar__stats">
          {viewSwitch}
          <div className="seg-stat-pill"><strong>{ALL.length}</strong><span>Recipes</span></div>
          <div className="seg-stat-pill"><strong>{inMenu}</strong><span>In this week</span></div>
          <Button variant="primary" iconLeft={<Icon n="plus" size={17} />} onClick={onNew}>New recipe</Button>
        </div>
      </div>

      <div className="rc-toolbar">
        <div className="rc-toolbar__search">
          <Input placeholder="Search by name" value={search} onChange={(e) => setSearch(e.target.value)} iconLeft={<Icon n="search" size={16} />} />
        </div>
        <Select value={cat} onChange={(e) => setCat(e.target.value)}
          options={[{ value: "", label: "All categories" }, ...REC_CATEGORIES.map((c) => ({ value: c.value, label: c.value }))]} />
        <Select value={diff} onChange={(e) => setDiff(e.target.value)}
          options={[{ value: "", label: "Any difficulty" }, ...DIFFICULTY_ORDER.map((d) => ({ value: d, label: d }))]} />
        <span className="rc-toolbar__spacer" />
        <div className="rc-sort">
          <Select value={sort.key} onChange={(e) => setSort((s) => ({ ...s, key: e.target.value }))}
            options={[{ value: "name", label: "Sort: name" }, { value: "category", label: "Sort: category" }]} />
          <Tooltip label={sort.dir === "asc" ? "Ascending" : "Descending"} side="bottom">
            <button className="rc-sort__dir" onClick={() => setSort((s) => ({ ...s, dir: s.dir === "asc" ? "desc" : "asc" }))} aria-label="Toggle sort direction">
              <Icon n={sort.dir === "asc" ? "arrow-down-a-z" : "arrow-up-z-a"} size={16} />
            </button>
          </Tooltip>
        </div>
      </div>

      <div className="rc-galleryscroll">
        {filtered.length === 0 ? (
          <div className="rc-gallery">
            <div className="rc-empty">
              <span className="rc-empty__icon"><Icon n="utensils-crossed" size={26} /></span>
              <h3>Nothing matches</h3>
              <p>No recipes match your search and filters — try clearing them.</p>
            </div>
          </div>
        ) : (
          <div className={"rc-gallery rc-gallery--" + variant}>
            {filtered.map((r) => <Card key={r.id} recipe={r} onOpen={onOpen} />)}
          </div>
        )}
      </div>

      <div className="seg-selector__foot" style={{ borderTop: "none", padding: "0 var(--space-2)", background: "transparent" }}>
        <span className="seg-pageinfo">{filtered.length === 0 ? "No results" : <React.Fragment>Showing <b>1–{filtered.length}</b> of <b>{filtered.length}</b> recipes</React.Fragment>}</span>
        <div style={{ display: "flex", alignItems: "center", gap: "var(--space-4)" }}>
          <Select value="25" options={[{ value: "25", label: "25 per page" }, { value: "50", label: "50 per page" }]} onChange={() => {}} />
          <Pager page={1} pages={1} />
        </div>
      </div>
    </React.Fragment>
  );
}

// ── Menu planner view ───────────────────────────────────────────
function PlannerView({ viewSwitch, onOpenRecipe, onEditMenu, onNewMenu }) {
  const [week, setWeek] = React.useState(R.WEEK_MONDAY);
  const days = R.weekDays(week);
  const today = R.isoOf(R.TODAY);
  const hasMenu = week === MENU.week;
  const shiftWeek = (n) => setWeek(R.isoOf(R.addDays(R.parseISO(week), n * 7)));
  const goCurrent = () => setWeek(R.isoOf(R.mondayOf(R.TODAY)));

  const placed = hasMenu
    ? Object.values(MENU.grid).reduce((n, day) => n + Object.values(day).reduce((m, arr) => m + arr.length, 0), 0)
    : 0;

  return (
    <React.Fragment>
      <div className="rc-plannerbar">
        <div>
          <div className="armali-eyebrow">Weekly menu planner</div>
          <h2>Menu planner</h2>
          <p>Lay your recipes across the week — four meal slots a day, Monday to Sunday.</p>
        </div>
        <div className="rc-plannerbar__controls">
          {viewSwitch}
          <div className="mood-nav">
            <button className="mood-nav__btn" onClick={() => shiftWeek(-1)} aria-label="Previous week"><Icon n="chevron-left" size={18} /></button>
            <div className="mood-nav__label">{R.fmtWeekRange(week)}<small>{hasMenu ? (MENU.name || "Menu") : "No menu"}</small></div>
            <button className="mood-nav__btn" onClick={() => shiftWeek(1)} aria-label="Next week"><Icon n="chevron-right" size={18} /></button>
          </div>
          <Button variant="outline" size="sm" iconLeft={<Icon n="calendar-check" size={15} />} onClick={goCurrent}>This week</Button>
          {hasMenu
            ? <Button variant="primary" iconLeft={<Icon n="pencil" size={16} />} onClick={() => onEditMenu(MENU)}>Edit menu</Button>
            : <Button variant="primary" iconLeft={<Icon n="plus" size={17} />} onClick={() => onNewMenu(week)}>New menu</Button>}
        </div>
      </div>

      {hasMenu ? (
        <div className="rc-gridscroll">
          <div className="rc-grid">
            <div className="rc-grid__corner" />
            {days.map((d, i) => {
              const isToday = R.isoOf(d) === today;
              return (
                <div key={i} className={"rc-grid__day" + (isToday ? " is-today" : "")}>
                  <span className="rc-grid__dow">{R.DOW_SHORT[i]}</span>
                  <span className="rc-grid__date">{d.getDate()} {R.MONTHS_SHORT[d.getMonth()]}</span>
                </div>
              );
            })}
            {MEAL_SLOTS.map((s) => (
              <React.Fragment key={s.key}>
                <div className="rc-grid__slotlabel"><Icon n={s.icon} size={15} /> {s.key}</div>
                {days.map((d, di) => {
                  const isToday = R.isoOf(d) === today;
                  const list = (MENU.grid[di] && MENU.grid[di][s.key]) || [];
                  return (
                    <div key={di} className={"rc-cell" + (isToday ? " is-today" : "")}>
                      {list.map((rid) => {
                        const rec = RCP[rid];
                        if (!rec) return null;
                        const c = CAT[rec.category];
                        return (
                          <button key={rid} className="rc-slotchip" onClick={() => onOpenRecipe(rec)}>
                            <span className={"rc-slotchip__thumb rc-tone--" + (c ? c.tone : "neutral") + (rec.hasImage ? " has-image" : " is-placeholder")}>
                              <Icon n={rec.hasImage ? (c ? c.icon : "utensils") : "utensils-crossed"} size={12} />
                            </span>
                            <span className="rc-slotchip__name">{rec.name}</span>
                          </button>
                        );
                      })}
                      <button className="rc-slotadd" onClick={() => onEditMenu(MENU)}><Icon n="plus" size={13} /> Add</button>
                    </div>
                  );
                })}
              </React.Fragment>
            ))}
          </div>
        </div>
      ) : (
        <div className="rc-noweek">
          <span className="rc-noweek__icon"><Icon n="calendar-plus" size={28} /></span>
          <h3>No menu for this week</h3>
          <p>This week has no menu yet. Create one to start planning meals — navigating to a week never creates a menu on its own.</p>
          <Button variant="primary" iconLeft={<Icon n="plus" size={17} />} onClick={() => onNewMenu(week)}>Create menu for this week</Button>
        </div>
      )}
    </React.Fragment>
  );
}

// ── Orchestrator ────────────────────────────────────────────────
function RecipesScreen({ view: initialView = "recipes", variant = "tile", initialDialog }) {
  const [view, setView] = React.useState(initialView);
  const [dialog, setDialog] = React.useState(initialDialog || null);
  const close = () => setDialog(null);

  const viewSwitch = (
    <div className="rc-viewswitch">
      <button className={"rc-viewswitch__btn" + (view === "recipes" ? " is-active" : "")} onClick={() => setView("recipes")}>
        <Icon n="book-open" size={15} /> Recipes
      </button>
      <button className={"rc-viewswitch__btn" + (view === "menus" ? " is-active" : "")} onClick={() => setView("menus")}>
        <Icon n="calendar-days" size={15} /> Menu planner
      </button>
    </div>
  );

  const recipeDialog = dialog && (dialog.mode === "edit" || dialog.mode === "new");
  const menuDialog = dialog && (dialog.mode === "menu-edit" || dialog.mode === "menu-new");

  return (
    <div className="seg-screen">
      {window.SegShellTopBar ? <window.SegShellTopBar eyebrow="Recipes" title={view === "menus" ? "Menu planner" : "Recipes"} /> : null}
      <div className="seg-page">
        <div className="seg-page__inner">
          {view === "recipes" ? (
            <GalleryView variant={variant} viewSwitch={viewSwitch}
              onOpen={(recipe) => setDialog({ mode: "edit", recipe })}
              onNew={() => setDialog({ mode: "new" })} />
          ) : (
            <PlannerView viewSwitch={viewSwitch}
              onOpenRecipe={(recipe) => setDialog({ mode: "edit", recipe })}
              onEditMenu={(menu) => setDialog({ mode: "menu-edit", menu })}
              onNewMenu={(week) => setDialog({ mode: "menu-new", week })} />
          )}
        </div>
      </div>

      {recipeDialog && (
        <window.RcRecipeEditDialog
          mode={dialog.mode === "new" ? "new" : "edit"}
          recipe={dialog.recipe}
          initialPick={dialog.pick || null}
          onClose={close} />
      )}
      {menuDialog && (
        <window.RcMenuEditDialog
          mode={dialog.mode === "menu-new" ? "new" : "edit"}
          menu={dialog.menu}
          weekMonday={dialog.week}
          initialSlot={dialog.slot || null}
          onClose={close} />
      )}
    </div>
  );
}

// ── Canvas variants ─────────────────────────────────────────────
const featured = RCP["rcp-carbonara"] || ALL[0];
const firstIng = featured.ingredients.find((i) => !i.itemId) || featured.ingredients[0];

const RecipesGalleryTile   = () => <RecipesScreen view="recipes" variant="tile" />;
const RecipesGalleryDetail = () => <RecipesScreen view="recipes" variant="detail" />;
const RecipesEditor        = () => <RecipesScreen view="recipes" variant="tile" initialDialog={{ mode: "edit", recipe: featured }} />;
const RecipesNew           = () => <RecipesScreen view="recipes" variant="tile" initialDialog={{ mode: "new" }} />;
const RecipesIngredientPick = () => <RecipesScreen view="recipes" variant="tile" initialDialog={{ mode: "edit", recipe: featured, pick: firstIng.id }} />;
const RecipesPlanner       = () => <RecipesScreen view="menus" />;
const RecipesMenuEditor    = () => <RecipesScreen view="menus" initialDialog={{ mode: "menu-edit", menu: MENU }} />;
const RecipesSlotPick      = () => <RecipesScreen view="menus" initialDialog={{ mode: "menu-edit", menu: MENU, slot: { day: 1, slot: "Dinner" } }} />;

Object.assign(window, {
  RecipesGalleryTile, RecipesGalleryDetail, RecipesEditor, RecipesNew,
  RecipesIngredientPick, RecipesPlanner, RecipesMenuEditor, RecipesSlotPick,
});
})();
