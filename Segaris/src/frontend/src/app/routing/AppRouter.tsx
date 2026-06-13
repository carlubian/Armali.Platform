import { Navigate, Route, Routes, useNavigate } from 'react-router-dom'
import type { ReactNode } from 'react'

import { AppErrorBoundary } from '@/app/errors/AppErrorBoundary'
import { useSession } from '@/app/session/SessionContext'
import { LoadingScreen } from '@/components/feedback/LoadingScreen'
import { NotFound, ServiceUnavailable } from '@/components/feedback/SystemScreens'
import { AppShell } from '@/components/shell/AppShell'
import { LoginPlaceholder } from '@/modules/auth/LoginPlaceholder'
import { LauncherPage } from '@/modules/launcher/LauncherPage'
import { ProfilePlaceholder } from '@/modules/profile/ProfilePlaceholder'

function ProtectedRoutes() {
  const { status, refresh } = useSession()
  if (status === 'loading') return <LoadingScreen />
  if (status === 'unavailable')
    return <ServiceUnavailable onRetry={() => void refresh()} />
  if (status === 'unauthenticated') return <Navigate to="/login" replace />
  return <AppShell />
}

function ModuleBoundary({ children }: { children: ReactNode }) {
  const navigate = useNavigate()
  return (
    <AppErrorBoundary onReturnToLauncher={() => void navigate('/')}>
      {children}
    </AppErrorBoundary>
  )
}

export function AppRouter() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPlaceholder />} />
      <Route element={<ProtectedRoutes />}>
        <Route
          index
          element={
            <ModuleBoundary>
              <LauncherPage />
            </ModuleBoundary>
          }
        />
        <Route
          path="profile"
          element={
            <ModuleBoundary>
              <ProfilePlaceholder />
            </ModuleBoundary>
          }
        />
      </Route>
      <Route path="*" element={<NotFound />} />
    </Routes>
  )
}
