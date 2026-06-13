import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { UserRound } from 'lucide-react'
import { describe, expect, it, vi } from 'vitest'

import { ModuleCard, type ModuleCardModel } from './ModuleCard'

const module: ModuleCardModel = {
  key: 'profile',
  title: 'My profile',
  description: 'Manage your account.',
  actionLabel: 'Open',
  href: '/profile',
  icon: UserRound,
  tone: 'aqua',
  attention: true,
  attentionLabel: 'Needs your attention',
}

describe('ModuleCard', () => {
  it('opens its route and exposes the optional attention state', async () => {
    const onOpen = vi.fn()
    render(<ModuleCard module={module} onOpen={onOpen} />)

    expect(screen.getByRole('status', { name: 'Needs your attention' })).toBeVisible()
    await userEvent.click(screen.getByRole('button', { name: /My profile/i }))
    expect(onOpen).toHaveBeenCalledWith('/profile')
  })
})
