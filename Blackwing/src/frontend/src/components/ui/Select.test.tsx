import { createRef } from 'react'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'

import { Select } from './Select'

describe('Select', () => {
  it('renders object and string options', () => {
    render(
      <Select
        aria-label="Language"
        options={[{ value: 'en-GB', label: 'English' }, 'es-ES']}
      />,
    )
    const select = screen.getByRole('combobox', { name: 'Language' })
    expect(select).toBeInTheDocument()
    expect(screen.getByRole('option', { name: 'English' })).toHaveValue('en-GB')
    expect(screen.getByRole('option', { name: 'es-ES' })).toHaveValue('es-ES')
  })

  it('renders a disabled placeholder option', () => {
    render(<Select aria-label="Role" placeholder="Choose a role" options={['Admin']} />)
    const placeholder = screen.getByRole('option', { name: 'Choose a role' })
    expect(placeholder).toBeDisabled()
    expect(placeholder).toHaveValue('')
  })

  it('reports selection changes as a controlled value', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()
    render(
      <Select
        aria-label="Status"
        value="active"
        onChange={onChange}
        options={['active', 'inactive']}
      />,
    )
    await user.selectOptions(
      screen.getByRole('combobox', { name: 'Status' }),
      'inactive',
    )
    expect(onChange).toHaveBeenCalled()
  })

  it('forwards the ref to the underlying select', () => {
    const ref = createRef<HTMLSelectElement>()
    render(<Select aria-label="Tier" ref={ref} options={['a']} />)
    expect(ref.current).toBeInstanceOf(HTMLSelectElement)
  })
})
