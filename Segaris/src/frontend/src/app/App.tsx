import { QueryClientProvider } from '@tanstack/react-query'
import { useMemo } from 'react'
import { createBrowserRouter, RouterProvider } from 'react-router-dom'

import { AppErrorBoundary } from '@/app/errors/AppErrorBoundary'
import '@/app/i18n/i18n'
import { createQueryClient } from '@/app/query/queryClient'
import { AppRouter } from '@/app/routing/AppRouter'
import { SessionProvider } from '@/app/session/SessionContext'

export const appQueryClient = createQueryClient()

export function App() {
  const router = useMemo(
    () =>
      createBrowserRouter([
        {
          path: '*',
          element: (
            <SessionProvider>
              <AppRouter />
            </SessionProvider>
          ),
        },
      ]),
    [],
  )

  return (
    <AppErrorBoundary level="root">
      <QueryClientProvider client={appQueryClient}>
        <RouterProvider router={router} />
      </QueryClientProvider>
    </AppErrorBoundary>
  )
}
