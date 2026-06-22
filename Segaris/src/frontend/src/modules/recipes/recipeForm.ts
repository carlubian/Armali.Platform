import { z } from 'zod'

import type {
  CreateRecipeRequest,
  Recipe,
  RecipeDifficulty,
  RecipeVisibility,
} from '@/app/api/recipes'

export interface RecipeIngredientFormValues {
  key: string
  name: string
  quantity: string
  itemId: number | null
  itemName: string
}

export interface RecipeStepFormValues {
  key: string
  instruction: string
}

export interface RecipeFormValues {
  name: string
  categoryId: string
  difficulty: RecipeDifficulty | ''
  servings: string
  preparationMinutes: string
  cookMinutes: string
  ingredients: RecipeIngredientFormValues[]
  steps: RecipeStepFormValues[]
  notes: string
  visibility: RecipeVisibility
}

export interface RecipeValidationMessages {
  nameRequired: string
  nameTooLong: string
  categoryRequired: string
  positiveNumber: string
  nonNegativeNumber: string
  ingredientNameRequired: string
  ingredientNameTooLong: string
  quantityTooLong: string
  stepRequired: string
  stepTooLong: string
  notesTooLong: string
}

let nextKey = 0
const makeKey = () => `recipe-field-${nextKey++}`

function optionalPositiveInt(message: string) {
  return z
    .string()
    .trim()
    .refine(
      (value) => value === '' || (/^\d+$/.test(value) && Number(value) > 0),
      message,
    )
}

function optionalNonNegativeInt(message: string) {
  return z
    .string()
    .trim()
    .refine((value) => value === '' || /^\d+$/.test(value), message)
}

export function createRecipeFormSchema(messages: RecipeValidationMessages) {
  return z.object({
    name: z.string().trim().min(1, messages.nameRequired).max(200, messages.nameTooLong),
    categoryId: z.string().trim().min(1, messages.categoryRequired),
    difficulty: z.union([z.literal(''), z.enum(['Easy', 'Medium', 'Hard'])]),
    servings: optionalPositiveInt(messages.positiveNumber),
    preparationMinutes: optionalNonNegativeInt(messages.nonNegativeNumber),
    cookMinutes: optionalNonNegativeInt(messages.nonNegativeNumber),
    ingredients: z.array(
      z.object({
        key: z.string(),
        name: z
          .string()
          .trim()
          .min(1, messages.ingredientNameRequired)
          .max(200, messages.ingredientNameTooLong),
        quantity: z.string().trim().max(100, messages.quantityTooLong),
        itemId: z.number().int().positive().nullable(),
        itemName: z.string(),
      }),
    ),
    steps: z.array(
      z.object({
        key: z.string(),
        instruction: z
          .string()
          .trim()
          .min(1, messages.stepRequired)
          .max(1000, messages.stepTooLong),
      }),
    ),
    notes: z.string().trim().max(2000, messages.notesTooLong),
    visibility: z.enum(['Public', 'Private']),
  })
}

export function buildDefaults(categoryId: string): RecipeFormValues {
  return {
    name: '',
    categoryId,
    difficulty: '',
    servings: '',
    preparationMinutes: '',
    cookMinutes: '',
    ingredients: [],
    steps: [],
    notes: '',
    visibility: 'Public',
  }
}

export function fromRecipe(recipe: Recipe): RecipeFormValues {
  return {
    name: recipe.name,
    categoryId: String(recipe.categoryId),
    difficulty: recipe.difficulty ?? '',
    servings: recipe.servings == null ? '' : String(recipe.servings),
    preparationMinutes:
      recipe.preparationMinutes == null ? '' : String(recipe.preparationMinutes),
    cookMinutes: recipe.cookMinutes == null ? '' : String(recipe.cookMinutes),
    ingredients: recipe.ingredients
      .slice()
      .sort((a, b) => a.position - b.position)
      .map((ingredient) => ({
        key: makeKey(),
        name: ingredient.name,
        quantity: ingredient.quantity ?? '',
        itemId: ingredient.itemId,
        itemName: ingredient.itemName ?? '',
      })),
    steps: recipe.steps
      .slice()
      .sort((a, b) => a.position - b.position)
      .map((step) => ({
        key: makeKey(),
        instruction: step.instruction,
      })),
    notes: recipe.notes ?? '',
    visibility: recipe.visibility,
  }
}

export function newIngredient(): RecipeIngredientFormValues {
  return { key: makeKey(), name: '', quantity: '', itemId: null, itemName: '' }
}

export function newStep(): RecipeStepFormValues {
  return { key: makeKey(), instruction: '' }
}

export function toRequest(values: RecipeFormValues): CreateRecipeRequest {
  const numberOrNull = (value: string): number | null =>
    value.trim() === '' ? null : Number(value)
  const textOrNull = (value: string): string | null => {
    const text = value.trim()
    return text === '' ? null : text
  }

  return {
    name: values.name.trim(),
    categoryId: Number(values.categoryId),
    difficulty: values.difficulty === '' ? null : values.difficulty,
    servings: numberOrNull(values.servings),
    preparationMinutes: numberOrNull(values.preparationMinutes),
    cookMinutes: numberOrNull(values.cookMinutes),
    ingredients: values.ingredients.map((ingredient) => ({
      name: ingredient.name.trim(),
      quantity: textOrNull(ingredient.quantity),
      itemId: ingredient.itemId,
    })),
    steps: values.steps.map((step) => ({ instruction: step.instruction.trim() })),
    notes: textOrNull(values.notes),
    visibility: values.visibility,
  }
}
