import { useQuery } from '@tanstack/react-query'
import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'

import {
  inventoryApi,
  type InventoryShoppingListBlock,
  type InventoryShoppingListEntry,
} from '@/app/api/inventory'
import { formatNumber } from '@/app/i18n/formatters'
import { Button, Dialog, Spinner } from '@/components/ui'

import { inventoryKeys } from './queries'

interface ShoppingListDialogProps {
  language: string
  onClose: () => void
}

interface CategoryGroup {
  categoryId: number
  categoryName: string
  entries: InventoryShoppingListEntry[]
}

interface BlockGroup {
  block: InventoryShoppingListBlock
  categories: CategoryGroup[]
}

/**
 * Groups the flat backend response by block and then by category purely by
 * consuming the received order. The backend guarantees the entries are already
 * ordered by block, category sort order, category identifier, and item name, so
 * a block or category that is empty simply never produces a group and nothing is
 * ever re-sorted here.
 */
export function groupShoppingListEntries(
  entries: readonly InventoryShoppingListEntry[],
): BlockGroup[] {
  const blocks: BlockGroup[] = []
  for (const entry of entries) {
    let blockGroup = blocks[blocks.length - 1]
    if (blockGroup == null || blockGroup.block !== entry.block) {
      blockGroup = { block: entry.block, categories: [] }
      blocks.push(blockGroup)
    }
    let categoryGroup = blockGroup.categories[blockGroup.categories.length - 1]
    if (categoryGroup == null || categoryGroup.categoryId !== entry.categoryId) {
      categoryGroup = {
        categoryId: entry.categoryId,
        categoryName: entry.categoryName,
        entries: [],
      }
      blockGroup.categories.push(categoryGroup)
    }
    categoryGroup.entries.push(entry)
  }
  return blocks
}

export function ShoppingListDialog({ language, onClose }: ShoppingListDialogProps) {
  const { t } = useTranslation('inventory')
  // The list is computed on demand, so every open recomputes it rather than
  // showing a cached snapshot of a stock level that may have moved since.
  const shoppingListQuery = useQuery({
    queryKey: inventoryKeys.shoppingList(),
    queryFn: ({ signal }) => inventoryApi.shoppingList(signal),
    staleTime: 0,
    refetchOnMount: 'always',
  })
  const entries = useMemo(
    () => shoppingListQuery.data?.entries ?? [],
    [shoppingListQuery.data],
  )
  const blocks = useMemo(() => groupShoppingListEntries(entries), [entries])

  return (
    <Dialog
      title={t('shoppingList.title')}
      description={t('shoppingList.description')}
      width={720}
      scrollable
      onClose={onClose}
      closeLabel={t('shoppingList.close')}
      footer={
        <Button variant="ghost" onClick={onClose}>
          {t('shoppingList.close')}
        </Button>
      }
    >
      {shoppingListQuery.isPending ? (
        <div className="seg-inv__shopping-loading">
          <Spinner />
        </div>
      ) : shoppingListQuery.isError ? (
        <p className="seg-inv__error" role="alert">
          {t('shoppingList.loadError')}
        </p>
      ) : blocks.length === 0 ? (
        <p className="seg-inv__empty">{t('shoppingList.empty')}</p>
      ) : (
        <div className="seg-inv__shopping">
          {blocks.map((block) => (
            <section className="seg-inv__shopping-block" key={block.block}>
              <h3 className="seg-inv__shopping-block-title">
                {t(`shoppingList.blocks.${block.block}`)}
              </h3>
              {block.categories.map((category) => (
                <section
                  className="seg-inv__shopping-category"
                  key={`${block.block}-${category.categoryId}`}
                >
                  <h4 className="seg-inv__shopping-category-title">
                    {category.categoryName}
                  </h4>
                  <ul className="seg-inv__shopping-items">
                    {category.entries.map((entry) => (
                      <li className="seg-inv__shopping-item" key={entry.itemId}>
                        <p className="seg-inv__shopping-item-head">
                          <span className="seg-inv__shopping-item-name">
                            {entry.name}
                          </span>
                          {/* The replenishment amount belongs to the Required
                              block only; Optional entries name the item alone. */}
                          {block.block === 'Required' &&
                            entry.requiredQuantity != null && (
                              <span className="seg-inv__shopping-quantity">
                                {t('shoppingList.quantity', {
                                  count: entry.requiredQuantity,
                                  quantity: formatNumber(
                                    entry.requiredQuantity,
                                    language,
                                  ),
                                })}
                              </span>
                            )}
                        </p>
                        <p className="seg-inv__shopping-suppliers">
                          {t('shoppingList.suppliers', {
                            suppliers:
                              entry.suppliers.length > 0
                                ? entry.suppliers.join(', ')
                                : t('common.none'),
                          })}
                        </p>
                      </li>
                    ))}
                  </ul>
                </section>
              ))}
            </section>
          ))}
        </div>
      )}
    </Dialog>
  )
}
