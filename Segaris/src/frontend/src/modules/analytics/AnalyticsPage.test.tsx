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

function groupedFixture(year: number, chartId: string) {
  return {
    selectedYear: year,
    previousYear: year - 1,
    charts: [
      {
        chartId,
        points: [
          {
            label: 'Property',
            selectedYearAmountEur: 14200,
            previousYearAmountEur: 11600,
          },
          {
            label: 'Vehicles',
            selectedYearAmountEur: 8600,
            previousYearAmountEur: 9300,
          },
        ],
      },
    ],
    missingExchangeRateCurrencyCodes: [],
  }
}

interface MockOptions {
  overview?: (year: number) => Response
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
          return json(groupedFixture(year, 'travel.expenseByCategory'))
        }
        if (url.includes('/analytics/cross-module')) {
          return json(groupedFixture(year, 'crossModule.expenseBySupplier'))
        }
        if (url.includes('/analytics/inventory')) {
          return json({
            selectedYear: year,
            previousYear: year - 1,
            groupedCharts: [
              {
                chartId: 'inventory.expenseByItemCategory',
                points: [
                  {
                    label: 'Groceries',
                    selectedYearAmountEur: 9200,
                    previousYearAmountEur: 8600,
                  },
                ],
              },
            ],
            averageCharts: [],
            topCharts: [],
            missingExchangeRateCurrencyCodes: [],
          })
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

  it('renders a tablist with every analytics section', async () => {
    mockBackend()
    render(<App />)
    await screen.findByRole('img', { name: /Total expenses by month/ })

    const tablist = screen.getByRole('tablist', { name: 'Analytics sections' })
    const tabs = within(tablist).getAllByRole('tab')
    expect(tabs).toHaveLength(6)
  })
})
