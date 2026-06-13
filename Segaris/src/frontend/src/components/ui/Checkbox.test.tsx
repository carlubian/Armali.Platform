import { createRef } from 'react'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'

import { Checkbox } from './Checkbox'

describe('Checkbox', () => {
  it('associates its label and toggles on click', async () => {
    const user = userEvent.setup()
    render(<Checkbox label="Keep me signed in" />)
    const checkbox = screen.getByRole('checkbox', { name: 'Keep me signed in' })
    expect(checkbox).not.toBeChecked()
    await user.click(checkbox)
    expect(checkbox).toBeChecked()
  })

  it('toggles with the keyboard', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()
    render(<Checkbox label="Notify" onChange={onChange} />)
    screen.getByRole('checkbox', { name: 'Notify' }).focus()
    await user.keyboard(' ')
    expect(onChange).toHaveBeenCalledTimes(1)
  })

  it('respects a controlled checked value', () => {
    render(<Checkbox label="Locked on" checked readOnly />)
    expect(screen.getByRole('checkbox', { name: 'Locked on' })).toBeChecked()
  })

  it('does not toggle when disabled', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()
    render(<Checkbox label="Off" disabled onChange={onChange} />)
    await user.click(screen.getByRole('checkbox', { name: 'Off' }))
    expect(onChange).not.toHaveBeenCalled()
  })

  it('forwards the ref to the input', () => {
    const ref = createRef<HTMLInputElement>()
    render(<Checkbox label="R" ref={ref} />)
    expect(ref.current).toBeInstanceOf(HTMLInputElement)
  })
})
