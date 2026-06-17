import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Plus } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import { travelApi, type TravelExpenseTotal } from '@/app/api/travel'
import { formatCurrency, formatDate } from '@/app/i18n/formatters'
import { Badge, Button, Spinner } from '@/components/ui'

import { ExpenseDialog } from './ExpenseDialog'
import { travelKeys } from './contracts'

type ExpenseDialogState =
  | { mode: 'closed' }
  | { mode: 'create' }
  | { mode: 'edit'; expenseId: number }

export interface TripExpensesProps {
  tripId: number
  totals: TravelExpenseTotal[]
  language: string
}

export function TripExpenses({ tripId, totals, language }: TripExpensesProps) {
  const { t } = useTranslation('travel')
  const queryClient = useQueryClient()
  const [dialog, setDialog] = useState<ExpenseDialogState>({ mode: 'closed' })

  const listQuery = useQuery({
    queryKey: travelKeys.expenseList(tripId, { pageSize: 100 }),
    queryFn: ({ signal }) => travelApi.listExpenses(tripId, { pageSize: 100 }, signal),
  })

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: travelKeys.expenses(tripId) })
    // Trip detail carries the per-currency totals, so refetch it too.
    void queryClient.invalidateQueries({ queryKey: travelKeys.trip(tripId) })
  }

  const handleSaved = () => {
    refresh()
    setDialog({ mode: 'closed' })
  }

  const handleDeleted = () => {
    refresh()
    setDialog({ mode: 'closed' })
  }

  const expenses = listQuery.data?.items ?? []

  return (
    <div className="seg-trv-expenses">
      <div className="seg-trv-editor__section-head">
        <h3>{t('expenses.title')}</h3>
        <Button
          variant="outline"
          size="sm"
          iconLeft={<Plus size={15} />}
          onClick={() => setDialog({ mode: 'create' })}
        >
          {t('expenses.addExpense')}
        </Button>
      </div>

      {totals.length > 0 && (
        <div
          className="seg-trv-expenses__totals"
          aria-label={t('expenses.totalsLabel')}
        >
          {totals.map((total) => (
            <Badge key={total.currencyId} tone="azure">
              {formatCurrency(total.amount, total.currencyCode, language)}
            </Badge>
          ))}
        </div>
      )}

      {listQuery.isPending ? (
        <div className="seg-trv-expenses__status">
          <Spinner />
        </div>
      ) : listQuery.isError ? (
        <p className="seg-trv-editor__error" role="alert">
          {t('expenses.loadError')}
        </p>
      ) : expenses.length === 0 ? (
        <p className="seg-trv-editor__hint">{t('expenses.empty')}</p>
      ) : (
        <table className="seg-trv-expenses__table">
          <thead>
            <tr>
              <th>{t('expenses.columns.description')}</th>
              <th>{t('expenses.columns.category')}</th>
              <th>{t('expenses.columns.date')}</th>
              <th className="seg-trv__num">{t('expenses.columns.amount')}</th>
            </tr>
          </thead>
          <tbody>
            {expenses.map((expense) => (
              <tr
                key={expense.id}
                className="seg-trv-expenses__row"
                onClick={() => setDialog({ mode: 'edit', expenseId: expense.id })}
              >
                <td>
                  <button
                    type="button"
                    className="seg-trv__row-open"
                    onClick={(event) => {
                      event.stopPropagation()
                      setDialog({ mode: 'edit', expenseId: expense.id })
                    }}
                    aria-label={t('expenses.openRow', { name: expense.description })}
                  >
                    {expense.description}
                  </button>
                </td>
                <td>{expense.expenseCategoryName}</td>
                <td>{formatDate(expense.date, language)}</td>
                <td className="seg-trv__num">
                  {formatCurrency(expense.amount, expense.currencyCode, language)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {dialog.mode !== 'closed' && (
        <ExpenseDialog
          tripId={tripId}
          mode={dialog.mode}
          expenseId={dialog.mode === 'edit' ? dialog.expenseId : undefined}
          onClose={() => setDialog({ mode: 'closed' })}
          onSaved={handleSaved}
          onDeleted={handleDeleted}
        />
      )}
    </div>
  )
}
