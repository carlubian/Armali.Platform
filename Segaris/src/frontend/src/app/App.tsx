import { appConfig } from '@/app/config/env'

/**
 * Placeholder application root.
 *
 * Wave 2 only needs a rendered page so the build and run commands succeed end to
 * end. The shared shell, routing, session context, and feature screens arrive in
 * later waves of FRONTEND_CORE_IMPLEMENTATION_PLAN.md.
 */
export function App() {
  return (
    <main>
      <h1>Segaris</h1>
      <p>Frontend scaffold is running.</p>
      <p>Version: {appConfig.appVersion}</p>
    </main>
  )
}
