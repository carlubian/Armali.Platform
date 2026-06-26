/* global React, ReactDOM */
(() => {
const { DesignCanvas, DCSection, DCArtboard } = window;
const { useTweaks, TweaksPanel, TweakSection, TweakColor, TweakSlider, TweakRadio } = window;
const Icon = window.SegIcon;
const { AnalyticsScreen } = window;

const ACCENTS = {
  azure: {
    "--accent": "var(--azure-500)", "--accent-hover": "var(--azure-600)", "--accent-press": "var(--azure-600)",
    "--accent-soft": "var(--azure-100)", "--ring-focus": "var(--azure-400)",
    "--ring": "0 0 0 3px rgba(94,153,189,0.42)",
    "--glow-aqua": "0 0 0 1px rgba(58,124,165,0.32), 0 8px 28px -8px rgba(58,124,165,0.55)",
    "--seg-brand-grad": "linear-gradient(140deg, var(--azure-400), var(--azure-600))",
  },
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
};
const ACCENT_SWATCH = { "#3A7CA5": "azure", "#16A6A6": "aqua", "#DBA63E": "gold" };

const TWEAK_DEFAULTS = /*EDITMODE-BEGIN*/{
  "accent": "#3A7CA5",
  "prevStyle": "faded",
  "aurora": 30,
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

function DesignNotes() {
  return (
    <div className="seg-notes">
      <div>
        <div className="armali-eyebrow" style={{ color: "var(--accent)" }}>Segaris · Analytics module</div>
        <h2>One calm, read-only surface for the financial year</h2>
      </div>
      <p style={{ maxWidth: "94ch" }}>
        Analytics is a <strong>cross-domain read view</strong>. It never mutates records — it consumes EUR-normalized
        income/expense projections published by the financial modules and draws the year as charts. Every value is
        normalized to <strong>EUR</strong> using the currently configured exchange rates, and every metric is
        recomputed when the module opens. The selected year <strong>N</strong> is always shown against the previous
        year <strong>N−1</strong>: current year solid, previous year as a faded overlay.
      </p>

      <div>
        <h3>The chart system</h3>
        <div className="seg-notes__grid">
          <div>
            <ul>
              <li><strong>Recharts</strong>, wrapped in module-owned components so a later library swap stays contained.</li>
              <li><strong>Year over year</strong> on every chart where it is meaningful — overlaid, not stacked.</li>
              <li><strong>Expense = azure, income = sea-green</strong>; net balance diverges sea / terracotta by sign.</li>
              <li><strong>Accessible by default</strong> — each chart carries a table-equivalent (toggle the table icon) and a text summary; tooltips are never the only way to read a value.</li>
            </ul>
          </div>
          <div>
            <ul>
              <li><strong>Tabs</strong> sit under the top bar; the <strong>year navigator</strong> (prev · next · this year) is on the right and survives across tabs.</li>
              <li><strong>Lazy</strong> — only the active tab's charts mount; switching tabs loads on demand.</li>
              <li><strong>Source rules respected</strong> — Capex completed only, Inventory excludes planning &amp; cancelled, etc.</li>
              <li>Built on Armali: warm bone, glass over a drifting aurora, colored glow instead of shadow.</li>
            </ul>
          </div>
        </div>
      </div>

      <div>
        <h3>In this mock</h3>
        <p style={{ maxWidth: "94ch" }}>
          Four of the six tabs are drawn out — <strong>Overview, Capex, Inventory</strong> and <strong>Cross-module</strong>.
          Opex and Travel reuse the exact same card and YoY patterns. Click any tab, step through years, and toggle a
          chart's <strong>table</strong> icon to read its underlying figures. Use <strong>Tweaks</strong> to shift accent
          emphasis, the previous-year style, aurora intensity and glass frost.
        </p>
      </div>
    </div>
  );
}

function Board({ tab, prevStyle }) {
  // key on tab so the fullscreen focus overlay (which reuses one mounted
  // subtree as you step between artboards) remounts to the right tab.
  return <div style={{ width: "100%", height: "100%" }}><AnalyticsScreen key={tab} initialTab={tab} prevStyle={prevStyle} /></div>;
}

function App() {
  const [t, setTweak] = useTweaks(TWEAK_DEFAULTS);
  useLucide();

  React.useEffect(() => {
    const root = document.documentElement;
    const accent = ACCENT_SWATCH[String(t.accent).toUpperCase()] || ACCENT_SWATCH[t.accent] || "azure";
    Object.entries(ACCENTS[accent]).forEach(([k, v]) => root.style.setProperty(k, v));
    const op = Math.max(0, Math.min(0.9, t.aurora / 100));
    root.style.setProperty("--seg-aurora-op", op.toFixed(3));
    root.style.setProperty("--seg-aurora-op2", (op * 0.55).toFixed(3));
    root.style.setProperty("--blur-glass", `blur(${t.glass}px) saturate(135%)`);
    root.style.setProperty("--blur-strong", `blur(${Math.round(t.glass * 1.5)}px) saturate(140%)`);
  }, [t.accent, t.aurora, t.glass]);

  const prevStyle = t.prevStyle || "faded";

  return (
    <React.Fragment>
      <DesignCanvas>
        <DCSection id="notes" title="Design notes" subtitle="What Analytics is & how the charts read">
          <DCArtboard id="notes" label="Read me" width={1200} height={760}><DesignNotes /></DCArtboard>
        </DCSection>

        <DCSection id="overview" title="Overview tab" subtitle="Year totals + monthly expense / income / net, each vs the previous year">
          <DCArtboard id="overview" label="Overview" width={1340} height={920}><Board tab="overview" prevStyle={prevStyle} /></DCArtboard>
        </DCSection>

        <DCSection id="capex" title="Capex tab" subtitle="Completed entries — category / supplier / cost-centre, income & expense">
          <DCArtboard id="capex" label="Capex" width={1340} height={920}><Board tab="capex" prevStyle={prevStyle} /></DCArtboard>
        </DCSection>

        <DCSection id="inventory" title="Inventory tab" subtitle="Item category · supplier · average order · top-5 items & suppliers with share">
          <DCArtboard id="inventory" label="Inventory" width={1340} height={920}><Board tab="inventory" prevStyle={prevStyle} /></DCArtboard>
        </DCSection>

        <DCSection id="cross" title="Cross-module tab" subtitle="Total expenses pooled across modules — by supplier, category & cost centre">
          <DCArtboard id="cross" label="Cross-module" width={1340} height={920}><Board tab="cross" prevStyle={prevStyle} /></DCArtboard>
        </DCSection>
      </DesignCanvas>

      <TweaksPanel>
        <TweakSection label="Accent emphasis" />
        <TweakColor label="Primary" value={t.accent}
          options={["#3A7CA5", "#16A6A6", "#DBA63E"]}
          onChange={(v) => setTweak("accent", v)} />
        <TweakSection label="Year-over-year" />
        <TweakRadio label="Previous year" value={t.prevStyle}
          options={[{ label: "Faded", value: "faded" }, { label: "Outline", value: "outline" }]}
          onChange={(v) => setTweak("prevStyle", v)} />
        <TweakSection label="Atmosphere" />
        <TweakSlider label="Aurora intensity" value={t.aurora} min={0} max={70} unit="%"
          onChange={(v) => setTweak("aurora", v)} />
        <TweakSlider label="Glass frost" value={t.glass} min={2} max={28} unit="px"
          onChange={(v) => setTweak("glass", v)} />
      </TweaksPanel>
    </React.Fragment>
  );
}

ReactDOM.createRoot(document.getElementById("root")).render(<App />);
})();
