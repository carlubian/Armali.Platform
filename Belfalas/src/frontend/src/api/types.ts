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

export interface EraSummary {
  id: string;
  name: string;
  startDate: string; // ISO date (yyyy-MM-dd)
  weeks: number;
  status: EraStatus;
  templateId: string;
  xpPerLevel: number;
}

// ---- Admin request payloads (mirror Belfalas.Api/Contracts/*.cs) ---------

export interface CreateAreaDraft {
  name: string;
  order: number;
}

export interface CreateDailyHabitDraft {
  areaOrder: number;
  label: string;
  xp: number;
}

export interface CreateWeeklyGoalDraft {
  areaOrder: number;
  label: string;
  xp: number;
}

export interface CreateEraRequest {
  name: string;
  startDate: string; // yyyy-MM-dd
  weeks: number;
  templateId: string;
  areas: CreateAreaDraft[];
  dailyHabits?: CreateDailyHabitDraft[];
  weeklyGoals?: CreateWeeklyGoalDraft[];
  xpPerLevel: number;
}

export interface UpsertDailyHabit {
  areaId: string;
  label: string;
  xp: number;
}

export interface UpsertWeeklyGoal {
  areaId: string;
  label: string;
  xp: number;
}

export interface OverrideWeeklySet {
  weeklyGoalIds: string[];
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

export interface WorldTemplateDenizenSocket {
  denizenSocketId: string;
  positionX: number;
  positionY: number;
  anchorX: number;
  anchorY: number;
  sortOffsetY: number;
  compatibleDenizenTypes: string[];
}

export interface WorldTemplateCategoryContract {
  categoryContractId: string;
  category: string;
  footprintWidth: number;
  footprintHeight: number;
  anchorX: number;
  anchorY: number;
  sortOffsetY: number;
  supportsDenizens: boolean;
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
  denizenSockets: WorldTemplateDenizenSocket[];
  evolutionStages: WorldTemplateEvolutionStage[];
}

export interface WorldTemplateRenderContract {
  tileWidth: number;
  tileHeight: number;
  mapWidth: number;
  mapHeight: number;
  originX: number;
  originY: number;
  cameraBounds: {
    minX: number;
    minY: number;
    maxX: number;
    maxY: number;
  };
  assetBasePath: string;
  atlasKey: string;
}

export interface WorldTemplate {
  id: string;
  theme: string;
  name: string;
  render: WorldTemplateRenderContract;
  districts: WorldTemplateDistrict[];
  categories: WorldTemplateCategoryContract[];
  variants: WorldTemplateVariant[];
}
