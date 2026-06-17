import { Navigate, Route, Routes, useNavigate } from 'react-router-dom'
import { lazy, Suspense, type ReactNode } from 'react'

import { AppErrorBoundary } from '@/app/errors/AppErrorBoundary'
import { useSession } from '@/app/session/SessionContext'
import { LoadingScreen } from '@/components/feedback/LoadingScreen'
import { NotFound, ServiceUnavailable } from '@/components/feedback/SystemScreens'
import { AppShell } from '@/components/shell/AppShell'
import { UsersPage } from '@/modules/admin/UsersPage'
import { LoginPage } from '@/modules/auth/LoginPage'
import { LauncherPage } from '@/modules/launcher/LauncherPage'
import { ProfilePage } from '@/modules/profile/ProfilePage'

// The Capex module is the first business module and is loaded lazily so its
// table, filters, and editor do not weigh down the initial platform bundle.
const CapexPage = lazy(() =>
  import('@/modules/capex/CapexPage').then((module) => ({ default: module.CapexPage })),
)

// The Opex module is lazily loaded so its table, filters, and editor stay out
// of the initial platform bundle.
const OpexPage = lazy(() =>
  import('@/modules/opex/OpexPage').then((module) => ({ default: module.OpexPage })),
)

// The Inventory module is lazily loaded so its tables, dialogs, and editors stay
// out of the initial platform bundle.
const InventoryPage = lazy(() =>
  import('@/modules/inventory/InventoryPage').then((module) => ({
    default: module.InventoryPage,
  })),
)

// The administrative Configuration experience is admin-only and lazily loaded so
// its catalog tables and dialogs stay out of the initial platform bundle.
const ConfigurationPage = lazy(() =>
  import('@/modules/configuration/ConfigurationPage').then((module) => ({
    default: module.ConfigurationPage,
  })),
)

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
        <Route
          path="capex"
          element={
            <ModuleBoundary>
              <Suspense fallback={<LoadingScreen />}>
                <CapexPage />
              </Suspense>
            </ModuleBoundary>
          }
        />
        <Route
          path="opex"
          element={
            <ModuleBoundary>
              <Suspense fallback={<LoadingScreen />}>
                <OpexPage />
              </Suspense>
            </ModuleBoundary>
          }
        />
        <Route
          path="inventory"
          element={
            <ModuleBoundary>
              <Suspense fallback={<LoadingScreen />}>
                <InventoryPage />
              </Suspense>
            </ModuleBoundary>
          }
        />
        {['configuration', 'configuration/:section'].map((path) => (
          <Route
            key={path}
            path={path}
            element={
              <ModuleBoundary>
                <Suspense fallback={<LoadingScreen />}>
                  <ConfigurationPage />
                </Suspense>
              </ModuleBoundary>
            }
          />
        ))}
      </Route>
      <Route path="*" element={<NotFound />} />
    </Routes>
  )
}
