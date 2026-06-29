import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import {
  completeDaily,
  completeWeekly,
  getActiveEra,
  getDailyQuests,
  getProgressionSummary,
  getWeeklySet,
  getWorldState,
  getWorldTemplates,
  uncompleteDaily,
  uncompleteWeekly,
} from "../api/endpoints";
import type {
  AreaProgress,
  DailyQuest,
  EraDetail,
  ProgressionSummary,
  QuestCompletion,
  WeeklySet,
  WorldDistrictState,
  WorldState,
  WorldTemplate,
  WorldTemplateDistrict,
} from "../api/types";
import { getAreaTheme, type AreaTheme } from "../lib/areaTheme";

/** A single area joined across progression, world state and the world template. */
export interface AreaView extends AreaProgress {
  theme: AreaTheme;
  districtId: string | null;
  districtName: string;
  builtPlots: WorldDistrictState["builtPlots"];
  denizens: WorldDistrictState["denizens"];
  evolutionStages: WorldTemplateDistrict["evolutionStages"];
  plotCount: number;
}

export interface Celebration {
  areaId: string;
  level: number;
}

interface EraData {
  loading: boolean;
  error: string | null;
  /** True once loaded and there is no active era to show. */
  hasActiveEra: boolean;
  era: EraDetail | null;
  progression: ProgressionSummary | null;
  daily: DailyQuest[];
  weekly: WeeklySet | null;
  world: WorldState | null;
  /** The world template instanced by the active era, joined from the catalogue. */
  template: WorldTemplate | null;
  areas: AreaView[];
  weekNumber: number; // 1-based for display
  weekCount: number;
  celebration: Celebration | null;
  dismissCelebration: () => void;
  refresh: () => Promise<void>;
  toggleDaily: (quest: DailyQuest) => Promise<void>;
  toggleWeekly: (quest: { weeklyGoalId: string; completed: boolean }) => Promise<void>;
}

const EraDataContext = createContext<EraData | null>(null);

function buildAreaViews(
  progression: ProgressionSummary | null,
  world: WorldState | null,
  templates: WorldTemplate[],
): AreaView[] {
  if (!progression) {
    return [];
  }

  const districtByArea = new Map<string, WorldDistrictState>();
  for (const district of world?.districts ?? []) {
    if (district.areaId) {
      districtByArea.set(district.areaId, district);
    }
  }

  const template = templates.find((candidate) => candidate.id === world?.templateId);
  const templateDistrictById = new Map<string, WorldTemplateDistrict>();
  for (const district of template?.districts ?? []) {
    templateDistrictById.set(district.districtId, district);
  }

  const sorted = [...progression.areas].sort((a, b) => a.order - b.order);
  return sorted.map((area, index) => {
    const district = districtByArea.get(area.areaId);
    const templateDistrict = district ? templateDistrictById.get(district.districtId) : undefined;
    return {
      ...area,
      theme: getAreaTheme(index),
      districtId: district?.districtId ?? null,
      districtName: district?.districtName ?? area.areaName,
      builtPlots: district?.builtPlots ?? [],
      denizens: district?.denizens ?? [],
      evolutionStages: templateDistrict?.evolutionStages ?? [],
      plotCount: templateDistrict?.plots.length ?? 0,
    };
  });
}

