import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'

import { SegmentedControl } from './SegmentedControl'

const options = [
  { value: 'income', label: 'Income' },
  { value: 'expense', label: 'Expense' },
]

describe('SegmentedControl', () => {
  it('renders a radio group with one radio per option', () => {
    render(
      <SegmentedControl
        name="type"
        aria-label="Type"
        options={options}
        defaultValue="income"
      />,
    )
    expect(screen.getByRole('radiogroup', { name: 'Type' })).toBeInTheDocument()
    expect(screen.getByRole('radio', { name: 'Income' })).toBeChecked()
    expect(screen.getByRole('radio', { name: 'Expense' })).not.toBeChecked()
  })

  it('selects an option on click and reports its value', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()
    let lastValue: string | undefined
    render(
      <SegmentedControl
        name="type"
        aria-label="Type"
        options={options}
        defaultValue="income"
        onChange={(event) => {
          lastValue = event.currentTarget.value
          onChange()
        }}
      />,
    )
    await user.click(screen.getByRole('radio', { name: 'Expense' }))
    expect(screen.getByRole('radio', { name: 'Expense' })).toBeChecked()
    expect(onChange).toHaveBeenCalledTimes(1)
    expect(lastValue).toBe('expense')
  })

  it('moves the selection with the arrow keys', async () => {
    const user = userEvent.setup()
    render(
      <SegmentedControl
        name="type"
        aria-label="Type"
        options={options}
        defaultValue="income"
      />,
    )
    screen.getByRole('radio', { name: 'Income' }).focus()
    await user.keyboard('{ArrowRight}')
    expect(screen.getByRole('radio', { name: 'Expense' })).toBeChecked()
  })

  it('reflects a controlled value', () => {
    render(
      <SegmentedControl
        name="type"
        aria-label="Type"
        options={options}
        value="expense"
        onChange={() => {}}
      />,
    )
    expect(screen.getByRole('radio', { name: 'Expense' })).toBeChecked()
  })

  it('does not change when disabled', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()
    render(
      <SegmentedControl
        name="type"
        aria-label="Type"
        options={options}
        defaultValue="income"
        disabled
        onChange={onChange}
      />,
    )
    await user.click(screen.getByRole('radio', { name: 'Expense' }))
    expect(onChange).not.toHaveBeenCalled()
    expect(screen.getByRole('radio', { name: 'Income' })).toBeChecked()
  })
})
