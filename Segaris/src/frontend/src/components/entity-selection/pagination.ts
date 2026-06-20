/**
 * Builds the items for a numbered pager: always the first and last page, a
 * window around the current page, and `'…'` gaps where pages are skipped.
 *
 * Ported from the entity-selector reference (`pageItems`).
 */
export function pageItems(page: number, pages: number): Array<number | '…'> {
  if (pages <= 7) return Array.from({ length: pages }, (_, index) => index + 1)

  const out: Array<number | '…'> = [1]
  const low = Math.max(2, page - 1)
  const high = Math.min(pages - 1, page + 1)

  if (low > 2) out.push('…')
  for (let p = low; p <= high; p++) out.push(p)
  if (high < pages - 1) out.push('…')
  out.push(pages)

  return out
}
