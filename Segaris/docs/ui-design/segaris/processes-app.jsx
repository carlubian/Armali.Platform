/* global React, ReactDOM */
(() => {
const { DesignCanvas, DCSection, DCArtboard } = window;
const { useTweaks, TweaksPanel, TweakSection, TweakColor, TweakSlider } = window;
const Icon = window.SegIcon;

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

function LegendOrb({ cls, content }) {
  return <span className={"proc-orb " + cls} style={{ "--orb-size": "34px" }}>{content}</span>;
}
function DesignNotes() {
  return (
    <div className="seg-notes">
      <div>
        <div className="armali-eyebrow" style={{ color: "var(--accent)" }}>Segaris · Processes module</div>
        <h2>Procedures as a path of connected steps</h2>
      </div>
      <p style={{ maxWidth: "94ch" }}>
        Processes tracks sequential bureaucratic, legal and administrative procedures — renewing a passport, applying for
        a mortgage, filing a tax return — that are completed <strong>strictly in order</strong> toward a due date. The module
        is a server-paginated <strong>table</strong> with a URL-aware popup editor and a dedicated <strong>step-timeline</strong> popup,
        where the procedure is drawn as a path of colored orbs. Status is derived from the steps; the next pending step is the
        <strong> frontier</strong> — the only one you can complete or skip. Everything is built on the Armali system: warm bone, sea-aqua,
        glass over a drifting aurora, colored glow instead of shadow.
      </p>

      <div>
        <h3>The orb language</h3>
        <div className="proc-legend">
          <div className="proc-legend__item"><LegendOrb cls="is-done" content={<Icon n="check" size={16} />} /><span className="proc-legend__txt"><b>Completed</b><span>resolved · aqua, filled</span></span></div>
          <div className="proc-legend__item"><LegendOrb cls="is-skipped" content={<Icon n="minus" size={15} />} /><span className="proc-legend__txt"><b>Skipped</b><span>optional steps only · sand</span></span></div>
          <div className="proc-legend__item"><LegendOrb cls="is-frontier" content={"4"} /><span className="proc-legend__txt"><b>Frontier</b><span>next actionable · gold, breathing</span></span></div>
          <div className="proc-legend__item"><LegendOrb cls="is-pending" content={"5"} /><span className="proc-legend__txt"><b>Pending</b><span>not yet reachable · hollow</span></span></div>
        </div>
      </div>

      <div className="seg-notes__grid">
        <div>
          <h3>What's here</h3>
          <ul>
            <li><strong>Table</strong> — search, category &amp; status filters, sortable, with an orb progress column.</li>
            <li><strong>Step timeline</strong> — two takes: a spread-out <em>path track</em> and a <em>spine + detail list</em>.</li>
            <li><strong>Restructure</strong> — add, rename, re-date &amp; reorder steps with the contiguity invariant protected.</li>
            <li><strong>Create / edit popup</strong> — name, category, due date, notes, visibility, attachments.</li>
          </ul>
        </div>
        <div>
          <h3>Try it</h3>
          <ul>
            <li>Click any table row to open its step timeline.</li>
            <li>In the timeline, use <strong>Complete</strong>, <strong>Skip</strong> and <strong>Undo</strong> — the path and status update live.</li>
            <li>Toggle <strong>Tweaks</strong> to shift accent emphasis, aurora intensity &amp; glass frost.</li>
            <li>Drag artboards to reorder; open any one fullscreen to focus.</li>
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
        <DCSection id="notes" title="Design notes" subtitle="The system & how the step path reads">
          <DCArtboard id="notes" label="Read me" width={1200} height={720}><DesignNotes /></DCArtboard>
        </DCSection>

        <DCSection id="table" title="Processes table" subtitle="Primary view · search · filters · sort · orb progress · click a row for its timeline">
          <DCArtboard id="table" label="Processes table" width={1340} height={900}><Frame><window.ProcessesTable /></Frame></DCArtboard>
        </DCSection>

        <DCSection id="timeline" title="Step timeline" subtitle="The visual path — two compositions of the same procedure">
          <DCArtboard id="tl-track" label="A · Path track + frontier bar" width={1340} height={900}><Frame><window.ProcessesTimelineA /></Frame></DCArtboard>
          <DCArtboard id="tl-list" label="B · Numbered spine + detail list" width={1340} height={900}><Frame><window.ProcessesTimelineB /></Frame></DCArtboard>
        </DCSection>

        <DCSection id="restructure" title="Restructure steps" subtitle="Add · rename · reorder · re-date — resolved prefix stays locked">
          <DCArtboard id="restructure" label="Restructure step list" width={1340} height={900}><Frame><window.ProcessesRestructure /></Frame></DCArtboard>
        </DCSection>

        <DCSection id="edit" title="Create / edit process" subtitle="URL-aware popup · name, category, due date, notes, visibility, attachments">
          <DCArtboard id="edit" label="Edit process popup" width={1340} height={900}><Frame><window.ProcessesEdit /></Frame></DCArtboard>
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
