import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'

import { Switch } from './Switch'

describe('Switch', () => {
  it('renders as a checkbox role with its label', async () => {
    const user = userEvent.setup()
    render(<Switch label="Live sync" />)
    const toggle = screen.getByRole('checkbox', { name: 'Live sync' })
    expect(toggle).not.toBeChecked()
    await user.click(toggle)
    expect(toggle).toBeChecked()
  })

  it('toggles with the keyboard', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()
    render(<Switch label="Alerts" onChange={onChange} />)
    screen.getByRole('checkbox', { name: 'Alerts' }).focus()
    await user.keyboard(' ')
    expect(onChange).toHaveBeenCalledTimes(1)
  })

  it('adds the live modifier class when requested', () => {
    const { container } = render(<Switch label="Pulse" live />)
    expect(container.querySelector('.arm-switch--live')).not.toBeNull()
  })

  it('does not toggle when disabled', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()
    render(<Switch label="Frozen" disabled onChange={onChange} />)
    await user.click(screen.getByRole('checkbox', { name: 'Frozen' }))
    expect(onChange).not.toHaveBeenCalled()
  })
})
