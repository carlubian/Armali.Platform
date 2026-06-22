import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import type {
  Recipe,
  RecipeSummary,
  WeeklyMenu,
  WeeklyMenuSummary,
} from '@/app/api/recipes'

const session = {
  userId: 7,
  userName: 'marina',
  displayName: 'Marina Velasco',
  language: 'en-GB',
  roles: ['User'],
  avatarUrl: null as string | null,
}

function json(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

function urlOf(input: RequestInfo | URL): string {
  return typeof input === 'string'
    ? input
    : input instanceof URL
      ? input.href
      : input.url
}

function makeRecipeSummary(
  id: number,
  overrides: Partial<RecipeSummary> = {},
): RecipeSummary {
  return {
    id,
    name: `Recipe ${id.toString().padStart(2, '0')}`,
    categoryId: 1,
    categoryName: 'Main',
    difficulty: 'Easy',
    visibility: 'Public',
    thumbnail: { attachmentId: null, url: null, source: 'placeholder' },
    creatorId: 7,
    creatorName: 'Marina Velasco',
    ...overrides,
  }
}

function makeRecipe(id: number, overrides: Partial<Recipe> = {}): Recipe {
  return {
    ...makeRecipeSummary(id),
    servings: 4,
    preparationMinutes: 10,
    cookMinutes: 20,
    ingredients: [
      {
        id: 1,
        name: 'Eggs',
        quantity: '4',
        itemId: null,
        itemName: null,
        position: 0,
      },
    ],
    steps: [{ id: 1, instruction: 'Cook gently', position: 0 }],
    notes: 'Family version',
    attachments: [],
    createdById: 7,
    createdByName: 'Marina Velasco',
    createdAt: '2026-06-22T10:00:00Z',
    updatedById: null,
    updatedByName: null,
    updatedAt: null,
    ...overrides,
  }
}

interface CreatedRecipeBody {
  name: string
  categoryId: number
}

interface BackendOptions {
  recipes?: RecipeSummary[]
  menus?: WeeklyMenuSummary[]
  menu?: WeeklyMenu
}

function mockBackend(options: BackendOptions = {}) {
  const recipes = options.recipes ?? [makeRecipeSummary(1)]
  const menus = options.menus ?? []
  const menu =
    options.menu ??
    ({
      id: 45,
      week: '2026-06-22',
      name: 'This week',
      visibility: 'Public',
      slots: [
        {
          day: 'Monday',
          slot: 'Breakfast',
          recipes: [
            {
              recipeId: 1,
              recipeName: 'Recipe 01',
              thumbnail: { attachmentId: null, url: null, source: 'placeholder' },
            },
          ],
        },
      ],
      createdById: 7,
      createdByName: 'Marina Velasco',
      createdAt: '2026-06-22T10:00:00Z',
      updatedById: null,
      updatedByName: null,
      updatedAt: null,
    } satisfies WeeklyMenu)
  const requests: Array<{ method: string; url: string; body?: unknown }> = []

  vi.spyOn(globalThis, 'fetch').mockImplementation(async (input, init) => {
    await Promise.resolve()
    const url = urlOf(input)
    const method = init?.method ?? 'GET'

    if (url === '/api/session/antiforgery') return json({ csrfToken: 'token' })
    if (url === '/api/session' && method === 'GET') return json(session)
    if (url === '/api/session/profile' && method === 'GET') {
      return json({
        displayName: session.displayName,
        language: session.language,
        avatarUrl: session.avatarUrl,
      })
    }
    if (url.startsWith('/api/launcher/attention')) return json({ modules: [] })
    if (url.startsWith('/api/recipes/categories')) {
      return json([
        { id: 1, name: 'Main', sortOrder: 1 },
        { id: 2, name: 'Dessert', sortOrder: 2 },
      ])
    }
    if (url.startsWith('/api/recipes/menus') && method === 'GET') {
      requests.push({ method, url })
      if (/\/api\/recipes\/menus\/\d+/.test(url)) return json(menu)
      return json(menus)
    }
    if (url === '/api/recipes/menus' && method === 'POST') {
      const bodyText = typeof init?.body === 'string' ? init.body : '{}'
      const body = JSON.parse(bodyText) as Record<string, unknown>
      requests.push({ method, url, body })
      return json({ ...menu, ...body, id: 99 })
    }
    if (url.startsWith('/api/recipes') && method === 'GET') {
      requests.push({ method, url })
      if (/\/api\/recipes\/\d+/.test(url)) return json(makeRecipe(1))
      const parsed = new URL(url, 'http://localhost')
      const search = parsed.searchParams.get('search')?.toLowerCase() ?? ''
      const filtered =
        search.length === 0
          ? recipes
          : recipes.filter((recipe) => recipe.name.toLowerCase().includes(search))
      return json({
        items: filtered,
        page: 1,
        pageSize: 25,
        totalCount: filtered.length,
      })
    }
    if (url === '/api/recipes' && method === 'POST') {
      const bodyText = typeof init?.body === 'string' ? init.body : '{}'
      const body = JSON.parse(bodyText) as CreatedRecipeBody
      requests.push({ method, url, body })
      return json(makeRecipe(8, { name: body.name, categoryId: body.categoryId }))
    }

    throw new Error(`Unexpected request: ${method} ${url}`)
  })

  return { requests }
}

beforeEach(() => {
  appQueryClient.clear()
  window.history.replaceState({}, '', '/recipes')
})

afterEach(() => vi.restoreAllMocks())

describe('Recipes collection', () => {
  it('renders the recipe gallery with filters and placeholder thumbnails', async () => {
    mockBackend({
      recipes: [makeRecipeSummary(1), makeRecipeSummary(2, { name: 'Lemon tart' })],
    })
    render(<App />)

    expect(await screen.findByText('Recipe 01')).toBeInTheDocument()
    expect(screen.getByText('Lemon tart')).toBeInTheDocument()
    expect(screen.getAllByText('No photo')).toHaveLength(2)
    expect(screen.getByLabelText('Search recipes')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /Menu planner/ })).toHaveAttribute(
      'href',
      '/recipes/menus',
    )
  })

  it('serializes the search term into the recipe request', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByText('Recipe 01')
    await user.type(screen.getByLabelText('Search recipes'), 'Recipe')

    await waitFor(() =>
      expect(requests.some((request) => request.url.includes('search=Recipe'))).toBe(
        true,
      ),
    )
  })

  it('creates a recipe with ordered ingredients and steps', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend({ recipes: [] })
    render(<App />)

    await screen.findByText('No recipes yet. Add the first dish to the cookbook.')
    await user.click(screen.getByRole('button', { name: 'New recipe' }))

    const dialog = await screen.findByRole('dialog', { name: 'New recipe' })
    await user.type(within(dialog).getByLabelText('Name'), 'Tortilla')
    await user.click(within(dialog).getByRole('button', { name: 'Add ingredient' }))
    await user.type(within(dialog).getByLabelText('Ingredient'), 'Eggs')
    await user.type(within(dialog).getByLabelText('Quantity'), '4')
    await user.click(within(dialog).getByRole('button', { name: 'Add step' }))
    await user.type(within(dialog).getByLabelText('Instruction'), 'Beat the eggs')
    await user.click(within(dialog).getByRole('button', { name: 'Create recipe' }))

    await waitFor(() =>
      expect(requests.some((request) => request.method === 'POST')).toBe(true),
    )
    const create = requests.find((request) => request.method === 'POST')
    expect(create?.body).toMatchObject({
      name: 'Tortilla',
      categoryId: 1,
      ingredients: [{ name: 'Eggs', quantity: '4', itemId: null }],
      steps: [{ instruction: 'Beat the eggs' }],
      visibility: 'Public',
    })
  })
})

