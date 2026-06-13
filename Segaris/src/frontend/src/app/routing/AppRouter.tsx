import { Navigate, Route, Routes, useNavigate } from 'react-router-dom'
import type { ReactNode } from 'react'

import { AppErrorBoundary } from '@/app/errors/AppErrorBoundary'
import { useSession } from '@/app/session/SessionContext'
import { LoadingScreen } from '@/components/feedback/LoadingScreen'
import { NotFound, ServiceUnavailable } from '@/components/feedback/SystemScreens'
import { AppShell } from '@/components/shell/AppShell'
import { UsersPage } from '@/modules/admin/UsersPage'
import { LoginPage } from '@/modules/auth/LoginPage'
import { LauncherPage } from '@/modules/launcher/LauncherPage'
import { ProfilePage } from '@/modules/profile/ProfilePage'

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
      <Route path="/login" element={<LoginPage />} />
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
              <ProfilePage />
            </ModuleBoundary>
          }
        />
        <Route
          path="users"
          element={
            <ModuleBoundary>
              <UsersPage />
            </ModuleBoundary>
          }
        />
      </Route>
      <Route path="*" element={<NotFound />} />
    </Routes>
  )
}
