import { useMutation } from '@tanstack/react-query'
import { Minus, Plus } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import {
  inventoryApi,
  type InventoryItem,
  type InventoryItemSummary,
  type InventoryStockAdjustmentDirection,
} from '@/app/api/inventory'
import { isApiError } from '@/app/api/errors'
import { formatNumber } from '@/app/i18n/formatters'
import {
  Button,
  Dialog,
  Input,
  SegmentedControl,
  type SegmentTone,
} from '@/components/ui'

import { parsePositiveAmount } from './orderForm'

export interface StockAdjustmentDialogProps {
  item: InventoryItemSummary
  onClose: () => void
  onAdjusted: (item: InventoryItem) => void
}

const directions: InventoryStockAdjustmentDirection[] = ['Increase', 'Decrease']

const directionMeta: Record<
  InventoryStockAdjustmentDirection,
  { icon: React.ReactNode; tone: SegmentTone }
> = {
  Increase: { icon: <Plus size={15} />, tone: 'success' },
  Decrease: { icon: <Minus size={15} />, tone: 'neutral' },
}

export function StockAdjustmentDialog({
  item,
  onClose,
  onAdjusted,
}: StockAdjustmentDialogProps) {
  const { t, i18n } = useTranslation('inventory')
  const [direction, setDirection] =
    useState<InventoryStockAdjustmentDirection>('Increase')
  const [quantity, setQuantity] = useState('1')
  const [fieldError, setFieldError] = useState<string | null>(null)
  const [serverError, setServerError] = useState<string | null>(null)

  const mutation = useMutation({
    mutationFn: (amount: number) =>
      inventoryApi.adjustStock(item.id, { direction, quantity: amount }),
    onSuccess: (updated) => onAdjusted(updated),
    onError: (error) => setServerError(mapServerError(error, t)),
  })

  const parsed = parsePositiveAmount(quantity)
  const preview =
    parsed == null
      ? null
      : direction === 'Increase'
        ? item.currentStock + parsed
        : item.currentStock - parsed
  const wouldGoNegative = preview != null && preview < 0

  const submit = () => {
    setServerError(null)
    if (parsed == null) {
      setFieldError(t('items.adjust.validation.quantityInvalid'))
      return
    }
    if (wouldGoNegative) {
      setFieldError(t('items.adjust.validation.negativeResult'))
      return
    }
    setFieldError(null)
    mutation.mutate(parsed)
  }

  const submitting = mutation.isPending

  return (
    <Dialog
      width={440}
      title={t('items.adjust.title')}
      description={t('items.adjust.description', { name: item.name })}
      onClose={onClose}
      closeLabel={t('editor.actions.cancel')}
      footer={
        <>
          <Button variant="ghost" onClick={onClose} disabled={submitting}>
            {t('editor.actions.cancel')}
          </Button>
          <Button onClick={submit} disabled={submitting}>
            {submitting ? t('items.adjust.applying') : t('items.adjust.apply')}
          </Button>
        </>
      }
    >
      <div className="seg-inv-adjust">
        {serverError != null && (
          <p className="seg-inv-editor__error" role="alert">
            {serverError}
          </p>
        )}

        <div className="seg-inv-adjust__current">
          <span>{t('items.adjust.currentStock')}</span>
          <strong>{formatNumber(item.currentStock, i18n.language)}</strong>
        </div>

        <div className="seg-inv-editor__field">
          <span className="seg-inv-editor__field-label" id="seg-inv-adjust-direction">
            {t('items.adjust.direction')}
          </span>
          <SegmentedControl
            aria-labelledby="seg-inv-adjust-direction"
            name="seg-inv-adjust-direction"
            value={direction}
            onChange={(event) =>
              setDirection(event.target.value as InventoryStockAdjustmentDirection)
            }
            options={directions.map((value) => ({
              value,
              label: t(`items.adjust.directions.${value}`),
              icon: directionMeta[value].icon,
              tone: directionMeta[value].tone,
            }))}
          />
        </div>

        <Input
          label={t('items.adjust.quantity')}
          inputMode="decimal"
          autoComplete="off"
          value={quantity}
          error={fieldError ?? undefined}
          onChange={(event) => {
            setQuantity(event.target.value)
            setFieldError(null)
          }}
        />

        {preview != null && !wouldGoNegative && (
          <div className="seg-inv-adjust__preview" aria-live="polite">
            <span>{t('items.adjust.resulting')}</span>
            <strong>{formatNumber(preview, i18n.language)}</strong>
          </div>
        )}
      </div>
    </Dialog>
  )
}

function mapServerError(error: unknown, t: (key: string) => string): string {
  if (isApiError(error)) {
    switch (error.problem?.code) {
      case 'inventory.stock.negative_result':
        return t('items.adjust.validation.negativeResult')
      case 'inventory.item.validation':
        return t('items.adjust.validation.quantityInvalid')
      case 'inventory.item.not_found':
        return t('items.adjust.errors.notFound')
    }
    if (error.kind === 'transient' || error.kind === 'unavailable') {
      return t('items.adjust.errors.conflict')
    }
  }
  return t('items.adjust.errors.generic')
}
