import { expect, test } from '@playwright/test'

/**
 * Full-stack sign-in journey.
 *
 * This spec drives the real interface against a running backend, so it needs a
 * seeded account. Credentials are supplied through the environment rather than
 * committed: set `SEGARIS_E2E_USERNAME` and `SEGARIS_E2E_PASSWORD` to the
 * bootstrap administrator created by `SEGARIS_BOOTSTRAP_USERNAME` /
 * `SEGARIS_BOOTSTRAP_PASSWORD` in the Compose stack. Without them — for example
 * a standalone `vite preview` run with no backend — the test is skipped.
 */
const username = process.env.SEGARIS_E2E_USERNAME
const password = process.env.SEGARIS_E2E_PASSWORD

test.describe('sign-in', () => {
  test.skip(
    !username || !password,
    'Requires a running backend and seeded credentials (SEGARIS_E2E_USERNAME / SEGARIS_E2E_PASSWORD).',
  )

  test('signs in with seeded credentials and reaches the launcher', async ({ page }) => {
    await page.goto('/login')

    await expect(page.getByRole('heading', { name: 'Welcome home' })).toBeVisible()

    await page.getByLabel('Username').fill(username!)
    await page.getByLabel('Password').fill(password!)
    await page.getByRole('button', { name: 'Sign in' }).click()

    await expect(page.getByRole('heading', { name: 'Choose a module' })).toBeVisible()
  })
})
