import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'

import '@/app/i18n/i18n'

import { AnalyticsChartCard } from './AnalyticsChartCard'
import { AnalyticsLegend } from './AnalyticsLegend'

const table = {
  caption: 'Category — 2026 versus 2025, EUR.',
  columns: ['Category', '2026', '2025', 'YoY'],
  rows: [['Property', '€14,200', '€11,600', '+22%']],
}

describe('AnalyticsChartCard', () => {
  it('exposes the chart as an image with its accessible summary', () => {
    render(
      <AnalyticsChartCard title="Expenses by category" summary="A readable summary.">
        <div data-testid="plot">plot</div>
      </AnalyticsChartCard>,
    )
    expect(screen.getByRole('img', { name: 'A readable summary.' })).toBeInTheDocument()
    expect(screen.getByTestId('plot')).toBeInTheDocument()
  })

  it('toggles between the chart and an equivalent data table', async () => {
    const user = userEvent.setup()
    render(
      <AnalyticsChartCard title="Expenses by category" summary="Summary." table={table}>
        <div data-testid="plot">plot</div>
      </AnalyticsChartCard>,
    )

    const toggle = screen.getByRole('button', { name: 'Show data table' })
    expect(toggle).toHaveAttribute('aria-pressed', 'false')
    expect(
      screen.queryByText('Category — 2026 versus 2025, EUR.'),
    ).not.toBeInTheDocument()

    await user.click(toggle)

    expect(screen.getByText('Category — 2026 versus 2025, EUR.')).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'YoY' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Show chart' })).toHaveAttribute(
      'aria-pressed',
      'true',
    )
    expect(screen.queryByTestId('plot')).not.toBeInTheDocument()
  })

  it('renders an empty placeholder and hides the table toggle when empty', () => {
    render(
      <AnalyticsChartCard
        title="Expenses by category"
        summary="Empty summary."
        table={table}
        isEmpty
      >
        <div data-testid="plot">plot</div>
      </AnalyticsChartCard>,
    )
    expect(screen.getByText('No data for this period')).toBeInTheDocument()
    expect(screen.queryByTestId('plot')).not.toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: 'Show data table' }),
    ).not.toBeInTheDocument()
  })

  it('renders the year-over-year legend labels', () => {
    render(
      <AnalyticsChartCard
        title="Expenses by category"
        summary="Summary."
        legend={
          <AnalyticsLegend selectedYear={2026} previousYear={2025} color="#3a7ca5" />
        }
      >
        <div>plot</div>
      </AnalyticsChartCard>,
    )
    expect(screen.getByText('2026')).toBeInTheDocument()
    expect(screen.getByText('2025')).toBeInTheDocument()
  })
})
