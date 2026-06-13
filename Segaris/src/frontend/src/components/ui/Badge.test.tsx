import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'

import { Badge } from './Badge'

describe('Badge', () => {
  it('renders its content with the default aqua tone', () => {
    render(<Badge>Admin</Badge>)
    const badge = screen.getByText('Admin')
    expect(badge).toHaveClass('arm-badge')
    expect(badge.className).not.toMatch(/arm-badge--/)
  })

  it('applies tone modifier classes', () => {
    render(<Badge tone="danger">Inactive</Badge>)
    expect(screen.getByText('Inactive')).toHaveClass('arm-badge--danger')
  })

  it('renders a pulsing status dot', () => {
    const { container } = render(
      <Badge tone="success" pulse>
        Online
      </Badge>,
    )
    expect(container.querySelector('.arm-badge__dot--pulse')).not.toBeNull()
  })

  it('does not render a dot by default', () => {
    const { container } = render(<Badge>Plain</Badge>)
    expect(container.querySelector('.arm-badge__dot')).toBeNull()
  })
})
