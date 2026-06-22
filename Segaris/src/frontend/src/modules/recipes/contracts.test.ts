import { describe, expect, it } from 'vitest'

import {
  recipeMenusRoutePath,
  recipePageSizes,
  recipesRoutePath,
} from '@/app/api/recipes'

import { recipeRequestSchema, recipesKeys, weeklyMenuRequestSchema } from './contracts'
import {
  addWeeks,
  currentWeekMonday,
  mondayOf,
  parseMenuDialogState,
  parseWeek,
} from './menusState'
import {
  defaultPageSize,
  defaultSort,
  defaultSortDirection,
  parseRecipeDialogState,
  parseRecipesState,
  toListQuery,
} from './recipesState'

describe('recipes contracts', () => {
  it('freezes route and pagination constants', () => {
    expect(recipesRoutePath).toBe('/recipes')
    expect(recipeMenusRoutePath).toBe('/recipes/menus')
    expect(recipePageSizes).toEqual([10, 25, 50, 100])
    expect(defaultPageSize).toBe(25)
    expect(defaultSort).toBe('name')
    expect(defaultSortDirection).toBe('asc')
  })

  it('freezes query keys for recipes, catalogues, attachments, and menus', () => {
    expect(recipesKeys.categories()).toEqual(['recipes', 'categories'])
    expect(recipesKeys.recipeList({ page: 1 })).toEqual([
      'recipes',
      'recipes',
      'list',
      { page: 1 },
    ])
    expect(recipesKeys.recipe(12)).toEqual(['recipes', 'recipes', 12])
    expect(recipesKeys.recipeAttachments(12)).toEqual([
      'recipes',
      'recipes',
      12,
      'attachments',
    ])
    expect(recipesKeys.menuWeek('2026-06-22')).toEqual([
      'recipes',
      'menus',
      'week',
      '2026-06-22',
    ])
    expect(recipesKeys.menu(45)).toEqual(['recipes', 'menus', 45])
  })

  it('parses URL-backed gallery state with defaults and bounded values', () => {
    const state = parseRecipesState(
      new URLSearchParams(
        'search=soup&category=2&difficulty=Hard&visibility=Private&creator=9&sort=category&sortDirection=desc&page=3&pageSize=50',
      ),
      9,
    )

    expect(state).toEqual({
      search: 'soup',
      category: 2,
      difficulty: 'Hard',
      visibility: 'Private',
      mine: true,
      sort: 'category',
      sortDirection: 'desc',
      page: 3,
      pageSize: 50,
    })
    expect(toListQuery(state, 9)).toEqual({
      search: 'soup',
      category: 2,
      difficulty: 'Hard',
      visibility: 'Private',
      creator: 9,
      page: 3,
      pageSize: 50,
      sort: 'category',
      sortDirection: 'desc',
    })
  })

  it('parses recipe dialog state separately from gallery state', () => {
    expect(parseRecipeDialogState(new URLSearchParams('newRecipe=true'))).toEqual({
      mode: 'create',
    })
    expect(parseRecipeDialogState(new URLSearchParams('recipeId=12'))).toEqual({
      mode: 'edit',
      recipeId: 12,
    })
    expect(parseRecipeDialogState(new URLSearchParams('recipeId=0'))).toEqual({
      mode: 'closed',
    })
  })

  it('validates recipe request boundaries', () => {
    const request = recipeRequestSchema.parse({
      name: '  Tortilla  ',
      categoryId: 1,
      difficulty: 'Easy',
      servings: 4,
      preparationMinutes: 10,
      cookMinutes: 15,
      ingredients: [
        { name: '  Egg  ', quantity: '4 units', itemId: 7 },
        { name: 'Salt', quantity: '', itemId: null },
      ],
      steps: [{ instruction: 'Beat the eggs' }],
      notes: '',
      visibility: 'Public',
    })

    expect(request.name).toBe('Tortilla')
    expect(request.ingredients[0].name).toBe('Egg')
    expect(request.ingredients[1].quantity).toBeNull()
    expect(request.notes).toBeNull()
    expect(
      recipeRequestSchema.safeParse({ ...request, difficulty: 'Extreme' }).success,
    ).toBe(false)
  })

  it('validates menu request boundaries and the fixed slot grid vocabulary', () => {
    const request = weeklyMenuRequestSchema.parse({
      week: '2026-06-22',
      name: '',
      visibility: 'Public',
      slots: [{ day: 'Monday', slot: 'Lunch', recipeIds: [1, 2] }],
    })

    expect(request.name).toBeNull()
    expect(request.slots[0].slot).toBe('Lunch')
    expect(
      weeklyMenuRequestSchema.safeParse({
        ...request,
        slots: [{ day: 'Funday', slot: 'Lunch', recipeIds: [] }],
      }).success,
    ).toBe(false)
    expect(
      weeklyMenuRequestSchema.safeParse({
        ...request,
        slots: [{ day: 'Monday', slot: 'Brunch', recipeIds: [] }],
      }).success,
    ).toBe(false)
  })
})

describe('menu week navigation', () => {
  it('anchors any civil date to the Monday of its ISO week', () => {
    expect(mondayOf('2026-06-22')).toBe('2026-06-22') // Monday
    expect(mondayOf('2026-06-24')).toBe('2026-06-22') // Wednesday
    expect(mondayOf('2026-06-28')).toBe('2026-06-22') // Sunday
    expect(mondayOf('2026-06-29')).toBe('2026-06-29') // next Monday
  })

  it('steps whole weeks forwards and backwards', () => {
    expect(addWeeks('2026-06-22', 1)).toBe('2026-06-29')
    expect(addWeeks('2026-06-22', -1)).toBe('2026-06-15')
  })

  it('parses the week parameter, normalising to Monday and defaulting to the current week', () => {
    expect(parseWeek(new URLSearchParams('week=2026-06-24'))).toBe('2026-06-22')
    expect(parseWeek(new URLSearchParams(''), new Date(2026, 5, 24))).toBe(
      currentWeekMonday(new Date(2026, 5, 24)),
    )
  })

  it('parses menu dialog state separately from week state', () => {
    expect(parseMenuDialogState(new URLSearchParams('newMenu=true'))).toEqual({
      mode: 'create',
    })
    expect(parseMenuDialogState(new URLSearchParams('menuId=45'))).toEqual({
      mode: 'edit',
      menuId: 45,
    })
    expect(parseMenuDialogState(new URLSearchParams('menuId=0'))).toEqual({
      mode: 'closed',
    })
  })
})
