import { SlidersHorizontal } from "lucide-react";
import { EmptyState } from "../components/EmptyState";

export function AdminScreen() {
  return (
    <div style={{ position: "absolute", inset: 0, background: "linear-gradient(180deg,var(--bone-100),var(--bone-200))" }}>
      <EmptyState icon={SlidersHorizontal} title="The era studio is on its way">
        Creating eras, authoring daily habits and weekly goals, and calibrating per-area XP arrive in a later wave.
        For now you can manage content through the API.
      </EmptyState>
    </div>
  );
}
