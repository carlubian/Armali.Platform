import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import type {
  Disease,
  DiseaseSummary,
  Medicine,
  MedicineSummary,
} from '@/app/api/health'
import type { InventoryItemSummary } from '@/app/api/inventory'

const session = {
  userId: 7,
  userName: 'marina',
  displayName: 'Marina Velasco',
  language: 'en-GB',
  roles: ['User', 'Admin'],
  avatarUrl: null as string | null,
}

function json(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

function urlOf(input: RequestInfo | URL): string {
  return typeof input === 'string'
    ? input
    : input instanceof URL
      ? input.href
      : input.url
}

function makeDisease(
  id: number,
  overrides: Partial<DiseaseSummary> = {},
): DiseaseSummary {
  return {
    id,
    name: `Disease ${id.toString().padStart(2, '0')}`,
    categoryId: 1,
    categoryName: 'Respiratory',
    visibility: 'Public',
    associatedMedicineCount: 0,
    creatorId: 7,
    creatorName: 'Marina Velasco',
    ...overrides,
  }
}

function makeDiseaseDetail(id: number, overrides: Partial<Disease> = {}): Disease {
  return {
    ...makeDisease(id),
    symptoms: 'Cough and fever.',
    averageDurationDays: 7,
    notes: null,
    createdAt: '2026-01-01T00:00:00Z',
    updatedById: null,
    updatedByName: null,
    updatedAt: null,
    ...overrides,
  }
}

function makeMedicine(
  id: number,
  overrides: Partial<MedicineSummary> = {},
): MedicineSummary {
  return {
    id,
    name: `Medicine ${id.toString().padStart(2, '0')}`,
    categoryId: 1,
    categoryName: 'Painkiller',
    requiresPrescription: false,
    inventoryItemId: null,
    inventoryItemName: null,
    visibility: 'Public',
    thumbnail: { attachmentId: null, url: null, source: 'placeholder' },
    creatorId: 7,
    creatorName: 'Marina Velasco',
    ...overrides,
  }
}

function makeMedicineDetail(id: number, overrides: Partial<Medicine> = {}): Medicine {
  return {
    ...makeMedicine(id),
    posology: 'After meals.',
    notes: null,
    attachments: [],
    createdAt: '2026-01-01T00:00:00Z',
    updatedById: null,
    updatedByName: null,
    updatedAt: null,
    ...overrides,
  }
}

function makeInventoryItem(
  id: number,
  overrides: Partial<InventoryItemSummary> = {},
): InventoryItemSummary {
  return {
    id,
    name: `Inventory item ${id}`,
    status: 'Active',
    categoryId: 1,
    categoryName: 'Medicine',
    locationId: 1,
    locationName: 'Bathroom',
    currentStock: 1,
    minimumStock: 0,
    visibility: 'Public',
    creatorId: 7,
    creatorName: 'Marina Velasco',
    ...overrides,
  }
}

interface BackendOptions {
  diseases?: DiseaseSummary[]
  medicines?: MedicineSummary[]
  diseaseMedicines?: MedicineSummary[]
  medicineDiseases?: DiseaseSummary[]
  inventoryItems?: InventoryItemSummary[]
}

function mockBackend(options: BackendOptions = {}) {
  const diseases = options.diseases ?? [makeDisease(1)]
  const medicines = options.medicines ?? [makeMedicine(1)]
  const diseaseMedicines = options.diseaseMedicines ?? []
  const medicineDiseases = options.medicineDiseases ?? []
  const inventoryItems = options.inventoryItems ?? [makeInventoryItem(1)]
  const requests: Array<{ method: string; url: string; body?: unknown }> = []

  vi.spyOn(globalThis, 'fetch').mockImplementation(async (input, init) => {
    await Promise.resolve()
    const url = urlOf(input)
    const method = init?.method ?? 'GET'
    const body: unknown =
      typeof init?.body === 'string' ? (JSON.parse(init.body) as unknown) : undefined

    if (url === '/api/session/antiforgery') return json({ csrfToken: 'token' })
    if (url === '/api/session' && method === 'GET') return json(session)
    if (url === '/api/session/profile' && method === 'GET') {
      return json({
        displayName: session.displayName,
        language: session.language,
        avatarUrl: session.avatarUrl,
      })
    }
    if (url.startsWith('/api/launcher/attention')) return json({ modules: [] })
    if (url.startsWith('/api/health/disease-categories')) {
      return json([{ id: 1, name: 'Respiratory', sortOrder: 1 }])
    }
    if (url.startsWith('/api/health/medicine-categories')) {
      return json([{ id: 1, name: 'Painkiller', sortOrder: 1 }])
    }
    if (url.startsWith('/api/inventory/categories')) {
      return json([{ id: 1, name: 'Medicine', sortOrder: 1 }])
    }
    if (url.startsWith('/api/inventory/locations')) {
      return json([{ id: 1, name: 'Bathroom', sortOrder: 1 }])
    }
    if (url.startsWith('/api/inventory/items') && method === 'GET') {
      requests.push({ method, url })
      const params = new URL(url, 'http://localhost').searchParams
      const page = Number(params.get('page') ?? '1')
      const pageSize = Number(params.get('pageSize') ?? '10')
      return json({
        items: inventoryItems,
        page,
        pageSize,
        totalCount: inventoryItems.length,
      })
    }
    const assoc = url.match(/^\/api\/health\/diseases\/(\d+)\/medicines\/(\d+)$/)
    if (assoc != null && (method === 'POST' || method === 'DELETE')) {
      requests.push({ method, url })
      return new Response(null, { status: 204 })
    }
    if (url.match(/^\/api\/health\/diseases\/\d+\/medicines$/) && method === 'GET') {
      requests.push({ method, url })
      return json(diseaseMedicines)
    }
    const medicineAssoc = url.match(
      /^\/api\/health\/medicines\/(\d+)\/diseases\/(\d+)$/,
    )
    if (medicineAssoc != null && (method === 'POST' || method === 'DELETE')) {
      requests.push({ method, url })
      return new Response(null, { status: 204 })
    }
    if (url.match(/^\/api\/health\/medicines\/\d+\/diseases$/) && method === 'GET') {
      requests.push({ method, url })
      return json(medicineDiseases)
    }
    if (url.match(/^\/api\/health\/medicines\/\d+\/attachments$/) && method === 'GET') {
      requests.push({ method, url })
      return json([])
    }
    if (url.match(/^\/api\/health\/diseases\/\d+$/) && method === 'GET') {
      requests.push({ method, url })
      const id = Number(url.match(/\/diseases\/(\d+)/)?.[1] ?? '1')
      return json(makeDiseaseDetail(id))
    }
    if (url.match(/^\/api\/health\/diseases\/\d+$/) && method === 'PUT') {
      requests.push({ method, url, body })
      const id = Number(url.match(/\/diseases\/(\d+)/)?.[1] ?? '1')
      return json({ ...makeDiseaseDetail(id), ...(body as object) })
    }
    if (url.match(/^\/api\/health\/diseases\/\d+$/) && method === 'DELETE') {
      requests.push({ method, url })
      return new Response(null, { status: 204 })
    }
    if (url === '/api/health/diseases' && method === 'POST') {
      requests.push({ method, url, body })
      return json({ ...makeDiseaseDetail(99), ...(body as object), id: 99 }, 201)
    }
    if (url.match(/^\/api\/health\/medicines\/\d+$/) && method === 'GET') {
      requests.push({ method, url })
      const id = Number(url.match(/\/medicines\/(\d+)/)?.[1] ?? '1')
      return json(makeMedicineDetail(id))
    }
    if (url.match(/^\/api\/health\/medicines\/\d+$/) && method === 'PUT') {
      requests.push({ method, url, body })
      const id = Number(url.match(/\/medicines\/(\d+)/)?.[1] ?? '1')
      return json({ ...makeMedicineDetail(id), ...(body as object) })
    }
    if (url.match(/^\/api\/health\/medicines\/\d+$/) && method === 'DELETE') {
      requests.push({ method, url })
      return new Response(null, { status: 204 })
    }
    if (url === '/api/health/medicines' && method === 'POST') {
      requests.push({ method, url, body })
      return json({ ...makeMedicineDetail(88), ...(body as object), id: 88 }, 201)
    }
    if (url.startsWith('/api/health/diseases') && method === 'GET') {
      requests.push({ method, url })
      const params = new URL(url, 'http://localhost').searchParams
      const page = Number(params.get('page') ?? '1')
      const pageSize = Number(params.get('pageSize') ?? '25')
      const search = params.get('search')?.toLowerCase() ?? ''
      const category = params.get('category')
      const filtered = diseases.filter(
        (disease) =>
          (search === '' || disease.name.toLowerCase().includes(search)) &&
          (category == null || String(disease.categoryId) === category),
      )
      return json({ items: filtered, page, pageSize, totalCount: filtered.length })
    }
    if (url.startsWith('/api/health/medicines') && method === 'GET') {
      requests.push({ method, url })
      const params = new URL(url, 'http://localhost').searchParams
      const page = Number(params.get('page') ?? '1')
      const pageSize = Number(params.get('pageSize') ?? '25')
      const search = params.get('search')?.toLowerCase() ?? ''
      const prescription = params.get('requiresPrescription')
      const filtered = medicines.filter(
        (medicine) =>
          (search === '' || medicine.name.toLowerCase().includes(search)) &&
          (prescription == null ||
            String(medicine.requiresPrescription) === prescription),
      )
      return json({ items: filtered, page, pageSize, totalCount: filtered.length })
    }

    throw new Error(`Unexpected request: ${method} ${url}`)
  })

  return { requests }
}

beforeEach(() => {
  appQueryClient.clear()
  window.history.replaceState({}, '', '/health')
})

afterEach(() => vi.restoreAllMocks())

describe('Health page — diseases tab', () => {
  it('renders the disease table with category, count, and visibility', async () => {
    mockBackend({
      diseases: [makeDisease(1, { name: 'Bronchitis', associatedMedicineCount: 2 })],
    })
    render(<App />)

    expect(await screen.findByText('Bronchitis')).toBeInTheDocument()
    const table = screen.getByRole('table')
    expect(within(table).getByText('Respiratory')).toBeInTheDocument()
    expect(within(table).getByText('2 medicines')).toBeInTheDocument()
    expect(within(table).getByText('Public')).toBeInTheDocument()
  })

  it('filters the list by name through the query', async () => {
    mockBackend({
      diseases: [
        makeDisease(1, { name: 'Bronchitis' }),
        makeDisease(2, { name: 'Flu' }),
      ],
    })
    const user = userEvent.setup()
    render(<App />)

    expect(await screen.findByText('Bronchitis')).toBeInTheDocument()
    await user.type(screen.getByLabelText('Search diseases'), 'Flu')

    await waitFor(() => {
      expect(screen.queryByText('Bronchitis')).not.toBeInTheDocument()
    })
    expect(screen.getByText('Flu')).toBeInTheDocument()
  })

  it('toggles the name sort direction', async () => {
    const { requests } = mockBackend()
    const user = userEvent.setup()
    render(<App />)

    expect(await screen.findByText('Disease 01')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Name' }))

    await waitFor(() => {
      expect(
        requests.some((request) => request.url.includes('sortDirection=desc')),
      ).toBe(true)
    })
  })

  it('switches to the medicines tab gallery', async () => {
    mockBackend({
      medicines: [
        makeMedicine(1, {
          name: 'Ibuprofen',
          requiresPrescription: true,
          inventoryItemId: 4,
          inventoryItemName: 'Bathroom ibuprofen',
        }),
      ],
    })
    const user = userEvent.setup()
    render(<App />)

    expect(await screen.findByText('Disease 01')).toBeInTheDocument()
    await user.click(screen.getByRole('tab', { name: /Medicines/ }))
    expect(await screen.findByText('Ibuprofen')).toBeInTheDocument()
    expect(screen.getAllByText('Prescription').length).toBeGreaterThan(1)
    expect(screen.getByText('Bathroom ibuprofen')).toBeInTheDocument()
  })

  it('validates the name before creating a disease', async () => {
    mockBackend()
    const user = userEvent.setup()
    render(<App />)

    await screen.findByText('Disease 01')
    await user.click(screen.getByRole('button', { name: 'New disease' }))

    const dialog = await screen.findByRole('dialog', { name: 'New disease' })
    await user.click(within(dialog).getByRole('button', { name: 'Create disease' }))
    expect(await within(dialog).findByText('Enter a disease name.')).toBeInTheDocument()
  })

  it('creates a disease and associates a medicine via the multi-select selector', async () => {
    const { requests } = mockBackend({
      medicines: [makeMedicine(5, { name: 'Ibuprofen' })],
    })
    const user = userEvent.setup()
    render(<App />)

    await screen.findByText('Disease 01')
    await user.click(screen.getByRole('button', { name: 'New disease' }))
    const dialog = await screen.findByRole('dialog', { name: 'New disease' })

    await user.type(within(dialog).getByLabelText(/Name/), 'Migraine')

    // Open the multi-select medicine selector and add a medicine.
    await user.click(within(dialog).getByRole('button', { name: 'Add medicines' }))
    const selector = await screen.findByRole('dialog', { name: /Select medicines/ })
    const ibuprofenRow = within(selector)
      .getByText('Ibuprofen')
      .closest('[role="row"]') as HTMLElement
    await user.click(within(ibuprofenRow).getByRole('button', { name: 'Add' }))
    expect(within(selector).getByText('1 selected')).toBeInTheDocument()
    await user.click(within(selector).getByRole('button', { name: 'Done' }))

    // The staged medicine appears as a chip in the editor.
    expect(await within(dialog).findByText('Ibuprofen')).toBeInTheDocument()

    await user.click(within(dialog).getByRole('button', { name: 'Create disease' }))

    await waitFor(() => {
      expect(
        requests.some(
          (request) =>
            request.method === 'POST' && request.url === '/api/health/diseases',
        ),
      ).toBe(true)
    })
    // The association is applied to the newly created disease (id 99).
    await waitFor(() => {
      expect(
        requests.some(
          (request) =>
            request.method === 'POST' &&
            request.url === '/api/health/diseases/99/medicines/5',
        ),
      ).toBe(true)
    })
  })

  it('creates a medicine and associates a disease from the medicine side', async () => {
    const { requests } = mockBackend({
      diseases: [makeDisease(3, { name: 'Migraine' })],
    })
    const user = userEvent.setup()
    window.history.replaceState({}, '', '/health?tab=medicines')
    render(<App />)

    await screen.findByText('Medicine 01')
    await user.click(screen.getByRole('button', { name: 'New medicine' }))
    const dialog = await screen.findByRole('dialog', { name: 'New medicine' })

    await user.type(within(dialog).getByLabelText(/Name/), 'Paracetamol')
    await user.click(
      within(dialog).getByRole('checkbox', { name: 'Requires prescription' }),
    )
    await user.click(within(dialog).getByRole('button', { name: 'Add diseases' }))

    const selector = await screen.findByRole('dialog', { name: /Select diseases/ })
    const migraineRow = within(selector)
      .getByText('Migraine')
      .closest('[role="row"]') as HTMLElement
    await user.click(within(migraineRow).getByRole('button', { name: 'Add' }))
    await user.click(within(selector).getByRole('button', { name: 'Done' }))

    expect(await within(dialog).findByText('Migraine')).toBeInTheDocument()
    await user.click(within(dialog).getByRole('button', { name: 'Create medicine' }))

    await waitFor(() => {
      expect(
        requests.some(
          (request) =>
            request.method === 'POST' && request.url === '/api/health/medicines',
        ),
      ).toBe(true)
    })
    await waitFor(() => {
      expect(
        requests.some(
          (request) =>
            request.method === 'POST' &&
            request.url === '/api/health/medicines/88/diseases/3',
        ),
      ).toBe(true)
    })
  })
})

describe('Configuration — Health disease categories', () => {
  beforeEach(() => {
    window.history.replaceState({}, '', '/configuration/health')
  })

  it('renders the disease category catalogue for administrators', async () => {
    mockBackend()
    render(<App />)

    expect(
      await screen.findByRole('heading', { name: 'Disease categories' }),
    ).toBeInTheDocument()
    expect(
      await screen.findByRole('tab', { name: 'Medicine categories' }),
    ).toBeInTheDocument()
    expect(await screen.findByText('Respiratory')).toBeInTheDocument()
  })
})
