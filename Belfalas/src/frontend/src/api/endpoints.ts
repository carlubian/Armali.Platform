import { del, get, getOrNull, post, put } from "./client";
import type {
  CreateEraRequest,
  DailyHabitResponse,
  DailyQuest,
  EraDetail,
  EraSummary,
  OverrideWeeklySet,
  ProgressionSummary,
  QuestCompletion,
  UpsertDailyHabit,
  UpsertWeeklyGoal,
  WeeklyGoalResponse,
  WeeklySet,
  WorldState,
  WorldTemplate,
} from "./types";

// Resources that only exist while an era is active resolve to null on 404.
export const getActiveEra = () => getOrNull<EraDetail>("/eras/active");
export const getProgressionSummary = () => getOrNull<ProgressionSummary>("/progression/summary");
export const getDailyQuests = () => getOrNull<DailyQuest[]>("/quests/daily");
export const getWeeklySet = () => getOrNull<WeeklySet>("/quests/weekly");
export const getWorldState = () => getOrNull<WorldState>("/world");

export const getWorldTemplates = () => get<WorldTemplate[]>("/world/templates");

export const completeDaily = (dailyHabitId: string) =>
  post<QuestCompletion>(`/quests/daily/${dailyHabitId}/complete`, {});

export const uncompleteDaily = (dailyHabitId: string) =>
  del<QuestCompletion>(`/quests/daily/${dailyHabitId}/complete`);

export const completeWeekly = (weeklyGoalId: string, weekIndex: number) =>
  post<QuestCompletion>(`/quests/weekly/${weeklyGoalId}/complete`, { weekIndex });

export const uncompleteWeekly = (weeklyGoalId: string) =>
  del<QuestCompletion>(`/quests/weekly/${weeklyGoalId}/complete`);

// ---- Admin (era authoring) ------------------------------------------------

export const listEras = () => get<EraSummary[]>("/eras");

export const createEra = (request: CreateEraRequest) => post<EraDetail>("/eras", request);

export const archiveEra = (eraId: string) => post<EraDetail>(`/eras/${eraId}/archive`, {});

export const listDailyHabits = (eraId: string) =>
  get<DailyHabitResponse[]>(`/admin/eras/${eraId}/daily-habits`);

export const createDailyHabit = (eraId: string, request: UpsertDailyHabit) =>
  post<DailyHabitResponse>(`/admin/eras/${eraId}/daily-habits`, request);

export const updateDailyHabit = (dailyHabitId: string, request: UpsertDailyHabit) =>
  put<DailyHabitResponse>(`/admin/daily-habits/${dailyHabitId}`, request);

export const deleteDailyHabit = (dailyHabitId: string) =>
  del<void>(`/admin/daily-habits/${dailyHabitId}`);

export const listWeeklyGoals = (eraId: string) =>
  get<WeeklyGoalResponse[]>(`/admin/eras/${eraId}/weekly-goals`);

export const createWeeklyGoal = (eraId: string, request: UpsertWeeklyGoal) =>
  post<WeeklyGoalResponse>(`/admin/eras/${eraId}/weekly-goals`, request);

export const updateWeeklyGoal = (weeklyGoalId: string, request: UpsertWeeklyGoal) =>
  put<WeeklyGoalResponse>(`/admin/weekly-goals/${weeklyGoalId}`, request);

export const deleteWeeklyGoal = (weeklyGoalId: string) =>
  del<void>(`/admin/weekly-goals/${weeklyGoalId}`);

export const overrideWeeklySet = (eraId: string, weekIndex: number, weeklyGoalIds: string[]) =>
  put<WeeklySet>(`/admin/eras/${eraId}/weekly-sets/${weekIndex}`, {
    weeklyGoalIds,
  } satisfies OverrideWeeklySet);
