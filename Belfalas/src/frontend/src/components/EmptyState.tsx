import { Compass, type LucideIcon } from "lucide-react";
import type { ReactNode } from "react";

interface EmptyStateProps {
  icon?: LucideIcon;
  title: string;
  children: ReactNode;
}

export function EmptyState({ icon: Icon = Compass, title, children }: EmptyStateProps) {
  return (
    <div
      style={{
        position: "absolute",
        inset: 0,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        padding: 32,
      }}
    >
      <div
        style={{
          maxWidth: 440,
          textAlign: "center",
          padding: "36px 34px",
          borderRadius: "var(--radius-card)",
          background: "var(--surface-card-solid)",
          border: "1px solid var(--border-subtle)",
          boxShadow: "var(--glow-card)",
        }}
      >
        <div
          style={{
            width: 64,
            height: 64,
            margin: "0 auto 18px",
            borderRadius: 18,
            background: "linear-gradient(150deg,var(--aqua-50),var(--gold-50))",
            border: "1px solid var(--border-subtle)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
          }}
        >
          <Icon size={28} color="var(--aqua-600)" aria-hidden />
        </div>
        <h2 style={{ fontSize: 22 }}>{title}</h2>
        <p style={{ marginTop: 10, fontSize: 14.5, color: "var(--text-secondary)", lineHeight: 1.5 }}>
          {children}
        </p>
      </div>
    </div>
  );
}
