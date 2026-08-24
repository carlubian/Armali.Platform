import { render, screen } from '@testing-library/react'
import axe from 'axe-core'
import { beforeEach, describe, expect, it } from 'vitest'

import { App, appQueryClient } from '@/app/App'

beforeEach(() => {
  appQueryClient.clear()
  window.history.replaceState({}, '', '/')
})

describe('core screen accessibility', () => {
  it('has no automated violations on the boot screen', async () => {
    render(<App />)
    await screen.findByRole('heading', { level: 1, name: 'Galería' })

    // Colour contrast is excluded: jsdom does not apply the stylesheets, so
    // every computed colour would be the browser default.
    const result = await axe.run(document.body, {
      rules: { 'color-contrast': { enabled: false } },
    })

    expect(result.violations).toEqual([])
  })
})
