import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterEach } from 'vitest'

// vitest doesn't run with globals enabled, so Testing Library's automatic
// afterEach(cleanup) never registers. Unmount rendered trees between tests
// ourselves, otherwise DOM from one test leaks into the next.
afterEach(cleanup)
