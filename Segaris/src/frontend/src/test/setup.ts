import '@testing-library/jest-dom/vitest'

import { cleanup } from '@testing-library/react'
import { afterEach } from 'vitest'

// Unmount React trees and clear the jsdom document between tests so component
// tests stay isolated. Shared test helpers live here and must not be imported by
// production modules.
afterEach(() => {
  cleanup()
})
