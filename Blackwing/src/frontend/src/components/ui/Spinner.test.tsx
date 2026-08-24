import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'

import { Spinner } from './Spinner'

describe('Spinner', () => {
  it('exposes a status role named by the required label', () => {
    render(<Spinner label="Cargando" />)
    expect(screen.getByRole('status', { name: 'Cargando' })).toBeInTheDocument()
  })

  it('sets the size custom property from a numeric size', () => {
    render(<Spinner size={48} label="Busy" />)
    expect(
      screen.getByRole('status', { name: 'Busy' }).style.getPropertyValue('--_s'),
    ).toBe('48px')
  })
})
