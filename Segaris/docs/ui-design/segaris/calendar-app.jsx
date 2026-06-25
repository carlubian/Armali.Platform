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
  return <div style={{ width: "100%", height: "100%", background: "var(--surface-app)", position: "relative", isolation: "isolate" }}>{children}</div>;
}

function DesignNotes() {
  const C = window.CalData;
  const total = C.ENTRIES.length;
  const notes = C.ENTRIES.filter((e) => e.isNote).length;
  return (
    <div className="seg-notes">
      <div>
        <div className="armali-eyebrow" style={{ color: "var(--accent)" }}>Segaris · Calendar module</div>
        <h2>One shared view of everything date-bound</h2>
      </div>
      <p style={{ maxWidth: "94ch" }}>
        Calendar is a <strong>cross-domain read view</strong>. It does not own most of what it shows — it
        <strong> projects</strong> date-bound entries published by other modules through narrow contracts: Firebird
        <strong> birthdays</strong>, Travel <strong>trips</strong> (continuous all-day spans), and the <strong>Other</strong> family,
        which gathers Inventory expected receipts, Assets end-of-life dates, Maintenance due dates and Process step due
        dates. Each source stays authoritative for its own records, visibility and the action a calendar entry opens.
        Calendar's <em>only</em> persisted entity is the <strong>manual daily note</strong> — a private or shared free-form
        note pinned to one civil date. The surface is a <strong>Monday-first month grid</strong> (Europe/Madrid) with a
        day-detail surface, source &amp; family filters, today / selected highlighting and a URL-aware note editor. Built on
        Armali: warm bone, sea-aqua, glass over a drifting aurora, glow not shadow.
      </p>

      <div className="seg-notes__grid">
        <div>
          <h3>What's here</h3>
          <ul>
            <li><strong>Month view · Rich</strong> — continuous travel bars + family chips, with a persistent day-detail rail.</li>
            <li><strong>Month view · Compact</strong> — the priority-fallback dot indicators, with the day detail in a popover.</li>
            <li><strong>Day detail</strong> — every entry for the day, grouped by family, each with its source &amp; open action.</li>
            <li><strong>Note editor</strong> — create / edit a daily note: date, optional title, body, Private/Public visibility.</li>
          </ul>
        </div>
        <div>
          <h3>Decisions in play</h3>
          <ul>
            <li>Four visual families — <strong>Travel · Birthday · Note · Other</strong>; up to four indicators per day, else a priority fallback.</li>
            <li>{total} seeded projections ({notes} manual notes); trips render as <strong>one continuous span</strong>, never duplicated.</li>
            <li>Notes default to <strong>Private</strong>; only the creator changes visibility. No recurrence, alarms, ranges or attachments.</li>
            <li>Filters by <strong>source module</strong> &amp; <strong>family</strong> affect indicators and detail together — never authorization.</li>
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
        <DCSection id="notes" title="Design notes" subtitle="The module & how the calendar reads">
          <DCArtboard id="notes" label="Read me" width={1200} height={680}><DesignNotes /></DCArtboard>
        </DCSection>

        <DCSection id="month" title="Month view" subtitle="Module entry · Monday-first month grid · today & selected highlight · source/family filters · day detail">
          <DCArtboard id="board" label="A · Rich — travel bars, family chips & day-detail rail" width={1400} height={920}><Frame><window.CalMonthBoard /></Frame></DCArtboard>
          <DCArtboard id="compact" label="B · Compact — family-dot indicators & floating day popover" width={1400} height={920}><Frame><window.CalMonthCompact /></Frame></DCArtboard>
        </DCSection>

        <DCSection id="note-editor" title="Daily-note editor" subtitle="URL-aware popup · Calendar's only owned entity · date, optional title, body, Private/Public visibility">
          <DCArtboard id="note-new" label="A · New note — creation defaults (today · Private)" width={1400} height={920}><Frame><window.CalMonthBoard /><window.CalNoteEditor mode="new" /></Frame></DCArtboard>
          <DCArtboard id="note-edit" label="B · Edit note — public, with delete" width={1400} height={920}><Frame><window.CalMonthBoard /><window.CalNoteEditor mode="edit" note={window.CalData.ENTRIES.find((e) => e.isNote && e.visibility === "Public")} /></Frame></DCArtboard>
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
