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
  const G = window.SegGames;
  const games = G.GAMES.length;
  const pts = G.PLAYTHROUGHS.length;
  return (
    <div className="seg-notes">
      <div>
        <div className="armali-eyebrow" style={{ color: "var(--accent)" }}>Segaris · Games module</div>
        <h2>Progress across every game the household plays</h2>
      </div>
      <p style={{ maxWidth: "94ch" }}>
        Games records the household's progress through video games, board games and tabletop campaigns. A <strong>Game</strong>
        {" "}is an admin-managed catalogue entry (name + a fixed platform); a <strong>Playthrough</strong> is the user-owned run
        where progress is tracked. Each playthrough owns ordered <strong>Sections</strong> that group <strong>Goals</strong> —
        and section &amp; playthrough progress are always <strong>derived on demand</strong> from goals, never stored. It opens on a
        server-paginated <strong>card collection</strong> with search, platform / status / visibility filters and five sort keys;
        every card leads to a dedicated <strong>progress page</strong> (sections on the left, the selected section's goals on the
        right). Built on Armali: warm bone, sea-aqua, glass over a drifting aurora, glow not shadow.
      </p>

      <div className="seg-notes__grid">
        <div>
          <h3>What's here</h3>
          <ul>
            <li><strong>Collection</strong> — two takes: progress cards and compact rows. Cards surface game, status &amp; derived progress.</li>
            <li><strong>Progress page</strong> — section list + a section's goals; tick goals inline, add goals, and an empty-playthrough state.</li>
            <li><strong>Playthrough editor</strong> — name, game reference, start month/year, manual status, free-text tags, visibility.</li>
            <li><strong>Manage sections</strong> — rename, recolour (10-token palette), reorder &amp; remove — not on the main progress view.</li>
            <li><strong>Game catalogue</strong> — the admin Configuration surface, with replace-on-delete when a game is referenced.</li>
          </ul>
        </div>
        <div>
          <h3>Decisions in play</h3>
          <ul>
            <li><strong>{games}</strong> catalogue games · <strong>{pts}</strong> playthroughs. Status is a fixed <strong>Planning / Active / Completed</strong> enum — manual, never auto-derived.</li>
            <li>Start date is a <strong>month + year</strong> pair, not a synthetic date. No end or completion dates in this release.</li>
            <li>Goals keep <strong>creation order</strong> permanently; completing one never moves it. Deletion is immediate after confirmation.</li>
            <li>Visibility follows the Segaris baseline; the launcher never requests attention. Toggle <strong>Tweaks</strong> to shift accent, aurora &amp; glass. Drag artboards to reorder.</li>
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
        <DCSection id="notes" title="Design notes" subtitle="The module & how progress reads">
          <DCArtboard id="notes" label="Read me" width={1200} height={720}><DesignNotes /></DCArtboard>
        </DCSection>

        <DCSection id="collection" title="Playthrough collection" subtitle="Module entry · search · platform/status/visibility filters · five sort keys · click a card to open its progress">
          <DCArtboard id="c-cards" label="A · Progress cards" width={1340} height={900}><Frame><window.GamesCollectionCards /></Frame></DCArtboard>
          <DCArtboard id="c-rows" label="B · Compact rows" width={1340} height={900}><Frame><window.GamesCollectionRows /></Frame></DCArtboard>
        </DCSection>

        <DCSection id="progress" title="Progress page" subtitle="Per playthrough · section list left, the selected section's goals right · tick & add goals inline">
          <DCArtboard id="p-main" label="A · Sections & goals" width={1340} height={900}><Frame><window.GamesProgress /></Frame></DCArtboard>
          <DCArtboard id="p-empty" label="B · Playthrough with no sections" width={1340} height={900}><Frame><window.GamesProgressEmpty /></Frame></DCArtboard>
        </DCSection>

        <DCSection id="editor" title="Playthrough editor" subtitle="URL-aware popup · name, game reference, start month/year, manual status, free-text tags, visibility">
          <DCArtboard id="e-edit" label="A · Edit playthrough" width={1340} height={900}><Frame><window.GamesPlaythroughEdit /></Frame></DCArtboard>
          <DCArtboard id="e-new" label="B · New playthrough — creation defaults" width={1340} height={900}><Frame><window.GamesPlaythroughNew /></Frame></DCArtboard>
        </DCSection>

        <DCSection id="game-pick" title="Game reference selector" subtitle="The shared entity selector · links a playthrough to a catalogue game · floats over the dimmed editor">
          <DCArtboard id="g-pick" label="Choose a game" width={1340} height={900}><Frame><window.GamesGamePick /></Frame></DCArtboard>
        </DCSection>

        <DCSection id="sections" title="Manage sections" subtitle="Dedicated popup · rename, recolour (10-token palette), reorder & remove — kept off the main progress view">
          <DCArtboard id="s-manage" label="Reorder & recolour sections" width={1340} height={900}><Frame><window.GamesManageSections /></Frame></DCArtboard>
        </DCSection>

        <DCSection id="config" title="Game catalogue · Configuration" subtitle="Admin surface · required unique name, fixed platform enum, ordering & replace-only deletion when referenced">
          <DCArtboard id="cfg-table" label="A · Catalogue table" width={1340} height={900}><Frame><window.GamesConfig /></Frame></DCArtboard>
          <DCArtboard id="cfg-replace" label="B · Replace & delete a referenced game" width={1340} height={900}><Frame><window.GamesConfigReplace /></Frame></DCArtboard>
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
