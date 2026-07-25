import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor, within } from '@testing-library/react'
import { I18nextProvider } from 'react-i18next'
import { afterEach, describe, expect, it, vi } from 'vitest'

import { inventoryApi, type InventoryShoppingListEntry } from '@/app/api/inventory'
import { i18n } from '@/app/i18n/i18n'

import { ShoppingListDialog } from './ShoppingListDialog'

function entry(
  overrides: Partial<InventoryShoppingListEntry> = {},
): InventoryShoppingListEntry {
  return {
    itemId: 1,
    name: 'Olive oil',
    categoryId: 1,
    categoryName: 'Pantry',
    categorySortOrder: 1,
    block: 'Required',
    requiredQuantity: 3,
    suppliers: ['Alcampo'],
    ...overrides,
  }
}

/**
 * Renders the dialog over a stubbed shopping list read. The entries are handed
 * over exactly as the backend would return them: flat and already ordered, so
 * every grouping assertion below also asserts that the dialog never re-sorts.
 */
function renderDialog(entries: InventoryShoppingListEntry[]) {
  const shoppingList = vi
    .spyOn(inventoryApi, 'shoppingList')
    .mockResolvedValue({ entries })
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })
  render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <ShoppingListDialog language="en-GB" onClose={() => {}} />
      </QueryClientProvider>
    </I18nextProvider>,
  )
  return { shoppingList }
}

function renderFailingDialog() {
  vi.spyOn(inventoryApi, 'shoppingList').mockRejectedValue(new Error('unavailable'))
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })
  render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <ShoppingListDialog language="en-GB" onClose={() => {}} />
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

afterEach(() => vi.restoreAllMocks())

describe('Inventory shopping list dialog', () => {
  it('groups the received entries by block and then by category', async () => {
    renderDialog([
      entry({ itemId: 1, name: 'Olive oil', categoryId: 1, categoryName: 'Pantry' }),
      entry({ itemId: 2, name: 'Rice', categoryId: 1, categoryName: 'Pantry' }),
      entry({
        itemId: 3,
        name: 'Bleach',
        categoryId: 2,
        categoryName: 'Cleaning',
        categorySortOrder: 2,
      }),
      entry({
        itemId: 4,
        name: 'Sponges',
        categoryId: 2,
        categoryName: 'Cleaning',
        categorySortOrder: 2,
        block: 'Optional',
        requiredQuantity: null,
      }),
    ])

    expect(await screen.findByText('Olive oil')).toBeInTheDocument()

    // "Cleaning" opens a category under Required and a second one under
    // Optional, and the rendered order mirrors the received order.
    const headings = screen.getAllByRole('heading').map((node) => node.textContent)
    expect(headings).toEqual(['Required', 'Pantry', 'Cleaning', 'Optional', 'Cleaning'])

    const items = screen.getAllByRole('listitem').map((node) => node.textContent)
    expect(items).toHaveLength(4)
    expect(items[0]).toContain('Olive oil')
    expect(items[1]).toContain('Rice')
    expect(items[2]).toContain('Bleach')
    expect(items[3]).toContain('Sponges')
  })

  it('shows the quantity in the required block and omits it in the optional one', async () => {
    renderDialog([
      entry({ itemId: 1, name: 'Olive oil', requiredQuantity: 3 }),
      entry({ itemId: 2, name: 'Rice', block: 'Optional', requiredQuantity: null }),
    ])

    const required = (await screen.findByText('Olive oil')).closest('li') as HTMLElement
    const optional = screen.getByText('Rice').closest('li') as HTMLElement

    expect(within(required).getByText('At least 3 units')).toBeInTheDocument()
    expect(optional.textContent).not.toContain('At least')
  })

  it('uses the singular quantity wording for a single unit', async () => {
    renderDialog([entry({ requiredQuantity: 1 })])

    expect(await screen.findByText('At least 1 unit')).toBeInTheDocument()
  })

  it('renders the suppliers on their own line separated by commas', async () => {
    renderDialog([entry({ suppliers: ['Alcampo', 'Carrefour', 'Mercadona'] })])

    expect(
      await screen.findByText('Suppliers: Alcampo, Carrefour, Mercadona'),
    ).toBeInTheDocument()
  })

  it('omits an empty block entirely instead of rendering it', async () => {
    renderDialog([
      entry({ itemId: 1, name: 'Rice', block: 'Optional', requiredQuantity: null }),
    ])

    expect(await screen.findByText('Rice')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Optional' })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Required' })).not.toBeInTheDocument()
  })

  it('shows the empty state when nothing needs replenishing', async () => {
    renderDialog([])

    expect(
      await screen.findByText('Nothing needs replenishing right now.'),
    ).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Required' })).not.toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Optional' })).not.toBeInTheDocument()
  })

  it('shows the error state when the list cannot be loaded', async () => {
    renderFailingDialog()

    await waitFor(() =>
      expect(screen.getByRole('alert')).toHaveTextContent(
        'The shopping list could not be loaded. Please try again.',
      ),
    )
  })
})
