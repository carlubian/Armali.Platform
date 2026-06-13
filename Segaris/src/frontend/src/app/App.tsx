import { QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'

import { AppErrorBoundary } from '@/app/errors/AppErrorBoundary'
import '@/app/i18n/i18n'
import { createQueryClient } from '@/app/query/queryClient'
import { AppRouter } from '@/app/routing/AppRouter'
import { SessionProvider } from '@/app/session/SessionContext'

export const appQueryClient = createQueryClient()

export function App() {
  return (
    <AppErrorBoundary level="root">
      <QueryClientProvider client={appQueryClient}>
        <BrowserRouter>
          <SessionProvider>
            <AppRouter />
          </SessionProvider>
        </BrowserRouter>
      </QueryClientProvider>
    </AppErrorBoundary>
  )
}
