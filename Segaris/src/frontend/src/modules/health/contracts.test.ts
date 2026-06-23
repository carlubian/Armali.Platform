import { describe, expect, it } from 'vitest'

import { healthPageSizes, healthRoutePath } from '@/app/api/health'

import {
  diseaseRequestSchema,
  healthCategoryRequestSchema,
  healthKeys,
  medicineRequestSchema,
} from './contracts'
import {
  defaultDiseaseSort,
  defaultHealthTab,
  defaultMedicineSort,
  defaultPageSize,
  defaultSortDirection,
  parseHealthDialogState,
  parseHealthState,
  toDiseaseListQuery,
  toMedicineListQuery,
} from './healthState'

describe('health contracts', () => {
  it('freezes route, tab, pagination, and sort defaults', () => {
    expect(healthRoutePath).toBe('/health')
    expect(healthPageSizes).toEqual([10, 25, 50, 100])
    expect(defaultHealthTab).toBe('diseases')
    expect(defaultDiseaseSort).toBe('name')
    expect(defaultMedicineSort).toBe('name')
    expect(defaultSortDirection).toBe('asc')
    expect(defaultPageSize).toBe(25)
  })

  it('freezes query keys for catalogues, lists, associations, and attachments', () => {
    expect(healthKeys.diseaseCategories()).toEqual(['health', 'disease-categories'])
    expect(healthKeys.medicineCategories()).toEqual(['health', 'medicine-categories'])
    expect(healthKeys.diseaseList({ page: 1 })).toEqual([
      'health',
      'diseases',
      'list',
      { page: 1 },
    ])
    expect(healthKeys.diseaseMedicines(12)).toEqual([
      'health',
      'diseases',
      12,
      'medicines',
    ])
    expect(healthKeys.medicineDiseases(8)).toEqual([
      'health',
      'medicines',
      8,
      'diseases',
    ])
    expect(healthKeys.medicineAttachments(8)).toEqual([
      'health',
      'medicines',
      8,
      'attachments',
    ])
  })

  it('parses URL-backed two-tab state with independent list parameters', () => {
    const state = parseHealthState(
      new URLSearchParams(
        'tab=medicines&diseaseSearch=flu&diseaseCategory=2&diseaseVisibility=Private&diseaseCreator=9&diseaseSort=category&diseaseSortDirection=desc&diseasePage=3&diseasePageSize=50&medicineSearch=ibu&medicineCategory=4&requiresPrescription=true&medicineVisibility=Public&medicineCreator=9&medicineSort=category&medicinePage=2&medicinePageSize=100',
      ),
      9,
    )

    expect(state.tab).toBe('medicines')
    expect(toDiseaseListQuery(state.diseases, 9)).toEqual({
      search: 'flu',
      category: 2,
      visibility: 'Private',
      creator: 9,
      page: 3,
      pageSize: 50,
      sort: 'category',
      sortDirection: 'desc',
    })
    expect(toMedicineListQuery(state.medicines, 9)).toEqual({
      search: 'ibu',
      category: 4,
      requiresPrescription: true,
      visibility: 'Public',
      creator: 9,
      page: 2,
      pageSize: 100,
      sort: 'category',
      sortDirection: 'asc',
    })
  })

  it('parses disease and medicine dialog state separately from list state', () => {
    expect(parseHealthDialogState(new URLSearchParams('newDisease=true'))).toEqual({
      mode: 'createDisease',
    })
    expect(parseHealthDialogState(new URLSearchParams('diseaseId=12'))).toEqual({
      mode: 'editDisease',
      diseaseId: 12,
    })
    expect(parseHealthDialogState(new URLSearchParams('newMedicine=true'))).toEqual({
      mode: 'createMedicine',
    })
    expect(parseHealthDialogState(new URLSearchParams('medicineId=8'))).toEqual({
      mode: 'editMedicine',
      medicineId: 8,
    })
  })

  it('validates disease request boundaries including average duration bounds', () => {
    const request = diseaseRequestSchema.parse({
      name: ' Flu ',
      categoryId: 1,
      symptoms: '',
      averageDurationDays: 7,
      notes: '',
      visibility: 'Public',
    })

    expect(request.name).toBe('Flu')
    expect(request.symptoms).toBeNull()
    expect(request.notes).toBeNull()
    expect(
      diseaseRequestSchema.safeParse({ ...request, averageDurationDays: 0 }).success,
    ).toBe(false)
    expect(
      diseaseRequestSchema.safeParse({
        ...request,
        averageDurationDays: 100_001,
      }).success,
    ).toBe(false)
  })

  it('validates medicine request boundaries and prescription default', () => {
    const request = medicineRequestSchema.parse({
      name: ' Ibuprofen ',
      categoryId: 1,
      posology: '',
      inventoryItemId: null,
      notes: '',
      visibility: 'Private',
    })

    expect(request.name).toBe('Ibuprofen')
    expect(request.requiresPrescription).toBe(false)
    expect(request.posology).toBeNull()
    expect(request.notes).toBeNull()
  })

  it('validates Health category names', () => {
    expect(healthCategoryRequestSchema.parse({ name: ' Chronic ' }).name).toBe(
      'Chronic',
    )
    expect(healthCategoryRequestSchema.safeParse({ name: '' }).success).toBe(false)
  })
})
