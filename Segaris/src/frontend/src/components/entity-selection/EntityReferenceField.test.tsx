import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'

import { EntityReferenceField } from './EntityReferenceField'

const labels = {
  browseLabel: 'Browse',
  changeLabel: 'Change',
  clearLabel: 'Clear link',
}

describe('EntityReferenceField', () => {
  it('renders the empty state with placeholder, helper text, and Browse', () => {
    render(
      <EntityReferenceField
        {...labels}
        placeholder="No asset linked"
        helperText="Link the asset this task maintains."
        onBrowse={vi.fn()}
      />,
    )

    expect(screen.getByText('No asset linked')).toBeInTheDocument()
    expect(screen.getByText('Link the asset this task maintains.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Browse' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Change' })).toBeNull()
    expect(screen.queryByRole('button', { name: 'Clear link' })).toBeNull()
  })

  it('opens the selector from Browse', async () => {
    const user = userEvent.setup()
    const onBrowse = vi.fn()
    render(
      <EntityReferenceField
        {...labels}
        placeholder="No asset linked"
        onBrowse={onBrowse}
      />,
    )

    await user.click(screen.getByRole('button', { name: 'Browse' }))
    expect(onBrowse).toHaveBeenCalledTimes(1)
  })

  it('renders the selected state with primary, secondary, Change, and Clear', () => {
    render(
      <EntityReferenceField
        {...labels}
        placeholder="No asset linked"
        value={{ primary: 'Espresso machine', secondary: 'AST-0058 · Kitchen' }}
        onBrowse={vi.fn()}
        onClear={vi.fn()}
      />,
    )

    expect(screen.getByText('Espresso machine')).toBeInTheDocument()
    expect(screen.getByText('AST-0058 · Kitchen')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Change' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Clear link' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Browse' })).toBeNull()
  })

  it('reopens the selector from Change and clears from Clear', async () => {
    const user = userEvent.setup()
    const onBrowse = vi.fn()
    const onClear = vi.fn()
    render(
      <EntityReferenceField
        {...labels}
        placeholder="No asset linked"
        value={{ primary: 'Espresso machine' }}
        onBrowse={onBrowse}
        onClear={onClear}
      />,
    )

    await user.click(screen.getByRole('button', { name: 'Change' }))
    await user.click(screen.getByRole('button', { name: 'Clear link' }))
    expect(onBrowse).toHaveBeenCalledTimes(1)
    expect(onClear).toHaveBeenCalledTimes(1)
  })

  it('omits Clear when clearing is not allowed', () => {
    render(
      <EntityReferenceField
        {...labels}
        placeholder="No asset linked"
        value={{ primary: 'Espresso machine' }}
        onBrowse={vi.fn()}
      />,
    )

    expect(screen.queryByRole('button', { name: 'Clear link' })).toBeNull()
    expect(screen.getByRole('button', { name: 'Change' })).toBeInTheDocument()
  })

  it('renders an unavailable selection while still allowing Change and Clear', () => {
    render(
      <EntityReferenceField
        {...labels}
        placeholder="No asset linked"
        value={{ primary: 'Linked asset unavailable', unavailable: true }}
        onBrowse={vi.fn()}
        onClear={vi.fn()}
      />,
    )

    const group = screen.getByRole('group')
    expect(group).toHaveClass('seg-ref--unavailable')
    expect(screen.getByRole('button', { name: 'Change' })).toBeEnabled()
    expect(screen.getByRole('button', { name: 'Clear link' })).toBeEnabled()
  })

  it('disables every action when disabled', () => {
    render(
      <EntityReferenceField
        {...labels}
        placeholder="No asset linked"
        value={{ primary: 'Espresso machine' }}
        onBrowse={vi.fn()}
        onClear={vi.fn()}
        disabled
      />,
    )

    expect(screen.getByRole('button', { name: 'Change' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Clear link' })).toBeDisabled()
  })

  it('marks the control busy and disables actions while resolving', () => {
    render(
      <EntityReferenceField
        {...labels}
        placeholder="No asset linked"
        onBrowse={vi.fn()}
        busy
        busyLabel="Resolving link"
      />,
    )

    expect(screen.getByRole('group')).toHaveAttribute('aria-busy', 'true')
    expect(screen.getByRole('status', { name: 'Resolving link' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Browse' })).toBeDisabled()
  })

  it('associates an external label through aria-labelledby', () => {
    render(
      <>
        <span id="asset-link-label">Linked asset</span>
        <EntityReferenceField
          {...labels}
          placeholder="No asset linked"
          aria-labelledby="asset-link-label"
          onBrowse={vi.fn()}
        />
      </>,
    )

    expect(screen.getByRole('group', { name: 'Linked asset' })).toBeInTheDocument()
  })
})
