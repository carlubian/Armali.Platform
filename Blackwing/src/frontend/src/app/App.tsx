import { QueryClientProvider } from '@tanstack/react-query'
import { useMemo } from 'react'
import { createBrowserRouter, RouterProvider } from 'react-router-dom'

import '@/app/i18n/i18n'
import { createQueryClient } from '@/app/query/queryClient'
import { AppRouter } from '@/app/routing/AppRouter'

export const appQueryClient = createQueryClient()

/**
 * Application root: i18next is initialised by importing it, the query client is
 * provided once for the whole tree, and routing is delegated to `AppRouter`.
 * The session provider and the root error boundary land with the session phase.
 */
export function App() {
  const router = useMemo(
    () =>
      createBrowserRouter([
        {
          path: '*',
          element: <AppRouter />,
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
