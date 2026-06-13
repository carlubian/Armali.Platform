import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'

import { Toast } from './Toast'

describe('Toast', () => {
  it('renders a title and message in a status region', () => {
    render(<Toast title="Signed in">Welcome back.</Toast>)
    const toast = screen.getByRole('status')
    expect(toast).toHaveTextContent('Signed in')
    expect(toast).toHaveTextContent('Welcome back.')
  })

  it('applies tone modifier classes', () => {
    render(<Toast tone="danger">Something went wrong</Toast>)
    expect(screen.getByRole('status')).toHaveClass('arm-toast--danger')
  })

  it('renders a dismiss button only when onClose is provided', async () => {
    const user = userEvent.setup()
    const onClose = vi.fn()
    const { rerender } = render(<Toast title="Saved" />)
    expect(screen.queryByRole('button')).toBeNull()

    rerender(
      <Toast title="Saved" onClose={onClose}>
        Done
      </Toast>,
    )
    await user.click(screen.getByRole('button', { name: 'Dismiss' }))
    expect(onClose).toHaveBeenCalledTimes(1)
  })
})
