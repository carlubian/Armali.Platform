import '@testing-library/jest-dom/vitest'

import { cleanup } from '@testing-library/react'
import { afterEach } from 'vitest'

// jsdom has no ResizeObserver, which Recharts' ResponsiveContainer relies on.
// A no-op observer lets chart components mount without throwing; charts measure
// to zero in jsdom, but Analytics keeps every value reachable through the chart
// card's accessible summary and data table rather than the SVG itself.
if (!('ResizeObserver' in globalThis)) {
  class ResizeObserverStub {
    observe(): void {}
    unobserve(): void {}
    disconnect(): void {}
  }
  globalThis.ResizeObserver = ResizeObserverStub
}

// Unmount React trees and clear the jsdom document between tests so component
// tests stay isolated. Shared test helpers live here and must not be imported by
// production modules.
afterEach(() => {
  cleanup()
})
