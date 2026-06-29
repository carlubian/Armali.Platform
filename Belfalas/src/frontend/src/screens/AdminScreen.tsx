import { EraStudio } from "../components/admin/EraStudio";
import { EraWizard } from "../components/admin/EraWizard";
import { useEraData } from "../state/EraDataContext";

export function AdminScreen() {
  const { hasActiveEra } = useEraData();

  return (
    <div
      style={{
        position: "absolute",
        inset: 0,
        overflowY: "auto",
        background: "linear-gradient(180deg,var(--bone-100),var(--bone-200))",
      }}
    >
      {hasActiveEra ? <EraStudio /> : <EraWizard />}
    </div>
  );
}
