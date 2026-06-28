import { CalendarRange } from "lucide-react";
import { useEraData } from "../state/EraDataContext";

export function Topbar() {
  const { era, progression, weekNumber, weekCount, hasActiveEra } = useEraData();

  const globalLevel = progression ? Math.round(progression.globalLevel) : 0;
  const districtCount = progression?.areas.length ?? 0;

  return (
    <header
      style={{
        height: 68,
        flex: "0 0 68px",
        display: "flex",
        alignItems: "center",
        gap: 18,
        padding: "0 26px",
        borderBottom: "1px solid var(--border-subtle)",
        background: "rgba(253,251,246,0.86)",
        backdropFilter: "var(--blur-glass)",
        WebkitBackdropFilter: "var(--blur-glass)",
        zIndex: 10,
      }}
    >
      <div style={{ display: "flex", flexDirection: "column", gap: 2 }}>
        <span className="armali-eyebrow" style={{ fontSize: 10 }}>
          Your world
        </span>
        <div style={{ display: "flex", alignItems: "baseline", gap: 10 }}>
          <h1 style={{ fontSize: 21 }}>{era?.name ?? "Belfalas"}</h1>
          <span style={{ fontSize: 13, color: "var(--text-muted)", fontWeight: 600 }}>
            {hasActiveEra ? "Belfalas" : "No active era"}
          </span>
        </div>
      </div>

      <div style={{ flex: 1 }} />

      {hasActiveEra && (
        <>
          <div
            style={{
              display: "flex",
              alignItems: "center",
              gap: 8,
              padding: "7px 12px 7px 8px",
              borderRadius: 999,
              background: "var(--bone-200)",
              border: "1px solid var(--border-subtle)",
            }}
          >
            <CalendarRange size={16} color="var(--text-secondary)" aria-hidden />
            <span style={{ fontSize: 13, color: "var(--text-secondary)", fontWeight: 600 }}>
              Week {weekNumber} of {weekCount}
            </span>
          </div>

          <div
            style={{
              display: "flex",
              alignItems: "center",
              gap: 11,
              padding: "6px 16px 6px 6px",
              borderRadius: 999,
              background: "linear-gradient(120deg,var(--aqua-50),var(--gold-50))",
              border: "1px solid var(--border-default)",
              boxShadow: "var(--glow-soft)",
            }}
          >
            <div
              style={{
                width: 40,
                height: 40,
                borderRadius: "50%",
                background: "linear-gradient(140deg,var(--aqua-400),var(--aqua-600))",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                color: "#fff",
                fontFamily: "var(--font-display)",
                fontWeight: 600,
                fontSize: 17,
                boxShadow: "var(--glow-aqua)",
              }}
            >
              {globalLevel}
            </div>
            <div style={{ display: "flex", flexDirection: "column", lineHeight: 1.1 }}>
              <span style={{ fontFamily: "var(--font-display)", fontWeight: 600, fontSize: 13 }}>
                Global level
              </span>
              <span style={{ fontSize: 11, color: "var(--text-muted)" }}>
                average of {districtCount} districts
              </span>
            </div>
          </div>
        </>
      )}
    </header>
  );
}
