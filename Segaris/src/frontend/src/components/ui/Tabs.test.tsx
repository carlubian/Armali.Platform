import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'

import { Tabs } from './Tabs'

describe('Tabs', () => {
  it('renders a tablist and marks the first tab active by default', () => {
    render(<Tabs tabs={['Overview', 'Regions']} />)
    expect(screen.getByRole('tablist')).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: 'Overview' })).toHaveAttribute(
      'aria-selected',
      'true',
    )
  })

  it('selects a tab on click when uncontrolled', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()
    render(<Tabs tabs={['Overview', 'Regions']} onChange={onChange} />)
    await user.click(screen.getByRole('tab', { name: 'Regions' }))
    expect(onChange).toHaveBeenCalledWith('Regions')
    expect(screen.getByRole('tab', { name: 'Regions' })).toHaveAttribute(
      'aria-selected',
      'true',
    )
  })

  it('honours a controlled value and activates with the keyboard', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()
    render(<Tabs tabs={['Overview', 'Regions']} value="Overview" onChange={onChange} />)
    const regions = screen.getByRole('tab', { name: 'Regions' })
    // Controlled: stays on Overview until the parent updates value.
    expect(regions).toHaveAttribute('aria-selected', 'false')

    regions.focus()
    await user.keyboard('{Enter}')
    expect(onChange).toHaveBeenCalledWith('Regions')
    expect(regions).toHaveAttribute('aria-selected', 'false')
  })

  it('renders an optional count pill', () => {
    render(<Tabs tabs={[{ value: 'users', label: 'Users', count: 12 }]} />)
    expect(screen.getByText('12')).toHaveClass('arm-tab__count')
  })
})
