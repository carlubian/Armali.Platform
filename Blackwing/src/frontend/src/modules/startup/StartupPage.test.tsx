import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'

import '@/app/i18n/i18n'
import { StartupPage } from '@/modules/startup/StartupPage'

describe('StartupPage', () => {
  it('renders the foundations card with its spinner', () => {
    render(<StartupPage />)
    expect(screen.getByText('Cimientos listos')).toBeVisible()
    expect(screen.getByRole('status', { name: 'Cargando' })).toBeInTheDocument()
    expect(screen.getByText('Comprobando los cimientos')).toBeVisible()
  })

  it('acknowledges once and then disables the action', async () => {
    const user = userEvent.setup()
    render(<StartupPage />)

    const action = screen.getByRole('button', { name: 'Entendido' })
    await user.click(action)

    expect(action).toBeDisabled()
    expect(
      screen.getByText('Anotado. Nada más que hacer aquí por ahora.'),
    ).toBeVisible()
    expect(screen.queryByRole('status')).not.toBeInTheDocument()
  })
})
