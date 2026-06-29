import { Archive, CalendarRange, Globe2, Layers } from "lucide-react";
import { useState } from "react";
import { archiveEra } from "../../api/endpoints";
import { useEraData } from "../../state/EraDataContext";
import { Button } from "./fields";
import { QuestPoolCard } from "./QuestPoolCard";
import { WeeklySetOverrideCard } from "./WeeklySetOverrideCard";
import { XpCalibrationCard } from "./XpCalibrationCard";

export function EraStudio() {
  const { era, weekly, template, weekNumber, weekCount, refresh } = useEraData();
  const [confirmingArchive, setConfirmingArchive] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (!era) {
    return null;
  }

  const archive = async () => {
    setBusy(true);
    setError(null);
    try {
      await archiveEra(era.id);
      setConfirmingArchive(false);
      await refresh();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Could not archive the era.");
    } finally {
      setBusy(false);
    }
  };

  return (
    <div style={{ maxWidth: 1080, margin: "0 auto", padding: "22px 30px 64px" }}>
      {/* Header */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 18,
          padding: "20px 24px",
          borderRadius: "var(--radius-card)",
          background: "var(--surface-card-solid)",
          border: "1px solid var(--border-subtle)",
          boxShadow: "var(--glow-card)",
          flexWrap: "wrap",
        }}
      >
        <div style={{ flex: 1, minWidth: 240 }}>
          <span className="armali-eyebrow" style={{ fontSize: 10 }}>
            Era studio
          </span>
          <h1 style={{ fontSize: 28, marginTop: 4 }}>{era.name}</h1>
          <div
            style={{
              display: "flex",
              flexWrap: "wrap",
              gap: 14,
              marginTop: 10,
              fontSize: 12.5,
              color: "var(--text-secondary)",
            }}
          >
            <Meta icon={Globe2}>{template?.name ?? era.templateId}</Meta>
            <Meta icon={CalendarRange}>
              Week {weekNumber} of {weekCount}
            </Meta>
            <Meta icon={Layers}>
              {era.areas.length} area{era.areas.length === 1 ? "" : "s"}
            </Meta>
          </div>
        </div>

        {confirmingArchive ? (
          <div style={{ display: "flex", flexDirection: "column", gap: 8, alignItems: "flex-end" }}>
            <span style={{ fontSize: 12.5, color: "var(--text-secondary)", maxWidth: 280, textAlign: "right" }}>
              Archiving snapshots this era's progress and world as read-only. You can then start a new era.
            </span>
            <div style={{ display: "flex", gap: 10 }}>
              <Button variant="ghost" onClick={() => setConfirmingArchive(false)} disabled={busy}>
                Cancel
              </Button>
              <Button variant="danger" onClick={() => void archive()} disabled={busy}>
                <Archive size={15} aria-hidden /> Confirm archive
              </Button>
            </div>
          </div>
        ) : (
          <Button variant="ghost" onClick={() => setConfirmingArchive(true)} disabled={busy}>
            <Archive size={15} aria-hidden /> Archive era
          </Button>
        )}
      </div>

      {error ? <p style={{ marginTop: 12, fontSize: 12.5, color: "var(--danger-hover)" }}>{error}</p> : null}

      <div style={{ display: "flex", flexDirection: "column", gap: 18, marginTop: 18 }}>
        <XpCalibrationCard
          areas={era.areas}
          dailyHabits={era.dailyHabits}
          weeklyGoals={era.weeklyGoals}
          weeklySet={weekly}
          xpPerLevel={era.xpPerLevel}
        />
        <QuestPoolCard kind="daily" eraId={era.id} areas={era.areas} items={era.dailyHabits} onChanged={refresh} />
        <QuestPoolCard kind="weekly" eraId={era.id} areas={era.areas} items={era.weeklyGoals} onChanged={refresh} />
        <WeeklySetOverrideCard
          eraId={era.id}
          areas={era.areas}
          weeklyGoals={era.weeklyGoals}
          weeklySet={weekly}
          weekNumber={weekNumber}
          onChanged={refresh}
        />
      </div>
    </div>
  );
}

function Meta({ icon: Icon, children }: { icon: typeof Globe2; children: React.ReactNode }) {
  return (
    <span style={{ display: "inline-flex", alignItems: "center", gap: 6 }}>
      <Icon size={14} aria-hidden /> {children}
    </span>
  );
}
