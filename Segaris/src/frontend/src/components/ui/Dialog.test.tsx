import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'

import { Dialog } from './Dialog'

describe('Dialog', () => {
  it('renders nothing when closed', () => {
    render(
      <Dialog open={false} title="Hidden">
        Body
      </Dialog>,
    )
    expect(screen.queryByRole('dialog')).toBeNull()
  })

  it('renders a modal dialog with title, description, body, and footer', () => {
    const { container } = render(
      <div className="screen-content">
        <Dialog
          title="Deactivate user"
          description="This will revoke their access."
          footer={<button type="button">Confirm</button>}
        >
          Are you sure?
        </Dialog>
      </div>,
    )
    const dialog = screen.getByRole('dialog', { name: 'Deactivate user' })
    expect(dialog).toHaveAttribute('aria-modal', 'true')
    expect(container.querySelector('.arm-dialog__scrim')).toBeNull()
    expect(dialog.parentElement?.parentElement).toBe(document.body)
    expect(screen.getByText('This will revoke their access.')).toBeInTheDocument()
    expect(screen.getByText('Are you sure?')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Confirm' })).toBeInTheDocument()
  })

  it('moves focus to the dialog panel on open', () => {
    render(<Dialog title="Focus me">Body</Dialog>)
    expect(screen.getByRole('dialog', { name: 'Focus me' })).toHaveFocus()
  })

  it('closes via the close button, the Escape key, and the backdrop', async () => {
    const user = userEvent.setup()
    const onClose = vi.fn()
    render(
      <Dialog title="Closable" onClose={onClose}>
        Body
      </Dialog>,
    )

    await user.click(screen.getByRole('button', { name: 'Close' }))
    await user.keyboard('{Escape}')
    const scrim = document.body.querySelector('.arm-dialog__scrim')
    expect(scrim).not.toBeNull()
    await user.click(scrim as Element)

    expect(onClose).toHaveBeenCalledTimes(3)
  })

  it('wraps Tab from the last control back to the first', async () => {
    const user = userEvent.setup()
    render(
      <Dialog
        title="Trapped"
        onClose={() => {}}
        footer={<button type="button">Confirm</button>}
      >
        <button type="button">Body</button>
      </Dialog>,
    )

    const close = screen.getByRole('button', { name: 'Close' })
    screen.getByRole('button', { name: 'Confirm' }).focus()
    await user.tab()

    expect(close).toHaveFocus()
  })

  it('wraps Shift+Tab from the first control to the last', async () => {
    const user = userEvent.setup()
    render(
      <Dialog
        title="Trapped"
        onClose={() => {}}
        footer={<button type="button">Confirm</button>}
      >
        <button type="button">Body</button>
      </Dialog>,
    )

    const confirm = screen.getByRole('button', { name: 'Confirm' })
    screen.getByRole('button', { name: 'Close' }).focus()
    await user.tab({ shift: true })

    expect(confirm).toHaveFocus()
  })

  it('keeps Tab focus inside the topmost dialog when dialogs are stacked', async () => {
    const user = userEvent.setup()
    render(
      <>
        <Dialog title="Editor" onClose={() => {}}>
          <button type="button">Editor body</button>
        </Dialog>
        <Dialog
          title="Selector"
          onClose={() => {}}
          footer={<button type="button">Pick</button>}
        >
          <button type="button">Selector body</button>
        </Dialog>
      </>,
    )

    const selectorClose = screen.getAllByRole('button', { name: 'Close' })[1]
    screen.getByRole('button', { name: 'Pick' }).focus()
    await user.tab()

    expect(selectorClose).toHaveFocus()
    expect(screen.getByRole('button', { name: 'Editor body' })).not.toHaveFocus()
  })

  it('only closes the topmost dialog on Escape when dialogs are stacked', async () => {
    const user = userEvent.setup()
    const onCloseEditor = vi.fn()
    const onCloseSelector = vi.fn()
    render(
      <>
        <Dialog title="Editor" onClose={onCloseEditor}>
          Editor body
        </Dialog>
        <Dialog title="Selector" onClose={onCloseSelector}>
          Selector body
        </Dialog>
      </>,
    )

    await user.keyboard('{Escape}')

    expect(onCloseSelector).toHaveBeenCalledTimes(1)
    expect(onCloseEditor).not.toHaveBeenCalled()
  })
})
