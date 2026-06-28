import { useNavigate } from "react-router-dom";
import { useEraData } from "../state/EraDataContext";

const CONFETTI_COUNT = 16;

export function Celebration() {
  const navigate = useNavigate();
  const { celebration, areas, dismissCelebration } = useEraData();

  if (!celebration) {
    return null;
  }

  const area = areas.find((candidate) => candidate.areaId === celebration.areaId);
  const hex = area?.theme.hex ?? "#16A6A6";
  const hex2 = area?.theme.hex2 ?? "#0E8E92";
  const soft = area?.theme.soft ?? "#D8F0EC";
  const glow = area?.theme.glow ?? "var(--glow-aqua)";
  const districtName = area?.districtName ?? area?.areaName ?? "A district";
  const confettiColours = [hex, hex2, "#DBA63E", "#16A6A6", "#E9A98F"];

  const visitDistrict = () => {
    dismissCelebration();
    navigate(`/area/${celebration.areaId}`);
  };

  return (
    <div
      role="dialog"
      aria-modal="true"
      onClick={dismissCelebration}
      style={{
        position: "fixed",
        inset: 0,
        zIndex: 60,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        background: "rgba(44,40,35,0.34)",
        backdropFilter: "blur(3px)",
        WebkitBackdropFilter: "blur(3px)",
      }}
    >
      <div
        onClick={(event) => event.stopPropagation()}
        style={{
          position: "relative",
          width: "min(440px,90%)",
          padding: "32px 30px",
          borderRadius: "var(--radius-2xl)",
          background: "var(--surface-card-solid)",
          border: "1px solid var(--border-glass)",
          boxShadow: "var(--glow-card)",
          overflow: "hidden",
          animation: "belf-pop 0.42s var(--ease-spring) both",
        }}
      >
        <div style={{ position: "absolute", inset: 0, overflow: "hidden", borderRadius: "inherit", pointerEvents: "none" }}>
          {Array.from({ length: CONFETTI_COUNT }, (_, index) => (
            <span
              key={index}
              style={{
                position: "absolute",
                top: -12,
                left: `${6 + index * 6}%`,
                width: 8,
                height: 12,
                borderRadius: 2,
                background: confettiColours[index % confettiColours.length],
                animation: `belf-confetti ${1.4 + (index % 5) * 0.22}s var(--ease-out) ${(index % 4) * 0.1}s forwards`,
              }}
            />
          ))}
        </div>

        <div style={{ position: "relative", width: 96, height: 96, margin: "0 auto 4px" }}>
          <span
            style={{
              position: "absolute",
              inset: -8,
              borderRadius: "50%",
              background: soft,
              animation: "belf-spark 1.6s var(--ease-out) infinite",
            }}
          />
          <div
            style={{
              position: "relative",
              width: 96,
              height: 96,
              borderRadius: "50%",
              background: `linear-gradient(140deg,${hex},${hex2})`,
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              boxShadow: glow,
            }}
          >
            <span style={{ fontFamily: "var(--font-display)", fontWeight: 600, fontSize: 40, color: "#fff" }}>
              {celebration.level}
            </span>
          </div>
        </div>

        <div className="armali-eyebrow" style={{ textAlign: "center", color: hex }}>
          Level up
        </div>
        <h2 style={{ textAlign: "center", fontSize: 27, marginTop: 6 }}>{districtName} grew</h2>
        <p
          style={{
            textAlign: "center",
            marginTop: 8,
            fontSize: 14.5,
            color: "var(--text-secondary)",
            maxWidth: 340,
            marginLeft: "auto",
            marginRight: "auto",
          }}
        >
          The district reached level {celebration.level} — about one level's worth of progress. A new stage rose with it.
        </p>

        <div style={{ marginTop: 18, display: "flex", justifyContent: "center", gap: 10 }}>
          <button
            type="button"
            className="belf-pill-btn"
            onClick={visitDistrict}
            style={{
              padding: "11px 20px",
              borderRadius: 12,
              border: "1px solid var(--border-default)",
              background: "var(--surface-card-solid)",
              fontFamily: "var(--font-display)",
              fontWeight: 600,
              fontSize: 14,
              color: "var(--text-primary)",
            }}
          >
            Visit district
          </button>
          <button
            type="button"
            onClick={dismissCelebration}
            style={{
              padding: "11px 22px",
              borderRadius: 12,
              border: "none",
              background: `linear-gradient(120deg,${hex},${hex2})`,
              color: "#fff",
              fontFamily: "var(--font-display)",
              fontWeight: 600,
              fontSize: 14,
              cursor: "pointer",
              boxShadow: glow,
            }}
          >
            Keep going
          </button>
        </div>
      </div>
    </div>
  );
}
