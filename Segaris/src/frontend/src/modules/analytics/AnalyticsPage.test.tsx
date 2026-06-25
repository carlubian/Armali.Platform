import { configure, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'

// The lazy Analytics chunk pulls in Recharts, whose first evaluation under the
// test transform can exceed the default 1s async timeout.
configure({ asyncUtilTimeout: 10000 })

const session = {
  userId: 7,
  userName: 'marina',
  displayName: 'Marina Velasco',
  language: 'en-GB',
  roles: ['Admin'],
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

function monthlyPoints() {
  return Array.from({ length: 12 }, (_, index) => ({
    month: index + 1,
    selectedYearAmountEur: 1000 + index * 10,
    previousYearAmountEur: 900 + index * 10,
  }))
}

function overviewFixture(year: number, missing: string[] = []) {
  return {
    selectedYear: year,
    previousYear: year - 1,
    totals: {
      selectedYearExpenseAmountEur: 12000,
      previousYearExpenseAmountEur: 11000,
      selectedYearIncomeAmountEur: 14000,
      previousYearIncomeAmountEur: 13000,
      selectedYearNetBalanceEur: 2000,
      previousYearNetBalanceEur: 2000,
    },
    charts: [
      { chartId: 'overview.monthlyExpense', points: monthlyPoints() },
      { chartId: 'overview.monthlyIncome', points: monthlyPoints() },
      { chartId: 'overview.monthlyNetBalance', points: monthlyPoints() },
    ],
    missingExchangeRateCurrencyCodes: missing,
  }
}

function groupedPoints() {
  return [
    { label: 'Property', selectedYearAmountEur: 14200, previousYearAmountEur: 11600 },
    { label: 'Vehicles', selectedYearAmountEur: 8600, previousYearAmountEur: 9300 },
  ]
}

function groupedChart(chartId: string, points = groupedPoints()) {
  return { chartId, points }
}

function groupedFixture(year: number, chartId: string) {
  return {
    selectedYear: year,
    previousYear: year - 1,
    charts: [groupedChart(chartId)],
    missingExchangeRateCurrencyCodes: [],
  }
}

function travelFixture(year: number, missing: string[] = []) {
  return {
    selectedYear: year,
    previousYear: year - 1,
    charts: [
      groupedChart('travel.expenseByCategory'),
      groupedChart('travel.expenseBySupplier'),
      groupedChart('travel.expenseByCostCenter'),
      groupedChart('travel.expenseByDestination'),
    ],
    missingExchangeRateCurrencyCodes: missing,
  }
}

function crossModuleFixture(year: number, missing: string[] = []) {
  return {
    selectedYear: year,
    previousYear: year - 1,
    charts: [
      groupedChart('crossModule.expenseBySupplier'),
      groupedChart('crossModule.expenseByCategory'),
      groupedChart('crossModule.expenseByCostCenter'),
    ],
    missingExchangeRateCurrencyCodes: missing,
  }
}

function inventoryFixture(year: number, missing: string[] = []) {
  return {
    selectedYear: year,
    previousYear: year - 1,
    groupedCharts: [
      groupedChart('inventory.expenseByItemCategory'),
      groupedChart('inventory.expenseBySupplier'),
    ],
    averageCharts: [
      {
        chartId: 'inventory.averageOrderBySupplier',
        points: [
          {
            label: 'Acme',
            selectedYearAverageEur: 320,
            previousYearAverageEur: 280,
            selectedYearCount: 12,
            previousYearCount: 10,
          },
        ],
      },
    ],
    topCharts: [
      {
        chartId: 'inventory.topItems',
        points: [
          {
            label: 'Laptop',
            selectedYearAmountEur: 4200,
            previousYearAmountEur: 3000,
            selectedYearPercent: 35,
            previousYearPercent: 28,
          },
          {
            label: 'Monitor',
            selectedYearAmountEur: 1800,
            previousYearAmountEur: 1500,
            selectedYearPercent: 15,
            previousYearPercent: 14,
          },
        ],
      },
      {
        chartId: 'inventory.topSuppliers',
        points: [
          {
            label: 'Acme',
            selectedYearAmountEur: 5200,
            previousYearAmountEur: 4100,
            selectedYearPercent: 43,
            previousYearPercent: 38,
          },
        ],
      },
    ],
    missingExchangeRateCurrencyCodes: missing,
  }
}

interface MockOptions {
  overview?: (year: number) => Response
  inventory?: (year: number) => Response
  travel?: (year: number) => Response
  crossModule?: (year: number) => Response
}

function mockBackend(options: MockOptions = {}) {
  const requests: string[] = []
  vi.spyOn(globalThis, 'fetch').mockImplementation(
    // eslint-disable-next-line @typescript-eslint/require-await -- fetch returns a Promise
    async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = urlOf(input)
      const method = (init?.method ?? 'GET').toUpperCase()
      if (url === '/api/session' && method === 'GET') return json(session)
      if (url === '/api/session/profile' && method === 'GET') {
        return json({
          displayName: session.displayName,
          language: session.language,
          avatarUrl: session.avatarUrl,
        })
      }
      if (url.startsWith('/api/launcher/attention')) return json({ modules: [] })
      if (url.startsWith('/api/analytics/')) {
        requests.push(url)
        const parsed = new URL(url, 'http://localhost')
        const year = Number(parsed.searchParams.get('year') ?? '2026')
        if (url.includes('/analytics/overview')) {
          return options.overview ? options.overview(year) : json(overviewFixture(year))
        }
        if (url.includes('/analytics/capex')) {
          return json(groupedFixture(year, 'capex.expenseByCategory'))
        }
        if (url.includes('/analytics/opex')) {
          return json(groupedFixture(year, 'opex.expenseByCategory'))
        }
        if (url.includes('/analytics/travel')) {
          return options.travel ? options.travel(year) : json(travelFixture(year))
        }
        if (url.includes('/analytics/cross-module')) {
          return options.crossModule
            ? options.crossModule(year)
            : json(crossModuleFixture(year))
        }
        if (url.includes('/analytics/inventory')) {
          return options.inventory
            ? options.inventory(year)
            : json(inventoryFixture(year))
        }
      }
      throw new Error(`Unexpected request: ${method} ${url}`)
    },
  )
  return { requests }
}

beforeEach(() => {
  vi.useFakeTimers({ toFake: ['Date'] })
  vi.setSystemTime(new Date('2026-06-25T10:00:00.000Z'))
  appQueryClient.clear()
  window.history.replaceState({}, '', '/analytics')
})

afterEach(() => {
  vi.useRealTimers()
  vi.restoreAllMocks()
})

describe('Analytics shell', () => {
  it('defaults to the current year and Overview tab, fetching only the active tab', async () => {
    const { requests } = mockBackend()
    render(<App />)

    expect(
      await screen.findByRole('img', { name: /Total expenses by month/ }),
    ).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: /Overview/ })).toHaveAttribute(
      'aria-selected',
      'true',
    )
    const yearNav = screen.getByRole('group', { name: 'Select year' })
    expect(within(yearNav).getByText('2026')).toBeInTheDocument()
    expect(requests.some((url) => url.includes('/analytics/overview?year=2026'))).toBe(
      true,
    )
    expect(requests.some((url) => url.includes('/analytics/capex'))).toBe(false)
  })

  it('opens another tab lazily and reflects it in the URL', async () => {
    const { requests } = mockBackend()
    const user = userEvent.setup()
    render(<App />)
    await screen.findByRole('img', { name: /Total expenses by month/ })

    await user.click(screen.getByRole('tab', { name: /Capex/ }))

    expect(
      await screen.findByRole('img', { name: /Expenses by category/ }),
    ).toBeInTheDocument()
    await waitFor(() => expect(window.location.search).toContain('tab=capex'))
    expect(requests.some((url) => url.includes('/analytics/capex?year=2026'))).toBe(
      true,
    )
  })

  it('navigates years through the URL and refetches', async () => {
    const { requests } = mockBackend()
    const user = userEvent.setup()
    render(<App />)
    await screen.findByRole('img', { name: /Total expenses by month/ })

    expect(screen.getByRole('button', { name: 'This year' })).toBeDisabled()
    await user.click(screen.getByRole('button', { name: 'Previous year' }))

    await waitFor(() => expect(window.location.search).toContain('year=2025'))
    await waitFor(() =>
      expect(
        requests.some((url) => url.includes('/analytics/overview?year=2025')),
      ).toBe(true),
    )
    expect(screen.getByRole('button', { name: 'This year' })).toBeEnabled()
  })

  it('surfaces a configuration-incomplete state for missing exchange rates', async () => {
    mockBackend({ overview: (year) => json(overviewFixture(year, ['USD'])) })
    render(<App />)

    expect(await screen.findByText('Exchange rates are incomplete')).toBeInTheDocument()
    expect(screen.getByText('USD')).toBeInTheDocument()
    expect(
      screen.queryByRole('img', { name: /Total expenses by month/ }),
    ).not.toBeInTheDocument()
  })

  it('shows an error state and retries on demand', async () => {
    let attempts = 0
    mockBackend({
      overview: (year) => {
        attempts += 1
        // A non-transient 404 fails immediately without a retry/backoff delay.
        return attempts === 1
          ? json({ title: 'Not found' }, 404)
          : json(overviewFixture(year))
      },
    })
    const user = userEvent.setup()
    render(<App />)

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'These charts could not be loaded.',
    )
    await user.click(screen.getByRole('button', { name: 'Try again' }))

    expect(
      await screen.findByRole('img', { name: /Total expenses by month/ }),
    ).toBeInTheDocument()
  })

  it('shows Overview year totals with directional deltas and EUR values', async () => {
    mockBackend({
      overview: (year) =>
        json({
          ...overviewFixture(year),
          totals: {
            selectedYearExpenseAmountEur: 12000,
            previousYearExpenseAmountEur: 10000,
            selectedYearIncomeAmountEur: 15000,
            previousYearIncomeAmountEur: 10000,
            selectedYearNetBalanceEur: 3000,
            previousYearNetBalanceEur: 0,
          },
        }),
    })
    render(<App />)

    const totals = await screen.findByRole('group', { name: 'Year totals' })
    const stats = within(totals)

    expect(stats.getByText('Total expenses')).toBeInTheDocument()
    expect(stats.getByText('€12,000')).toBeInTheDocument()
    // Spending more than last year is unfavourable, so the delta reads "down".
    expect(stats.getByText('+20%')).toHaveClass('an-delta', 'is-down')

    expect(stats.getByText('Total income')).toBeInTheDocument()
    expect(stats.getByText('€15,000')).toBeInTheDocument()
    expect(stats.getByText('+50%')).toHaveClass('an-delta', 'is-up')

    // No previous-year baseline collapses the delta to a neutral em dash.
    expect(stats.getByText('Net balance')).toBeInTheDocument()
    expect(stats.getByText('€3,000')).toBeInTheDocument()
    expect(stats.getByText('—')).toHaveClass('an-delta', 'is-flat')
  })

  it('hides Overview totals behind the configuration-incomplete state', async () => {
    mockBackend({ overview: (year) => json(overviewFixture(year, ['USD'])) })
    render(<App />)

    await screen.findByText('Exchange rates are incomplete')
    expect(screen.queryByRole('group', { name: 'Year totals' })).not.toBeInTheDocument()
  })

  it('renders a tablist with every analytics section', async () => {
    mockBackend()
    render(<App />)
    await screen.findByRole('img', { name: /Total expenses by month/ })

    const tablist = screen.getByRole('tablist', { name: 'Analytics sections' })
    const tabs = within(tablist).getAllByRole('tab')
    expect(tabs).toHaveLength(6)
  })
})

