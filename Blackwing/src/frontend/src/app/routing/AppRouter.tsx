import { Navigate, Outlet, Route, Routes } from 'react-router-dom'

import { useSession } from '@/app/session/SessionContext'
import { LoadingScreen } from '@/components/feedback/LoadingScreen'
import { NotFound, ServiceUnavailable } from '@/components/feedback/SystemScreens'
import { AppShell } from '@/components/shell/AppShell'
import { UsersPage } from '@/modules/admin/UsersPage'
import { LoginPage } from '@/modules/auth/LoginPage'
import { StartupPage } from '@/modules/startup/StartupPage'

/**
 * The authentication guard. `/login` is the only public route; everything else
 * hangs off this element, so a screen can never render before the session query
 * has answered.
 */
function ProtectedRoutes() {
  const { status, refresh } = useSession()
  if (status === 'loading') return <LoadingScreen />
  if (status === 'unavailable') {
    return <ServiceUnavailable onRetry={() => void refresh()} />
  }
  if (status === 'unauthenticated') return <Navigate to="/login" replace />
  return <AppShell />
}

/**
 * The administration guard.
 *
 * A `User` who reaches `/admin/users` by typing the URL sees the not-found
 * screen, not an access-denied one: the answer must not confirm that the area
 * exists. It is the same reasoning the backend applies when it answers 404 for
 * another account's resource.
 */
function AdminRoutes() {
  const { session } = useSession()
  if (!(session?.roles.includes('Admin') ?? false)) return <NotFound />
  return <Outlet />
}

/**
 * Application routes.
 *
 * The shell is the layout route for every authenticated screen. Only the
 * gallery placeholder and account administration exist at this stage; the rest
 * of the product arrives in later phases.
 */
export function AppRouter() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<ProtectedRoutes />}>
        <Route index element={<StartupPage />} />
        <Route element={<AdminRoutes />}>
          <Route path="admin/users" element={<UsersPage />} />
        </Route>
        <Route path="*" element={<NotFound />} />
      </Route>
    </Routes>
  )
}
