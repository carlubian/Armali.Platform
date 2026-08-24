import { describe, expect, it } from 'vitest'

import { createQueryClient } from './queryClient'

describe('createQueryClient', () => {
  it('applies the Blackwing query and mutation defaults', () => {
    const defaults = createQueryClient().getDefaultOptions()

    // Refetching on focus would reshuffle a gallery the user is scrolling.
    expect(defaults.queries?.refetchOnWindowFocus).toBe(false)
    expect(defaults.queries?.retry).toBe(1)
    // A duplicated upload or tag write is worse than a visible failure.
    expect(defaults.mutations?.retry).toBe(false)
  })

  it('returns an independent client on every call', () => {
    expect(createQueryClient()).not.toBe(createQueryClient())
  })
})
