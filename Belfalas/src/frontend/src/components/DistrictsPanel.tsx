import { useNavigate } from "react-router-dom";
import type { AreaView } from "../state/EraDataContext";
import { useEraData } from "../state/EraDataContext";

const RING_RADIUS = 18;
const RING_CIRCUMFERENCE = 2 * Math.PI * RING_RADIUS;

export function DistrictsPanel() {
  const navigate = useNavigate();
  const { areas } = useEraData();

  return (
    <div
      style={{
        position: "absolute",
        top: 22,
        left: 22,
        width: 298,
        padding: 18,
        borderRadius: "var(--radius-card)",
        background: "var(--surface-card)",
        backdropFilter: "var(--blur-glass)",
        WebkitBackdropFilter: "var(--blur-glass)",
        border: "1px solid var(--border-glass)",
        boxShadow: "var(--glow-card)",
      }}
    >
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 12 }}>
        <span className="armali-eyebrow" style={{ fontSize: 10 }}>
          Districts
        </span>
        <span style={{ fontSize: 11, color: "var(--text-muted)", fontWeight: 600 }}>level / {areas[0]?.maxLevel ?? 50}</span>
      </div>
      <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
        {areas.map((area) => (
          <DistrictRow key={area.areaId} area={area} onOpen={() => navigate(`/area/${area.areaId}`)} />
        ))}
      </div>
    </div>
  );
}

function DistrictRow({ area, onOpen }: { area: AreaView; onOpen: () => void }) {
  const pct = area.xpPerLevel === 0 ? 0 : area.xpIntoLevel / area.xpPerLevel;
  const dashOffset = RING_CIRCUMFERENCE * (1 - pct);

  return (
    <button type="button" className="belf-district-row" onClick={onOpen}>
      <div style={{ position: "relative", width: 44, height: 44, flex: "0 0 44px" }}>
        <svg width="44" height="44" viewBox="0 0 44 44" style={{ transform: "rotate(-90deg)" }}>
          <circle cx="22" cy="22" r={RING_RADIUS} fill="none" stroke="rgba(124,110,86,0.16)" strokeWidth="5" />
          <circle
            cx="22"
            cy="22"
            r={RING_RADIUS}
            fill="none"
            stroke={area.theme.hex}
            strokeWidth="5"
            strokeLinecap="round"
            strokeDasharray={RING_CIRCUMFERENCE.toFixed(1)}
            strokeDashoffset={dashOffset.toFixed(1)}
            style={{ transition: "stroke-dashoffset 0.5s var(--ease-out)" }}
          />
        </svg>
        <span
          style={{
            position: "absolute",
            inset: 0,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            fontFamily: "var(--font-display)",
            fontWeight: 600,
            fontSize: 14,
            color: area.theme.hex,
          }}
        >
          {area.level}
        </span>
      </div>

      <div style={{ flex: 1, minWidth: 0 }}>
        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 8 }}>
          <span
            style={{
              fontFamily: "var(--font-display)",
              fontWeight: 600,
              fontSize: 13.5,
              whiteSpace: "nowrap",
              overflow: "hidden",
              textOverflow: "ellipsis",
            }}
          >
            {area.districtName}
          </span>
          <span style={{ fontSize: 10, color: "var(--text-muted)", fontWeight: 600, whiteSpace: "nowrap" }}>
            {area.areaName}
          </span>
        </div>
        <div style={{ marginTop: 3, fontSize: 11, color: "var(--text-muted)" }}>
          {area.isComplete
            ? "Fully grown"
            : `${area.xpIntoLevel} / ${area.xpPerLevel} XP to next`}
        </div>
      </div>
    </button>
  );
}
