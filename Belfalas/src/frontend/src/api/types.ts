// TypeScript mirrors of the backend contracts (Belfalas.Api/Contracts/*.cs).
// ASP.NET minimal APIs serialize with the web defaults, so every field is camelCase.

export type EraStatus = "Active" | "Archived";

export interface AreaResponse {
  id: string;
  name: string;
  order: number;
  districtId: string | null;
}

export interface DailyHabitResponse {
  id: string;
  eraId: string;
  areaId: string;
  areaName: string;
  label: string;
  xp: number;
}

export interface WeeklyGoalResponse {
  id: string;
  eraId: string;
  areaId: string;
  areaName: string;
  label: string;
  xp: number;
}

export interface EraDetail {
  id: string;
  name: string;
  startDate: string; // ISO date (yyyy-MM-dd)
  weeks: number;
  status: EraStatus;
  templateId: string;
  xpPerLevel: number;
  areas: AreaResponse[];
  dailyHabits: DailyHabitResponse[];
  weeklyGoals: WeeklyGoalResponse[];
}

export interface DailyQuest {
  dailyHabitId: string;
  areaId: string;
  areaName: string;
  label: string;
  xp: number;
  completed: boolean;
}

export interface WeeklyQuest {
  weeklyGoalId: string;
  areaId: string;
  areaName: string;
  label: string;
  xp: number;
  completed: boolean;
}

export interface WeeklySet {
  id: string;
  eraId: string;
  weekIndex: number;
  goals: WeeklyQuest[];
}

export interface QuestCompletion {
  areaId: string;
  areaName: string;
  completed: boolean;
  xpDelta: number;
  areaXp: number;
  areaLevel: number;
  previousLevel: number;
  levelChanged: boolean;
}

export interface AreaProgress {
  areaId: string;
  areaName: string;
  order: number;
  level: number;
  xp: number;
  xpPerLevel: number;
  xpIntoLevel: number;
  xpForNextLevel: number;
  maxLevel: number;
  isComplete: boolean;
}

export interface ProgressionSummary {
  eraId: string;
  eraName: string;
  globalLevel: number;
  maxLevel: number;
  areas: AreaProgress[];
}

// ---- World ---------------------------------------------------------------

export type EvolutionStageKind = "Building" | "Denizen" | "Upgrade";

export interface BuiltPlot {
  builtPlotId: string;
  plotId: string;
  category: string;
  positionX: number;
  positionY: number;
  variantId: string;
  spriteKey: string;
}

export interface DenizenCount {
  denizenType: string;
  count: number;
}

export interface WorldDistrictState {
  districtId: string;
  districtName: string;
  slot: number;
  areaId: string | null;
  areaName: string | null;
  areaLevel: number;
  builtPlots: BuiltPlot[];
  denizens: DenizenCount[];
}

export interface WorldState {
  eraId: string;
  eraName: string;
  templateId: string;
  districts: WorldDistrictState[];
}

export interface WorldTemplatePlot {
  plotId: string;
  category: string;
  positionX: number;
  positionY: number;
}

export interface WorldTemplateVariant {
  variantId: string;
  category: string;
  spriteKey: string;
}

export interface WorldTemplateEvolutionStage {
  evolutionStageId: string;
  order: number;
  kind: EvolutionStageKind;
  denizenType: string | null;
}

export interface WorldTemplateDistrict {
  districtId: string;
  name: string;
  slot: number;
  plots: WorldTemplatePlot[];
  evolutionStages: WorldTemplateEvolutionStage[];
}

export interface WorldTemplate {
  id: string;
  theme: string;
  name: string;
  districts: WorldTemplateDistrict[];
  variants: WorldTemplateVariant[];
}
