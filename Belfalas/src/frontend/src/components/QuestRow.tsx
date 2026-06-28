import { Check } from "lucide-react";

interface QuestRowProps {
  label: string;
  xp: number;
  hex: string;
  soft: string;
  completed: boolean;
  onToggle: () => void;
  /** Optional second line (used by weekly goals to show the district · area). */
  subLabel?: string;
  /** Weekly goals show the XP inside a soft pill; daily habits show a bare swatch. */
  variant?: "daily" | "weekly";
}

export function QuestRow({
  label,
  xp,
  hex,
  soft,
  completed,
  onToggle,
  subLabel,
  variant = "daily",
}: QuestRowProps) {
  return (
    <button
      type="button"
      className="belf-quest-row"
      onClick={onToggle}
      style={{ background: completed ? soft : "var(--bone-50)" }}
    >
      <span
        style={{
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          flex: "0 0 auto",
          width: 26,
          height: 26,
          borderRadius: 9,
          transition: "all 0.2s var(--ease-spring)",
          border: `2px solid ${completed ? hex : "var(--border-strong)"}`,
          background: completed ? hex : "transparent",
        }}
      >
        <Check size={15} color="#fff" strokeWidth={3.5} style={{ opacity: completed ? 1 : 0 }} aria-hidden />
      </span>

      <span style={{ flex: 1, minWidth: 0, textAlign: "left" }}>
        <span
          style={{
            display: "block",
            fontSize: 14,
            fontWeight: 600,
            color: completed ? "var(--text-muted)" : "var(--text-primary)",
            textDecoration: completed ? "line-through" : "none",
          }}
        >
          {label}
        </span>
        {subLabel && (
          <span style={{ display: "block", marginTop: 2, fontSize: 11.5, color: "var(--text-muted)" }}>
            {subLabel}
          </span>
        )}
      </span>

      {variant === "weekly" ? (
        <span
          style={{
            display: "inline-flex",
            alignItems: "center",
            gap: 5,
            flex: "0 0 auto",
            padding: "4px 9px",
            borderRadius: 999,
            background: soft,
          }}
        >
          <span style={{ fontFamily: "var(--font-display)", fontWeight: 600, fontSize: 12, color: hex }}>
            +{xp} XP
          </span>
        </span>
      ) : (
        <span style={{ display: "inline-flex", alignItems: "center", gap: 5, flex: "0 0 auto" }}>
          <span style={{ width: 8, height: 8, borderRadius: 3, background: hex }} />
          <span style={{ fontFamily: "var(--font-display)", fontWeight: 600, fontSize: 12, color: "var(--text-secondary)" }}>
            +{xp}
          </span>
        </span>
      )}
    </button>
  );
}
