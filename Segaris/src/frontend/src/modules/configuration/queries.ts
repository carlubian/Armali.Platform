import type { QueryClient } from '@tanstack/react-query'

import { capexKeys } from '@/modules/capex/queries'

import type { CatalogDescriptor } from './catalogs'

/**
 * Invalidates the caches affected by a catalog mutation.
 *
 * The catalog's own read key is always invalidated so the table and any business
 * form that consumes it pick up the change. When a mutation can alter the
 * denormalised names or references shown in Capex (a rename, deletion, or
 * reference migration), the known Capex entry queries are invalidated too. A pure
 * reorder leaves entries untouched.
 */
export function invalidateCatalog(
  queryClient: QueryClient,
  descriptor: CatalogDescriptor,
  options: { affectsEntries: boolean },
): Promise<void> {
  const work = [queryClient.invalidateQueries({ queryKey: descriptor.queryKey })]
  if (options.affectsEntries) {
    work.push(queryClient.invalidateQueries({ queryKey: capexKeys.entries() }))
  }
  return Promise.all(work).then(() => undefined)
}
