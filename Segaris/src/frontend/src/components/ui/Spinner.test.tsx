import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'

import { Spinner } from './Spinner'

describe('Spinner', () => {
  it('exposes a status role with a default accessible label', () => {
    render(<Spinner />)
    expect(screen.getByRole('status', { name: 'Loading' })).toBeInTheDocument()
  })

  it('accepts a custom label', () => {
    render(<Spinner label="Signing in" />)
    expect(screen.getByRole('status', { name: 'Signing in' })).toBeInTheDocument()
  })

  it('sets the size custom property from a numeric size', () => {
    render(<Spinner size={48} label="Busy" />)
    expect(
      screen.getByRole('status', { name: 'Busy' }).style.getPropertyValue('--_s'),
    ).toBe('48px')
  })
})