export function EraDataProvider({ children }: { children: ReactNode }) {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [loaded, setLoaded] = useState(false);

  const [era, setEra] = useState<EraDetail | null>(null);
  const [progression, setProgression] = useState<ProgressionSummary | null>(null);
  const [daily, setDaily] = useState<DailyQuest[]>([]);
  const [weekly, setWeekly] = useState<WeeklySet | null>(null);
  const [world, setWorld] = useState<WorldState | null>(null);
  const [templates, setTemplates] = useState<WorldTemplate[]>([]);
  const [celebration, setCelebration] = useState<Celebration | null>(null);

  const loadDynamic = useCallback(async () => {
    const [eraResult, progressionResult, dailyResult, weeklyResult, worldResult] = await Promise.all([
      getActiveEra(),
      getProgressionSummary(),
      getDailyQuests(),
      getWeeklySet(),
      getWorldState(),
    ]);
    setEra(eraResult);
    setProgression(progressionResult);
    setDaily(dailyResult ?? []);
    setWeekly(weeklyResult);
    setWorld(worldResult);
  }, []);

  const initialLoad = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [, templatesResult] = await Promise.all([loadDynamic(), getWorldTemplates()]);
      setTemplates(templatesResult);
      setLoaded(true);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Could not reach Belfalas.");
    } finally {
      setLoading(false);
    }
  }, [loadDynamic]);

  useEffect(() => {
    void initialLoad();
  }, [initialLoad]);

  const refresh = useCallback(async () => {
    try {
      await loadDynamic();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Could not refresh.");
    }
  }, [loadDynamic]);

  const handleCompletion = useCallback((result: QuestCompletion) => {
    // A level-up (only ever when completing, never when un-completing) is celebrated.
    if (result.levelChanged && result.completed && result.areaLevel > result.previousLevel) {
      setCelebration({ areaId: result.areaId, level: result.areaLevel });
    }
  }, []);

  const toggleDaily = useCallback(
    async (quest: DailyQuest) => {
      const next = !quest.completed;
      setDaily((current) =>
        current.map((item) =>
          item.dailyHabitId === quest.dailyHabitId ? { ...item, completed: next } : item,
        ),
      );
      try {
        const result = next
          ? await completeDaily(quest.dailyHabitId)
          : await uncompleteDaily(quest.dailyHabitId);
        handleCompletion(result);
        await refresh();
      } catch (cause) {
        // Revert the optimistic flip and surface the failure.
        setDaily((current) =>
          current.map((item) =>
            item.dailyHabitId === quest.dailyHabitId ? { ...item, completed: quest.completed } : item,
          ),
        );
        setError(cause instanceof Error ? cause.message : "Could not update the quest.");
      }
    },
    [handleCompletion, refresh],
  );

  const toggleWeekly = useCallback(
    async (quest: { weeklyGoalId: string; completed: boolean }) => {
      if (!weekly) {
        return;
      }
      const next = !quest.completed;
      setWeekly((current) =>
        current
          ? {
              ...current,
              goals: current.goals.map((item) =>
                item.weeklyGoalId === quest.weeklyGoalId ? { ...item, completed: next } : item,
              ),
            }
          : current,
      );
      try {
        const result = next
          ? await completeWeekly(quest.weeklyGoalId, weekly.weekIndex)
          : await uncompleteWeekly(quest.weeklyGoalId);
        handleCompletion(result);
        await refresh();
      } catch (cause) {
        setWeekly((current) =>
          current
            ? {
                ...current,
                goals: current.goals.map((item) =>
                  item.weeklyGoalId === quest.weeklyGoalId
                    ? { ...item, completed: quest.completed }
                    : item,
                ),
              }
            : current,
        );
        setError(cause instanceof Error ? cause.message : "Could not update the goal.");
      }
    },
    [weekly, handleCompletion, refresh],
  );

  const areas = useMemo(
    () => buildAreaViews(progression, world, templates),
    [progression, world, templates],
  );

  const template = useMemo(
    () => templates.find((candidate) => candidate.id === world?.templateId) ?? null,
    [templates, world],
  );

  const value: EraData = useMemo(
    () => ({
      loading,
      error,
      hasActiveEra: loaded && era !== null,
      era,
      progression,
      daily,
      weekly,
      world,
      template,
      areas,
      weekNumber: (weekly?.weekIndex ?? 0) + 1,
      weekCount: era?.weeks ?? 0,
      celebration,
      dismissCelebration: () => setCelebration(null),
      refresh,
      toggleDaily,
      toggleWeekly,
    }),
    [
      loading,
      error,
      loaded,
      era,
      progression,
      daily,
      weekly,
      world,
      template,
      areas,
      celebration,
      refresh,
      toggleDaily,
      toggleWeekly,
    ],
  );

  return <EraDataContext.Provider value={value}>{children}</EraDataContext.Provider>;
}

export function useEraData(): EraData {
  const value = useContext(EraDataContext);
  if (!value) {
    throw new Error("useEraData must be used within an EraDataProvider");
  }
  return value;
}
