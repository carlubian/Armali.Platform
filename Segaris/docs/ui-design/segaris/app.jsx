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
  "aurora": 40,
  "glass": 18
}/*EDITMODE-END*/;

// Keep lucide icons upgraded as React mounts/updates nodes anywhere
// (including the focus overlay portal and in-frame modals).
function useLucide() {
  React.useEffect(() => {
    let queued = false;
    const run = () => {
      queued = false;
      if (document.querySelector("[data-lucide]") && window.lucide) window.lucide.createIcons();
    };
    const schedule = () => { if (!queued) { queued = true; requestAnimationFrame(run); } };
    schedule();
    const mo = new MutationObserver(schedule);
    mo.observe(document.body, { childList: true, subtree: true });
    return () => mo.disconnect();
  }, []);
}

function Frame({ children }) {
  // Artboards clip; give each screen a clean white base behind the aurora.
  return <div style={{ width: "100%", height: "100%", background: "var(--surface-app)" }}>{children}</div>;
}

function DesignNotes() {
  return (
    <div className="seg-notes">
      <div>
        <div className="armali-eyebrow" style={{ color: "var(--accent)" }}>Segaris · common pages</div>
        <h2>Shared shell, built on Project Armali</h2>
      </div>
      <p style={{ maxWidth: "92ch" }}>
        Segaris is a desktop-first household ERP — a private app for one household of a few users. These are the
        <strong> common pages</strong> that every immersive module sits inside. The launcher is a calm entry point
        (not a dashboard), modules are switched by returning to it, and admins manage accounts. All visuals come
        straight from the Armali design system — warm bone, sea-aqua, glass over a drifting aurora, colored glow
        instead of shadow.
      </p>
      <div className="seg-notes__grid">
        <div>
          <h3>What's here</h3>
          <ul>
            <li><strong>Login</strong> — centered card and split-panel variants. Username + password only.</li>
            <li><strong>Launcher</strong> — all 12 modules + Analytics, two with attention indicators.</li>
            <li><strong>User management</strong> — table and card variants, with a working create/edit dialog.</li>
            <li><strong>My profile</strong>, <strong>service-unavailable</strong>, and <strong>404</strong> states.</li>
          </ul>
        </div>
        <div>
          <h3>Decisions to confirm</h3>
          <ul>
            <li>Module icons &amp; tones are a first pass — easy to re-key per module.</li>
            <li>Attention indicator is a breathing dot, no counts (per the UX doc).</li>
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
    const vars = ACCENTS[accent];
    Object.entries(vars).forEach(([k, v]) => root.style.setProperty(k, v));
    const op = Math.max(0, Math.min(0.9, t.aurora / 100));
    root.style.setProperty("--seg-aurora-op", op.toFixed(3));
    root.style.setProperty("--seg-aurora-op2", (op * 0.55).toFixed(3));
    root.style.setProperty("--blur-glass", `blur(${t.glass}px) saturate(135%)`);
    root.style.setProperty("--blur-strong", `blur(${Math.round(t.glass * 1.5)}px) saturate(140%)`);
  }, [t.accent, t.aurora, t.glass]);

  return (
    <React.Fragment>
      <DesignCanvas>
        <DCSection id="notes" title="Design notes" subtitle="The system & what to review">
          <DCArtboard id="notes" label="Read me" width={1180} height={680}><DesignNotes /></DCArtboard>
        </DCSection>

        <DCSection id="login" title="Login" subtitle="Username + password · two compositions">
          <DCArtboard id="centered" label="A · Centered card" width={1180} height={760}><Frame><window.LoginCentered /></Frame></DCArtboard>
          <DCArtboard id="split" label="B · Split panel" width={1180} height={760}><Frame><window.LoginSplit /></Frame></DCArtboard>
        </DCSection>

        <DCSection id="launcher" title="Launcher" subtitle="Entry point to every module — not a dashboard">
          <DCArtboard id="launcher" label="Module launcher" width={1280} height={1240}><Frame><window.Launcher /></Frame></DCArtboard>
        </DCSection>

        <DCSection id="users" title="User management" subtitle="Admin only · click “New user” or “Edit”">
          <DCArtboard id="table" label="A · Table" width={1280} height={820}><Frame><window.UserMgmtTable /></Frame></DCArtboard>
          <DCArtboard id="cards" label="B · Cards" width={1280} height={820}><Frame><window.UserMgmtCards /></Frame></DCArtboard>
        </DCSection>

        <DCSection id="entity-selector" title="Entity selector" subtitle="Cross-module reference · Asset → Capex · click “Browse” / “Select”">
          <DCArtboard id="es-editor" label="A · Edit popup + picker control" width={1280} height={840}><Frame><window.EntitySelectorEditor /></Frame></DCArtboard>
          <DCArtboard id="es-top" label="B · Selector — top filter bar" width={1280} height={840}><Frame><window.EntitySelectorTop /></Frame></DCArtboard>
          <DCArtboard id="es-rail" label="C · Selector — left filter rail" width={1280} height={840}><Frame><window.EntitySelectorRail /></Frame></DCArtboard>
        </DCSection>

        <DCSection id="profile" title="My profile" subtitle="Self-service: name, photo, language, password">
          <DCArtboard id="profile" label="Profile" width={1180} height={840}><Frame><window.Profile /></Frame></DCArtboard>
        </DCSection>

        <DCSection id="mood-log" title="Mood · Log" subtitle="Private weekly check-ins · Mon–Sun · click an entry or “New entry”">
          <DCArtboard id="mood-board" label="A · Week board" width={1320} height={900}><Frame><window.MoodLogBoard /></Frame></DCArtboard>
          <DCArtboard id="mood-list" label="B · Day-grouped list" width={1200} height={900}><Frame><window.MoodLogList /></Frame></DCArtboard>
        </DCSection>

        <DCSection id="mood-dash" title="Mood · Dashboard" subtitle="Calendar-period trends · switch scale & navigate periods">
          <DCArtboard id="mood-dash-score" label="A · Score emphasis" width={1320} height={940}><Frame><window.MoodDashScore /></Frame></DCArtboard>
          <DCArtboard id="mood-dash-criteria" label="B · Criteria emphasis" width={1320} height={940}><Frame><window.MoodDashCriteria /></Frame></DCArtboard>
        </DCSection>

        <DCSection id="states" title="System states" subtitle="Explicit failure & not-found experiences">
          <DCArtboard id="unavailable" label="Service unavailable" width={1080} height={680}><Frame><window.ServiceUnavailable /></Frame></DCArtboard>
          <DCArtboard id="notfound" label="404 · Not found" width={1080} height={680}><Frame><window.NotFound /></Frame></DCArtboard>
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
