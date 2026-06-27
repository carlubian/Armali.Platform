import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { Activity, Compass, ListChecks, Settings, Sparkles } from "lucide-react";
import "./styles.css";

const apiGroups = [
  { icon: ListChecks, label: "Quests", routes: ["GET /api/quests/daily", "GET /api/quests/weekly"] },
  { icon: Activity, label: "Progression", routes: ["GET /api/progression/summary", "GET /api/progression/areas/{areaId}"] },
  { icon: Compass, label: "World", routes: ["GET /api/world", "GET /api/world/templates"] },
  { icon: Settings, label: "Admin", routes: ["POST /api/eras", "PUT /api/admin/eras/{eraId}/weekly-sets/{weekIndex}"] },
];

function App() {
  return (
    <main className="app-shell">
      <aside className="sidebar" aria-label="Primary navigation">
        <div className="brand-mark">
          <Sparkles aria-hidden="true" size={24} />
          <span>Belfalas</span>
        </div>
        <nav>
          <a href="#quests">Quests</a>
          <a href="#progression">Progression</a>
          <a href="#world">World</a>
          <a href="#admin">Admin</a>
        </nav>
      </aside>

      <section className="workspace" aria-labelledby="page-title">
        <header className="topbar">
          <div>
            <p className="eyebrow">Wave 0</p>
            <h1 id="page-title">Scaffolding & contracts</h1>
          </div>
          <span className="status-pill">API stubs: 501</span>
        </header>

        <div className="quest-board">
          <section className="today-panel" aria-labelledby="today-title">
            <h2 id="today-title">Today</h2>
            <div className="placeholder-list">
              <label>
                <input type="checkbox" disabled />
                Daily habits will appear here.
              </label>
              <label>
                <input type="checkbox" disabled />
                Weekly goals will join the same light list.
              </label>
            </div>
          </section>

          <section className="world-panel" aria-labelledby="world-title">
            <div>
              <h2 id="world-title">World preview</h2>
              <p>PixiJS canvas mount point reserved for Wave 6.</p>
            </div>
            <div className="isometric-grid" aria-hidden="true">
              <span />
              <span />
              <span />
              <span />
              <span />
              <span />
            </div>
          </section>
        </div>

        <section className="contracts" aria-label="Frozen API groups">
          {apiGroups.map(({ icon: Icon, label, routes }) => (
            <article className="contract-card" key={label}>
              <header>
                <Icon aria-hidden="true" size={20} />
                <h2>{label}</h2>
              </header>
              <ul>
                {routes.map((route) => (
                  <li key={route}>{route}</li>
                ))}
              </ul>
            </article>
          ))}
        </section>
      </section>
    </main>
  );
}

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
