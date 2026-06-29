import { RotateCcw, Shuffle } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { overrideWeeklySet } from "../../api/endpoints";
import type { AreaResponse, WeeklyGoalResponse, WeeklySet } from "../../api/types";
import { getAreaTheme } from "../../lib/areaTheme";
import { AdminCard, Button, CardHeader } from "./fields";

export function WeeklySetOverrideCard({
  eraId,
  areas,
  weeklyGoals,
  weeklySet,
  weekNumber,
  onChanged,
}: {
  eraId: string;
  areas: AreaResponse[];
  weeklyGoals: WeeklyGoalResponse[];
  weeklySet: WeeklySet | null;
  weekNumber: number;
  onChanged: () => Promise<void>;
}) {
  const currentIds = useMemo(
    () => (weeklySet?.goals ?? []).map((goal) => goal.weeklyGoalId),
    [weeklySet],
  );

  const [selected, setSelected] = useState<Set<string>>(new Set(currentIds));
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Re-sync local selection whenever the drawn set changes underneath us.
  useEffect(() => {
    setSelected(new Set(currentIds));
  }, [currentIds]);

  const grouped = useMemo(() => {
    const sortedAreas = [...areas].sort((a, b) => a.order - b.order);
    return sortedAreas.map((area, index) => ({
      area,
      accent: getAreaTheme(index).hex,
      goals: weeklyGoals
        .filter((goal) => goal.areaId === area.id)
        .sort((a, b) => a.label.localeCompare(b.label)),
    }));
  }, [areas, weeklyGoals]);

  if (!weeklySet) {
    return (
      <AdminCard>
        <CardHeader title="Weekly set override" subtitle="Manually choose which goals make up a week's set." />
        <p style={{ marginTop: 14, fontSize: 13, color: "var(--text-muted)" }}>
          The override becomes available once the era has an active week. It edits the current week's drawn set.
        </p>
      </AdminCard>
    );
  }

  const toggle = (id: string) => {
    setSelected((current) => {
      const next = new Set(current);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  };

  const dirty =
    selected.size !== currentIds.length || currentIds.some((id) => !selected.has(id));

  const save = async () => {
    setBusy(true);
    setError(null);
    try {
      await overrideWeeklySet(eraId, weeklySet.weekIndex, [...selected]);
      await onChanged();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Could not save the weekly set.");
    } finally {
      setBusy(false);
    }
  };

  return (
    <AdminCard>
      <CardHeader
        title="Weekly set override"
        subtitle={`Hand-pick the goals shown for week ${weekNumber}. This replaces the automatic rotation for that week.`}
        action={
          <Button variant="ghost" onClick={() => setSelected(new Set(currentIds))} disabled={busy || !dirty}>
            <RotateCcw size={14} aria-hidden /> Reset
          </Button>
        }
      />

      {weeklyGoals.length === 0 ? (
        <p style={{ marginTop: 14, fontSize: 13, color: "var(--text-muted)" }}>
          Add some weekly goals above before composing a set.
        </p>
      ) : (
        <>
          <div style={{ display: "flex", flexDirection: "column", gap: 16, marginTop: 16 }}>
            {grouped.map(({ area, accent, goals }) => (
              <div key={area.id}>
                <div style={{ display: "flex", alignItems: "center", gap: 8, marginBottom: 8 }}>
                  <span style={{ width: 8, height: 8, borderRadius: 3, background: accent }} />
                  <span className="armali-eyebrow" style={{ fontSize: 9.5 }}>
                    {area.name}
                  </span>
                </div>
                {goals.length === 0 ? (
                  <p style={{ fontSize: 12.5, color: "var(--text-muted)", paddingLeft: 16 }}>No goals in this area.</p>
                ) : (
                  <div style={{ display: "flex", flexDirection: "column", gap: 7 }}>
                    {goals.map((goal) => {
                      const checked = selected.has(goal.id);
                      return (
                        <label
                          key={goal.id}
                          style={{
                            display: "flex",
                            alignItems: "center",
                            gap: 11,
                            padding: "9px 12px",
                            borderRadius: 12,
                            border: `1px solid ${checked ? "transparent" : "var(--border-subtle)"}`,
                            background: checked ? "var(--aqua-50)" : "var(--bone-50)",
                            cursor: busy ? "default" : "pointer",
                          }}
                        >
                          <input
                            type="checkbox"
                            checked={checked}
                            disabled={busy}
                            onChange={() => toggle(goal.id)}
                            style={{ accentColor: "var(--aqua-500)", width: 16, height: 16 }}
                          />
                          <span style={{ flex: 1, minWidth: 0, fontSize: 13.5, fontWeight: 600 }}>{goal.label}</span>
                          <span
                            style={{
                              fontFamily: "var(--font-display)",
                              fontWeight: 600,
                              fontSize: 12,
                              color: accent,
                            }}
                          >
                            +{goal.xp}
                          </span>
                        </label>
                      );
                    })}
                  </div>
                )}
              </div>
            ))}
          </div>

          <div style={{ display: "flex", alignItems: "center", gap: 12, marginTop: 18 }}>
            <Button onClick={() => void save()} disabled={busy || !dirty}>
              <Shuffle size={15} aria-hidden /> Save week {weekNumber}'s set
            </Button>
            <span style={{ fontSize: 12.5, color: "var(--text-muted)" }}>
              {selected.size} goal{selected.size === 1 ? "" : "s"} selected
            </span>
          </div>
        </>
      )}

      {error ? <p style={{ marginTop: 12, fontSize: 12.5, color: "var(--danger-hover)" }}>{error}</p> : null}
    </AdminCard>
  );
}
