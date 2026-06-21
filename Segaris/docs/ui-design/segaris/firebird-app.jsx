/* global React, ReactDOM */
(() => {
const { DesignCanvas, DCSection, DCArtboard } = window;
const { useTweaks, TweaksPanel, TweakSection, TweakColor, TweakSlider } = window;
const Icon = window.SegIcon;
const F = window.SegFire;

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
  const soon = window.SEG_PEOPLE.filter((p) => p.birthday && F.birthdaySoon(p.birthday)).length;
  return (
    <div className="seg-notes">
      <div>
        <div className="armali-eyebrow" style={{ color: "var(--accent)" }}>Segaris · Firebird module</div>
        <h2>A quiet register of the people you know</h2>
      </div>
      <p style={{ maxWidth: "94ch" }}>
        Firebird records the people your household knows — the <strong>identities</strong> they use across services and a
        chronological <strong>log of interactions</strong> with them — and gently reminds you of <strong>upcoming birthdays</strong>.
        It opens on a server-paginated <strong>avatar gallery</strong> with search, category &amp; status filters and name / birthday
        sorting. People are created and edited through a URL-aware popup, and each person's usernames and interactions are managed
        in their own dedicated popups reached from the editor. Everything is built on the Armali system: warm bone, sea-aqua, glass
        over a drifting aurora, colored glow instead of shadow.
      </p>

      <div className="seg-notes__grid">
        <div>
          <h3>What's here</h3>
          <ul>
            <li><strong>Avatar gallery</strong> — two card takes: a portrait gallery and a detail card with handles &amp; last contact.</li>
            <li><strong>Person editor</strong> — name, category, fixed status, day/month birthday, notes, avatar, visibility.</li>
            <li><strong>Usernames popup</strong> — platform + handle rows; the same platform may repeat.</li>
            <li><strong>Interactions popup</strong> — a dated log, most recent first, with an inline composer.</li>
          </ul>
        </div>
        <div>
          <h3>Decisions in play</h3>
          <ul>
            <li>Categories use your <strong>Cat A – Cat F</strong> convention; statuses are the fixed enum (Unknown, Active, Unavailable, Blocked).</li>
            <li>Birthdays store <strong>day &amp; month only</strong> — sorted Jan→Dec; a 29 Feb birthday is observed on 1 Mar off-years.</li>
            <li><strong>{soon}</strong> {soon === 1 ? "person has a birthday" : "people have birthdays"} within 7 days — the launcher attention signal.</li>
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
        <DCSection id="notes" title="Design notes" subtitle="The module & how the register reads">
          <DCArtboard id="notes" label="Read me" width={1200} height={680}><DesignNotes /></DCArtboard>
        </DCSection>

        <DCSection id="gallery" title="People gallery" subtitle="Module entry · search · filters · name/birthday sort · click a card to edit">
          <DCArtboard id="g-portrait" label="A · Portrait gallery" width={1340} height={900}><Frame><window.FirebirdGalleryPortrait /></Frame></DCArtboard>
          <DCArtboard id="g-detail" label="B · Detail cards — handles & last contact" width={1340} height={900}><Frame><window.FirebirdGalleryDetail /></Frame></DCArtboard>
        </DCSection>

        <DCSection id="editor" title="Person editor" subtitle="URL-aware popup · name, category, status, birthday, notes, avatar, visibility">
          <DCArtboard id="edit" label="A · Edit person" width={1340} height={900}><Frame><window.FirebirdEditor /></Frame></DCArtboard>
          <DCArtboard id="new" label="B · New person — creation defaults" width={1340} height={900}><Frame><window.FirebirdNew /></Frame></DCArtboard>
        </DCSection>

        <DCSection id="usernames" title="Usernames" subtitle="Dedicated popup from the editor · platform + handle · repeats allowed">
          <DCArtboard id="usernames" label="Manage usernames" width={1340} height={900}><Frame><window.FirebirdUsernames /></Frame></DCArtboard>
        </DCSection>

        <DCSection id="interactions" title="Interactions" subtitle="Dedicated popup from the editor · a dated log, most recent first">
          <DCArtboard id="interactions" label="Interaction log" width={1340} height={900}><Frame><window.FirebirdInteractions /></Frame></DCArtboard>
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
