/* global React */
// Mood module — shared data, derived-emotion matrix, aggregation helpers,
// and small presentational primitives. Exposed on window for the other
// babel scripts (each <script type="text/babel"> has its own scope).
(() => {

// ── Fixed criteria enums (exact spelling per the spec) ─────────
const ENERGY    = ["Low", "Medium", "High"];
const ALIGNMENT = ["Negative", "Medium", "Positive"];
const DIRECTION = ["Harmony", "Defensive", "Offensive", "Stability"];
const SOURCE    = ["Internal", "External"];

// Tone keys map each enum value to a design-system color family.
const TONE = {
  energy:    { Low: "azure", Medium: "aqua", High: "gold" },
  alignment: { Negative: "rose", Medium: "gold", Positive: "sea" },
  direction: { Harmony: "aqua", Defensive: "azure", Offensive: "rose", Stability: "gold" },
  source:    { Internal: "aqua", External: "azure" },
};
// Resolve a tone key to a [soft-bg, ink] CSS var pair.
const TONE_VARS = {
  aqua:  ["var(--aqua-100)",       "var(--aqua-700)"],
  gold:  ["var(--gold-100)",       "var(--gold-600)"],
  azure: ["var(--azure-100)",      "var(--azure-600)"],
  sea:   ["var(--sea-100)",        "var(--sea-600)"],
  rose:  ["var(--terracotta-100)", "var(--terracotta-600)"],
  neutral: ["var(--surface-sunken)", "var(--text-secondary)"],
};

// ── Derived-emotion matrix ─────────────────────────────────────
// Mood does not persist the concrete emotion. It is derived in read models
// from a static module-owned matrix covering all 3×3×4×2 = 72 combinations.
// (The product CSV lives outside the repo; this is a synthesized stand-in
//  that exposes one and only one emotion per combination.)
// Keyed Direction → Alignment → Energy → [Internal, External].
const EMOTION_MATRIX = {
  Harmony: {
    Negative: { Low: ["Indecisive", "Lonely"], Medium: ["Awkward", "Wary"], High: ["FOMO", "Disappointed"] },
    Medium:   { Low: ["Self-Care", "Nostalgic"], Medium: ["Thoughtful", "Cheeky"], High: ["Healing", "Excited"] },
    Positive: { Low: ["Safe", "Caring"], Medium: ["Optimistic", "Inspired"], High: ["Happy", "Playful"] },
  },
  Defensive: {
    Negative: { Low: ["Sad", "Depleted"], Medium: ["Insecure", "Confused"], High: ["Tense", "Scared"] },
    Medium:   { Low: ["Withdrawn", "Protective"], Medium: ["Productive", "Curious"], High: ["Analytic", "Startled"] },
    Positive: { Low: ["Indoor", "Satisfied"], Medium: ["Daydreaming", "Relieved"], High: ["Vibing", "Amazed"] },
  },
  Offensive: {
    Negative: { Low: ["Bitter", "Apathetic"], Medium: ["Ashamed", "Uncomfortable"], High: ["Frustrated", "Angry"] },
    Medium:   { Low: ["Indifferent", "Distrustful"], Medium: ["Conflicted", "Betrayed"], High: ["Disciplined", "Crusader"] },
    Positive: { Low: ["Introspective", "Self-Assured"], Medium: ["Daring", "Bold"], High: ["Empowered", "Determined"] },
  },
  Stability: {
    Negative: { Low: ["Tired", "Burnout"], Medium: ["Doubtful", "Worried"], High: ["Unstable", "Anxious"] },
    Medium:   { Low: ["Lazy", "Relaxed"], Medium: ["Absorbed", "Surprised"], High: ["Energetic", "Focused"] },
    Positive: { Low: ["Peaceful", "Connected"], Medium: ["Serene", "Grateful"], High: ["Confident", "Proud"] },
  },
};

function deriveEmotion(energy, alignment, direction, source) {
  const cell = EMOTION_MATRIX?.[direction]?.[alignment]?.[energy];
  if (!cell) return "—";
  return cell[source === "External" ? 1 : 0];
}

// ── Deterministic sample data ──────────────────────────────────
// A tiny seeded PRNG so the mockup is stable across reloads.
function mulberry32(seed) {
  return function () {
    seed |= 0; seed = (seed + 0x6D2B79F5) | 0;
    let t = Math.imul(seed ^ (seed >>> 15), 1 | seed);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}
const pick = (rnd, arr) => arr[Math.floor(rnd() * arr.length)];

// Build a year (2026) of entries up to "today" = Wed 17 Jun 2026.
// Roughly 3–5 entries per day, with the occasional skipped day.
function buildYearEntries() {
  const rnd = mulberry32(20260617);
  const out = [];
  let id = 1;
  const start = new Date(2026, 0, 1);
  const today = new Date(2026, 5, 17);
  // Seasonal score drift makes the dashboard charts feel human.
  for (let d = new Date(start); d <= today; d.setDate(d.getDate() + 1)) {
    if (rnd() < 0.12) continue; // some days have no entries
    const n = 2 + Math.floor(rnd() * 4); // 2–5
    const month = d.getMonth();
    const lift = [0.0, 0.1, 0.3, 0.4, 0.6, 0.5][month] || 0.3; // spring lifts mood
    for (let k = 0; k < n; k++) {
      const base = 2 + rnd() * 2.4 + lift * 1.6;
      const score = Math.max(1, Math.min(5, Math.round(base)));
      const alignment = score >= 4 ? pick(rnd, ["Positive", "Positive", "Medium"])
                     : score <= 2 ? pick(rnd, ["Negative", "Negative", "Medium"])
                     : pick(rnd, ALIGNMENT);
      out.push({
        id: id++,
        date: new Date(d.getFullYear(), d.getMonth(), d.getDate()),
        score,
        energy: pick(rnd, ENERGY),
        alignment,
        direction: pick(rnd, DIRECTION),
        source: pick(rnd, SOURCE),
        order: k,
      });
    }
  }
  return out;
}
const YEAR_ENTRIES = buildYearEntries();

// Curated entries for the *current* week (Mon 15 – Sun 21 Jun 2026) so the
// Log view reads like a real, partially-filled week (today = Wed 17).
const WEEK_ENTRIES = [
  { id: 901, date: new Date(2026,5,15), score: 4, energy:"Medium", alignment:"Positive", direction:"Harmony",   source:"Internal", order:0, notes:"Slow start but a good long walk by the harbour cleared my head." },
  { id: 902, date: new Date(2026,5,15), score: 3, energy:"Low",    alignment:"Medium",   direction:"Stability", source:"External", order:1, notes:"" },
  { id: 903, date: new Date(2026,5,15), score: 2, energy:"High",   alignment:"Negative", direction:"Offensive", source:"External", order:2, notes:"Tense call in the afternoon — left me wound up for a while." },
  { id: 911, date: new Date(2026,5,16), score: 5, energy:"High",   alignment:"Positive", direction:"Harmony",   source:"External", order:0, notes:"Dinner with Diego and Nora. Felt completely at home." },
  { id: 912, date: new Date(2026,5,16), score: 4, energy:"Medium", alignment:"Positive", direction:"Offensive", source:"Internal", order:1, notes:"" },
  { id: 913, date: new Date(2026,5,16), score: 3, energy:"Medium", alignment:"Medium",   direction:"Defensive", source:"Internal", order:2, notes:"" },
  { id: 914, date: new Date(2026,5,16), score: 4, energy:"Low",    alignment:"Positive", direction:"Stability", source:"Internal", order:3, notes:"Quiet evening, read for an hour. Settled." },
  { id: 921, date: new Date(2026,5,17), score: 3, energy:"Medium", alignment:"Medium",   direction:"Stability", source:"Internal", order:0, notes:"" },
  { id: 922, date: new Date(2026,5,17), score: 4, energy:"High",   alignment:"Positive", direction:"Offensive", source:"Internal", order:1, notes:"Good momentum on the Segaris plan this morning." },
];

// ── Date helpers (Europe/Madrid civil dates, Mon–Sun weeks) ────
const DOW = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];
const MON = ["Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec"];
const sameDay = (a, b) => a.getFullYear()===b.getFullYear() && a.getMonth()===b.getMonth() && a.getDate()===b.getDate();
// Monday index 0 … Sunday 6
const dowIndex = (d) => (d.getDay() + 6) % 7;
function weekDays(monday) {
  return Array.from({ length: 7 }, (_, i) => {
    const x = new Date(monday); x.setDate(monday.getDate() + i); return x;
  });
}
const avg = (nums) => nums.length ? nums.reduce((a, b) => a + b, 0) / nums.length : null;

// Score → tone color (1 terracotta … 5 sea green)
const SCORE_TONE = { 1: "rose", 2: "rose", 3: "gold", 4: "sea", 5: "sea" };
const scoreColor = (s) => TONE_VARS[SCORE_TONE[Math.round(s)] || "neutral"][1];

// ── Small presentational primitives ───────────────────────────
const Icon = window.SegIcon;

function Pill({ label, tone = "neutral", icon }) {
  const [bg, fg] = TONE_VARS[tone] || TONE_VARS.neutral;
  return (
    <span className="mood-pill" style={{ background: bg, color: fg }}>
      {icon && <Icon n={icon} size={12} />}{label}
    </span>
  );
}

function CriteriaPills({ e }) {
  return (
    <div className="mood-pills">
      <Pill label={e.energy}    tone={TONE.energy[e.energy]} />
      <Pill label={e.alignment} tone={TONE.alignment[e.alignment]} />
      <Pill label={e.direction} tone={TONE.direction[e.direction]} />
      <Pill label={e.source}    tone={TONE.source[e.source]} />
    </div>
  );
}

function ScoreChip({ score, size = 34 }) {
  const tone = SCORE_TONE[Math.round(score)] || "neutral";
  const [bg, fg] = TONE_VARS[tone];
  return (
    <span className="mood-scorechip" style={{ width: size, height: size, background: bg, color: fg }}>
      {Number.isInteger(score) ? score : score.toFixed(1)}
    </span>
  );
}

// Weekly average-score chart — 7 bars, missing days shown as dashed gaps.
function WeekScoreChart({ days }) {
  return (
    <div className="mood-weekchart" role="img" aria-label="Average score per day this week">
      {days.map((d, i) => {
        const a = d.avg;
        const h = a == null ? 0 : (a - 0.5) / 4.5 * 100;
        return (
          <div key={i} className="mood-weekchart__col">
            <div className="mood-weekchart__track">
              {a == null
                ? <span className="mood-weekchart__empty" title="No entries" />
                : <span className="mood-weekchart__bar" style={{ height: h + "%", background: scoreColor(a) }} />}
            </div>
            <span className="mood-weekchart__val">{a == null ? "·" : a.toFixed(1)}</span>
            <span className="mood-weekchart__lbl">{DOW[i]}</span>
          </div>
        );
      })}
    </div>
  );
}

window.MoodData = {
  ENERGY, ALIGNMENT, DIRECTION, SOURCE,
  TONE, TONE_VARS, deriveEmotion,
  YEAR_ENTRIES, WEEK_ENTRIES,
  DOW, MON, sameDay, dowIndex, weekDays, avg,
  SCORE_TONE, scoreColor,
  Pill, CriteriaPills, ScoreChip, WeekScoreChart,
};
})();
