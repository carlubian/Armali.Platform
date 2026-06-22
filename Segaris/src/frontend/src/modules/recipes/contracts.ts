import { z } from 'zod'

import type {
  CreateRecipeRequest,
  CreateWeeklyMenuRequest,
  RecipeListQuery,
} from '@/app/api/recipes'

export const recipesKeys = {
  all: ['recipes'] as const,
  categories: () => [...recipesKeys.all, 'categories'] as const,
  recipes: () => [...recipesKeys.all, 'recipes'] as const,
  recipeList: (query: RecipeListQuery) =>
    [...recipesKeys.recipes(), 'list', query] as const,
  recipe: (recipeId: number) => [...recipesKeys.recipes(), recipeId] as const,
  recipeAttachments: (recipeId: number) =>
    [...recipesKeys.recipe(recipeId), 'attachments'] as const,
  menus: () => [...recipesKeys.all, 'menus'] as const,
  menuWeek: (week: string) => [...recipesKeys.menus(), 'week', week] as const,
  menu: (menuId: number) => [...recipesKeys.menus(), menuId] as const,
}

const optionalText = (max: number) =>
  z
    .string()
    .trim()
    .max(max)
    .transform((value) => (value.length === 0 ? null : value))
    .nullable()

const optionalPositiveInt = z.number().int().positive().nullable()

const optionalNonNegativeInt = z.number().int().nonnegative().nullable()

export const recipeIngredientSchema = z.object({
  name: z.string().trim().min(1).max(200),
  quantity: optionalText(100),
  itemId: z.number().int().positive().nullable(),
})

export const recipeStepSchema = z.object({
  instruction: z.string().trim().min(1).max(1000),
})

export const recipeRequestSchema = z.object({
  name: z.string().trim().min(1).max(200),
  categoryId: z.number().int().positive(),
  difficulty: z.enum(['Easy', 'Medium', 'Hard']).nullable(),
  servings: optionalPositiveInt,
  preparationMinutes: optionalNonNegativeInt,
  cookMinutes: optionalNonNegativeInt,
  ingredients: z.array(recipeIngredientSchema),
  steps: z.array(recipeStepSchema),
  notes: optionalText(2000),
  visibility: z.enum(['Public', 'Private']),
}) satisfies z.ZodType<CreateRecipeRequest>

export const weeklyMenuSlotSchema = z.object({
  day: z.enum([
    'Monday',
    'Tuesday',
    'Wednesday',
    'Thursday',
    'Friday',
    'Saturday',
    'Sunday',
  ]),
  slot: z.enum(['Breakfast', 'Lunch', 'Snack', 'Dinner']),
  recipeIds: z.array(z.number().int().positive()),
})

export const weeklyMenuRequestSchema = z.object({
  week: z.string().regex(/^\d{4}-\d{2}-\d{2}$/),
  name: optionalText(200),
  visibility: z.enum(['Public', 'Private']),
  slots: z.array(weeklyMenuSlotSchema),
}) satisfies z.ZodType<CreateWeeklyMenuRequest>

export const recipeCategoryRequestSchema = z.object({
  name: z.string().trim().min(1).max(200),
})
