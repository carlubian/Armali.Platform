import { del, get, getOrNull, post } from "./client";
import type {
  DailyQuest,
  EraDetail,
  ProgressionSummary,
  QuestCompletion,
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
