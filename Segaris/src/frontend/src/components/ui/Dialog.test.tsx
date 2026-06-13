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
})
