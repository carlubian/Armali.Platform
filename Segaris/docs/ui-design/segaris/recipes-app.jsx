/* global React, ReactDOM */
(() => {
const { DesignCanvas, DCSection, DCArtboard } = window;
const { useTweaks, TweaksPanel, TweakSection, TweakColor, TweakSlider } = window;

const ACCENTS = {
  aqua: {
    "--accent": "var(--aqua-500)", "--accent-hover": "var(--aqua-600)", "--accent-press": "var(--aqua-700)",
    "--accent-soft": "var(--aqua-100)", "--ring-focus": "var(--aqua-400)",
    "--ring": "0 0 0 3px rgba(69,188,184,0.40)",
    "--glow-aqua": "0 0 0 1px rgba(22,166,166,0.30), 0 8px 28px -8px rgba(22,166,166,0.55)",
    "--seg-brand-grad": "linear-gradient(140deg, var(--aqua-400), var(--azure-500))",
  },
  gold: {
    "--accent": "var(--gold-500)", "--accent-hover": "var(--gold-600)", "--accent-press": "var(--gold-600)",
    "--accent-soft": "var(--gold-100)", "--ring-focus": "var(--gold-400)",
    "--ring": "0 0 0 3px rgba(219,166,62,0.42)",
    "--glow-aqua": "0 0 0 1px rgba(219,166,62,0.32), 0 8px 28px -8px rgba(219,166,62,0.55)",
    "--seg-brand-grad": "linear-gradient(140deg, var(--gold-400), var(--gold-600))",
  },
  azure: {
    "--accent": "var(--azure-500)", "--accent-hover": "var(--azure-600)", "--accent-press": "var(--azure-600)",
    "--accent-soft": "var(--azure-100)", "--ring-focus": "var(--azure-400)",
    "--ring": "0 0 0 3px rgba(94,153,189,0.42)",
    "--glow-aqua": "0 0 0 1px rgba(58,124,165,0.32), 0 8px 28px -8px rgba(58,124,165,0.55)",
    "--seg-brand-grad": "linear-gradient(140deg, var(--azure-400), var(--azure-600))",
  },
};
const ACCENT_SWATCH = { "#16A6A6": "aqua", "#DBA63E": "gold", "#3A7CA5": "azure" };

const TWEAK_DEFAULTS = /*EDITMODE-BEGIN*/{
  "accent": "#16A6A6",
  "aurora": 38,
  "glass": 18
}/*EDITMODE-END*/;

function useLucide() {
  React.useEffect(() => {
    let queued = false;
    const run = () => { queued = false; if (document.querySelector("[data-lucide]") && window.lucide) window.lucide.createIcons(); };
    const schedule = () => { if (!queued) { queued = true; requestAnimationFrame(run); } };
    schedule();
    const mo = new MutationObserver(schedule);
    mo.observe(document.body, { childList: true, subtree: true });
    return () => mo.disconnect();
  }, []);
}

function Frame({ children }) {
  return <div style={{ width: "100%", height: "100%", background: "var(--surface-app)" }}>{children}</div>;
}

function DesignNotes() {
  const R = window.SegRec;
  const total = window.SEG_RECIPES.length;
  const cats = R.REC_CATEGORIES.length;
  return (
    <div className="seg-notes">
      <div>
        <div className="armali-eyebrow" style={{ color: "var(--accent)" }}>Segaris · Recipes module</div>
        <h2>The household's cooking module</h2>
      </div>
      <p style={{ maxWidth: "94ch" }}>
        Recipes maintains a <strong>recipe collection</strong> — each dish records its ingredients, an ordered list of
        <strong> preparation steps</strong>, a photo, optional difficulty, servings and times — and lets the household plan
        <strong> weekly menus</strong> that lay recipes across a 7-day × 4-slot grid. It opens on a server-paginated
        <strong> thumbnail gallery</strong> with search, category &amp; difficulty filters and name / category sorting; the
        <strong> menu planner</strong> is reached through internal navigation. Recipes are created and edited through URL-aware
        popups, an ingredient may link to an <strong>Inventory item</strong> through the shared entity selector, and a menu
        slot references recipes the same way. Built on Armali: warm bone, sea-aqua, glass over a drifting aurora, glow not shadow.
      </p>

      <div className="seg-notes__grid">
        <div>
          <h3>What's here</h3>
          <ul>
            <li><strong>Recipe gallery</strong> — two takes: image-forward tiles and detail cards with ingredient / step counts.</li>
            <li><strong>Recipe editor</strong> — image, name, category, difficulty, servings &amp; times, ordered ingredients &amp; steps, notes, visibility.</li>
            <li><strong>Ingredient → Inventory</strong> — the shared selector links a line to a pantry item (cross-module reference).</li>
            <li><strong>Menu planner &amp; editor</strong> — a Monday-anchored weekly grid; slots reference recipes through the shared selector.</li>
          </ul>
        </div>
        <div>
          <h3>Decisions in play</h3>
          <ul>
            <li><strong>{cats}</strong> seeded categories (Breakfast → Other); difficulty is a fixed <strong>Easy / Medium / Hard</strong> enum.</li>
            <li>Ingredients carry free-text name + optional quantity; the <strong>item link is optional</strong> and clears to free text if the item is gone.</li>
            <li>Menus pin to one ISO week; a public menu references only public recipes. The launcher never requests attention.</li>
            <li>Toggle <strong>Tweaks</strong> to shift accent emphasis, aurora intensity &amp; glass frost. Drag artboards to reorder.</li>
          </ul>
        </div>
      </div>
    </div>
  );
}

function App() {
  const [t, setTweak] = useTweaks(TWEAK_DEFAULTS);
  useLucide();

  React.useEffect(() => {
    const root = document.documentElement;
    const accent = ACCENT_SWATCH[String(t.accent).toUpperCase()] || "aqua";
    Object.entries(ACCENTS[accent]).forEach(([k, v]) => root.style.setProperty(k, v));
    const op = Math.max(0, Math.min(0.9, t.aurora / 100));
    root.style.setProperty("--seg-aurora-op", op.toFixed(3));
    root.style.setProperty("--seg-aurora-op2", (op * 0.55).toFixed(3));
    root.style.setProperty("--blur-glass", `blur(${t.glass}px) saturate(135%)`);
    root.style.setProperty("--blur-strong", `blur(${Math.round(t.glass * 1.5)}px) saturate(140%)`);
  }, [t.accent, t.aurora, t.glass]);

  return (
    <React.Fragment>
      <DesignCanvas>
        <DCSection id="notes" title="Design notes" subtitle="The module & how the cookbook reads">
          <DCArtboard id="notes" label="Read me" width={1200} height={700}><DesignNotes /></DCArtboard>
        </DCSection>

        <DCSection id="gallery" title="Recipe gallery" subtitle="Module entry · search · category & difficulty filters · name/category sort · click a card to edit">
          <DCArtboard id="g-tile" label="A · Image tiles" width={1340} height={900}><Frame><window.RecipesGalleryTile /></Frame></DCArtboard>
          <DCArtboard id="g-detail" label="B · Detail cards — counts & difficulty" width={1340} height={900}><Frame><window.RecipesGalleryDetail /></Frame></DCArtboard>
        </DCSection>

        <DCSection id="editor" title="Recipe editor" subtitle="URL-aware popup · image, category, difficulty, times, ordered ingredients & steps, notes, visibility">
          <DCArtboard id="edit" label="A · Edit recipe" width={1340} height={900}><Frame><window.RecipesEditor /></Frame></DCArtboard>
          <DCArtboard id="new" label="B · New recipe — creation defaults" width={1340} height={900}><Frame><window.RecipesNew /></Frame></DCArtboard>
        </DCSection>

        <DCSection id="ingredient-link" title="Ingredient → Inventory item" subtitle="The shared entity selector · links an ingredient line to a pantry item · floats over the dimmed editor">
          <DCArtboard id="ing-pick" label="Link a pantry item" width={1340} height={900}><Frame><window.RecipesIngredientPick /></Frame></DCArtboard>
        </DCSection>

        <DCSection id="planner" title="Menu planner" subtitle="Internal navigation · a 7-day × 4-slot grid · Monday-anchored week navigation">
          <DCArtboard id="plan" label="Weekly grid" width={1340} height={900}><Frame><window.RecipesPlanner /></Frame></DCArtboard>
        </DCSection>

        <DCSection id="menu-editor" title="Menu editor" subtitle="URL-aware popup · week, optional name, visibility, and the editable slot grid">
          <DCArtboard id="menu-edit" label="A · Edit weekly menu" width={1340} height={900}><Frame><window.RecipesMenuEditor /></Frame></DCArtboard>
          <DCArtboard id="slot-pick" label="B · Slot → recipe selector" width={1340} height={900}><Frame><window.RecipesSlotPick /></Frame></DCArtboard>
        </DCSection>
      </DesignCanvas>

      <TweaksPanel>
        <TweakSection label="Accent emphasis" />
        <TweakColor label="Primary" value={t.accent}
          options={["#16A6A6", "#DBA63E", "#3A7CA5"]}
          onChange={(v) => setTweak("accent", v)} />
        <TweakSection label="Atmosphere" />
        <TweakSlider label="Aurora intensity" value={t.aurora} min={0} max={75} unit="%"
          onChange={(v) => setTweak("aurora", v)} />
        <TweakSlider label="Glass frost" value={t.glass} min={2} max={28} unit="px"
          onChange={(v) => setTweak("glass", v)} />
      </TweaksPanel>
    </React.Fragment>
  );
}

ReactDOM.createRoot(document.getElementById("root")).render(<App />);
})();
