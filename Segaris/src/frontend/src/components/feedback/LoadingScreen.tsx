import { Spinner } from '@/components/ui'

import './SystemScreens.css'

export function LoadingScreen() {
  return (
    <main className="seg-system-screen" aria-busy="true">
      <Spinner size="lg" label="Loading Segaris" />
    </main>
  )
}
