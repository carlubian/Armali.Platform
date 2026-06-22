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

// The Travel module is lazily loaded so its trip table and dialogs stay out of
// the initial platform bundle.
const TravelPage = lazy(() =>
  import('@/modules/travel/TravelPage').then((module) => ({
    default: module.TravelPage,
  })),
)

const DestinationsPage = lazy(() =>
  import('@/modules/destinations/DestinationsPage').then((module) => ({
    default: module.DestinationsPage,
  })),
)

const DestinationPlacesPage = lazy(() =>
  import('@/modules/destinations/DestinationPlacesPage').then((module) => ({
    default: module.DestinationPlacesPage,
  })),
)

const ClothesPage = lazy(() =>
  import('@/modules/clothes/ClothesPage').then((module) => ({
    default: module.ClothesPage,
  })),
)

const AssetsPage = lazy(() =>
  import('@/modules/assets/AssetsPage').then((module) => ({
    default: module.AssetsPage,
  })),
)

const MaintenancePage = lazy(() =>
  import('@/modules/maintenance/MaintenancePage').then((module) => ({
    default: module.MaintenancePage,
  })),
)

const ProjectsPage = lazy(() =>
  import('@/modules/projects/ProjectsPage').then((module) => ({
    default: module.ProjectsPage,
  })),
)

// The Processes module is lazily loaded so its table, dialogs, and step timeline
// stay out of the initial platform bundle.
const ProcessesPage = lazy(() =>
  import('@/modules/processes/ProcessesPage').then((module) => ({
    default: module.ProcessesPage,
  })),
)

const FirebirdPage = lazy(() =>
  import('@/modules/firebird/FirebirdPage').then((module) => ({
    default: module.FirebirdPage,
  })),
)

// The Recipes module is lazily loaded so its gallery, recipe editor, and menu
// planner stay out of the initial platform bundle.
const RecipesPage = lazy(() =>
  import('@/modules/recipes/RecipesPage').then((module) => ({
    default: module.RecipesPage,
  })),
)

// The Mood module's two immersive screens are lazily loaded so their week board,
// charts, and entry dialog stay out of the initial platform bundle.
const MoodLogPage = lazy(() =>
  import('@/modules/mood/MoodLogPage').then((module) => ({
    default: module.MoodLogPage,
  })),
)

const MoodDashboardPage = lazy(() =>
  import('@/modules/mood/MoodDashboardPage').then((module) => ({
    default: module.MoodDashboardPage,
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
        <Route
          path="travel"
          element={
            <ModuleBoundary>
              <Suspense fallback={<LoadingScreen />}>
                <TravelPage />
              </Suspense>
            </ModuleBoundary>
          }
        />
        <Route
          path="destinations"
          element={
            <ModuleBoundary>
              <Suspense fallback={<LoadingScreen />}>
                <DestinationsPage />
              </Suspense>
            </ModuleBoundary>
          }
        />
        <Route
          path="destinations/:destinationId/places"
          element={
            <ModuleBoundary>
              <Suspense fallback={<LoadingScreen />}>
                <DestinationPlacesPage />
              </Suspense>
            </ModuleBoundary>
          }
        />
        <Route
          path="clothes"
          element={
            <ModuleBoundary>
              <Suspense fallback={<LoadingScreen />}>
                <ClothesPage />
              </Suspense>
            </ModuleBoundary>
          }
        />
        <Route
          path="assets"
          element={
            <ModuleBoundary>
              <Suspense fallback={<LoadingScreen />}>
                <AssetsPage />
              </Suspense>
            </ModuleBoundary>
          }
        />
        <Route
          path="projects"
          element={
            <ModuleBoundary>
              <Suspense fallback={<LoadingScreen />}>
                <ProjectsPage />
              </Suspense>
            </ModuleBoundary>
          }
        />
        <Route
          path="maintenance"
          element={
            <ModuleBoundary>
              <Suspense fallback={<LoadingScreen />}>
                <MaintenancePage />
              </Suspense>
            </ModuleBoundary>
          }
        />
        <Route
          path="processes"
          element={
            <ModuleBoundary>
              <Suspense fallback={<LoadingScreen />}>
                <ProcessesPage />
              </Suspense>
            </ModuleBoundary>
          }
        />
        <Route
          path="people"
          element={
            <ModuleBoundary>
              <Suspense fallback={<LoadingScreen />}>
                <FirebirdPage />
              </Suspense>
            </ModuleBoundary>
          }
        />
        {['recipes', 'recipes/menus'].map((path) => (
          <Route
            key={path}
            path={path}
            element={
              <ModuleBoundary>
                <Suspense fallback={<LoadingScreen />}>
                  <RecipesPage />
                </Suspense>
              </ModuleBoundary>
            }
          />
        ))}
        <Route path="mood" element={<Navigate to="/mood/log" replace />} />
        <Route
          path="mood/log"
          element={
            <ModuleBoundary>
              <Suspense fallback={<LoadingScreen />}>
                <MoodLogPage />
              </Suspense>
            </ModuleBoundary>
          }
        />
        <Route
          path="mood/dashboard"
          element={
            <ModuleBoundary>
              <Suspense fallback={<LoadingScreen />}>
                <MoodDashboardPage />
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
