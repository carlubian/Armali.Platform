import { expect, test } from '@playwright/test'

test('the application renders its root page', async ({ page }) => {
  await page.goto('/')

  await expect(page.getByRole('heading', { name: 'Segaris' })).toBeVisible()
})
