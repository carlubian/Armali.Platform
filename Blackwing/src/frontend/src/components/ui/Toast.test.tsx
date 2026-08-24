import { act, fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'

import { Toast, ToastProvider, useToast } from './Toast'

describe('Toast', () => {
  it('renders a title and message in a status region', () => {
    render(
      <Toast closeLabel="Dismiss" title="Signed in">
        Welcome back.
      </Toast>,
    )
    const toast = screen.getByRole('status')
    expect(toast).toHaveTextContent('Signed in')
    expect(toast).toHaveTextContent('Welcome back.')
  })

  it('applies tone modifier classes', () => {
    render(
      <Toast closeLabel="Dismiss" tone="danger">
        Something went wrong
      </Toast>,
    )
    expect(screen.getByRole('status')).toHaveClass('arm-toast--danger')
  })

  it('renders a dismiss button only when onClose is provided', async () => {
    const user = userEvent.setup()
    const onClose = vi.fn()
    const { rerender } = render(<Toast closeLabel="Dismiss" title="Saved" />)
    expect(screen.queryByRole('button')).toBeNull()

    rerender(
      <Toast closeLabel="Dismiss" title="Saved" onClose={onClose}>
        Done
      </Toast>,
    )
    await user.click(screen.getByRole('button', { name: 'Dismiss' }))
    expect(onClose).toHaveBeenCalledTimes(1)
  })
})

function Raiser() {
  const { show } = useToast()
  return (
    <button type="button" onClick={() => show({ title: 'Saved', body: 'All good.' })}>
      Raise
    </button>
  )
}

describe('ToastProvider', () => {
  afterEach(() => vi.useRealTimers())

  it('queues a toast raised from a descendant screen', async () => {
    const user = userEvent.setup()
    render(
      <ToastProvider closeLabel="Dismiss">
        <Raiser />
      </ToastProvider>,
    )

    await user.click(screen.getByRole('button', { name: 'Raise' }))
    const toast = screen.getByRole('status')
    expect(toast).toHaveTextContent('Saved')
    expect(toast).toHaveTextContent('All good.')
  })

  it('lets the reader dismiss a queued toast', async () => {
    const user = userEvent.setup()
    render(
      <ToastProvider closeLabel="Dismiss">
        <Raiser />
      </ToastProvider>,
    )

    await user.click(screen.getByRole('button', { name: 'Raise' }))
    await user.click(screen.getByRole('button', { name: 'Dismiss' }))
    expect(screen.queryByRole('status')).toBeNull()
  })

  it('retires a toast on its own after its lifetime', () => {
    vi.useFakeTimers()
    render(
      <ToastProvider closeLabel="Dismiss">
        <Raiser />
      </ToastProvider>,
    )

    // `fireEvent` rather than `userEvent`: the latter awaits real timers, which
    // never advance while the fake ones are installed.
    fireEvent.click(screen.getByRole('button', { name: 'Raise' }))
    expect(screen.getByRole('status')).toBeInTheDocument()

    act(() => {
      vi.advanceTimersByTime(6_000)
    })
    expect(screen.queryByRole('status')).toBeNull()
  })
})
