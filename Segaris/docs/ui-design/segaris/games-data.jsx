/* global React */
// Games module — the household's progress tracker for video games, board games
// and tabletop campaigns. A `Game` is an admin-managed catalogue entry; a
// `Playthrough` is the user-owned run where progress is tracked through ordered
// Sections and their Goals. Progress is always derived on demand from goals and
// never persisted. Sample data + small formatting helpers, exposed on window for
// the other babel scripts. No invented colors — everything maps onto Armali tokens
// (the ten fixed section colours are a required product enum, tuned to sit in the
// coastal palette).
(() => {

// ── Reference "today" (Europe/Madrid) ───────────────────────────
const TODAY = new Date(2026, 6, 6); // Mon 6 Jul 2026
const MONTHS_SHORT = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
const MONTHS = ["January", "February", "March", "April", "May", "June",
  "July", "August", "September", "October", "November", "December"];

// ── Catalogue: Game platform (fixed enum, not configurable) ──────
// token → label + a Lucide glyph + a gentle tone for visual rhythm.
const PLATFORMS = [
  { value: "PC",          label: "PC",           icon: "monitor",      tone: "aqua" },
  { value: "Console",     label: "Console",      icon: "gamepad-2",    tone: "azure" },
  { value: "Mobile",      label: "Mobile",       icon: "smartphone",   tone: "sea" },
  { value: "BoardGame",   label: "Board game",   icon: "dices",        tone: "gold" },
  { value: "TabletopRpg", label: "Tabletop RPG", icon: "swords",       tone: "rose" },
  { value: "Other",       label: "Other",        icon: "shapes",       tone: "neutral" },
];
const PLAT = Object.fromEntries(PLATFORMS.map((p) => [p.value, p]));

// ── Fixed enum: Playthrough status ──────────────────────────────
// Manual, descriptive. Never auto-changes and never validates against goals.
const STATUSES = [
  { value: "Planning",  label: "Planning",  tone: "neutral", icon: "map" },
  { value: "Active",    label: "Active",    tone: "aqua",    icon: "play" },
  { value: "Completed", label: "Completed", tone: "success", icon: "flag" },
];
const STATUS = Object.fromEntries(STATUSES.map((s) => [s.value, s]));

// ── Fixed enum: Section colour palette (persisted as the token) ──
// Ten distinct tokens; the frontend maps each to a presentation style
// (see .gm-sec--<token> in games.css).
const SECTION_COLORS = [
  { token: "Blue",   label: "Blue" },
  { token: "Green",  label: "Green" },
  { token: "Amber",  label: "Amber" },
  { token: "Red",    label: "Red" },
  { token: "Purple", label: "Purple" },
  { token: "Pink",   label: "Pink" },
  { token: "Teal",   label: "Teal" },
  { token: "Indigo", label: "Indigo" },
  { token: "Slate",  label: "Slate" },
  { token: "Orange", label: "Orange" },
];

// ── Game catalogue ──────────────────────────────────────────────
// Admin-managed. `SortOrder` is the array order here. `refs` is a derived
// count of playthroughs pointing at the game (privacy-neutral — never lists them).
const GAMES = [
  { id: "gm-eldenring",  name: "Elden Ring",                            platform: "Console",     order: 1,  refs: 2 },
  { id: "gm-bg3",        name: "Baldur's Gate 3",                       platform: "PC",          order: 2,  refs: 2 },
  { id: "gm-totk",       name: "The Legend of Zelda: Tears of the Kingdom", platform: "Console", order: 3,  refs: 1 },
  { id: "gm-stardew",    name: "Stardew Valley",                        platform: "PC",          order: 4,  refs: 1 },
  { id: "gm-hollow",     name: "Hollow Knight",                         platform: "PC",          order: 5,  refs: 1 },
  { id: "gm-catan",      name: "Catan",                                 platform: "BoardGame",   order: 6,  refs: 0 },
  { id: "gm-gloomhaven", name: "Gloomhaven",                            platform: "BoardGame",   order: 7,  refs: 1 },
  { id: "gm-strahd",     name: "Curse of Strahd",                       platform: "TabletopRpg", order: 8,  refs: 1 },
  { id: "gm-discoelysium", name: "Disco Elysium",                       platform: "PC",          order: 9,  refs: 1 },
  { id: "gm-slaythespire", name: "Slay the Spire",                      platform: "Mobile",      order: 10, refs: 1 },
  { id: "gm-wingspan",   name: "Wingspan",                              platform: "BoardGame",   order: 11, refs: 0 },
];
const GM = Object.fromEntries(GAMES.map((g) => [g.id, g]));

// ── Builders ────────────────────────────────────────────────────
let _sid = 0, _gid = 0;
const goal = (content, done = false) => ({ id: "gl" + (++_gid), content, done });
const section = (name, color, goals) => ({ id: "sc" + (++_sid), name, color, goals: goals || [] });

// ── Playthroughs (the primary user-facing entity) ───────────────
// Each links one game and owns ordered sections; sections own goals in creation
// order. startMonth is 1–12, startYear a full year.
const PLAYTHROUGHS = [
  {
    id: "pt-elden-first", name: "First run — no summons", gameId: "gm-eldenring",
    startMonth: 3, startYear: 2026, status: "Active", visibility: "Public",
    owner: "Diego Salas", created: "12 Mar 2026", updated: "2 days ago",
    tags: ["Melee", "SL1-ish", "Blind"],
    sections: [
      section("Main bosses", "Red", [
        goal("Margit, the Fell Omen", true),
        goal("Godrick the Grafted", true),
        goal("Rennala, Queen of the Full Moon", true),
        goal("Starscourge Radahn", true),
        goal("Morgott, the Omen King", false),
        goal("Fire Giant", false),
        goal("Maliketh, the Black Blade", false),
        goal("Malenia, Blade of Miquella", false),
      ]),
      section("Legacy dungeons", "Amber", [
        goal("Stormveil Castle", true),
        goal("Raya Lucaria Academy", true),
        goal("Leyndell, Royal Capital", false),
        goal("Crumbling Farum Azula", false),
      ]),
      section("Great Runes", "Purple", [
        goal("Godrick's Great Rune — restored", true),
        goal("Radahn's Great Rune — restored", true),
        goal("Morgott's Great Rune", false),
      ]),
      section("Exploration", "Teal", [
        goal("Reach the Altus Plateau", true),
        goal("Find the Dectus Medallion halves", true),
        goal("Unlock Ainsel River", false),
        goal("Reach Mohgwyn Palace", false),
        goal("Find all golden seeds in Limgrave", false),
      ]),
    ],
  },
  {
    id: "pt-bg3-tav", name: "Tav — Durge honour mode", gameId: "gm-bg3",
    startMonth: 1, startYear: 2026, status: "Active", visibility: "Public",
    owner: "Marina Velasco", created: "8 Jan 2026", updated: "Yesterday",
    tags: ["Honour mode", "Sorcerer", "Origin: Dark Urge"],
    sections: [
      section("Act 1 — Wilderness", "Green", [
        goal("Escape the Nautiloid", true),
        goal("Save the tieflings at the grove", true),
        goal("Clear the Goblin Camp", true),
        goal("Enter the Underdark", true),
        goal("Reach the Mountain Pass", true),
      ]),
      section("Act 2 — Shadow-Cursed Lands", "Indigo", [
        goal("Reach Last Light Inn", true),
        goal("Cleanse the Shadow Curse", true),
        goal("Defeat Ketheric Thorm", false),
      ]),
      section("Companions", "Pink", [
        goal("Recruit Shadowheart", true),
        goal("Recruit Astarion", true),
        goal("Recruit Gale", true),
        goal("Recruit Karlach", true),
        goal("Recruit Lae'zel", true),
        goal("Resolve Shadowheart's Act 2 choice", false),
      ]),
    ],
  },
  {
    id: "pt-totk", name: "100% shrines & lightroots", gameId: "gm-totk",
    startMonth: 11, startYear: 2025, status: "Active", visibility: "Public",
    owner: "Diego Salas", created: "19 Nov 2025", updated: "1 week ago",
    tags: ["Completionist"],
    sections: [
      section("Regional phenomena", "Blue", [
        goal("Wind Temple — Tulin", true),
        goal("Fire Temple — Yunobo", true),
        goal("Water Temple — Sidon", true),
        goal("Lightning Temple — Riju", false),
        goal("Spirit Temple", false),
      ]),
      section("Shrines", "Teal", [
        goal("Great Sky Island (4)", true),
        goal("Surface shrines — Hyrule Field", true),
        goal("Sky shrines — Sky Archipelago", false),
        goal("Depths — all lightroots", false),
      ]),
    ],
  },
  {
    id: "pt-stardew", name: "Perfection farm — year 3", gameId: "gm-stardew",
    startMonth: 6, startYear: 2025, status: "Active", visibility: "Public",
    owner: "Nora Quintana", created: "2 Jun 2025", updated: "4 days ago",
    tags: ["Perfection", "Ginger Island"],
    sections: [
      section("Community Center", "Amber", [
        goal("Crafts Room", true),
        goal("Pantry", true),
        goal("Fish Tank", true),
        goal("Boiler Room", true),
        goal("Bulletin Board", true),
        goal("Vault", false),
      ]),
      section("Collections", "Green", [
        goal("Ship every crop", false),
        goal("Catch every fish", false),
        goal("Cook every recipe", false),
      ]),
      section("Ginger Island", "Orange", [
        goal("Repair the resort", true),
        goal("Restore Island Field Office", false),
        goal("Complete Qi's walnut room", false),
      ]),
    ],
  },
  {
    id: "pt-hollow", name: "Steel Soul attempt", gameId: "gm-hollow",
    startMonth: 5, startYear: 2026, status: "Planning", visibility: "Private",
    owner: "Diego Salas", created: "28 May 2026", updated: "3 weeks ago",
    tags: ["Permadeath", "112%"],
    sections: [],
  },
  {
    id: "pt-gloomhaven", name: "Table campaign — The Voice", gameId: "gm-gloomhaven",
    startMonth: 9, startYear: 2025, status: "Active", visibility: "Public",
    owner: "Hugo Belmonte", created: "14 Sep 2025", updated: "5 days ago",
    tags: ["Co-op", "4 players"],
    sections: [
      section("Personal quests", "Purple", [
        goal("Brute — The Fall of Man", true),
        goal("Tinkerer — Zealot of the Blood God", false),
        goal("Spellweaver — Seeker of Xorn", false),
        goal("Scoundrel — A Study of Anatomy", false),
      ]),
      section("City events resolved", "Slate", [
        goal("Event 01 — resolved", true),
        goal("Event 14 — resolved", true),
        goal("Event 27 — resolved", true),
      ]),
      section("Scenarios cleared", "Red", [
        goal("01 · Black Barrow", true),
        goal("02 · Barrow Lair", true),
        goal("03 · Inox Encampment", true),
        goal("04 · Crypt of the Damned", true),
        goal("05 · Ruinous Rift", false),
      ]),
    ],
  },
  {
    id: "pt-strahd", name: "Barovia — the Amber Temple pact", gameId: "gm-strahd",
    startMonth: 2, startYear: 2026, status: "Active", visibility: "Public",
    owner: "Marina Velasco", created: "3 Feb 2026", updated: "6 days ago",
    tags: ["D&D 5e", "Fortnightly"],
    sections: [
      section("Chapters", "Indigo", [
        goal("Death House", true),
        goal("Village of Barovia", true),
        goal("Vallaki", true),
        goal("Krezk & the Abbey", false),
        goal("Argynvostholt", false),
        goal("Castle Ravenloft", false),
      ]),
      section("Tarokka reading", "Pink", [
        goal("Tome of Strahd located", true),
        goal("Holy Symbol of Ravenkind located", false),
        goal("Sunsword located", false),
        goal("Ally identified", true),
      ]),
    ],
  },
  {
    id: "pt-disco", name: "Sorry Cop route", gameId: "gm-discoelysium",
    startMonth: 4, startYear: 2026, status: "Completed", visibility: "Public",
    owner: "Nora Quintana", created: "10 Apr 2026", updated: "2 weeks ago",
    tags: ["Story", "Thought Cabinet"],
    sections: [
      section("Case progress", "Blue", [
        goal("Recover the body from the tree", true),
        goal("Complete the autopsy", true),
        goal("Interview the union", true),
        goal("Reach the island", true),
        goal("Confront the deserter", true),
      ]),
      section("Thought Cabinet", "Purple", [
        goal("Internalise Volumetric Shit Compressor", true),
        goal("Internalise Hobocop", true),
        goal("Internalise Actual Art Degree", true),
      ]),
    ],
  },
  {
    id: "pt-spire", name: "Ascension 20 — Watcher", gameId: "gm-slaythespire",
    startMonth: 6, startYear: 2026, status: "Active", visibility: "Public",
    owner: "Diego Salas", created: "21 Jun 2026", updated: "Today",
    tags: ["Roguelike", "Daily"],
    sections: [
      section("Ascension ladder", "Orange", [
        goal("A15 — heart clear", true),
        goal("A17 — heart clear", true),
        goal("A20 — heart clear", false),
      ]),
    ],
  },
];

// ── Derived progress (never persisted) ──────────────────────────
function sectionProgress(sec) {
  const total = sec.goals.length;
  const done = sec.goals.filter((g) => g.done).length;
  return { done, total };
}
function playthroughProgress(pt) {
  return pt.sections.reduce((acc, s) => {
    acc.total += s.goals.length;
    acc.done += s.goals.filter((g) => g.done).length;
    return acc;
  }, { done: 0, total: 0 });
}
function pct(done, total) { return total > 0 ? Math.round((done / total) * 100) : 0; }

// ── Formatting helpers ──────────────────────────────────────────
function fmtStart(month, year) { return `${MONTHS_SHORT[(month - 1) % 12]} ${year}`; }
function fmtStartLong(month, year) { return `${MONTHS[(month - 1) % 12]} ${year}`; }
function gameOf(pt) { return GM[pt.gameId] || null; }

window.SEG_GAMES = GAMES;
window.SEG_PLAYTHROUGHS = PLAYTHROUGHS;
window.SegGames = {
  TODAY, MONTHS, MONTHS_SHORT,
  PLATFORMS, PLAT, STATUSES, STATUS, SECTION_COLORS,
  GAMES, GM, PLAYTHROUGHS,
  goal, section,
  sectionProgress, playthroughProgress, pct,
  fmtStart, fmtStartLong, gameOf,
};
})();
