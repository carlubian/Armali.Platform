import { ArrowLeft, ArrowRight, Home, Sparkles, UsersRound, type LucideIcon } from "lucide-react";
import { useMemo } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { EmptyState } from "../components/EmptyState";
import type { DailyQuest, EvolutionStageKind, WeeklyQuest, WorldTemplateEvolutionStage } from "../api/types";
import type { AreaView } from "../state/EraDataContext";
import { useEraData } from "../state/EraDataContext";

const STAGE_ICON: Record<EvolutionStageKind, LucideIcon> = {
  Building: Home,
  Denizen: UsersRound,
  Upgrade: Sparkles,
};

function capitalize(value: string): string {
  return value.length === 0 ? value : value[0].toUpperCase() + value.slice(1);
}

function stageLabel(stage: WorldTemplateEvolutionStage): string {
  if (stage.kind === "Denizen" && stage.denizenType) {
    return capitalize(stage.denizenType);
  }
  return stage.kind;
}

const BIG_RADIUS = 52;
const BIG_CIRCUMFERENCE = 2 * Math.PI * BIG_RADIUS;

export function AreaScreen() {
  const { areaId } = useParams();
  const navigate = useNavigate();
  const { hasActiveEra, areas, daily, weekly, toggleDaily, toggleWeekly } = useEraData();

  const area = areas.find((candidate) => candidate.areaId === areaId);

  const stages = useMemo(
    () => (area ? [...area.evolutionStages].sort((a, b) => a.order - b.order) : []),
    [area],
  );

  if (!hasActiveEra) {
    return (
      <div style={{ position: "absolute", inset: 0, background: "linear-gradient(180deg,var(--bone-100),var(--bone-200))" }}>
        <EmptyState title="No active era yet">Create an era to bring its districts to life.</EmptyState>
      </div>
    );
  }

  if (!area) {
    return (
      <div style={{ position: "absolute", inset: 0, background: "linear-gradient(180deg,var(--bone-100),var(--bone-200))" }}>
        <EmptyState title="District not found">
          This district isn't part of the active era. Head back to your world to pick one.
        </EmptyState>
      </div>
    );
  }

  const { theme, level, xpIntoLevel, xpPerLevel, maxLevel } = area;
  const pct = xpPerLevel === 0 ? 0 : xpIntoLevel / xpPerLevel;
  const bigDashOffset = BIG_CIRCUMFERENCE * (1 - pct);

  const currentStage = [...stages].reverse().find((stage) => stage.order <= level) ?? null;
  const nextStage = stages.find((stage) => stage.order > level) ?? null;
  const reachedCount = stages.filter((stage) => stage.order <= level).length;
  const timelinePct = stages.length === 0 ? 0 : Math.round((reachedCount / stages.length) * 100);

  const dailyHere = daily.filter((quest) => quest.areaId === area.areaId);
  const weeklyHere = (weekly?.goals ?? []).filter((quest) => quest.areaId === area.areaId);

  return (
    <div
      style={{
        position: "absolute",
        inset: 0,
        overflowY: "auto",
        background: "linear-gradient(180deg,var(--bone-100),var(--bone-200))",
      }}
    >
      <div style={{ maxWidth: 1080, margin: "0 auto", padding: "22px 30px 64px" }}>
        {/* Breadcrumb + district tabs */}
        <div style={{ display: "flex", alignItems: "center", gap: 12, marginBottom: 18, flexWrap: "wrap" }}>
          <button
            type="button"
            className="belf-pill-btn"
            onClick={() => navigate("/")}
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 7,
              padding: "8px 14px",
              borderRadius: 999,
              border: "1px solid var(--border-subtle)",
              background: "var(--surface-card-solid)",
              fontFamily: "var(--font-display)",
              fontWeight: 600,
              fontSize: 13,
              color: "var(--text-secondary)",
            }}
          >
            <ArrowLeft size={15} aria-hidden />
            World
          </button>
          <div style={{ width: 1, height: 22, background: "var(--border-default)" }} />
          {areas.map((tab) => {
            const active = tab.areaId === area.areaId;
            return (
              <button
                key={tab.areaId}
                type="button"
                onClick={() => navigate(`/area/${tab.areaId}`)}
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: 8,
                  padding: "7px 14px",
                  borderRadius: 999,
                  cursor: "pointer",
                  fontFamily: "var(--font-display)",
                  fontWeight: 600,
                  fontSize: 13,
                  border: `1px solid ${active ? "transparent" : "var(--border-subtle)"}`,
                  background: active ? tab.theme.soft : "var(--bone-50)",
                  color: active ? tab.theme.hex2 : "var(--text-secondary)",
                }}
              >
                <span style={{ width: 8, height: 8, borderRadius: 3, background: tab.theme.hex }} />
                {tab.districtName} · {tab.level}
              </button>
            );
          })}
        </div>

        {/* Hero */}
        <div
          style={{
            display: "flex",
            gap: 28,
            padding: 28,
            borderRadius: "var(--radius-card)",
            background: "var(--surface-card-solid)",
            border: "1px solid var(--border-subtle)",
            boxShadow: "var(--glow-card)",
            alignItems: "center",
            flexWrap: "wrap",
          }}
        >
          <div style={{ position: "relative", width: 132, height: 132, flex: "0 0 132px" }}>
            <svg width="132" height="132" viewBox="0 0 132 132" style={{ transform: "rotate(-90deg)" }}>
              <circle cx="66" cy="66" r={BIG_RADIUS} fill="none" stroke="rgba(124,110,86,0.16)" strokeWidth="9" />
              <circle
                cx="66"
                cy="66"
                r={BIG_RADIUS}
                fill="none"
                stroke={theme.hex}
                strokeWidth="9"
                strokeLinecap="round"
                strokeDasharray={BIG_CIRCUMFERENCE.toFixed(1)}
                strokeDashoffset={bigDashOffset.toFixed(1)}
                style={{ transition: "stroke-dashoffset 0.5s var(--ease-out)" }}
              />
            </svg>
            <div
              style={{
                position: "absolute",
                inset: 0,
                display: "flex",
                flexDirection: "column",
                alignItems: "center",
                justifyContent: "center",
              }}
            >
              <span style={{ fontFamily: "var(--font-display)", fontWeight: 600, fontSize: 42, lineHeight: 1, color: theme.hex }}>
                {level}
              </span>
              <span className="armali-eyebrow" style={{ fontSize: 9, marginTop: 2 }}>
                of {maxLevel}
              </span>
            </div>
          </div>

          <div style={{ flex: 1, minWidth: 260 }}>
            <span className="armali-eyebrow" style={{ fontSize: 10 }}>
              District · {area.areaName}
            </span>
            <h1 style={{ fontSize: 32, marginTop: 4 }}>{area.districtName}</h1>
            <p style={{ marginTop: 6, fontSize: 14.5, color: "var(--text-secondary)" }}>
              This district grows as you complete {area.areaName.toLowerCase()} quests — one stage per level.
            </p>
            <div style={{ display: "flex", alignItems: "center", gap: 12, marginTop: 16, flexWrap: "wrap" }}>
              <div style={{ display: "flex", alignItems: "center", gap: 9, padding: "8px 14px", borderRadius: 12, background: theme.soft }}>
                <span style={{ fontSize: 12, color: "var(--text-muted)" }}>Now</span>
                <span style={{ fontFamily: "var(--font-display)", fontWeight: 600, fontSize: 14, color: theme.hex }}>
                  {currentStage ? stageLabel(currentStage) : "Founding plot"}
                </span>
              </div>
              <ArrowRight size={16} color="var(--text-muted)" aria-hidden />
              <div
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: 9,
                  padding: "8px 14px",
                  borderRadius: 12,
                  background: "var(--bone-100)",
                  border: "1px dashed var(--border-default)",
                }}
              >
                <span style={{ fontSize: 12, color: "var(--text-muted)" }}>
                  {nextStage ? `At Lv ${nextStage.order}` : "Complete"}
                </span>
                <span style={{ fontFamily: "var(--font-display)", fontWeight: 600, fontSize: 14, color: "var(--text-secondary)" }}>
                  {nextStage ? stageLabel(nextStage) : "Flourishing"}
                </span>
              </div>
            </div>
            <div style={{ marginTop: 16 }}>
              <div style={{ display: "flex", justifyContent: "space-between", fontSize: 12, color: "var(--text-muted)", marginBottom: 5 }}>
                <span>{nextStage ? `Progress to level ${level + 1}` : "Fully grown"}</span>
                <span>
                  {xpIntoLevel} / {xpPerLevel} XP
                </span>
              </div>
              <div style={{ height: 9, borderRadius: 999, background: "var(--bone-200)", overflow: "hidden" }}>
                <div
                  style={{
                    height: "100%",
                    borderRadius: 999,
                    background: theme.hex,
                    width: `${Math.round(pct * 100)}%`,
                    transition: "width 0.5s var(--ease-out)",
                  }}
                />
              </div>
            </div>
          </div>
        </div>

        {/* Evolution sequence */}
        <div
          style={{
            marginTop: 18,
            padding: "24px 30px 56px",
            borderRadius: "var(--radius-card)",
            background: "var(--surface-card-solid)",
            border: "1px solid var(--border-subtle)",
            boxShadow: "var(--glow-soft)",
          }}
        >
          <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
            <span className="armali-eyebrow" style={{ fontSize: 10 }}>
              Evolution sequence
            </span>
            <span style={{ fontSize: 12, color: "var(--text-muted)" }}>
              {reachedCount} of {stages.length} stages reached
            </span>
          </div>
          {stages.length === 0 ? (
            <p style={{ marginTop: 16, fontSize: 13, color: "var(--text-muted)" }}>
              This district's world template defines no evolution stages yet.
            </p>
          ) : (
            <div style={{ position: "relative", height: 8, margin: "44px 14px 0", borderRadius: 999, background: "var(--bone-200)" }}>
              <div
                style={{
                  position: "absolute",
                  left: 0,
                  top: 0,
                  height: "100%",
                  borderRadius: 999,
                  background: theme.hex,
                  width: `${timelinePct}%`,
                  transition: "width 0.5s var(--ease-out)",
                }}
              />
              {stages.map((stage, index) => {
                const reached = stage.order <= level;
                const leftPct = stages.length === 1 ? 50 : (index / (stages.length - 1)) * 100;
                const Icon = STAGE_ICON[stage.kind];
                return (
                  <div key={stage.evolutionStageId}>
                    <div
                      style={{
                        position: "absolute",
                        top: "50%",
                        left: `${leftPct}%`,
                        transform: "translate(-50%,-50%)",
                        width: reached ? 18 : 14,
                        height: reached ? 18 : 14,
                        borderRadius: "50%",
                        border: "3px solid var(--surface-card-solid)",
                        background: reached ? theme.hex : "rgba(124,110,86,0.30)",
                        boxShadow: reached ? theme.glow : "none",
                        zIndex: 2,
                      }}
                    />
                    <div
                      style={{
                        position: "absolute",
                        top: 26,
                        left: `${leftPct}%`,
                        transform: "translateX(-50%)",
                        width: 78,
                        textAlign: "center",
                      }}
                    >
                      <Icon size={14} color={reached ? "var(--text-primary)" : "var(--text-muted)"} aria-hidden />
                      <div
                        style={{
                          fontFamily: "var(--font-display)",
                          fontWeight: 600,
                          fontSize: 11.5,
                          lineHeight: 1.2,
                          marginTop: 3,
                          color: reached ? "var(--text-primary)" : "var(--text-muted)",
                        }}
                      >
                        {stageLabel(stage)}
                      </div>
                      <div style={{ fontSize: 10, color: "var(--text-muted)", marginTop: 1 }}>Lv {stage.order}</div>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>

        {/* Bottom grid */}
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 18, marginTop: 18 }}>
          <WhoAndWhatCard area={area} />
          <DistrictQuestsCard
            area={area}
            daily={dailyHere}
            weekly={weeklyHere}
            onToggleDaily={(quest) => void toggleDaily(quest)}
            onToggleWeekly={(quest) => void toggleWeekly(quest)}
          />
        </div>
      </div>
    </div>
  );
}

function WhoAndWhatCard({ area }: { area: AreaView }) {
  const builtCount = area.builtPlots.length;
  return (
    <div
      style={{
        padding: "22px 24px",
        borderRadius: "var(--radius-card)",
        background: "var(--surface-card-solid)",
        border: "1px solid var(--border-subtle)",
        boxShadow: "var(--glow-soft)",
      }}
    >
      <span className="armali-eyebrow" style={{ fontSize: 10 }}>
        Who &amp; what is here
      </span>
      <div style={{ display: "flex", alignItems: "baseline", gap: 8, marginTop: 12 }}>
        <span style={{ fontFamily: "var(--font-display)", fontWeight: 600, fontSize: 30, color: area.theme.hex }}>
          {builtCount}
        </span>
        <span style={{ fontSize: 14, color: "var(--text-secondary)" }}>
          {builtCount === 1 ? "building" : "buildings"} of ~{area.plotCount} plots
        </span>
      </div>
      <div style={{ display: "flex", flexWrap: "wrap", gap: 8, marginTop: 16 }}>
        {area.denizens.length === 0 && (
          <span style={{ fontSize: 12.5, color: "var(--text-muted)" }}>No denizens have arrived yet.</span>
        )}
        {area.denizens.map((denizen) => (
          <span
            key={denizen.denizenType}
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 7,
              padding: "7px 12px",
              borderRadius: 999,
              background: area.theme.soft,
            }}
          >
            <UsersRound size={14} color={area.theme.hex} aria-hidden />
            <span style={{ fontFamily: "var(--font-display)", fontWeight: 600, fontSize: 13, color: area.theme.hex }}>
              {denizen.count}
            </span>
            <span style={{ fontSize: 12.5, color: "var(--text-secondary)" }}>{denizen.denizenType}</span>
          </span>
        ))}
      </div>
      <p style={{ marginTop: 16, fontSize: 12.5, color: "var(--text-muted)", lineHeight: 1.5 }}>
        Buildings keep their spot for the whole era. Denizens are counted, not placed — they wander to new spots each
        time you open the world.
      </p>
    </div>
  );
}

