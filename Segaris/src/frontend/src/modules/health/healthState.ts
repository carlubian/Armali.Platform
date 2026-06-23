import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'

import {
  healthPageSizes,
  type DiseaseListQuery,
  type DiseaseSortField,
  type HealthPageSize,
  type HealthSortDirection,
  type HealthTab,
  type HealthVisibility,
  type MedicineListQuery,
  type MedicineSortField,
} from '@/app/api/health'

const tabs: readonly HealthTab[] = ['diseases', 'medicines']
const diseaseSortFields: readonly DiseaseSortField[] = ['name', 'category']
const medicineSortFields: readonly MedicineSortField[] = ['name', 'category']
const visibilities: readonly HealthVisibility[] = ['Public', 'Private']

export const defaultHealthTab: HealthTab = 'diseases'
export const defaultDiseaseSort: DiseaseSortField = 'name'
export const defaultMedicineSort: MedicineSortField = 'name'
export const defaultSortDirection: HealthSortDirection = 'asc'
export const defaultPageSize: HealthPageSize = 25

export interface DiseaseListState {
  search: string
  category: number | null
  visibility: HealthVisibility | ''
  mine: boolean
  sort: DiseaseSortField
  sortDirection: HealthSortDirection
  page: number
  pageSize: HealthPageSize
}

export interface MedicineListState {
  search: string
  category: number | null
  requiresPrescription: boolean | null
  visibility: HealthVisibility | ''
  mine: boolean
  sort: MedicineSortField
  sortDirection: HealthSortDirection
  page: number
  pageSize: HealthPageSize
}

export interface HealthState {
  tab: HealthTab
  diseases: DiseaseListState
  medicines: MedicineListState
}

export type HealthDialogState =
  | { mode: 'closed' }
  | { mode: 'createDisease' }
  | { mode: 'editDisease'; diseaseId: number }
  | { mode: 'createMedicine' }
  | { mode: 'editMedicine'; medicineId: number }

function oneOf<T extends string>(value: string | null, allowed: readonly T[]): T | '' {
  return value != null && (allowed as readonly string[]).includes(value)
    ? (value as T)
    : ''
}

function intOrNull(value: string | null): number | null {
  if (value == null) return null
  const parsed = Number.parseInt(value, 10)
  return Number.isFinite(parsed) && parsed > 0 ? parsed : null
}

function boolOrNull(value: string | null): boolean | null {
  if (value === 'true') return true
  if (value === 'false') return false
  return null
}

function pageSize(value: string | null): HealthPageSize {
  const parsed = Number.parseInt(value ?? '', 10)
  return (healthPageSizes as readonly number[]).includes(parsed)
    ? (parsed as HealthPageSize)
    : defaultPageSize
}

function page(value: string | null): number {
  const parsed = Number.parseInt(value ?? '', 10)
  return Number.isFinite(parsed) && parsed > 0 ? parsed : 1
}

function direction(value: string | null): HealthSortDirection {
  return value === 'desc' ? 'desc' : defaultSortDirection
}

export function parseHealthState(
  params: URLSearchParams,
  currentUserId: number | null,
): HealthState {
  const tab = oneOf(params.get('tab'), tabs)
  const diseaseCreator = intOrNull(params.get('diseaseCreator'))
  const medicineCreator = intOrNull(params.get('medicineCreator'))
  const diseaseSort = oneOf(params.get('diseaseSort'), diseaseSortFields)
  const medicineSort = oneOf(params.get('medicineSort'), medicineSortFields)

  return {
    tab: tab === '' ? defaultHealthTab : tab,
    diseases: {
      search: params.get('diseaseSearch') ?? '',
      category: intOrNull(params.get('diseaseCategory')),
      visibility: oneOf(params.get('diseaseVisibility'), visibilities),
      mine: diseaseCreator != null && diseaseCreator === currentUserId,
      sort: diseaseSort === '' ? defaultDiseaseSort : diseaseSort,
      sortDirection: direction(params.get('diseaseSortDirection')),
      page: page(params.get('diseasePage')),
      pageSize: pageSize(params.get('diseasePageSize')),
    },
    medicines: {
      search: params.get('medicineSearch') ?? '',
      category: intOrNull(params.get('medicineCategory')),
      requiresPrescription: boolOrNull(params.get('requiresPrescription')),
      visibility: oneOf(params.get('medicineVisibility'), visibilities),
      mine: medicineCreator != null && medicineCreator === currentUserId,
      sort: medicineSort === '' ? defaultMedicineSort : medicineSort,
      sortDirection: direction(params.get('medicineSortDirection')),
      page: page(params.get('medicinePage')),
      pageSize: pageSize(params.get('medicinePageSize')),
    },
  }
}

