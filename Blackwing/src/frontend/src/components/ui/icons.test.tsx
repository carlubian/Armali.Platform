import { render, screen } from '@testing-library/react'
import { ArrowRight, Waves } from 'lucide-react'
import { describe, expect, it } from 'vitest'

import { Button } from './Button'
import { IconButton } from './IconButton'

/**
 * The icon strategy replaces the prototype's CDN Lucide + `createIcons()`
 * pattern with the `lucide-react` package, passed into components as ordinary
 * React icon nodes. This guards that integration.
 */
describe('lucide-react icon integration', () => {
  it('renders a lucide icon inside a Button slot', () => {
    const { container } = render(
      <Button iconRight={<ArrowRight aria-hidden="true" />}>Continue</Button>,
    )
    expect(screen.getByRole('button', { name: 'Continue' })).toBeInTheDocument()
    expect(container.querySelector('svg.lucide')).not.toBeNull()
  })

  it('renders a lucide icon inside an IconButton', () => {
    const { container } = render(
      <IconButton label="Brand" icon={<Waves aria-hidden="true" />} />,
    )
    expect(screen.getByRole('button', { name: 'Brand' })).toBeInTheDocument()
    expect(container.querySelector('svg.lucide')).not.toBeNull()
  })
})