function DistrictQuestsCard({
  area,
  daily,
  weekly,
  onToggleDaily,
  onToggleWeekly,
}: {
  area: AreaView;
  daily: DailyQuest[];
  weekly: WeeklyQuest[];
  onToggleDaily: (quest: DailyQuest) => void;
  onToggleWeekly: (quest: WeeklyQuest) => void;
}) {
  const rows = [
    ...daily.map((quest) => ({
      key: `d-${quest.dailyHabitId}`,
      label: quest.label,
      xp: quest.xp,
      completed: quest.completed,
      onToggle: () => onToggleDaily(quest),
    })),
    ...weekly.map((quest) => ({
      key: `w-${quest.weeklyGoalId}`,
      label: quest.label,
      xp: quest.xp,
      completed: quest.completed,
      onToggle: () => onToggleWeekly(quest),
    })),
  ];

  return (
    <div
      style={{
        padding: "22px 24px",
        borderRadius: "var(--radius-card)",
        background: "var(--surface-card-solid)",
        border: "1px solid var(--border-subtle)",
        boxShadow: "var(--glow-soft)",
      }}
    >
      <span className="armali-eyebrow" style={{ fontSize: 10 }}>
        This district's quests
      </span>
      <div style={{ display: "flex", flexDirection: "column", gap: 9, marginTop: 12 }}>
        {rows.length === 0 && (
          <p style={{ fontSize: 13, color: "var(--text-muted)" }}>No quests target this district right now.</p>
        )}
        {rows.map((row) => (
          <button
            key={row.key}
            type="button"
            className="belf-quest-row"
            onClick={row.onToggle}
            style={{ background: row.completed ? area.theme.soft : "var(--bone-50)" }}
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
                border: `2px solid ${row.completed ? area.theme.hex : "var(--border-strong)"}`,
                background: row.completed ? area.theme.hex : "transparent",
                transition: "all 0.2s var(--ease-spring)",
              }}
            >
              {row.completed && (
                <span style={{ width: 7, height: 7, borderRadius: 2, background: "#fff" }} />
              )}
            </span>
            <span
              style={{
                flex: 1,
                minWidth: 0,
                textAlign: "left",
                fontSize: 14,
                fontWeight: 600,
                color: row.completed ? "var(--text-muted)" : "var(--text-primary)",
                textDecoration: row.completed ? "line-through" : "none",
              }}
            >
              {row.label}
            </span>
            <span style={{ fontFamily: "var(--font-display)", fontWeight: 600, fontSize: 12, color: area.theme.hex }}>
              +{row.xp}
            </span>
          </button>
        ))}
      </div>
    </div>
  );
}
