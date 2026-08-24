import { Navigate, Route, Routes } from 'react-router-dom'

import { AppShell } from '@/components/shell/AppShell'
import { StartupPage } from '@/modules/startup/StartupPage'

/**
 * Application routes.
 *
 * The shell is the layout route for every authenticated screen. Only the root
 * screen exists at this stage; unknown paths fall back to it rather than to a
 * not-found screen, which arrives with the first real product routes.
 */
export function AppRouter() {
  return (
    <Routes>
      <Route element={<AppShell />}>
        <Route index element={<StartupPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