describe('Analytics Inventory, Travel and Cross-module tabs', () => {
  // Land straight on the tab via its URL-backed state, so only that tab's data
  // is fetched and the heavy Overview Recharts pass never mounts.
  function openTab(tab: string) {
    window.history.replaceState({}, '', `/analytics?tab=${tab}&year=2026`)
    const user = userEvent.setup()
    render(<App />)
    return user
  }

  it('renders every Inventory chart with the average subtitle and top-5 note', async () => {
    mockBackend()
    openTab('inventory')

    expect(
      await screen.findByRole('img', { name: /Expenses by item category/ }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('img', { name: /Expenses by supplier/ }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('img', { name: /Top 5 items by spend/ }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('img', { name: /Top 5 suppliers by spend/ }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('img', { name: /Average order amount by supplier/ }),
    ).toBeInTheDocument()

    // Design-faithful guidance copy for the dense top-list and the average card.
    expect(screen.getByText('Mean EUR per received order')).toBeInTheDocument()
    expect(
      screen.getAllByText(/share of total Inventory expense/).length,
    ).toBeGreaterThan(0)
  }, 20000)

  it('exposes top-5 share of total in the accessible data table, not only the bar label', async () => {
    mockBackend()
    const user = openTab('inventory')
    const topImg = await screen.findByRole('img', { name: /Top 5 items by spend/ })
    const card = topImg.closest('.an-card') as HTMLElement

    await user.click(within(card).getByRole('button', { name: 'Show data table' }))

    expect(
      within(card).getByRole('columnheader', { name: '% of total' }),
    ).toBeInTheDocument()
    // 4,200 of the 35% share — surfaced without the SVG bar label or a tooltip.
    expect(within(card).getByText('35%')).toBeInTheDocument()
    expect(within(card).getByText('15%')).toBeInTheDocument()
  }, 20000)

  it('renders all four Travel charts including linked destination', async () => {
    mockBackend()
    openTab('travel')

    expect(
      await screen.findByRole('img', { name: /Expenses by category/ }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('img', { name: /Expenses by supplier/ }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('img', { name: /Expenses by cost centre/ }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('img', { name: /Expenses by destination/ }),
    ).toBeInTheDocument()
  })

  it('renders the three Cross-module charts with the normalization note', async () => {
    mockBackend()
    openTab('cross-module')

    expect(
      await screen.findByRole('img', { name: /Total expenses by supplier/ }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('img', { name: /Total expenses by category/ }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('img', { name: /Total expenses by cost centre/ }),
    ).toBeInTheDocument()
    expect(
      screen.getByText(/matched across modules by normalized display label/),
    ).toBeInTheDocument()
  })

  it('shows an empty chart placeholder when a Cross-module chart has no data', async () => {
    mockBackend({
      crossModule: (year) =>
        json({
          selectedYear: year,
          previousYear: year - 1,
          charts: [groupedChart('crossModule.expenseBySupplier', [])],
          missingExchangeRateCurrencyCodes: [],
        }),
    })
    openTab('cross-module')

    expect(await screen.findByText('No data for this period')).toBeInTheDocument()
  })

  it('surfaces the configuration-incomplete state on the Inventory tab', async () => {
    mockBackend({ inventory: (year) => json(inventoryFixture(year, ['GBP'])) })
    openTab('inventory')

    expect(await screen.findByText('Exchange rates are incomplete')).toBeInTheDocument()
    expect(screen.getByText('GBP')).toBeInTheDocument()
    expect(
      screen.queryByRole('img', { name: /Top 5 items by spend/ }),
    ).not.toBeInTheDocument()
  })
})
