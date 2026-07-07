import { z } from 'zod'

import {
  gamePlatforms,
  playthroughStatuses,
  sectionColors,
  type GamePlatform,
  type GameRequest,
  type GoalRequest,
  type PlaythroughListQuery,
  type PlaythroughRequest,
  type PlaythroughStatus,
  type SectionColor,
  type SectionRequest,
} from '@/app/api/games'

export const gamesKeys = {
  all: ['games'] as const,
  games: () => [...gamesKeys.all, 'games'] as const,
  playthroughs: () => [...gamesKeys.all, 'playthroughs'] as const,
  playthroughList: (query: PlaythroughListQuery) =>
    [...gamesKeys.playthroughs(), 'list', query] as const,
  playthrough: (playthroughId: number) =>
    [...gamesKeys.playthroughs(), playthroughId] as const,
  sections: (playthroughId: number) =>
    [...gamesKeys.playthrough(playthroughId), 'sections'] as const,
  goals: (playthroughId: number, sectionId: number) =>
    [...gamesKeys.sections(playthroughId), sectionId, 'goals'] as const,
}

const platformValues = gamePlatforms as [GamePlatform, ...GamePlatform[]]
const statusValues = playthroughStatuses as [PlaythroughStatus, ...PlaythroughStatus[]]
const colorValues = sectionColors as [SectionColor, ...SectionColor[]]

export const gameRequestSchema = z.object({
  name: z.string().trim().min(1).max(200),
  platform: z.enum(platformValues),
}) satisfies z.ZodType<GameRequest>

export const playthroughRequestSchema = z.object({
  name: z.string().trim().min(1).max(200),
  gameId: z.number().int().positive(),
  startYear: z.number().int().min(1).max(9999),
  startMonth: z.number().int().min(1).max(12),
  status: z.enum(statusValues),
  tags: z.array(z.string().trim().max(100)),
  visibility: z.enum(['Public', 'Private']),
}) satisfies z.ZodType<PlaythroughRequest>

export const sectionRequestSchema = z.object({
  name: z.string().trim().min(1).max(200),
  color: z.enum(colorValues),
}) satisfies z.ZodType<SectionRequest>

export const goalRequestSchema = z.object({
  text: z.string().trim().min(1).max(500),
}) satisfies z.ZodType<GoalRequest>
