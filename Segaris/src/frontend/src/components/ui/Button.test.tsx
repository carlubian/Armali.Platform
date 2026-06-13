import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'

import { Button } from './Button'

describe('Button', () => {
  it('renders its label and defaults to type="button"', () => {
    render(<Button>Save changes</Button>)
    const button = screen.getByRole('button', { name: 'Save changes' })
    expect(button).toHaveAttribute('type', 'button')
    expect(button).toHaveClass('arm-btn')
  })

  it('applies variant, size, and block modifier classes', () => {
    render(
      <Button variant="danger" size="lg" block>
        Delete
      </Button>,
    )
    const button = screen.getByRole('button', { name: 'Delete' })
    expect(button).toHaveClass('arm-btn--danger', 'arm-btn--lg', 'arm-btn--block')
  })

  it('omits modifier classes for the primary md default', () => {
    render(<Button>Continue</Button>)
    const button = screen.getByRole('button')
    expect(button.className).not.toMatch(/arm-btn--/)
  })

  it('activates by mouse click and keyboard', async () => {
    const user = userEvent.setup()
    const onClick = vi.fn()
    render(<Button onClick={onClick}>Run</Button>)
    const button = screen.getByRole('button', { name: 'Run' })

    await user.click(button)
    button.focus()
    await user.keyboard('{Enter}')
    await user.keyboard(' ')

    expect(onClick).toHaveBeenCalledTimes(3)
  })

  it('does not fire when disabled', async () => {
    const user = userEvent.setup()
    const onClick = vi.fn()
    render(
      <Button disabled onClick={onClick}>
        Run
      </Button>,
    )
    await user.click(screen.getByRole('button', { name: 'Run' }))
    expect(onClick).not.toHaveBeenCalled()
  })

  it('renders leading and trailing icon slots', () => {
    render(
      <Button
        iconLeft={<span data-testid="left" />}
        iconRight={<span data-testid="right" />}
      >
        Go
      </Button>,
    )
    expect(screen.getByTestId('left')).toBeInTheDocument()
    expect(screen.getByTestId('right')).toBeInTheDocument()
  })
})