describe('Recipes menu planner', () => {
  it('renders a weekly menu grid and serializes week navigation', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend({
      menus: [
        {
          id: 45,
          week: '2026-06-22',
          name: 'This week',
          visibility: 'Public',
          creatorId: 7,
          creatorName: 'Marina Velasco',
        },
      ],
    })
    window.history.replaceState({}, '', '/recipes/menus?week=2026-06-22')
    render(<App />)

    expect(await screen.findByText('Menu planner')).toBeInTheDocument()
    expect(screen.getByText('22 Jun - 28 Jun')).toBeInTheDocument()
    expect(await screen.findByText('Recipe 01')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: /Next week/ }))

    await waitFor(() =>
      expect(requests.some((request) => request.url.includes('week=2026-06-29'))).toBe(
        true,
      ),
    )
  })

  it('creates a menu with a recipe selected through the shared selector', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend({ menus: [], recipes: [makeRecipeSummary(1)] })
    window.history.replaceState({}, '', '/recipes/menus?week=2026-06-22')
    render(<App />)

    await screen.findByText('No menu for this week')
    await user.click(screen.getByRole('button', { name: 'New menu' }))

    const dialog = await screen.findByRole('dialog', { name: 'Plan a week' })
    await user.type(within(dialog).getByLabelText('Name'), 'Guest week')
    await user.click(within(dialog).getAllByRole('button', { name: 'Add' })[0])

    const selector = await screen.findByRole('dialog', { name: /Select recipe/ })
    await user.click(within(selector).getByRole('button', { name: 'Select' }))
    await user.click(within(dialog).getByRole('button', { name: 'Create menu' }))

    await waitFor(() =>
      expect(requests.some((request) => request.method === 'POST')).toBe(true),
    )
    const create = requests.find((request) => request.method === 'POST')
    expect(create?.body).toMatchObject({
      week: '2026-06-22',
      name: 'Guest week',
      visibility: 'Public',
      slots: [{ day: 'Monday', slot: 'Breakfast', recipeIds: [1] }],
    })
  })
})
