import { DistrictsPanel } from "../components/DistrictsPanel";
import { EmptyState } from "../components/EmptyState";
import { QuestRail } from "../components/QuestRail";
import { WorldCanvas } from "../components/WorldCanvas";
import { useEraData } from "../state/EraDataContext";

export function HomeScreen() {
  const { hasActiveEra, era, world, template, areas } = useEraData();

  if (!hasActiveEra) {
    return (
      <div style={{ position: "absolute", inset: 0, background: "linear-gradient(180deg,var(--bone-100),var(--bone-200))" }}>
        <EmptyState title="No active era yet">
          Belfalas grows an era at a time. Create one — name it, choose your areas of focus and a world — and your
          districts will start to take shape here.
        </EmptyState>
      </div>
    );
  }

  return (
    <div style={{ position: "absolute", inset: 0, display: "flex" }}>
      {/* World stage — the live PixiJS isometric scene, with floating overlays on top. */}
      <section className="armali-aurora" style={{ flex: 1, minWidth: 0, position: "relative", overflow: "hidden" }}>
        {world && template ? (
          <WorldCanvas template={template} world={world} areas={areas} />
        ) : null}

        <DistrictsPanel />

        <div
          style={{
            position: "absolute",
            left: 24,
            bottom: 22,
            display: "flex",
            alignItems: "center",
            gap: 10,
            padding: "9px 15px",
            borderRadius: 999,
            background: "var(--surface-overlay)",
            backdropFilter: "var(--blur-chip)",
            WebkitBackdropFilter: "var(--blur-chip)",
            border: "1px solid var(--border-glass)",
            boxShadow: "var(--glow-soft)",
            pointerEvents: "none",
          }}
        >
          <span
            style={{
              width: 9,
              height: 9,
              borderRadius: "50%",
              background: "var(--sea-500)",
              animation: "belf-ring-pulse 2.6s var(--ease-out) infinite",
            }}
          />
          <span style={{ fontSize: 12.5, fontWeight: 600, color: "var(--text-secondary)" }}>
            {era?.name} — drag to explore, scroll to zoom
          </span>
        </div>
      </section>

      <QuestRail />
    </div>
  );
}
