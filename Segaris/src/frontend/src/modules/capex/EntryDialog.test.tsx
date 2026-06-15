import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import type { CapexAttachment, CapexEntry } from '@/app/api/capex'

const session = {
  userId: 7,
  userName: 'marina',
  displayName: 'Marina Velasco',
  language: 'en-GB',
  roles: ['User'],
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

function makeDetail(overrides: Partial<CapexEntry> = {}): CapexEntry {
  return {
    id: 5,
    title: 'Existing entry',
    movementType: 'Expense',
    status: 'Planning',
    dueDate: '2026-06-10',
    categoryId: 14,
    categoryName: 'Other',
    supplierId: null,
    supplierName: null,
    costCenterId: null,
    costCenterName: null,
    currencyId: 1,
    currencyCode: 'EUR',
    notes: null,
    visibility: 'Public',
    totalAmount: 20,
    items: [
      {
        id: 1,
        position: 0,
        description: 'Existing entry',
        quantity: 1,
        unitAmount: 20,
        lineAmount: 20,
      },
    ],
    attachments: [],
    createdById: 7,
    createdByName: 'Marina Velasco',
    createdAt: '2026-06-01T00:00:00Z',
    updatedById: null,
    updatedByName: null,
    updatedAt: '2026-06-01T00:00:00Z',
    ...overrides,
  }
}

function makeAttachment(overrides: Partial<CapexAttachment> = {}): CapexAttachment {
  return {
    id: '1',
    fileName: 'receipt.pdf',
    contentType: 'application/pdf',
    size: 2048,
    createdById: 7,
    createdAt: '2026-06-01T00:00:00Z',
    ...overrides,
  }
}

function fileNameOf(body: BodyInit | null | undefined): string {
  if (body instanceof FormData) {
    const file = body.get('file')
    if (file instanceof File) return file.name
  }
  return 'upload.pdf'
}

interface BackendOptions {
  entry?: CapexEntry
  createResponse?: () => Response
  updateResponse?: () => Response
  deleteEntryResponse?: () => Response
  attachments?: CapexAttachment[]
  /** Per-call upload override; return a non-ok Response to fail that upload. */
  uploadResponse?: (call: number) => Response | undefined
}

function mockBackend(options: BackendOptions = {}) {
  const calls: Array<{ method: string; url: string; body?: unknown }> = []
  let attachments: CapexAttachment[] = [...(options.attachments ?? [])]
  let nextAttachmentId = 100
  let uploadCall = 0

  const fetchMock = vi
    .spyOn(globalThis, 'fetch')
    .mockImplementation(async (input, init) => {
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

      if (url.startsWith('/api/capex/categories')) {
        return json([
          { id: 14, code: 'OTHER', name: 'Other' },
          { id: 1, code: 'FURNITURE', name: 'Furniture' },
        ])
      }
      if (url.startsWith('/api/configuration/suppliers')) {
        return json([{ id: 1, code: 'AMAZON', name: 'Amazon' }])
      }
      if (url.startsWith('/api/configuration/cost-centers')) {
        return json([{ id: 1, code: 'HOUSEHOLD', name: 'Household' }])
      }
      if (url.startsWith('/api/configuration/currencies')) {
        return json([
          { id: 1, code: 'EUR', name: 'Euro' },
          { id: 2, code: 'USD', name: 'US Dollar' },
        ])
      }

      // Attachment routes are matched before the generic entry routes so an
      // upload POST is not mistaken for an entry create.
      const attachMatch = url.match(
        /^\/api\/capex\/entries\/(\d+)\/attachments(?:\/(\d+))?(?:\?.*)?$/,
      )
      if (attachMatch) {
        const attachmentId = attachMatch[2]
        if (method === 'GET') return json(attachments)
        if (method === 'POST') {
          calls.push({ method, url })
          const override = options.uploadResponse?.(uploadCall++)
          if (override != null) return override
          const created = makeAttachment({
            id: String(nextAttachmentId++),
            fileName: fileNameOf(init?.body),
          })
          attachments = [...attachments, created]
          return json(created, 201)
        }
        if (method === 'DELETE' && attachmentId != null) {
          calls.push({ method, url })
          attachments = attachments.filter((item) => item.id !== attachmentId)
          return new Response(null, { status: 204 })
        }
      }

      const detailMatch = url.match(/^\/api\/capex\/entries\/(\d+)(?:\?.*)?$/)
      if (detailMatch && method === 'GET') {
        return json(options.entry ?? makeDetail())
      }
      if (detailMatch && method === 'PUT') {
        calls.push({ method, url, body })
        return (
          options.updateResponse?.() ??
          json(makeDetail({ ...(body as object), id: Number(detailMatch[1]) }))
        )
      }
      if (detailMatch && method === 'DELETE') {
        calls.push({ method, url })
        return options.deleteEntryResponse?.() ?? new Response(null, { status: 204 })
      }

      if (url.startsWith('/api/capex/entries') && method === 'POST') {
        calls.push({ method, url, body })
        return (
          options.createResponse?.() ??
          json(makeDetail({ ...(body as object), id: 99 }), 201)
        )
      }

      if (url.startsWith('/api/capex/entries') && method === 'GET') {
        calls.push({ method, url })
        return json({ items: [], page: 1, pageSize: 25, totalCount: 0 })
      }

      throw new Error(`Unexpected request: ${method} ${url}`)
    })

  return { fetchMock, calls }
}

beforeEach(() => {
  appQueryClient.clear()
  window.history.replaceState({}, '', '/capex?new=true')
})

afterEach(() => vi.restoreAllMocks())

/**
 * Renders the app on the create route and waits for the editor *form* to mount.
 * The loading dialog shares the "New entry" name, so we wait for the Title field
 * (unique to the editor) before returning the resolved dialog element.
 */
async function openCreate(): Promise<HTMLElement> {
  render(<App />)
  const title = await screen.findByLabelText('Title')
  return title.closest('[role="dialog"]') as HTMLElement
}

/** Renders the app on an existing entry and resolves the editor form dialog. */
async function openEdit(): Promise<HTMLElement> {
  window.history.replaceState({}, '', '/capex?entryId=5')
  render(<App />)
  const title = await screen.findByLabelText('Title', {}, { timeout: 5000 })
  return title.closest('[role="dialog"]') as HTMLElement
}

function fileInputOf(dialog: HTMLElement): HTMLInputElement {
  return dialog.querySelector('input[type="file"]') as HTMLInputElement
}

function pdf(name: string): File {
  return new File(['%PDF-1.4'], name, { type: 'application/pdf' })
}

describe('Capex entry editor — creation', () => {
  it('applies the creation defaults', async () => {
    mockBackend()
    const dialog = await openCreate()
    const fields = within(dialog)

    expect(fields.getByRole('radio', { name: 'Expense' })).toBeChecked()
    expect(fields.getByLabelText('Status')).toHaveValue('Planning')
    expect(fields.getByLabelText('Currency')).toHaveValue('1')
    expect(fields.getByRole('radio', { name: 'Public' })).toBeChecked()
    expect(fields.getByLabelText('Date')).toHaveDisplayValue(/^\d{4}-\d{2}-\d{2}$/)
  })

  it('seeds the item description from the title and lets it diverge once itemized', async () => {
    const user = userEvent.setup()
    mockBackend()
    const dialog = await openCreate()
    const fields = within(dialog)

    // Simplified view: the title doubles as the single item's description, which
    // is not shown separately.
    const title = fields.getByLabelText('Title')
    await user.type(title, 'Books')
    expect(fields.queryByLabelText('Description')).not.toBeInTheDocument()

    // Itemising carries the title into the first item's description.
    await user.click(fields.getByRole('button', { name: 'Add item' }))
    const rows = within(fields.getByRole('list')).getAllByRole('listitem')
    const firstDescription = within(rows[0]).getByLabelText('Description')
    expect(firstDescription).toHaveValue('Books')

    // From here the description and title are independent.
    await user.clear(firstDescription)
    await user.type(firstDescription, 'Hardbacks')
    await user.type(title, ' set')
    expect(title).toHaveValue('Books set')
    expect(firstDescription).toHaveValue('Hardbacks')
  })

  it('blocks submission and shows an error when the title is empty', async () => {
    const user = userEvent.setup()
    const { calls } = mockBackend()
    const dialog = await openCreate()

    await user.click(within(dialog).getByRole('button', { name: 'Create entry' }))

    expect(await within(dialog).findByText('A title is required.')).toBeInTheDocument()
    expect(calls.some((call) => call.method === 'POST')).toBe(false)
  })

  it('itemizes, previews totals, and reorders item lines', async () => {
    const user = userEvent.setup()
    mockBackend()
    const dialog = await openCreate()
    const fields = within(dialog)

    await user.type(fields.getByLabelText('Title'), 'Office')
    await user.clear(fields.getByLabelText('Amount'))
    await user.type(fields.getByLabelText('Amount'), '10')

    await user.click(fields.getByRole('button', { name: 'Add item' }))

    // Now in itemized mode: two item rows are present.
    const rows = within(fields.getByRole('list')).getAllByRole('listitem')
    expect(rows).toHaveLength(2)

    // Fill the second line: 2 x 5.00 = 10.00.
    await user.type(within(rows[1]).getByLabelText('Description'), 'Pens')
    await user.clear(within(rows[1]).getByLabelText('Quantity'))
    await user.type(within(rows[1]).getByLabelText('Quantity'), '2')
    await user.clear(within(rows[1]).getByLabelText('Unit amount'))
    await user.type(within(rows[1]).getByLabelText('Unit amount'), '5')

    // Total preview = 10 (line 1) + 10 (line 2) = 20.
    const total = fields.getByText('Total').parentElement as HTMLElement
    expect(within(total).getByText(/20/)).toBeInTheDocument()

    // Reorder: move the second line up.
    await user.click(within(rows[1]).getByRole('button', { name: 'Move item up' }))
    const reordered = within(fields.getByRole('list')).getAllByRole('listitem')
    expect(within(reordered[0]).getByLabelText('Description')).toHaveValue('Pens')
  })

  it('creates an entry, shows a toast, and closes the dialog', async () => {
    const user = userEvent.setup()
    const { calls } = mockBackend()
    const dialog = await openCreate()
    const fields = within(dialog)

    await user.type(fields.getByLabelText('Title'), 'New television')
    await user.clear(fields.getByLabelText('Amount'))
    await user.type(fields.getByLabelText('Amount'), '500')
    await user.click(fields.getByRole('button', { name: 'Create entry' }))

    expect(await screen.findByText('Entry created')).toBeInTheDocument()
    await waitFor(() =>
      expect(
        screen.queryByRole('dialog', { name: 'New entry' }),
      ).not.toBeInTheDocument(),
    )

    const post = calls.find((call) => call.method === 'POST')
    expect(post?.body).toMatchObject({
      title: 'New television',
      movementType: 'Expense',
      items: [{ description: 'New television', quantity: 1, unitAmount: 500 }],
    })
  })

  it('keeps the dialog open and surfaces a server validation error', async () => {
    const user = userEvent.setup()
    mockBackend({
      createResponse: () => json({ code: 'capex.entry.validation' }, 400),
    })
    const dialog = await openCreate()
    const fields = within(dialog)

    await user.type(fields.getByLabelText('Title'), 'Bad entry')
    await user.click(fields.getByRole('button', { name: 'Create entry' }))

    expect(
      await fields.findByText('Please review the highlighted fields and try again.'),
    ).toBeInTheDocument()
    expect(screen.getByRole('dialog', { name: 'New entry' })).toBeInTheDocument()
  })

  it('confirms before discarding a dirty editor', async () => {
    const user = userEvent.setup()
    mockBackend()
    const dialog = await openCreate()

    await user.type(within(dialog).getByLabelText('Title'), 'Draft')
    await user.keyboard('{Escape}')

    expect(
      await screen.findByRole('dialog', { name: 'Discard changes?' }),
    ).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Discard' }))

    await waitFor(() =>
      expect(
        screen.queryByRole('dialog', { name: 'New entry' }),
      ).not.toBeInTheDocument(),
    )
  })
})

describe('Capex entry editor — editing and accessibility', () => {
  it('loads an existing entry and disables visibility for non-creators', async () => {
    window.history.replaceState({}, '', '/capex?entryId=5')
    mockBackend({ entry: makeDetail({ createdById: 999, visibility: 'Public' }) })
    render(<App />)

    // Wait for the form (the loading dialog shares the "Edit entry" name).
    const title = await screen.findByLabelText('Title', {}, { timeout: 5000 })
    const dialog = title.closest('[role="dialog"]') as HTMLElement
    expect(title).toHaveValue('Existing entry')
    expect(within(dialog).getByRole('radio', { name: 'Public' })).toBeDisabled()
  })

  it('names the dialog and associates the title error with the field', async () => {
    const user = userEvent.setup()
    mockBackend()
    const dialog = await openCreate()
    const fields = within(dialog)

    expect(dialog).toHaveAccessibleName('New entry')

    const title = fields.getByLabelText('Title')
    await user.click(fields.getByRole('button', { name: 'Create entry' }))

    await fields.findByText('A title is required.')
    expect(title).toHaveAttribute('aria-invalid', 'true')
    expect(title).toHaveAccessibleDescription('A title is required.')
  })
})

describe('Capex entry editor — attachments and deletion', () => {
  it('lists, uploads, and removes attachments while editing', async () => {
    const user = userEvent.setup()
    mockBackend({
      entry: makeDetail({ id: 5 }),
      attachments: [makeAttachment({ id: '1', fileName: 'receipt.pdf' })],
    })
    const dialog = await openEdit()
    const fields = within(dialog)

    // The existing attachment is listed.
    expect(await fields.findByText('receipt.pdf')).toBeInTheDocument()

    // Uploading a valid file adds it to the list.
    await user.upload(fileInputOf(dialog), pdf('invoice.pdf'))
    expect(await fields.findByText('invoice.pdf')).toBeInTheDocument()

    // Removing the original attachment drops it from the list.
    const row = fields.getByText('receipt.pdf').closest('li') as HTMLElement
    await user.click(within(row).getByRole('button', { name: 'Remove attachment' }))
    await waitFor(() =>
      expect(fields.queryByText('receipt.pdf')).not.toBeInTheDocument(),
    )
  })

  it('surfaces a failed upload and retries it', async () => {
    const user = userEvent.setup()
    mockBackend({
      entry: makeDetail({ id: 5 }),
      uploadResponse: (call) =>
        call === 0 ? json({ code: 'capex.attachment.invalid' }, 400) : undefined,
    })
    const dialog = await openEdit()
    const fields = within(dialog)

    await user.upload(fileInputOf(dialog), pdf('invoice.pdf'))
    expect(await fields.findByText('Upload failed. Please retry.')).toBeInTheDocument()

    // The retry uses the default (successful) upload path.
    await user.click(fields.getByRole('button', { name: 'Retry upload' }))
    await waitFor(() =>
      expect(
        fields.queryByText('Upload failed. Please retry.'),
      ).not.toBeInTheDocument(),
    )
  })

  it('deletes the entry after an explicit confirmation', async () => {
    const user = userEvent.setup()
    const { calls } = mockBackend({ entry: makeDetail({ id: 5, title: 'Old TV' }) })
    const dialog = await openEdit()

    await user.click(within(dialog).getByRole('button', { name: 'Delete entry' }))
    const confirm = await screen.findByRole('dialog', { name: 'Delete this entry?' })
    await user.click(within(confirm).getByRole('button', { name: 'Delete entry' }))

    expect(await screen.findByText('Entry deleted')).toBeInTheDocument()
    await waitFor(() =>
      expect(
        screen.queryByRole('dialog', { name: 'Edit entry' }),
      ).not.toBeInTheDocument(),
    )
    expect(
      calls.some((call) => call.method === 'DELETE' && /\/entries\/5$/.test(call.url)),
    ).toBe(true)
  })

  it('stages a file and uploads it after the entry is created', async () => {
    const user = userEvent.setup()
    const { calls } = mockBackend()
    const dialog = await openCreate()
    const fields = within(dialog)

    await user.type(fields.getByLabelText('Title'), 'New laptop')
    await user.clear(fields.getByLabelText('Amount'))
    await user.type(fields.getByLabelText('Amount'), '900')

    // Stage a file before the entry exists.
    await user.upload(fileInputOf(dialog), pdf('quote.pdf'))
    expect(fields.getByText('quote.pdf')).toBeInTheDocument()

    await user.click(fields.getByRole('button', { name: 'Create entry' }))

    // The post-create panel uploads the staged file against the new entry.
    const panel = await screen.findByRole('dialog', { name: 'Upload attachments' })
    expect(await within(panel).findByText('quote.pdf')).toBeInTheDocument()
    await waitFor(() =>
      expect(
        calls.some((call) => call.method === 'POST' && /\/attachments$/.test(call.url)),
      ).toBe(true),
    )

    await user.click(within(panel).getByRole('button', { name: 'Done' }))
    expect(await screen.findByText('Entry created')).toBeInTheDocument()
  })
})
