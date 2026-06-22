/* global React */
// Recipes — shared presentational bits + the URL-aware popups: the recipe
// editor (create / edit, with ordered ingredient & step lists, an image, times,
// and the ingredient→Inventory item link) and the weekly-menu editor (a 7×4
// slot grid whose cells reference recipes). Both float their entity selectors
// over the dimmed editor (the entity-selector pattern). Exposed on window.
(() => {
const A = window.ProjectArmaliDesignSystem_ce5329;
const { Badge, Button, IconButton, Tooltip, Input, Select } = A;
const Icon = window.SegIcon;
const R = window.SegRec;
const { CAT, REC_CATEGORIES, REC_DIFFICULTY, DIFFICULTY_ORDER, MEAL_SLOTS, INV, RCP } = R;

// ── Shared presentational bits (consumed across all module files) ──
function RcThumb({ recipe, className = "", glyph = 34 }) {
  const c = CAT[recipe.category];
  const tone = c ? c.tone : "neutral";
  const icon = recipe.hasImage ? (c ? c.icon : "utensils-crossed") : "utensils-crossed";
  return (
    <div className={"rc-thumb rc-tone--" + tone + (recipe.hasImage ? " has-image" : " is-placeholder") + (className ? " " + className : "")}>
      <span className="rc-thumb__glyph"><Icon n={icon} size={glyph} /></span>
    </div>
  );
}
function RcCatChip({ category }) {
  const c = CAT[category];
  const tone = c ? c.tone : "neutral";
  return <span className={"rc-cat rc-tone--" + tone}><Icon n={c ? c.icon : "utensils-crossed"} size={12} />{category}</span>;
}
function RcDiff({ difficulty }) {
  if (!difficulty) return <span className="rc-diff" style={{ color: "var(--text-muted)" }}>No difficulty</span>;
  return (
    <span className={"rc-diff rc-diff--" + difficulty.toLowerCase()}>
      <span className="rc-diff__dots"><i className="rc-diff__dot" /><i className="rc-diff__dot" /><i className="rc-diff__dot" /></span>
      {difficulty}
    </span>
  );
}
function RcMeta({ recipe }) {
  const t = R.totalTime(recipe);
  return (
    <span className="rc-meta">
      {t != null && <span><Icon n="clock" size={13} />{R.fmtMins(t)}</span>}
      {recipe.servings ? <span><Icon n="users" size={13} />{recipe.servings} serv</span> : null}
    </span>
  );
}

// ── Recipe editor (create / edit) ───────────────────────────────
let _nid = 7000;
function RecipeEditDialog({ mode, recipe, onClose, initialPick = null }) {
  const editing = mode === "edit";
  const r = editing ? recipe : null;
  const [cat, setCat] = React.useState(editing ? r.category : "Main");
  const [diff, setDiff] = React.useState(editing ? (r.difficulty || "") : "");
  const [vis, setVis] = React.useState(editing ? r.visibility : "Public");
  const [ings, setIngs] = React.useState(() => editing ? r.ingredients.map((i) => ({ ...i })) : []);
  const [steps, setSteps] = React.useState(() => editing ? r.steps.map((s) => ({ ...s })) : []);
  const [picking, setPicking] = React.useState(initialPick);

  const tone = (CAT[cat] && CAT[cat].tone) || "neutral";
  const hasImage = editing ? r.hasImage : false;

  const setIng = (id, key, val) => setIngs((xs) => xs.map((x) => x.id === id ? { ...x, [key]: val } : x));
  const addIng = () => setIngs((xs) => [...xs, { id: "ing" + (++_nid), name: "", qty: "", itemId: null }]);
  const delIng = (id) => setIngs((xs) => xs.filter((x) => x.id !== id));
  const addStep = () => setSteps((xs) => [...xs, { id: "stp" + (++_nid), text: "" }]);
  const delStep = (id) => setSteps((xs) => xs.filter((x) => x.id !== id));

  const pickItem = (item) => { if (picking) setIng(picking, "itemId", item ? item.id : null); setPicking(null); };

  return (
    <div className={"seg-modal" + (picking ? " is-under" : "")} onClick={picking ? undefined : onClose}>
      <div className={"seg-modal__card rc-editdlg" + (picking ? " is-behind" : "")} onClick={(e) => e.stopPropagation()}>
        <div className="seg-modal__head">
          <h3>{editing ? "Edit recipe" : "New recipe"}</h3>
          <p>{editing
            ? "Update the dish, its ingredients and its method. Each ingredient may link to a pantry item."
            : "Add a recipe to the collection. New recipes start Public, with no difficulty, times or content."}</p>
        </div>

        <div className="rc-editdlg__body">
          {/* Identity: image + name / category / difficulty / times */}
          <div className="rc-identity">
            <div className="rc-imguploader">
              <button type="button" className={"rc-imgdrop rc-tone--" + tone + (hasImage ? " has-image" : " is-placeholder")} aria-label="Change dish photo">
                <span className="rc-imgdrop__glyph"><Icon n={hasImage ? (CAT[cat] ? CAT[cat].icon : "utensils") : "image-plus"} size={30} /></span>
                <span className="rc-imgdrop__edit"><Icon n="camera" size={13} /></span>
              </button>
              <span className="rc-imguploader__hint">{hasImage ? "Primary image · replace" : "Add a dish photo"}</span>
            </div>

            <div className="rc-identity__fields">
              <Input label="Name" defaultValue={editing ? r.name : ""} placeholder="What's the dish called?" iconLeft={<Icon n="utensils" size={16} />} />
              <div className="rc-grid2">
                <div>
                  <span className="seg-field-label">Category</span>
                  <Select value={cat} onChange={(e) => setCat(e.target.value)}
                    options={REC_CATEGORIES.map((c) => ({ value: c.value, label: c.value }))} />
                </div>
                <div>
                  <span className="seg-field-label">Difficulty</span>
                  <Select value={diff} onChange={(e) => setDiff(e.target.value)}
                    options={[{ value: "", label: "No difficulty" }, ...DIFFICULTY_ORDER.map((d) => ({ value: d, label: d }))]} />
                </div>
              </div>
              <div className="rc-grid3">
                <div>
                  <span className="seg-field-label">Servings</span>
                  <div className="rc-numfield"><input type="number" min="1" defaultValue={editing && r.servings ? r.servings : ""} placeholder="—" /><span className="rc-numfield__unit">serv</span></div>
                </div>
                <div>
                  <span className="seg-field-label">Prep time</span>
                  <div className="rc-numfield"><input type="number" min="0" defaultValue={editing && r.prep != null ? r.prep : ""} placeholder="—" /><span className="rc-numfield__unit">min</span></div>
                </div>
                <div>
                  <span className="seg-field-label">Cook time</span>
                  <div className="rc-numfield"><input type="number" min="0" defaultValue={editing && r.cook != null ? r.cook : ""} placeholder="—" /><span className="rc-numfield__unit">min</span></div>
                </div>
              </div>
            </div>
          </div>

          {/* Ingredients — ordered, each may link a pantry item */}
          <div className="rc-section">
            <div className="rc-section__head">
              <span className="rc-section__icon"><Icon n="list" size={16} /></span>
              <h4>Ingredients</h4>
              <span className="rc-section__n">{ings.length}</span>
              <span className="rc-section__hint">Name, optional quantity, optional pantry link</span>
            </div>
            {ings.length === 0 ? (
              <div className="rc-subempty">
                <span className="rc-subempty__icon"><Icon n="carrot" size={22} /></span>
                <p>Nothing here yet — add the first ingredient. A quantity and a pantry link are optional.</p>
              </div>
            ) : (
              <div className="rc-inglist">
                {ings.map((ing) => {
                  const item = ing.itemId ? INV[ing.itemId] : null;
                  return (
                    <div key={ing.id} className="rc-ingrow">
                      <span className="rc-ingrow__handle"><Icon n="grip-vertical" size={15} /></span>
                      <input className="rc-input" value={ing.name} placeholder="Ingredient" onChange={(e) => setIng(ing.id, "name", e.target.value)} />
                      <input className="rc-input rc-input--qty" value={ing.qty || ""} placeholder="Qty" onChange={(e) => setIng(ing.id, "qty", e.target.value)} />
                      <button type="button" className={"rc-itemlink" + (item ? " is-linked" : "")} onClick={() => setPicking(ing.id)}>
                        <span className="rc-itemlink__icon"><Icon n={item ? "package" : "link"} size={14} /></span>
                        <span className="rc-itemlink__txt">{item ? item.name : "Link pantry item"}</span>
                        {item && <span className="rc-itemlink__clear" role="button" aria-label="Clear item link"
                          onClick={(e) => { e.stopPropagation(); setIng(ing.id, "itemId", null); }}><Icon n="x" size={12} /></span>}
                      </button>
                      <Tooltip label="Remove" side="left">
                        <IconButton className="rc-rowdel" size="sm" variant="ghost" label="Remove ingredient" icon={<Icon n="trash-2" size={15} />} onClick={() => delIng(ing.id)} />
                      </Tooltip>
                    </div>
                  );
                })}
              </div>
            )}
            <button className="rc-addrow" onClick={addIng}><Icon n="plus" size={16} /> Add ingredient</button>
          </div>

          {/* Steps — ordered method */}
          <div className="rc-section">
            <div className="rc-section__head">
              <span className="rc-section__icon"><Icon n="list-ordered" size={16} /></span>
              <h4>Method</h4>
              <span className="rc-section__n">{steps.length}</span>
              <span className="rc-section__hint">Shown in order</span>
            </div>
            {steps.length === 0 ? (
              <div className="rc-subempty">
                <span className="rc-subempty__icon"><Icon n="chef-hat" size={22} /></span>
                <p>No steps yet — add the first instruction. Steps are numbered automatically in order.</p>
              </div>
            ) : (
              <div className="rc-steplist">
                {steps.map((s, i) => (
                  <div key={s.id} className="rc-steprow">
                    <span className="rc-stepnum">{i + 1}</span>
                    <textarea className="rc-steptext" value={s.text} placeholder="Describe this step…"
                      onChange={(e) => setSteps((xs) => xs.map((x) => x.id === s.id ? { ...x, text: e.target.value } : x))} />
                    <Tooltip label="Remove" side="left">
                      <IconButton className="rc-rowdel" size="sm" variant="ghost" label="Remove step" icon={<Icon n="trash-2" size={15} />} onClick={() => delStep(s.id)} />
                    </Tooltip>
                  </div>
                ))}
              </div>
            )}
            <button className="rc-addrow" onClick={addStep}><Icon n="plus" size={16} /> Add step</button>
          </div>

          {/* Notes */}
          <div className="seg-field">
            <span className="seg-field-label">Notes</span>
            <textarea className="mood-textarea" defaultValue={editing ? (r.notes || "") : ""}
              placeholder="Tips, substitutions, where it came from… (optional)" />
            <span className="seg-field-hint">Up to 2,000 characters.</span>
          </div>

          {/* Attachments */}
          <div className="seg-field">
            <span className="seg-field-label">Attachments</span>
            <div className="rc-attach">
              {hasImage && <span className="rc-attachchip is-primary"><Icon n="star" size={13} /> dish.jpg · primary</span>}
              <span className="rc-attachchip"><Icon n="paperclip" size={13} /> {hasImage ? "Add another" : "Add an image or file"}</span>
            </div>
            <span className="seg-field-hint">One image can be the primary thumbnail. Falls back to the first image, then a placeholder.</span>
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
            <span className="seg-field-hint">A public recipe may link only public pantry items. Only the creator can change visibility.</span>
          </div>
        </div>

        <div className="seg-modal__foot">
          <span className="seg-modal__foot-note">
            {editing
              ? <React.Fragment><Icon n="user-round" size={13} /> {r.owner} · created {r.created}</React.Fragment>
              : <React.Fragment><Icon n="sparkles" size={13} /> Starts Public · no difficulty · empty</React.Fragment>}
          </span>
          <div className="seg-modal__foot-actions">
            {editing && <Button variant="ghost" size="sm" className="seg-danger" iconLeft={<Icon n="trash-2" size={15} />}>Delete</Button>}
            <Button variant="ghost" onClick={onClose}>Cancel</Button>
            <Button variant="primary" iconLeft={<Icon n={editing ? "check" : "plus"} size={17} />} onClick={onClose}>
              {editing ? "Save changes" : "Create recipe"}
            </Button>
          </div>
        </div>
      </div>

      {picking && window.RcInventorySelector && (
        <window.RcInventorySelector
          recipeVisibility={vis}
          currentId={(ings.find((x) => x.id === picking) || {}).itemId || null}
          onClose={() => setPicking(null)}
          onSelect={pickItem}
        />
      )}
    </div>
  );
}

// ── Weekly-menu editor (create / edit) ──────────────────────────
function MenuEditDialog({ mode, menu, weekMonday, onClose, initialSlot = null }) {
  const editing = mode === "edit";
  const week = editing ? menu.week : (weekMonday || R.WEEK_MONDAY);
  const [vis, setVis] = React.useState(editing ? menu.visibility : "Public");
  const [grid, setGrid] = React.useState(() => {
    const g = {};
    for (let d = 0; d < 7; d++) { g[d] = {}; MEAL_SLOTS.forEach((s) => { g[d][s.key] = (editing && menu.grid[d] && menu.grid[d][s.key]) ? menu.grid[d][s.key].slice() : []; }); }
    return g;
  });
  const [slot, setSlot] = React.useState(initialSlot); // { day, slot } | null
  const days = R.weekDays(week);
  const today = R.isoOf(R.TODAY);

  const addToSlot = (rcpId) => {
    if (!slot) return;
    setGrid((g) => {
      const cur = g[slot.day][slot.slot];
      if (cur.includes(rcpId)) return g;
      return { ...g, [slot.day]: { ...g[slot.day], [slot.slot]: [...cur, rcpId] } };
    });
    setSlot(null);
  };
  const removeFromSlot = (day, slotKey, rcpId) =>
    setGrid((g) => ({ ...g, [day]: { ...g[day], [slotKey]: g[day][slotKey].filter((x) => x !== rcpId) } }));

  const count = Object.values(grid).reduce((n, day) => n + Object.values(day).reduce((m, arr) => m + arr.length, 0), 0);

  return (
    <div className={"seg-selector" + (slot ? " is-under" : "")} onClick={slot ? undefined : onClose}>
      <div className={"seg-selector__card rc-menudlg" + (slot ? " is-behind" : "")} onClick={(e) => e.stopPropagation()}>
        <div className="seg-selector__head">
          <div className="seg-selector__head-txt">
            <div className="armali-eyebrow">{editing ? "Edit menu" : "New menu"}</div>
            <h3>{editing ? "Weekly menu" : "Plan a week"}</h3>
            <p>Drop recipes into any of the four meal slots across the week. Slots reference recipes only.</p>
          </div>
          <IconButton label="Close" variant="ghost" icon={<Icon n="x" size={18} />} onClick={onClose} />
        </div>

        <div className="rc-menudlg__meta">
          <div className="rc-menudlg__field">
            <span>Week</span>
            <span className="rc-menudlg__weektag"><Icon n="calendar-days" size={15} /> {R.fmtWeekRange(week)}</span>
          </div>
          <div className="rc-menudlg__field rc-menudlg__field--name">
            <span>Name <span style={{ color: "var(--text-muted)", fontWeight: 500 }}>· optional</span></span>
            <Input defaultValue={editing ? (menu.name || "") : ""} placeholder="e.g. Diet week, Guests" />
          </div>
          <div className="rc-menudlg__field">
            <span>Visibility</span>
            <div className="mood-seg seg-visseg">
              {[["Public", "globe"], ["Private", "lock"]].map(([v, ic]) => (
                <button key={v} className={"mood-seg__btn" + (vis === v ? " is-active" : "")} onClick={() => setVis(v)}>
                  <Icon n={ic} size={14} /> {v}
                </button>
              ))}
            </div>
          </div>
        </div>

        <div className="rc-menudlg__gridwrap">
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
                    const list = grid[di][s.key];
                    return (
                      <div key={di} className={"rc-cell" + (isToday ? " is-today" : "")}>
                        {list.map((rid) => {
                          const rec = RCP[rid];
                          if (!rec) return null;
                          const c = CAT[rec.category];
                          return (
                            <div key={rid} className="rc-slotchip">
                              <span className={"rc-slotchip__thumb rc-tone--" + (c ? c.tone : "neutral") + (rec.hasImage ? " has-image" : " is-placeholder")}>
                                <Icon n={rec.hasImage ? (c ? c.icon : "utensils") : "utensils-crossed"} size={12} />
                              </span>
                              <span className="rc-slotchip__name">{rec.name}</span>
                              <IconButton size="sm" variant="ghost" label="Remove from slot" icon={<Icon n="x" size={12} />} onClick={() => removeFromSlot(di, s.key, rid)} />
                            </div>
                          );
                        })}
                        <button className="rc-slotadd" onClick={() => setSlot({ day: di, slot: s.key })}><Icon n="plus" size={13} /> Add</button>
                      </div>
                    );
                  })}
                </React.Fragment>
              ))}
            </div>
          </div>
        </div>

        <div className="seg-selector__foot">
          <span className="seg-pageinfo"><Icon n="info" size={14} /> {count} {count === 1 ? "recipe" : "recipes"} placed · {vis.toLowerCase()} menu</span>
          <div className="seg-modal__foot-actions">
            {editing && <Button variant="ghost" size="sm" className="seg-danger" iconLeft={<Icon n="trash-2" size={15} />}>Delete menu</Button>}
            <Button variant="ghost" onClick={onClose}>Cancel</Button>
            <Button variant="primary" iconLeft={<Icon n={editing ? "check" : "plus"} size={17} />} onClick={onClose}>{editing ? "Save menu" : "Create menu"}</Button>
          </div>
        </div>
      </div>

      {slot && window.RcRecipeSelector && (
        <window.RcRecipeSelector
          menuVisibility={vis}
          slotLabel={slot.slot + " · " + R.DOW_LONG[slot.day]}
          chosen={grid[slot.day][slot.slot]}
          onClose={() => setSlot(null)}
          onSelect={(rec) => addToSlot(rec.id)}
        />
      )}
    </div>
  );
}

Object.assign(window, {
  RcThumb, RcCatChip, RcDiff, RcMeta,
  RcRecipeEditDialog: RecipeEditDialog,
  RcMenuEditDialog: MenuEditDialog,
});
})();
