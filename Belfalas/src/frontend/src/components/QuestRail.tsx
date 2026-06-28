import { ShieldCheck, Sunrise, Target } from "lucide-react";
import { useMemo } from "react";
import { getAreaTheme, type AreaTheme } from "../lib/areaTheme";
import { useEraData } from "../state/EraDataContext";
import { QuestRow } from "./QuestRow";

export function QuestRail() {
  const { daily, weekly, areas, toggleDaily, toggleWeekly } = useEraData();

  // Quests carry an areaId; resolve each to its district theme. Fall back to a
  // palette slot if the area is missing from the progression view.
  const themeByArea = useMemo(() => {
    const map = new Map<string, { theme: AreaTheme; districtName: string; areaName: string }>();
    areas.forEach((area) => {
      map.set(area.areaId, { theme: area.theme, districtName: area.districtName, areaName: area.areaName });
    });
    return map;
  }, [areas]);

  const themeFor = (areaId: string, fallbackIndex: number): AreaTheme =>
    themeByArea.get(areaId)?.theme ?? getAreaTheme(fallbackIndex);

  const weeklyGoals = weekly?.goals ?? [];
  const total = daily.length + weeklyGoals.length;
  const done = daily.filter((q) => q.completed).length + weeklyGoals.filter((q) => q.completed).length;
  const overallPct = total === 0 ? 0 : Math.round((done / total) * 100);

  return (
    <aside
      style={{
        width: 400,
        flex: "0 0 400px",
        background: "var(--surface-card-solid)",
        borderLeft: "1px solid var(--border-subtle)",
        display: "flex",
        flexDirection: "column",
        minHeight: 0,
      }}
    >
      <div style={{ padding: "22px 22px 14px", borderBottom: "1px solid var(--border-subtle)" }}>
        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
          <h2 style={{ fontSize: 20 }}>This week's quests</h2>
          <span
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 6,
              padding: "5px 11px",
              borderRadius: 999,
              background: "var(--aqua-100)",
              color: "var(--aqua-700)",
              fontFamily: "var(--font-display)",
              fontWeight: 600,
              fontSize: 12,
            }}
          >
            {done} / {total} done
          </span>
        </div>
        <p style={{ marginTop: 6, fontSize: 13, color: "var(--text-secondary)" }}>
          A light, mixed list. Finish enough — not everything — for about one level in each district.
        </p>
        <div style={{ marginTop: 12, height: 8, borderRadius: 999, background: "var(--bone-200)", overflow: "hidden" }}>
          <div
            style={{
              height: "100%",
              borderRadius: 999,
              background: "linear-gradient(90deg,var(--aqua-500),var(--gold-400))",
              width: `${overallPct}%`,
              transition: "width 0.5s var(--ease-out)",
            }}
          />
        </div>
      </div>

      <div style={{ flex: 1, minHeight: 0, overflowY: "auto", padding: "18px 18px 26px" }}>
        {/* Daily habits */}
        <SectionHeader icon={<Sunrise size={17} color="var(--gold-500)" aria-hidden />} label="Daily habits">
          <span style={{ fontSize: 11, color: "var(--text-muted)" }}>resets tonight · Madrid</span>
        </SectionHeader>
        <div style={{ display: "flex", flexDirection: "column", gap: 9 }}>
          {daily.length === 0 && <EmptyHint>No daily habits in this era yet.</EmptyHint>}
          {daily.map((quest, index) => {
            const theme = themeFor(quest.areaId, index);
            return (
              <QuestRow
                key={quest.dailyHabitId}
                label={quest.label}
                xp={quest.xp}
                hex={theme.hex}
                soft={theme.soft}
                completed={quest.completed}
                onToggle={() => void toggleDaily(quest)}
                variant="daily"
              />
            );
          })}
        </div>

        {/* Weekly goals */}
        <div style={{ marginTop: 22 }}>
          <SectionHeader icon={<Target size={17} color="var(--azure-500)" aria-hidden />} label="Weekly goals">
            <span style={{ fontSize: 11, color: "var(--text-muted)" }}>larger · once each</span>
          </SectionHeader>
        </div>
        <div style={{ display: "flex", flexDirection: "column", gap: 9 }}>
          {weeklyGoals.length === 0 && <EmptyHint>This week's set has no goals yet.</EmptyHint>}
          {weeklyGoals.map((quest, index) => {
            const theme = themeFor(quest.areaId, index);
            const meta = themeByArea.get(quest.areaId);
            const subLabel = meta ? `${meta.districtName} · ${meta.areaName}` : quest.areaName;
            return (
              <QuestRow
                key={quest.weeklyGoalId}
                label={quest.label}
                xp={quest.xp}
                hex={theme.hex}
                soft={theme.soft}
                completed={quest.completed}
                subLabel={subLabel}
                onToggle={() => void toggleWeekly(quest)}
                variant="weekly"
              />
            );
          })}
        </div>
      </div>

      <div
        style={{
          padding: "14px 20px",
          borderTop: "1px solid var(--border-subtle)",
          background: "var(--bone-50)",
          display: "flex",
          alignItems: "center",
          gap: 11,
        }}
      >
        <ShieldCheck size={18} color="var(--sea-500)" style={{ flex: "0 0 auto" }} aria-hidden />
        <span style={{ fontSize: 12.5, color: "var(--text-secondary)" }}>
          No streak penalties — XP you don't earn is simply not gained. Weekly goals help you catch up.
        </span>
      </div>
    </aside>
  );
}

function SectionHeader({
  icon,
  label,
  children,
}: {
  icon: React.ReactNode;
  label: string;
  children?: React.ReactNode;
}) {
  return (
    <div style={{ display: "flex", alignItems: "center", gap: 9, margin: "2px 4px 10px" }}>
      {icon}
      <span className="armali-eyebrow" style={{ fontSize: 10 }}>
        {label}
      </span>
      {children}
    </div>
  );
}

function EmptyHint({ children }: { children: React.ReactNode }) {
  return (
    <p style={{ padding: "10px 4px", fontSize: 13, color: "var(--text-muted)" }}>{children}</p>
  );
}
