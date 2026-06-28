import { Sparkles } from "lucide-react";
import { DistrictsPanel } from "../components/DistrictsPanel";
import { EmptyState } from "../components/EmptyState";
import { QuestRail } from "../components/QuestRail";
import { useEraData } from "../state/EraDataContext";

export function HomeScreen() {
  const { hasActiveEra, era } = useEraData();

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
      {/* World stage — a calm placeholder until the PixiJS canvas lands in Wave 6. */}
      <section className="armali-aurora" style={{ flex: 1, minWidth: 0, position: "relative", overflow: "hidden" }}>
        <DistrictsPanel />

        <div
          style={{
            position: "absolute",
            inset: 0,
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
            justifyContent: "center",
            gap: 10,
            textAlign: "center",
            padding: 24,
            pointerEvents: "none",
          }}
        >
          <Sparkles size={30} color="var(--aqua-600)" aria-hidden />
          <span style={{ fontFamily: "var(--font-display)", fontWeight: 600, fontSize: 16, color: "var(--text-secondary)" }}>
            Your world takes shape here
          </span>
          <span style={{ fontSize: 13, color: "var(--text-muted)", maxWidth: 320 }}>
            The living isometric map arrives in a later wave. For now, watch your districts level up from the panel and
            your quests.
          </span>
        </div>

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
            {era?.name} — growing as you go
          </span>
        </div>
      </section>

      <QuestRail />
    </div>
  );
}
