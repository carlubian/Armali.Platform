import { Gauge } from "lucide-react";
import { useMemo } from "react";
import type { AreaResponse, DailyHabitResponse, WeeklyGoalResponse, WeeklySet } from "../../api/types";
import { getAreaTheme } from "../../lib/areaTheme";
import { AdminCard, CardHeader } from "./fields";

// Pacing target: each area should offer a little more than one level's worth of XP
// per week, so completing a *subset* (not everything) yields ~1 level, while the
// weekly budget still bounds gains to roughly one level. Outside this band the
// admin is nudged that the area is too tight or too generous.
const TARGET_MIN = 1.1;
const TARGET_MAX = 1.6;

interface AreaCalibration {
  areaId: string;
  name: string;
  accent: string;
  dailyPerDay: number;
  dailyPerWeek: number;
  weekly: number;
  weeklyEstimated: boolean;
  total: number;
  levelsPerWeek: number;
}

type Status = "tight" | "good" | "generous";

function statusOf(levelsPerWeek: number): Status {
  if (levelsPerWeek < TARGET_MIN) return "tight";
  if (levelsPerWeek > TARGET_MAX) return "generous";
  return "good";
}

const STATUS_COPY: Record<Status, { label: string; color: string; soft: string }> = {
  tight: { label: "Tight", color: "var(--gold-600)", soft: "var(--gold-100)" },
  good: { label: "On pace", color: "var(--sea-600)", soft: "var(--sea-100)" },
  generous: { label: "Generous", color: "var(--terracotta-600)", soft: "var(--terracotta-100)" },
};

export function XpCalibrationCard({
  areas,
  dailyHabits,
  weeklyGoals,
  weeklySet,
  xpPerLevel,
}: {
  areas: AreaResponse[];
  dailyHabits: DailyHabitResponse[];
  weeklyGoals: WeeklyGoalResponse[];
  weeklySet: WeeklySet | null;
  xpPerLevel: number;
}) {
  const rows = useMemo<AreaCalibration[]>(() => {
    const setGoalIds = new Set(weeklySet?.goals.map((goal) => goal.weeklyGoalId) ?? []);
    const sorted = [...areas].sort((a, b) => a.order - b.order);

    return sorted.map((area, index) => {
      const dailyPerDay = dailyHabits
        .filter((habit) => habit.areaId === area.id)
        .reduce((sum, habit) => sum + habit.xp, 0);
      const dailyPerWeek = dailyPerDay * 7;

      const areaGoals = weeklyGoals.filter((goal) => goal.areaId === area.id);
      const drawn = areaGoals.filter((goal) => setGoalIds.has(goal.id));
      // Use the week's actual set when it covers this area; otherwise estimate the
      // rotation (one goal per area per week) with the pool average.
      const weeklyEstimated = drawn.length === 0 && areaGoals.length > 0;
      const weekly = drawn.length > 0
        ? drawn.reduce((sum, goal) => sum + goal.xp, 0)
        : weeklyEstimated
          ? Math.round(areaGoals.reduce((sum, goal) => sum + goal.xp, 0) / areaGoals.length)
          : 0;

      const total = dailyPerWeek + weekly;
      return {
        areaId: area.id,
        name: area.name,
        accent: getAreaTheme(index).hex,
        dailyPerDay,
        dailyPerWeek,
        weekly,
        weeklyEstimated,
        total,
        levelsPerWeek: xpPerLevel > 0 ? total / xpPerLevel : 0,
      };
    });
  }, [areas, dailyHabits, weeklyGoals, weeklySet, xpPerLevel]);

  return (
    <AdminCard>
      <CardHeader
        title="XP calibration"
        subtitle={`Each level costs ${xpPerLevel} XP. Aim for a little over one level's worth of obtainable XP per area each week, so finishing a subset earns ~1 level.`}
        action={
          <span
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 7,
              padding: "6px 11px",
              borderRadius: 999,
              background: "var(--aqua-50)",
              color: "var(--aqua-700)",
              fontFamily: "var(--font-display)",
              fontWeight: 600,
              fontSize: 12,
            }}
          >
            <Gauge size={15} aria-hidden /> {xpPerLevel} XP / level
          </span>
        }
      />

      {rows.length === 0 ? (
        <p style={{ marginTop: 14, fontSize: 13, color: "var(--text-muted)" }}>No areas to calibrate.</p>
      ) : (
        <div style={{ marginTop: 16, overflowX: "auto" }}>
          <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 13 }}>
            <thead>
              <tr>
                <Th align="left">Area</Th>
                <Th>Daily / day</Th>
                <Th>Daily / week</Th>
                <Th>Weekly goal</Th>
                <Th>Total / week</Th>
                <Th>~ levels / week</Th>
                <Th align="right">Pacing</Th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => {
                const status = STATUS_COPY[statusOf(row.levelsPerWeek)];
                return (
                  <tr key={row.areaId} style={{ borderTop: "1px solid var(--border-subtle)" }}>
                    <Td align="left">
                      <span style={{ display: "inline-flex", alignItems: "center", gap: 8 }}>
                        <span style={{ width: 8, height: 8, borderRadius: 3, background: row.accent }} />
                        <span style={{ fontWeight: 600 }}>{row.name}</span>
                      </span>
                    </Td>
                    <Td>{row.dailyPerDay}</Td>
                    <Td>{row.dailyPerWeek}</Td>
                    <Td>
                      {row.weekly}
                      {row.weeklyEstimated ? <span style={{ color: "var(--text-muted)" }}> est.</span> : null}
                    </Td>
                    <Td>
                      <strong>{row.total}</strong>
                    </Td>
                    <Td>{row.levelsPerWeek.toFixed(2)}</Td>
                    <Td align="right">
                      <span
                        style={{
                          display: "inline-block",
                          padding: "3px 10px",
                          borderRadius: 999,
                          background: status.soft,
                          color: status.color,
                          fontFamily: "var(--font-display)",
                          fontWeight: 600,
                          fontSize: 11.5,
                        }}
                      >
                        {status.label}
                      </span>
                    </Td>
                  </tr>
                );
              })}
            </tbody>
          </table>
          <p style={{ marginTop: 12, fontSize: 11.5, color: "var(--text-muted)", lineHeight: 1.5 }}>
            Daily habits can be completed every day, so their weekly XP is ×7. The weekly figure reflects the goal the
            rotation draws for the current week (one per area); “est.” uses the pool average until a set is drawn.
            “On pace” means {TARGET_MIN}–{TARGET_MAX}× a level is on offer.
          </p>
        </div>
      )}
    </AdminCard>
  );
}

function Th({ children, align = "center" }: { children: React.ReactNode; align?: "left" | "center" | "right" }) {
  return (
    <th
      style={{
        textAlign: align,
        padding: "0 10px 8px",
        fontSize: 10,
        letterSpacing: "0.04em",
        textTransform: "uppercase",
        color: "var(--text-muted)",
        fontWeight: 600,
        whiteSpace: "nowrap",
      }}
    >
      {children}
    </th>
  );
}

function Td({ children, align = "center" }: { children: React.ReactNode; align?: "left" | "center" | "right" }) {
  return (
    <td style={{ textAlign: align, padding: "10px", color: "var(--text-secondary)", whiteSpace: "nowrap" }}>
      {children}
    </td>
  );
}
