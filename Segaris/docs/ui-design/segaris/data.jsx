/* global React */
// Segaris shared data + icon helper. Exposed on window for the other
// babel scripts (each <script type="text/babel"> has its own scope).
(() => {

// Lucide icon wrapper. Renders an <i data-lucide> that lucide.createIcons()
// upgrades to an <svg>. Keyed by name+size so it re-mounts cleanly.
function SegIcon({ n, size = 20, strokeWidth = 2, className, style }) {
  return React.createElement("i", {
    key: n + ":" + size,
    "data-lucide": n,
    className,
    style: { width: size, height: size, display: "inline-flex", strokeWidth, ...style },
  });
}

// The full Segaris module catalog (12 business modules + Analytics).
// `attn` flags the lightweight launcher attention indicator (no counts).
const SEG_MODULES = [
  { key: "capex",       name: "Capex",       icon: "receipt-text",   tone: "aqua",  desc: "Atomic income and expense records." },
  { key: "opex",        name: "Opex",        icon: "repeat",         tone: "aqua",  desc: "Recurring income and expenses through contracts." },
  { key: "inventory",   name: "Inventory",   icon: "package",        tone: "gold",  desc: "Consumables, stock, purchasing and replenishment.", attn: true },
  { key: "travel",      name: "Travel",      icon: "plane",          tone: "azure", desc: "Trips, plans, bookings, documents and costs.", attn: true },
  { key: "assets",      name: "Assets",      icon: "armchair",       tone: "aqua",  desc: "Durable objects where stock is not the model." },
  { key: "maintenance", name: "Maintenance", icon: "wrench",         tone: "gold",  desc: "Repairs and upkeep for physical elements." },
  { key: "projects",    name: "Projects",    icon: "folder-kanban",  tone: "azure", desc: "Project hierarchies, work products and tasks." },
  { key: "processes",   name: "Processes",   icon: "list-checks",    tone: "sea",   desc: "Ordered multi-step activities with due dates." },
  { key: "archive",     name: "Archive",     icon: "archive",        tone: "aqua",  desc: "Long-term document records and reference." },
  { key: "firebird",    name: "Firebird",    icon: "contact-round",  tone: "rose",  desc: "People, contacts and interactions." },
  { key: "clothes",     name: "Clothes",     icon: "shirt",          tone: "sea",   desc: "Wardrobe items, accessories and care." },
  { key: "mood",        name: "Mood",        icon: "smile-plus",     tone: "gold",  desc: "Private or shared mood and emotion trends." },
  { key: "analytics",   name: "Analytics",   icon: "chart-spline",   tone: "azure", desc: "Financial trends across Capex and Opex." },
];

// Household members used across the admin & profile screens.
const SEG_USERS = [
  { name: "Marina Velasco", username: "marina",  role: "Admin", status: "active",   last: "2 minutes ago",  entities: 184 },
  { name: "Diego Salas",    username: "diego",   role: "User",  status: "active",   last: "1 hour ago",     entities: 96 },
  { name: "Nora Quintana",  username: "nora",    role: "User",  status: "active",   last: "Yesterday",      entities: 52 },
  { name: "Hugo Belmonte",  username: "hugo",    role: "User",  status: "inactive", last: "12 Mar 2026",    entities: 7 },
];

window.SegIcon = SegIcon;
window.SEG_MODULES = SEG_MODULES;
window.SEG_USERS = SEG_USERS;
})();
