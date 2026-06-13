import { createRef } from 'react'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'

import { Input } from './Input'

describe('Input', () => {
  it('associates the label with the input', () => {
    render(<Input label="Work email" />)
    expect(screen.getByLabelText('Work email')).toBeInstanceOf(HTMLInputElement)
  })

  it('renders an error state with described message and aria-invalid', () => {
    render(<Input label="Password" error="This field is required" />)
    const input = screen.getByLabelText('Password')
    expect(input).toHaveAttribute('aria-invalid', 'true')
    const messageId = input.getAttribute('aria-describedby')
    expect(messageId).toBeTruthy()
    expect(screen.getByText('This field is required')).toHaveAttribute('id', messageId)
  })

  it('shows a hint when there is no error', () => {
    render(<Input label="Name" hint="As shown to your household" />)
    expect(screen.getByLabelText('Name')).toHaveAttribute('aria-invalid', 'false')
    expect(screen.getByText('As shown to your household')).toBeInTheDocument()
  })

  it('reports typed input through onChange', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()
    render(<Input label="City" onChange={onChange} />)
    await user.type(screen.getByLabelText('City'), 'Cádiz')
    expect(onChange).toHaveBeenCalled()
    expect(screen.getByLabelText('City')).toHaveValue('Cádiz')
  })

  it('forwards the ref to the underlying input', () => {
    const ref = createRef<HTMLInputElement>()
    render(<Input label="Token" ref={ref} />)
    expect(ref.current).toBeInstanceOf(HTMLInputElement)
  })

  it('disables interaction when disabled', () => {
    render(<Input label="Locked" disabled />)
    expect(screen.getByLabelText('Locked')).toBeDisabled()
  })
})
