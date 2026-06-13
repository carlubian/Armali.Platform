import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'

import { Avatar } from './Avatar'

describe('Avatar', () => {
  it('derives initials and an accessible name without an image', () => {
    render(<Avatar name="Lina Harbour" />)
    const avatar = screen.getByRole('img', { name: 'Lina Harbour' })
    expect(avatar).toHaveTextContent('LH')
  })

  it('falls back to a placeholder glyph for an empty name', () => {
    render(<Avatar />)
    expect(screen.getByText('?')).toBeInTheDocument()
  })

  it('renders an image with alt text when a source is given', () => {
    render(<Avatar name="Lina" src="/avatar.png" />)
    const image = screen.getByRole('img', { name: 'Lina' })
    expect(image.tagName).toBe('IMG')
    expect(image).toHaveAttribute('src', '/avatar.png')
  })

  it('applies the size modifier and renders a status dot', () => {
    const { container } = render(<Avatar name="A" size="lg" status="busy" />)
    expect(container.querySelector('.arm-avatar--lg')).not.toBeNull()
    expect(container.querySelector('.arm-avatar__status--busy')).not.toBeNull()
  })
})