export function parseHealthDialogState(params: URLSearchParams): HealthDialogState {
  if (params.get('newDisease') === 'true') return { mode: 'createDisease' }
  if (params.get('newMedicine') === 'true') return { mode: 'createMedicine' }
  const diseaseId = intOrNull(params.get('diseaseId'))
  if (diseaseId != null) return { mode: 'editDisease', diseaseId }
  const medicineId = intOrNull(params.get('medicineId'))
  return medicineId == null ? { mode: 'closed' } : { mode: 'editMedicine', medicineId }
}

export function toDiseaseListQuery(
  state: DiseaseListState,
  currentUserId: number | null,
): DiseaseListQuery {
  return {
    search: state.search.trim() === '' ? null : state.search.trim(),
    category: state.category,
    visibility: state.visibility === '' ? null : state.visibility,
    creator: state.mine ? currentUserId : null,
    page: state.page,
    pageSize: state.pageSize,
    sort: state.sort,
    sortDirection: state.sortDirection,
  }
}

export function toMedicineListQuery(
  state: MedicineListState,
  currentUserId: number | null,
): MedicineListQuery {
  return {
    search: state.search.trim() === '' ? null : state.search.trim(),
    category: state.category,
    requiresPrescription: state.requiresPrescription,
    visibility: state.visibility === '' ? null : state.visibility,
    creator: state.mine ? currentUserId : null,
    page: state.page,
    pageSize: state.pageSize,
    sort: state.sort,
    sortDirection: state.sortDirection,
  }
}

export function useHealthState(currentUserId: number | null) {
  const [searchParams, setSearchParams] = useSearchParams()
  const state = useMemo(
    () => parseHealthState(searchParams, currentUserId),
    [searchParams, currentUserId],
  )
  const dialog = useMemo(() => parseHealthDialogState(searchParams), [searchParams])
  const diseaseListQuery = useMemo(
    () => toDiseaseListQuery(state.diseases, currentUserId),
    [state.diseases, currentUserId],
  )
  const medicineListQuery = useMemo(
    () => toMedicineListQuery(state.medicines, currentUserId),
    [state.medicines, currentUserId],
  )

  const patchParams = useCallback(
    (patch: Record<string, string | null>) => {
      setSearchParams((current) => {
        const next = new URLSearchParams(current)
        Object.entries(patch).forEach(([key, value]) => {
          if (value == null) next.delete(key)
          else next.set(key, value)
        })
        return next
      })
    },
    [setSearchParams],
  )

  return {
    state,
    dialog,
    diseaseListQuery,
    medicineListQuery,
    setTab: (tab: HealthTab) => patchParams({ tab }),
    openCreateDisease: () =>
      patchParams({
        newDisease: 'true',
        diseaseId: null,
        newMedicine: null,
        medicineId: null,
      }),
    openEditDisease: (diseaseId: number) =>
      patchParams({
        diseaseId: String(diseaseId),
        newDisease: null,
        newMedicine: null,
        medicineId: null,
      }),
    openCreateMedicine: () =>
      patchParams({
        newMedicine: 'true',
        medicineId: null,
        newDisease: null,
        diseaseId: null,
      }),
    openEditMedicine: (medicineId: number) =>
      patchParams({
        medicineId: String(medicineId),
        newMedicine: null,
        newDisease: null,
        diseaseId: null,
      }),
    closeDialog: () =>
      patchParams({
        newDisease: null,
        diseaseId: null,
        newMedicine: null,
        medicineId: null,
      }),
  }
}
