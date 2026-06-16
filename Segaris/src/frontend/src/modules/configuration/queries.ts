import type { QueryClient } from '@tanstack/react-query'

import type { CatalogDescriptor } from './catalogs'

/**
 * Invalidates the caches affected by a catalog mutation.
 *
 * The catalog's own read key is always invalidated so the table and any business
 * form that consumes it pick up the change. When a mutation can alter the
 * denormalised names or references shown in a business module (a rename,
 * deletion, or reference migration), that module's read caches — declared on the
 * descriptor as `dependentKeys` — are invalidated too. A pure reorder leaves them
 * untouched.
 */
export function invalidateCatalog(
  queryClient: QueryClient,
  descriptor: CatalogDescriptor,
  options: { affectsEntries: boolean },
): Promise<void> {
  const work = [queryClient.invalidateQueries({ queryKey: descriptor.queryKey })]
  if (options.affectsEntries) {
    for (const queryKey of descriptor.dependentKeys ?? []) {
      work.push(queryClient.invalidateQueries({ queryKey }))
    }
  }
  return Promise.all(work).then(() => undefined)
}
