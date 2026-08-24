import { QueryClientProvider } from '@tanstack/react-query'
import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { createBrowserRouter, RouterProvider } from 'react-router-dom'

import '@/app/i18n/i18n'
import { createQueryClient } from '@/app/query/queryClient'
import { AppRouter } from '@/app/routing/AppRouter'
import { SessionProvider } from '@/app/session/SessionContext'
import { ToastProvider } from '@/components/ui'

export const appQueryClient = createQueryClient()

/**
 * The routed tree, with everything a screen may reach for in scope.
 *
 * `SessionProvider` lives inside the router because it navigates on expiry, and
 * `ToastProvider` wraps the routes so a confirmation raised by one screen is not
 * lost when that screen navigates away.
 */
function AppTree() {
  const { t } = useTranslation('platform')
  return (
    <SessionProvider>
      <ToastProvider closeLabel={t('common.close')}>
        <AppRouter />
      </ToastProvider>
    </SessionProvider>
  )
}

/**
 * Application root: i18next is initialised by importing it, the query client is
 * provided once for the whole tree, and routing is delegated to `AppRouter`.
 * The root error boundary lands with a later phase.
 */
export function App() {
  const router = useMemo(
    () =>
      createBrowserRouter([
        {
          path: '*',
          element: <AppTree />,
        },
      ]),
    [],
  )

  return (
    <QueryClientProvider client={appQueryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  )
}
