import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'

import { Card } from './Card'

describe('Card', () => {
  it('renders header, body, action, and footer regions', () => {
    render(
      <Card
        title="Replication"
        subtitle="Across your coast"
        action={<button type="button">Manage</button>}
        footer={<span>Last run 4 minutes ago</span>}
      >
        All regions are up to date.
      </Card>,
    )
    expect(screen.getByText('Replication')).toBeInTheDocument()
    expect(screen.getByText('Across your coast')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Manage' })).toBeInTheDocument()
    expect(screen.getByText('All regions are up to date.')).toBeInTheDocument()
    expect(screen.getByText('Last run 4 minutes ago')).toBeInTheDocument()
  })

  it('omits the header when there is no title, subtitle, or action', () => {
    const { container } = render(<Card>Body only</Card>)
    expect(container.querySelector('.arm-card__header')).toBeNull()
  })

  it('applies glass, accent, and interactive modifier classes', () => {
    const { container } = render(
      <Card glass accent interactive>
        Content
      </Card>,
    )
    const card = container.querySelector('.arm-card')
    expect(card).toHaveClass(
      'arm-card--glass',
      'arm-card--accent',
      'arm-card--interactive',
    )
  })
})
