import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'

import { Tooltip } from './Tooltip'

describe('Tooltip', () => {
  it('renders the trigger children and the tooltip bubble', () => {
    render(
      <Tooltip label="Return to launcher">
        <button type="button">Home</button>
      </Tooltip>,
    )
    expect(screen.getByRole('button', { name: 'Home' })).toBeInTheDocument()
    expect(screen.getByRole('tooltip')).toHaveTextContent('Return to launcher')
  })

  it('applies the bottom-side modifier class', () => {
    const { container } = render(
      <Tooltip label="Hint" side="bottom">
        <span>x</span>
      </Tooltip>,
    )
    expect(container.querySelector('.arm-tooltip--bottom')).not.toBeNull()
  })
})
