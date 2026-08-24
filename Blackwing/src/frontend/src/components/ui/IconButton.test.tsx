import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'

import { IconButton } from './IconButton'

describe('IconButton', () => {
  it('exposes the label as its accessible name', () => {
    render(<IconButton label="Return to launcher" icon={<svg />} />)
    const button = screen.getByRole('button', { name: 'Return to launcher' })
    expect(button).toHaveAttribute('title', 'Return to launcher')
    expect(button).toHaveAttribute('type', 'button')
  })

  it('applies size and variant modifier classes', () => {
    render(<IconButton label="Sign out" variant="solid" size="lg" icon={<svg />} />)
    const button = screen.getByRole('button', { name: 'Sign out' })
    expect(button).toHaveClass('arm-iconbtn--solid', 'arm-iconbtn--lg')
  })

  it('activates by keyboard', async () => {
    const user = userEvent.setup()
    const onClick = vi.fn()
    render(<IconButton label="Edit" icon={<svg />} onClick={onClick} />)
    screen.getByRole('button', { name: 'Edit' }).focus()
    await user.keyboard('{Enter}')
    expect(onClick).toHaveBeenCalledTimes(1)
  })
})
