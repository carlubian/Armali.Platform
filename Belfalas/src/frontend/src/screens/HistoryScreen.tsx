import { History } from "lucide-react";
import { EmptyState } from "../components/EmptyState";

export function HistoryScreen() {
  return (
    <div style={{ position: "absolute", inset: 0, background: "linear-gradient(180deg,var(--bone-100),var(--bone-200))" }}>
      <EmptyState icon={History} title="Past eras will live here">
        Once you archive an era, its world and progress become a read-only chapter you can browse. The history viewer
        arrives in a later wave.
      </EmptyState>
    </div>
  );
}
