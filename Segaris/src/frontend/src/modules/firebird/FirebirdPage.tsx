import { zodResolver } from '@hookform/resolvers/zod'
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query'
import {
  ArrowDownAZ,
  ArrowUpZA,
  AtSign,
  Cake,
  Camera,
  ChevronLeft,
  ChevronRight,
  Globe,
  Lock,
  MessagesSquare,
  Pencil,
  Plus,
  Search,
  Trash2,
  UserRound,
} from 'lucide-react'
import { useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'

import {
  firebirdApi,
  firebirdPageSizes,
  firebirdPersonStatuses,
  firebirdVisibilities,
  type CreatePersonRequest,
  type FirebirdPageSize,
  type FirebirdPersonSortField,
  type FirebirdPersonStatus,
  type FirebirdVisibility,
  type Interaction,
  type InteractionRequest,
  type PersonCategory,
  type PersonSummary,
  type Person,
  type Username,
  type UsernamePlatform,
  type UsernameRequest,
} from '@/app/api/firebird'
import { isApiError } from '@/app/api/errors'
import { useSession } from '@/app/session/SessionContext'
import { ServiceUnavailable } from '@/components/feedback/SystemScreens'
import {
  Avatar,
  Badge,
  Button,
  Dialog,
  IconButton,
  Input,
  SegmentedControl,
  Select,
  Spinner,
  Toast,
  Tooltip,
  type BadgeTone,
  type SegmentTone,
} from '@/components/ui'

import { firebirdKeys } from './contracts'
import {
  activePeopleFilterCount,
  type PeopleFilterPatch,
  type PeopleState,
  type PersonDialogState,
  usePeopleState,
} from './peopleState'
import {
  buildDefaults,
  createPersonSchema,
  daysInMonth,
  fromPerson,
  toRequest,
  type PersonFormValues,
} from './personForm'
import { usePersonCategories, useUsernamePlatforms } from './queries'

import './FirebirdPage.css'

type ToastKind =
  | 'created'
  | 'updated'
  | 'deleted'
  | 'usernameSaved'
  | 'usernameDeleted'
  | 'interactionSaved'
  | 'interactionDeleted'

interface ToastState {
  kind: ToastKind
  name: string
}

// The gallery offers the two sort fields the register reads by: alphabetical
// name and calendar birthday. The URL state supports more, but these match the
// avatar gallery's day-to-day use.
const sortFields: FirebirdPersonSortField[] = ['name', 'birthday']

const statusToneMap: Record<FirebirdPersonStatus, BadgeTone> = {
  Unknown: 'neutral',
  Active: 'success',
  Unavailable: 'gold',
  Blocked: 'danger',
}

const visibilityMeta: Record<
  FirebirdVisibility,
  { icon: ReactNode; tone: SegmentTone }
> = {
  Public: { icon: <Globe size={15} />, tone: 'accent' },
  Private: { icon: <Lock size={15} />, tone: 'neutral' },
}

function birthdayLabel(month: number, day: number, locale: string): string {
  // 2020 is a leap year, so 29 February formats correctly; the year is never shown.
  return new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'short' }).format(
    new Date(2020, month - 1, day),
  )
}

export function FirebirdPage() {
  const { t, i18n } = useTranslation('firebird')
  const { session } = useSession()
  const queryClient = useQueryClient()
  const currentUserId = session?.userId ?? null
  const [toast, setToast] = useState<ToastState | null>(null)

  const {
    state,
    dialog,
    listQuery,
    setFilters,
    setSort,
    setPage,
    setPageSize,
    clearFilters,
    openCreateDialog,
    openEditDialog,
    openUsernamesDialog,
    openInteractionsDialog,
    closeDialog,
  } = usePeopleState(currentUserId)

  const categories = usePersonCategories()
  const peopleQuery = useQuery({
    queryKey: firebirdKeys.personList(listQuery),
    queryFn: ({ signal }) => firebirdApi.listPeople(listQuery, signal),
    placeholderData: keepPreviousData,
  })

  const data = peopleQuery.data
  const people = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = Math.max(1, Math.ceil(totalCount / state.pageSize))
  const hasFilters = activePeopleFilterCount(state) > 0

  const invalidatePeople = (personId?: number) => {
    void queryClient.invalidateQueries({ queryKey: firebirdKeys.people() })
    if (personId != null)
      void queryClient.invalidateQueries({ queryKey: firebirdKeys.person(personId) })
  }

  const handleSaved = (person: Person, mode: 'create' | 'edit') => {
    queryClient.setQueryData(firebirdKeys.person(person.id), person)
    invalidatePeople(person.id)
    setToast({ kind: mode === 'create' ? 'created' : 'updated', name: person.name })
    closeDialog()
  }

  const handleDeleted = (person: Person) => {
    invalidatePeople()
    setToast({ kind: 'deleted', name: person.name })
    closeDialog()
  }

  const closeSubEntityDialog = () => {
    if (dialog.mode === 'usernames' || dialog.mode === 'interactions') {
      if (dialog.returnToEdit) {
        openEditDialog(dialog.personId)
        return
      }

      closeDialog()
      return
    }
    closeDialog()
  }

  const handleSubEntityChanged = (kind: ToastKind, name: string) => {
    setToast({ kind, name })
  }

  useEffect(() => {
    if (data != null && state.page > totalPages) setPage(totalPages)
  }, [data, state.page, totalPages, setPage])

  if (peopleQuery.isError) {
    const error = peopleQuery.error
    if (isApiError(error) && ['unavailable', 'transient'].includes(error.kind)) {
      return <ServiceUnavailable onRetry={() => void peopleQuery.refetch()} />
    }
  }

  return (
    <main className="seg-firebird armali-aurora">
      <section className="seg-firebird__head">
        <div>
          <div className="armali-eyebrow">{t('page.eyebrow')}</div>
          <h1>{t('page.title')}</h1>
          <p>{t('page.description')}</p>
        </div>
      </section>

      <section className="seg-firebird__panel-head">
        <Badge tone="neutral">{t('gallery.count', { count: totalCount })}</Badge>
        <Button iconLeft={<Plus size={16} />} onClick={openCreateDialog}>
          {t('gallery.newPerson')}
        </Button>
      </section>

      <PeopleFilters
        state={state}
        categories={categories.data ?? []}
        onChange={setFilters}
        onClear={clearFilters}
        onSort={setSort}
      />

      {peopleQuery.isPending ? (
        <div className="seg-firebird__loading">
          <Spinner />
        </div>
      ) : peopleQuery.isError ? (
        <p className="seg-firebird__error" role="alert">
          {t('states.loadError')}
        </p>
      ) : people.length === 0 ? (
        <p className="seg-firebird__empty">
          {hasFilters ? t('states.emptyFiltered') : t('states.empty')}
        </p>
      ) : (
        <PeopleGallery
          people={people}
          locale={i18n.language}
          busy={peopleQuery.isFetching && !peopleQuery.isPending}
          onOpen={openEditDialog}
          onUsernames={openUsernamesDialog}
          onInteractions={openInteractionsDialog}
        />
      )}

      <Pager
        page={state.page}
        pageSize={state.pageSize}
        totalPages={totalPages}
        fetching={peopleQuery.isFetching}
        onPage={setPage}
        onPageSize={setPageSize}
      />

      {(dialog.mode === 'create' || dialog.mode === 'edit') && (
        <PersonDialog
          dialog={dialog}
          currentUserId={currentUserId}
          onClose={closeDialog}
          onSaved={handleSaved}
          onDeleted={handleDeleted}
          onOpenUsernames={openUsernamesDialog}
          onOpenInteractions={openInteractionsDialog}
        />
      )}

      {dialog.mode === 'usernames' && (
        <UsernamesDialog
          personId={dialog.personId}
          onClose={closeSubEntityDialog}
          onChanged={handleSubEntityChanged}
        />
      )}

      {dialog.mode === 'interactions' && (
        <InteractionsDialog
          personId={dialog.personId}
          locale={i18n.language}
          onClose={closeSubEntityDialog}
          onChanged={handleSubEntityChanged}
        />
      )}

      {toast != null && (
        <div className="seg-firebird__toast">
          <Toast
            tone="success"
            title={t(`toast.${toast.kind}`)}
            onClose={() => setToast(null)}
            closeLabel={t('editor.actions.cancel')}
          >
            {t(`toast.${toast.kind}Body`, { name: toast.name })}
          </Toast>
        </div>
      )}
    </main>
  )
}

interface PeopleFiltersProps {
  state: PeopleState
  categories: PersonCategory[]
  onChange: (patch: PeopleFilterPatch) => void
  onClear: () => void
  onSort: (sort: FirebirdPersonSortField) => void
}

function PeopleFilters({
  state,
  categories,
  onChange,
  onClear,
  onSort,
}: PeopleFiltersProps) {
  const { t } = useTranslation('firebird')
  const count = activePeopleFilterCount(state)
  const directionIcon =
    state.sortDirection === 'asc' ? <ArrowDownAZ size={16} /> : <ArrowUpZA size={16} />

  return (
    <section
      className="seg-firebird__filters"
      aria-label={t('filters.active', { count })}
    >
      <div className="seg-firebird__filters-primary">
        <Input
          className="seg-firebird__search"
          label={t('filters.search')}
          placeholder={t('filters.searchPlaceholder')}
          iconLeft={<Search size={16} />}
          value={state.search}
          onChange={(event) => onChange({ search: event.target.value })}
        />
        <FilterSelect
          label={t('filters.category')}
          value={state.category == null ? '' : String(state.category)}
          onChange={(value) =>
            onChange({ category: value === '' ? null : Number(value) })
          }
          options={categories.map((category) => ({
            value: String(category.id),
            label: category.name,
          }))}
        />
        <FilterSelect
          label={t('filters.status')}
          value={state.status}
          onChange={(value) => onChange({ status: value as FirebirdPersonStatus | '' })}
          options={firebirdPersonStatuses.map((status) => ({
            value: status,
            label: t(`status.${status}`),
          }))}
        />
        <div className="seg-firebird__sort">
          <span className="seg-firebird__field-label">{t('sort.label')}</span>
          <div className="seg-firebird__sort-controls">
            <Select
              aria-label={t('sort.label')}
              value={state.sort === 'birthday' ? 'birthday' : 'name'}
              onChange={(event) =>
                onSort(event.target.value as FirebirdPersonSortField)
              }
              options={sortFields.map((field) => ({
                value: field,
                label: t(`sort.${field}`),
              }))}
            />
            <Tooltip
              label={
                state.sortDirection === 'asc'
                  ? t('sort.ascending')
                  : t('sort.descending')
              }
            >
              <IconButton
                variant="bare"
                label={t('sort.toggle')}
                icon={directionIcon}
                onClick={() => onSort(state.sort)}
              />
            </Tooltip>
          </div>
        </div>
        {count > 0 && (
          <Button variant="ghost" onClick={onClear}>
            {t('filters.clear')}
          </Button>
        )}
      </div>
    </section>
  )
}

interface FilterSelectProps {
  label: string
  value: string
  options: Array<{ value: string; label: string }>
  onChange: (value: string) => void
}

function FilterSelect({ label, value, options, onChange }: FilterSelectProps) {
  const { t } = useTranslation('firebird')
  return (
    <label className="seg-firebird__field">
      <span>{label}</span>
      <Select
        value={value}
        onChange={(event) => onChange(event.target.value)}
        options={[{ value: '', label: t('filters.all') }, ...options]}
      />
    </label>
  )
}

interface PeopleGalleryProps {
  people: PersonSummary[]
  locale: string
  busy: boolean
  onOpen: (personId: number) => void
  onUsernames: (personId: number) => void
  onInteractions: (personId: number) => void
}

function PeopleGallery({
  people,
  locale,
  busy,
  onOpen,
  onUsernames,
  onInteractions,
}: PeopleGalleryProps) {
  return (
    <section className="seg-firebird__gallery-wrap" aria-busy={busy}>
      <div className="seg-firebird__gallery">
        {people.map((person) => (
          <PersonCard
            key={person.id}
            person={person}
            locale={locale}
            onOpen={onOpen}
            onUsernames={onUsernames}
            onInteractions={onInteractions}
          />
        ))}
      </div>
    </section>
  )
}

interface PersonCardProps {
  person: PersonSummary
  locale: string
  onOpen: (personId: number) => void
  onUsernames: (personId: number) => void
  onInteractions: (personId: number) => void
}

function PersonCard({
  person,
  locale,
  onOpen,
  onUsernames,
  onInteractions,
}: PersonCardProps) {
  const { t } = useTranslation('firebird')
  const dim = person.status === 'Blocked'
  return (
    <article className={'seg-person-card' + (dim ? ' seg-person-card--dim' : '')}>
      <span
        className={
          'seg-person-card__vis' +
          (person.visibility === 'Private' ? ' seg-person-card__vis--private' : '')
        }
      >
        <Tooltip label={t(`visibility.${person.visibility}`)}>
          <span className="seg-person-card__vis-icon">
            {person.visibility === 'Private' ? (
              <Lock size={13} aria-hidden="true" />
            ) : (
              <Globe size={13} aria-hidden="true" />
            )}
          </span>
        </Tooltip>
      </span>
      <button
        type="button"
        className="seg-person-card__open"
        onClick={() => onOpen(person.id)}
        aria-label={t('gallery.open', { name: person.name })}
      >
        <span className="seg-person-card__top">
          <Avatar size="lg" name={person.name} src={person.avatar.url ?? undefined} />
          <span className="seg-person-card__id">
            <span className="seg-person-card__name">{person.name}</span>
            <span className="seg-person-card__meta">
              <span className="seg-person-card__cat">{person.categoryName}</span>
              <Badge tone={statusToneMap[person.status]} dot>
                {t(`status.${person.status}`)}
              </Badge>
            </span>
          </span>
        </span>
        <BirthdayLine
          month={person.birthdayMonth}
          day={person.birthdayDay}
          locale={locale}
        />
      </button>
      <div className="seg-person-card__actions">
        <button
          type="button"
          className="seg-person-card__action"
          onClick={() => onUsernames(person.id)}
          aria-label={t('gallery.openUsernames', { name: person.name })}
        >
          <AtSign size={15} aria-hidden="true" />
          {t('gallery.usernames')}
        </button>
        <button
          type="button"
          className="seg-person-card__action"
          onClick={() => onInteractions(person.id)}
          aria-label={t('gallery.openInteractions', { name: person.name })}
        >
          <MessagesSquare size={15} aria-hidden="true" />
          {t('gallery.interactions')}
        </button>
      </div>
    </article>
  )
}

interface BirthdayLineProps {
  month: number | null
  day: number | null
  locale: string
}

function BirthdayLine({ month, day, locale }: BirthdayLineProps) {
  const { t } = useTranslation('firebird')
  if (month == null || day == null) {
    return (
      <span className="seg-person-card__bday seg-person-card__bday--none">
        <Cake size={14} aria-hidden="true" />
        {t('birthday.none')}
      </span>
    )
  }
  return (
    <span className="seg-person-card__bday">
      <Cake size={14} aria-hidden="true" />
      {birthdayLabel(month, day, locale)}
    </span>
  )
}

interface PagerProps {
  page: number
  pageSize: FirebirdPageSize
  totalPages: number
  fetching: boolean
  onPage: (page: number) => void
  onPageSize: (pageSize: FirebirdPageSize) => void
}

function Pager({
  page,
  pageSize,
  totalPages,
  fetching,
  onPage,
  onPageSize,
}: PagerProps) {
  const { t } = useTranslation('firebird')
  return (
    <nav className="seg-firebird__pager" aria-label={t('pagination.label')}>
      <label className="seg-firebird__rows">
        <span>{t('pagination.rowsPerPage')}</span>
        <Select
          value={String(pageSize)}
          onChange={(event) =>
            onPageSize(Number(event.target.value) as FirebirdPageSize)
          }
          options={firebirdPageSizes.map((size) => ({
            value: String(size),
            label: String(size),
          }))}
        />
      </label>
      <div className="seg-firebird__pager-nav">
        <Button
          variant="ghost"
          size="sm"
          iconLeft={<ChevronLeft size={16} />}
          disabled={page <= 1 || fetching}
          onClick={() => onPage(Math.max(1, page - 1))}
        >
          {t('pagination.previous')}
        </Button>
        <span className="seg-firebird__page" aria-live="polite">
          {t('pagination.status', { page, pages: totalPages })}
        </span>
        <Button
          variant="ghost"
          size="sm"
          iconRight={<ChevronRight size={16} />}
          disabled={page >= totalPages || fetching}
          onClick={() => onPage(Math.min(totalPages, page + 1))}
        >
          {t('pagination.next')}
        </Button>
      </div>
    </nav>
  )
}

interface PersonDialogProps {
  dialog: PersonDialogState
  currentUserId: number | null
  onClose: () => void
  onSaved: (person: Person, mode: 'create' | 'edit') => void
  onDeleted: (person: Person) => void
  onOpenUsernames: (personId: number, options?: { returnToEdit?: boolean }) => void
  onOpenInteractions: (personId: number, options?: { returnToEdit?: boolean }) => void
}

function PersonDialog({
  dialog,
  currentUserId,
  onClose,
  onSaved,
  onDeleted,
  onOpenUsernames,
  onOpenInteractions,
}: PersonDialogProps) {
  const { t } = useTranslation('firebird')
  const mode = dialog.mode === 'create' ? 'create' : 'edit'
  const personId =
    dialog.mode === 'closed' || dialog.mode === 'create' ? undefined : dialog.personId
  const categories = usePersonCategories()
  const personQuery = useQuery({
    queryKey: firebirdKeys.person(personId as number),
    queryFn: ({ signal }) => firebirdApi.getPerson(personId as number, signal),
    enabled: mode === 'edit' && personId != null,
  })

  const title = mode === 'create' ? t('editor.createTitle') : t('editor.editTitle')
  const description =
    mode === 'create' ? t('editor.createDescription') : t('editor.editDescription')
  const catalogsReady = categories.data != null

  if (!catalogsReady || (mode === 'edit' && personQuery.isPending)) {
    return (
      <Dialog
        scrollable
        width={760}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
      >
        <div className="seg-person-editor__status">
          <Spinner />
          <span>{t('editor.loading')}</span>
        </div>
      </Dialog>
    )
  }

  if (mode === 'edit' && personQuery.isError) {
    const notFound =
      isApiError(personQuery.error) && personQuery.error.kind === 'not-found'
    return (
      <Dialog
        width={560}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
        footer={<Button onClick={onClose}>{t('editor.actions.cancel')}</Button>}
      >
        <p className="seg-person-editor__error" role="alert">
          {notFound ? t('editor.notFound') : t('editor.loadError')}
        </p>
      </Dialog>
    )
  }

  const person = mode === 'edit' ? (personQuery.data as Person) : undefined
  const initialValues =
    person != null ? fromPerson(person) : buildDefaults(firstCatalogId(categories.data))
  const canChangeVisibility =
    person == null || (currentUserId != null && person.createdById === currentUserId)

  return (
    <PersonEditorForm
      mode={mode}
      personId={personId}
      person={person}
      title={title}
      description={description}
      initialValues={initialValues}
      categories={categories.data ?? []}
      canChangeVisibility={canChangeVisibility}
      onClose={onClose}
      onSaved={onSaved}
      onDeleted={onDeleted}
      onOpenUsernames={onOpenUsernames}
      onOpenInteractions={onOpenInteractions}
    />
  )
}

function firstCatalogId(items: ReadonlyArray<{ id: number }> | undefined): string {
  const first = items?.[0]
  return first != null ? String(first.id) : ''
}

interface PersonEditorFormProps {
  mode: 'create' | 'edit'
  personId?: number
  person?: Person
  title: string
  description: string
  initialValues: PersonFormValues
  categories: PersonCategory[]
  canChangeVisibility: boolean
  onClose: () => void
  onSaved: (person: Person, mode: 'create' | 'edit') => void
  onDeleted: (person: Person) => void
  onOpenUsernames: (personId: number, options?: { returnToEdit?: boolean }) => void
  onOpenInteractions: (personId: number, options?: { returnToEdit?: boolean }) => void
}

function PersonEditorForm({
  mode,
  personId,
  person,
  title,
  description,
  initialValues,
  categories,
  canChangeVisibility,
  onClose,
  onSaved,
  onDeleted,
  onOpenUsernames,
  onOpenInteractions,
}: PersonEditorFormProps) {
  const { t } = useTranslation('firebird')
  const schema = useMemo(
    () =>
      createPersonSchema({
        nameRequired: t('editor.validation.nameRequired'),
        nameTooLong: t('editor.validation.nameTooLong'),
        categoryRequired: t('editor.validation.categoryRequired'),
        notesTooLong: t('editor.validation.notesTooLong'),
      }),
    [t],
  )
  const form = useForm<PersonFormValues>({
    resolver: zodResolver(schema),
    defaultValues: initialValues,
  })
  const { register, control, handleSubmit, formState, setValue } = form
  const hasBirthday = useWatch({ control, name: 'hasBirthday' })
  const birthdayMonth = useWatch({ control, name: 'birthdayMonth' })
  const birthdayDay = useWatch({ control, name: 'birthdayDay' })

  const [serverError, setServerError] = useState<string | null>(null)
  const [confirmingClose, setConfirmingClose] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const [stagedAvatar, setStagedAvatar] = useState<File | null>(null)
  const editedRef = useRef(false)

  const maxDay = daysInMonth(birthdayMonth)

  // Keep the day select in range when the month changes to a shorter one.
  useEffect(() => {
    if (birthdayDay > maxDay) setValue('birthdayDay', maxDay)
  }, [birthdayDay, maxDay, setValue])

  const mutation = useMutation({
    mutationFn: (request: CreatePersonRequest) =>
      mode === 'create'
        ? firebirdApi.createPerson(request)
        : firebirdApi.updatePerson(personId as number, request),
    onSuccess: async (saved) => {
      if (mode === 'create' && stagedAvatar != null) {
        // The avatar endpoint needs the new person id, so the photo is uploaded
        // immediately after creation. A failed upload leaves the person created.
        try {
          await firebirdApi.uploadAvatar(saved.id, stagedAvatar)
        } catch {
          /* The person exists; the photo can be added again from the editor. */
        }
      }
      onSaved(saved, mode)
    },
    onError: (error) => setServerError(mapServerError(error, t)),
  })

  const deleteMutation = useMutation({
    mutationFn: () => firebirdApi.deletePerson(personId as number),
    onSuccess: () => {
      if (person != null) onDeleted(person)
    },
    onError: (error) => {
      setConfirmingDelete(false)
      setServerError(mapServerError(error, t))
    },
  })

  const submit = (event: React.FormEvent<HTMLFormElement>) => {
    void handleSubmit((values) => {
      setServerError(null)
      mutation.mutate(toRequest(values))
    })(event)
  }

  const requestClose = () => {
    if (editedRef.current && !mutation.isSuccess) {
      setConfirmingClose(true)
      return
    }
    onClose()
  }

  const submitting = mutation.isPending

  return (
    <>
      <Dialog
        scrollable
        width={760}
        title={title}
        description={description}
        onClose={requestClose}
        closeLabel={t('editor.actions.cancel')}
        footer={
          <>
            {mode === 'edit' && (
              <Button
                variant="ghost"
                className="seg-person-editor__delete"
                iconLeft={<Trash2 size={15} />}
                onClick={() => setConfirmingDelete(true)}
                disabled={submitting || deleteMutation.isPending}
              >
                {t('editor.delete.action')}
              </Button>
            )}
            <Button variant="ghost" onClick={requestClose} disabled={submitting}>
              {t('editor.actions.cancel')}
            </Button>
            <Button type="submit" form="seg-firebird-form" disabled={submitting}>
              {mode === 'create'
                ? submitting
                  ? t('editor.actions.creating')
                  : t('editor.actions.create')
                : submitting
                  ? t('editor.actions.saving')
                  : t('editor.actions.save')}
            </Button>
          </>
        }
      >
        <form
          id="seg-firebird-form"
          className="seg-person-editor"
          onSubmit={submit}
          onChange={() => {
            editedRef.current = true
          }}
          noValidate
        >
          {serverError != null && (
            <p className="seg-person-editor__error" role="alert">
              {serverError}
            </p>
          )}

          <section className="seg-person-editor__section">
            <h3>{t('editor.sections.identity')}</h3>
            <div className="seg-person-editor__identity">
              <AvatarControl
                mode={mode}
                personId={personId}
                name={person?.name ?? ''}
                currentUrl={person?.avatar.url ?? null}
                hasAvatar={person?.avatar.source === 'avatar'}
                stagedAvatar={stagedAvatar}
                onStage={(file) => {
                  editedRef.current = true
                  setStagedAvatar(file)
                }}
              />
              <div className="seg-person-editor__identity-fields">
                <Input
                  label={t('editor.fields.name')}
                  placeholder={t('editor.fields.namePlaceholder')}
                  required
                  iconLeft={<UserRound size={16} />}
                  error={formState.errors.name?.message}
                  {...register('name')}
                />
                <div className="seg-person-editor__grid">
                  <Field
                    label={t('editor.fields.category')}
                    error={formState.errors.categoryId?.message}
                  >
                    <Select
                      {...register('categoryId')}
                      aria-invalid={formState.errors.categoryId != null}
                      options={categories.map((category) => ({
                        value: String(category.id),
                        label: category.name,
                      }))}
                    />
                  </Field>
                  <Field label={t('editor.fields.status')}>
                    <Select
                      {...register('status')}
                      options={firebirdPersonStatuses.map((status) => ({
                        value: status,
                        label: t(`status.${status}`),
                      }))}
                    />
                  </Field>
                </div>
              </div>
            </div>
          </section>

          <section className="seg-person-editor__section">
            <h3>{t('editor.sections.birthday')}</h3>
            <label className="seg-person-editor__check">
              <input type="checkbox" {...register('hasBirthday')} />
              <span>{t('editor.birthday.toggle')}</span>
            </label>
            {hasBirthday && (
              <div className="seg-person-editor__grid">
                <Field label={t('editor.birthday.month')}>
                  <Select
                    {...register('birthdayMonth', { valueAsNumber: true })}
                    options={Array.from({ length: 12 }, (_, index) => ({
                      value: String(index + 1),
                      label: new Intl.DateTimeFormat(undefined, {
                        month: 'long',
                      }).format(new Date(2020, index, 1)),
                    }))}
                  />
                </Field>
                <Field label={t('editor.birthday.day')}>
                  <Select
                    {...register('birthdayDay', { valueAsNumber: true })}
                    options={Array.from({ length: maxDay }, (_, index) => ({
                      value: String(index + 1),
                      label: String(index + 1),
                    }))}
                  />
                </Field>
              </div>
            )}
            <span className="seg-person-editor__hint">{t('editor.birthday.hint')}</span>
          </section>

          <section className="seg-person-editor__section">
            <h3>{t('editor.sections.notes')}</h3>
            <label className="seg-person-editor__notes">
              <span className="seg-person-editor__field-label">
                {t('editor.fields.notes')}
              </span>
              <textarea
                className="seg-person-editor__textarea"
                rows={4}
                placeholder={t('editor.fields.notesPlaceholder')}
                aria-invalid={formState.errors.notes != null}
                {...register('notes')}
              />
              {formState.errors.notes?.message != null && (
                <span className="seg-person-editor__field-error" role="alert">
                  {formState.errors.notes.message}
                </span>
              )}
            </label>
          </section>

          <section className="seg-person-editor__section">
            <h3>{t('editor.sections.visibility')}</h3>
            <div className="seg-person-editor__field">
              <SegmentedControl
                aria-label={t('editor.sections.visibility')}
                disabled={!canChangeVisibility}
                {...register('visibility')}
                options={firebirdVisibilities.map((visibility) => ({
                  value: visibility,
                  label: t(`visibility.${visibility}`),
                  icon: visibilityMeta[visibility].icon,
                  tone: visibilityMeta[visibility].tone,
                }))}
              />
              <span className="seg-person-editor__field-hint">
                {canChangeVisibility
                  ? t('editor.visibility.hint')
                  : t('editor.visibility.locked')}
              </span>
            </div>
          </section>

          <section className="seg-person-editor__section">
            <h3>{t('editor.sections.manage')}</h3>
            <div className="seg-person-manage">
              <ManageCard
                icon={<AtSign size={17} />}
                title={t('editor.manage.usernamesTitle')}
                hint={
                  mode === 'edit'
                    ? t('editor.manage.usernamesHintEdit')
                    : t('editor.manage.usernamesHintNew')
                }
                action={t('editor.manage.usernamesAction')}
                disabled={mode !== 'edit' || personId == null}
                onClick={() =>
                  personId != null && onOpenUsernames(personId, { returnToEdit: true })
                }
              />
              <ManageCard
                icon={<MessagesSquare size={17} />}
                title={t('editor.manage.interactionsTitle')}
                hint={
                  mode === 'edit'
                    ? t('editor.manage.interactionsHintEdit')
                    : t('editor.manage.interactionsHintNew')
                }
                action={t('editor.manage.interactionsAction')}
                disabled={mode !== 'edit' || personId == null}
                onClick={() =>
                  personId != null &&
                  onOpenInteractions(personId, { returnToEdit: true })
                }
              />
            </div>
          </section>
        </form>
      </Dialog>

      {confirmingClose && (
        <Dialog
          width={420}
          title={t('editor.unsaved.title')}
          description={t('editor.unsaved.description')}
          onClose={() => setConfirmingClose(false)}
          closeLabel={t('editor.unsaved.stay')}
          footer={
            <>
              <Button variant="ghost" onClick={() => setConfirmingClose(false)}>
                {t('editor.unsaved.stay')}
              </Button>
              <Button variant="danger" onClick={onClose}>
                {t('editor.unsaved.leave')}
              </Button>
            </>
          }
        />
      )}

      {confirmingDelete && (
        <Dialog
          width={460}
          title={t('editor.delete.title')}
          description={t('editor.delete.description')}
          onClose={() => setConfirmingDelete(false)}
          closeLabel={t('editor.delete.cancel')}
          footer={
            <>
              <Button
                variant="ghost"
                onClick={() => setConfirmingDelete(false)}
                disabled={deleteMutation.isPending}
              >
                {t('editor.delete.cancel')}
              </Button>
              <Button
                variant="danger"
                onClick={() => deleteMutation.mutate()}
                disabled={deleteMutation.isPending}
              >
                {deleteMutation.isPending
                  ? t('editor.delete.deleting')
                  : t('editor.delete.confirm')}
              </Button>
            </>
          }
        />
      )}
    </>
  )
}

interface AvatarControlProps {
  mode: 'create' | 'edit'
  personId?: number
  name: string
  currentUrl: string | null
  hasAvatar: boolean
  stagedAvatar: File | null
  onStage: (file: File | null) => void
}

function AvatarControl({
  mode,
  personId,
  name,
  currentUrl,
  hasAvatar,
  stagedAvatar,
  onStage,
}: AvatarControlProps) {
  const { t } = useTranslation('firebird')
  const queryClient = useQueryClient()
  const fileInput = useRef<HTMLInputElement>(null)
  const [busy, setBusy] = useState<'upload' | 'remove' | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [editUrl, setEditUrl] = useState<string | null>(currentUrl)
  const [editHasAvatar, setEditHasAvatar] = useState(hasAvatar)

  const stagedUrl = useMemo(
    () => (stagedAvatar != null ? URL.createObjectURL(stagedAvatar) : null),
    [stagedAvatar],
  )
  useEffect(
    () => () => {
      if (stagedUrl != null) URL.revokeObjectURL(stagedUrl)
    },
    [stagedUrl],
  )

  const previewUrl = mode === 'create' ? stagedUrl : editUrl
  const showRemove = mode === 'create' ? stagedAvatar != null : editHasAvatar

  const invalidate = () => {
    if (personId == null) return
    void queryClient.invalidateQueries({ queryKey: firebirdKeys.person(personId) })
    void queryClient.invalidateQueries({ queryKey: firebirdKeys.people() })
  }

  const choose = async (file: File) => {
    if (!file.type.startsWith('image/')) {
      setError(t('editor.avatar.typeError'))
      return
    }
    setError(null)
    if (mode === 'create') {
      onStage(file)
      return
    }
    if (personId == null) return
    setBusy('upload')
    try {
      const avatar = await firebirdApi.uploadAvatar(personId, file)
      setEditUrl(avatar.url)
      setEditHasAvatar(true)
      invalidate()
    } catch {
      setError(t('editor.avatar.uploadError'))
    } finally {
      setBusy(null)
    }
  }

  const remove = async () => {
    setError(null)
    if (mode === 'create') {
      onStage(null)
      return
    }
    if (personId == null) return
    setBusy('remove')
    try {
      await firebirdApi.deleteAvatar(personId)
      setEditUrl(null)
      setEditHasAvatar(false)
      invalidate()
    } catch {
      setError(t('editor.avatar.uploadError'))
    } finally {
      setBusy(null)
    }
  }

  return (
    <div className="seg-person-avatar">
      <button
        type="button"
        className="seg-person-avatar__btn"
        onClick={() => fileInput.current?.click()}
        disabled={busy != null}
        aria-label={
          previewUrl != null ? t('editor.avatar.replace') : t('editor.avatar.add')
        }
      >
        <Avatar size="lg" name={name} src={previewUrl ?? undefined} />
        <span className="seg-person-avatar__edit" aria-hidden="true">
          {busy === 'upload' ? <Spinner size={14} /> : <Camera size={14} />}
        </span>
      </button>
      <input
        ref={fileInput}
        type="file"
        accept="image/*"
        className="seg-person-avatar__input"
        tabIndex={-1}
        onChange={(event) => {
          const file = event.target.files?.[0]
          if (file != null) void choose(file)
          event.target.value = ''
        }}
        aria-label={t('editor.avatar.change')}
      />
      <div className="seg-person-avatar__meta">
        <span className="seg-person-avatar__hint">{t('editor.avatar.hint')}</span>
        {showRemove && (
          <button
            type="button"
            className="seg-person-avatar__remove"
            onClick={() => void remove()}
            disabled={busy != null}
          >
            {busy === 'remove'
              ? t('editor.avatar.removing')
              : t('editor.avatar.remove')}
          </button>
        )}
        {error != null && (
          <span className="seg-person-avatar__error" role="alert">
            {error}
          </span>
        )}
      </div>
    </div>
  )
}

interface ManageCardProps {
  icon: ReactNode
  title: string
  hint: string
  action: string
  disabled: boolean
  onClick: () => void
}

function ManageCard({ icon, title, hint, action, disabled, onClick }: ManageCardProps) {
  return (
    <div className="seg-person-managecard">
      <div className="seg-person-managecard__head">
        <span className="seg-person-managecard__icon" aria-hidden="true">
          {icon}
        </span>
        <span className="seg-person-managecard__title">{title}</span>
      </div>
      <span className="seg-person-managecard__hint">{hint}</span>
      <Button
        variant="outline"
        size="sm"
        iconLeft={icon}
        disabled={disabled}
        onClick={onClick}
      >
        {action}
      </Button>
    </div>
  )
}

interface SubEntityDialogProps {
  personId: number
  onClose: () => void
  onChanged: (kind: ToastKind, name: string) => void
}

type UsernameFormValues = {
  platformId: string
  handle: string
  notes: string
}

type InteractionFormValues = {
  date: string
  description: string
}

type EditState<T> = { mode: 'create' } | { mode: 'edit'; row: T }

function UsernamesDialog({ personId, onClose, onChanged }: SubEntityDialogProps) {
  const { t } = useTranslation('firebird')
  const queryClient = useQueryClient()
  const [editing, setEditing] = useState<EditState<Username> | null>(null)
  const usernamesQuery = useQuery({
    queryKey: firebirdKeys.usernames(personId),
    queryFn: ({ signal }) => firebirdApi.listUsernames(personId, signal),
  })
  const platforms = useUsernamePlatforms()
  const usernames = usernamesQuery.data ?? []

  const invalidate = async () => {
    await queryClient.invalidateQueries({ queryKey: firebirdKeys.usernames(personId) })
    await queryClient.invalidateQueries({ queryKey: firebirdKeys.person(personId) })
  }

  return (
    <Dialog
      scrollable
      width={780}
      title={t('usernames.title')}
      description={t('usernames.description')}
      onClose={onClose}
      closeLabel={t('subEntities.close')}
      footer={<Button onClick={onClose}>{t('subEntities.close')}</Button>}
    >
      <div className="seg-firebird-subentities">
        <div className="seg-firebird-subentities__head">
          <Badge tone="neutral">
            {t('usernames.count', { count: usernames.length })}
          </Badge>
          <Button
            size="sm"
            iconLeft={<Plus size={15} />}
            onClick={() => setEditing({ mode: 'create' })}
          >
            {t('usernames.add')}
          </Button>
        </div>

        {usernamesQuery.isPending || platforms.isPending ? (
          <div className="seg-person-editor__status">
            <Spinner />
            <span>{t('usernames.loading')}</span>
          </div>
        ) : usernamesQuery.isError || platforms.isError ? (
          <p className="seg-person-editor__error" role="alert">
            {t('usernames.loadError')}
          </p>
        ) : usernames.length === 0 ? (
          <p className="seg-firebird-subentities__empty">{t('usernames.empty')}</p>
        ) : (
          <ul className="seg-firebird-subentities__list">
            {usernames.map((username) => (
              <li key={username.id} className="seg-firebird-subentities__item">
                <div className="seg-firebird-subentities__item-main">
                  <strong>{username.handle}</strong>
                  <span>{username.platformName}</span>
                  {username.notes != null && <small>{username.notes}</small>}
                </div>
                <Button
                  size="sm"
                  variant="outline"
                  iconLeft={<Pencil size={14} />}
                  onClick={() => setEditing({ mode: 'edit', row: username })}
                >
                  {t('subEntities.edit')}
                </Button>
              </li>
            ))}
          </ul>
        )}

        {editing != null && platforms.data != null && (
          <UsernameEditor
            personId={personId}
            state={editing}
            platforms={platforms.data}
            onClose={() => setEditing(null)}
            onSaved={async (username) => {
              setEditing(null)
              await invalidate()
              onChanged('usernameSaved', username.handle)
            }}
            onDeleted={async (username) => {
              setEditing(null)
              await invalidate()
              onChanged('usernameDeleted', username.handle)
            }}
          />
        )}
      </div>
    </Dialog>
  )
}

function UsernameEditor({
  personId,
  state,
  platforms,
  onClose,
  onSaved,
  onDeleted,
}: {
  personId: number
  state: EditState<Username>
  platforms: UsernamePlatform[]
  onClose: () => void
  onSaved: (username: Username, mode: 'create' | 'edit') => Promise<void>
  onDeleted: (username: Username) => Promise<void>
}) {
  const { t } = useTranslation('firebird')
  const [serverError, setServerError] = useState<string | null>(null)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const row = state.mode === 'edit' ? state.row : null
  const schema = useMemo(
    () =>
      createUsernameSchema({
        platformRequired: t('usernames.validation.platformRequired'),
        handleRequired: t('usernames.validation.handleRequired'),
        handleTooLong: t('usernames.validation.handleTooLong'),
        notesTooLong: t('usernames.validation.notesTooLong'),
      }),
    [t],
  )
  const form = useForm<UsernameFormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      platformId: row == null ? firstCatalogId(platforms) : String(row.platformId),
      handle: row?.handle ?? '',
      notes: row?.notes ?? '',
    },
  })

  const mutation = useMutation({
    mutationFn: (request: UsernameRequest) =>
      row == null
        ? firebirdApi.createUsername(personId, request)
        : firebirdApi.updateUsername(personId, row.id, request),
    onSuccess: (saved) => void onSaved(saved, row == null ? 'create' : 'edit'),
    onError: (error) => setServerError(mapSubEntityError(error, t)),
  })
  const deleteMutation = useMutation({
    mutationFn: () => firebirdApi.deleteUsername(personId, row?.id as number),
    onSuccess: () => {
      if (row != null) void onDeleted(row)
    },
    onError: (error) => {
      setConfirmingDelete(false)
      setServerError(mapSubEntityError(error, t))
    },
  })

  const submit = form.handleSubmit((values) => {
    setServerError(null)
    mutation.mutate({
      platformId: Number(values.platformId),
      handle: values.handle.trim(),
      notes: values.notes.trim() === '' ? null : values.notes.trim(),
    })
  })

  return (
    <>
      <Dialog
        width={560}
        title={
          row == null
            ? t('usernames.editor.createTitle')
            : t('usernames.editor.editTitle')
        }
        onClose={onClose}
        closeLabel={t('subEntities.close')}
        footer={
          <>
            {row != null && (
              <Button
                variant="ghost"
                className="seg-person-editor__delete"
                iconLeft={<Trash2 size={15} />}
                onClick={() => setConfirmingDelete(true)}
              >
                {t('usernames.delete.action')}
              </Button>
            )}
            <Button variant="ghost" onClick={onClose}>
              {t('subEntities.cancel')}
            </Button>
            <Button
              type="submit"
              form="seg-username-form"
              disabled={mutation.isPending}
            >
              {mutation.isPending ? t('subEntities.saving') : t('subEntities.save')}
            </Button>
          </>
        }
      >
        <form
          id="seg-username-form"
          className="seg-firebird-subentities__form"
          onSubmit={(event) => void submit(event)}
          noValidate
        >
          {serverError != null && (
            <p className="seg-person-editor__error" role="alert">
              {serverError}
            </p>
          )}
          <Field
            label={t('usernames.fields.platform')}
            error={form.formState.errors.platformId?.message}
          >
            <Select
              aria-invalid={form.formState.errors.platformId != null}
              options={platforms.map((platform) => ({
                value: String(platform.id),
                label: platform.name,
              }))}
              {...form.register('platformId')}
            />
          </Field>
          <Input
            label={t('usernames.fields.handle')}
            required
            autoComplete="off"
            error={form.formState.errors.handle?.message}
            {...form.register('handle')}
          />
          <label className="seg-person-editor__notes">
            <span className="seg-person-editor__field-label">
              {t('usernames.fields.notes')}
            </span>
            <textarea
              className="seg-person-editor__textarea"
              rows={3}
              aria-invalid={form.formState.errors.notes != null}
              {...form.register('notes')}
            />
            {form.formState.errors.notes?.message != null && (
              <span className="seg-person-editor__field-error" role="alert">
                {form.formState.errors.notes.message}
              </span>
            )}
          </label>
          <button type="submit" hidden />
        </form>
      </Dialog>

      {confirmingDelete && row != null && (
        <Dialog
          width={440}
          title={t('usernames.delete.title')}
          description={t('usernames.delete.description')}
          onClose={() => setConfirmingDelete(false)}
          closeLabel={t('subEntities.close')}
          footer={
            <>
              <Button variant="ghost" onClick={() => setConfirmingDelete(false)}>
                {t('subEntities.cancel')}
              </Button>
              <Button
                variant="danger"
                onClick={() => deleteMutation.mutate()}
                disabled={deleteMutation.isPending}
              >
                {deleteMutation.isPending
                  ? t('subEntities.deleting')
                  : t('usernames.delete.confirm')}
              </Button>
            </>
          }
        />
      )}
    </>
  )
}

function InteractionsDialog({
  personId,
  locale,
  onClose,
  onChanged,
}: SubEntityDialogProps & { locale: string }) {
  const { t } = useTranslation('firebird')
  const queryClient = useQueryClient()
  const [editing, setEditing] = useState<EditState<Interaction> | null>(null)
  const interactionsQuery = useQuery({
    queryKey: firebirdKeys.interactions(personId),
    queryFn: ({ signal }) => firebirdApi.listInteractions(personId, signal),
  })
  const interactions = interactionsQuery.data ?? []

  const invalidate = async () => {
    await queryClient.invalidateQueries({
      queryKey: firebirdKeys.interactions(personId),
    })
    await queryClient.invalidateQueries({ queryKey: firebirdKeys.person(personId) })
  }

  return (
    <Dialog
      scrollable
      width={820}
      title={t('interactions.title')}
      description={t('interactions.description')}
      onClose={onClose}
      closeLabel={t('subEntities.close')}
      footer={<Button onClick={onClose}>{t('subEntities.close')}</Button>}
    >
      <div className="seg-firebird-subentities">
        <div className="seg-firebird-subentities__head">
          <Badge tone="neutral">
            {t('interactions.count', { count: interactions.length })}
          </Badge>
          <Button
            size="sm"
            iconLeft={<Plus size={15} />}
            onClick={() => setEditing({ mode: 'create' })}
          >
            {t('interactions.add')}
          </Button>
        </div>

        {interactionsQuery.isPending ? (
          <div className="seg-person-editor__status">
            <Spinner />
            <span>{t('interactions.loading')}</span>
          </div>
        ) : interactionsQuery.isError ? (
          <p className="seg-person-editor__error" role="alert">
            {t('interactions.loadError')}
          </p>
        ) : interactions.length === 0 ? (
          <p className="seg-firebird-subentities__empty">{t('interactions.empty')}</p>
        ) : (
          <ul className="seg-firebird-subentities__list">
            {interactions.map((interaction) => (
              <li key={interaction.id} className="seg-firebird-subentities__item">
                <div className="seg-firebird-subentities__item-main">
                  <strong>{formatCivilDate(interaction.date, locale)}</strong>
                  <span>{interaction.description}</span>
                </div>
                <Button
                  size="sm"
                  variant="outline"
                  iconLeft={<Pencil size={14} />}
                  onClick={() => setEditing({ mode: 'edit', row: interaction })}
                >
                  {t('subEntities.edit')}
                </Button>
              </li>
            ))}
          </ul>
        )}

        {editing != null && (
          <InteractionEditor
            personId={personId}
            state={editing}
            onClose={() => setEditing(null)}
            onSaved={async (interaction) => {
              setEditing(null)
              await invalidate()
              onChanged('interactionSaved', interaction.description)
            }}
            onDeleted={async (interaction) => {
              setEditing(null)
              await invalidate()
              onChanged('interactionDeleted', interaction.description)
            }}
          />
        )}
      </div>
    </Dialog>
  )
}

function InteractionEditor({
  personId,
  state,
  onClose,
  onSaved,
  onDeleted,
}: {
  personId: number
  state: EditState<Interaction>
  onClose: () => void
  onSaved: (interaction: Interaction) => Promise<void>
  onDeleted: (interaction: Interaction) => Promise<void>
}) {
  const { t } = useTranslation('firebird')
  const [serverError, setServerError] = useState<string | null>(null)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const row = state.mode === 'edit' ? state.row : null
  const today = new Date().toISOString().slice(0, 10)
  const schema = useMemo(
    () =>
      createInteractionSchema({
        dateRequired: t('interactions.validation.dateRequired'),
        dateFuture: t('interactions.validation.dateFuture'),
        descriptionRequired: t('interactions.validation.descriptionRequired'),
        descriptionTooLong: t('interactions.validation.descriptionTooLong'),
      }),
    [t],
  )
  const form = useForm<InteractionFormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      date: row?.date ?? today,
      description: row?.description ?? '',
    },
  })

  const mutation = useMutation({
    mutationFn: (request: InteractionRequest) =>
      row == null
        ? firebirdApi.createInteraction(personId, request)
        : firebirdApi.updateInteraction(personId, row.id, request),
    onSuccess: (saved) => void onSaved(saved),
    onError: (error) => setServerError(mapSubEntityError(error, t)),
  })
  const deleteMutation = useMutation({
    mutationFn: () => firebirdApi.deleteInteraction(personId, row?.id as number),
    onSuccess: () => {
      if (row != null) void onDeleted(row)
    },
    onError: (error) => {
      setConfirmingDelete(false)
      setServerError(mapSubEntityError(error, t))
    },
  })

  const submit = form.handleSubmit((values) => {
    setServerError(null)
    mutation.mutate({
      date: values.date,
      description: values.description.trim(),
    })
  })

  return (
    <>
      <Dialog
        width={560}
        title={
          row == null
            ? t('interactions.editor.createTitle')
            : t('interactions.editor.editTitle')
        }
        onClose={onClose}
        closeLabel={t('subEntities.close')}
        footer={
          <>
            {row != null && (
              <Button
                variant="ghost"
                className="seg-person-editor__delete"
                iconLeft={<Trash2 size={15} />}
                onClick={() => setConfirmingDelete(true)}
              >
                {t('interactions.delete.action')}
              </Button>
            )}
            <Button variant="ghost" onClick={onClose}>
              {t('subEntities.cancel')}
            </Button>
            <Button
              type="submit"
              form="seg-interaction-form"
              disabled={mutation.isPending}
            >
              {mutation.isPending ? t('subEntities.saving') : t('subEntities.save')}
            </Button>
          </>
        }
      >
        <form
          id="seg-interaction-form"
          className="seg-firebird-subentities__form"
          onSubmit={(event) => void submit(event)}
          noValidate
        >
          {serverError != null && (
            <p className="seg-person-editor__error" role="alert">
              {serverError}
            </p>
          )}
          <Input
            label={t('interactions.fields.date')}
            type="date"
            max={today}
            required
            error={form.formState.errors.date?.message}
            {...form.register('date')}
          />
          <label className="seg-person-editor__notes">
            <span className="seg-person-editor__field-label">
              {t('interactions.fields.description')}
            </span>
            <textarea
              className="seg-person-editor__textarea"
              rows={4}
              required
              aria-invalid={form.formState.errors.description != null}
              {...form.register('description')}
            />
            {form.formState.errors.description?.message != null && (
              <span className="seg-person-editor__field-error" role="alert">
                {form.formState.errors.description.message}
              </span>
            )}
          </label>
          <button type="submit" hidden />
        </form>
      </Dialog>

      {confirmingDelete && row != null && (
        <Dialog
          width={440}
          title={t('interactions.delete.title')}
          description={t('interactions.delete.description')}
          onClose={() => setConfirmingDelete(false)}
          closeLabel={t('subEntities.close')}
          footer={
            <>
              <Button variant="ghost" onClick={() => setConfirmingDelete(false)}>
                {t('subEntities.cancel')}
              </Button>
              <Button
                variant="danger"
                onClick={() => deleteMutation.mutate()}
                disabled={deleteMutation.isPending}
              >
                {deleteMutation.isPending
                  ? t('subEntities.deleting')
                  : t('interactions.delete.confirm')}
              </Button>
            </>
          }
        />
      )}
    </>
  )
}

interface FieldProps {
  label: string
  hint?: string
  error?: string
  children: ReactNode
}

function Field({ label, hint, error, children }: FieldProps) {
  const message = error ?? hint
  return (
    <div className="seg-person-editor__field">
      <label className="seg-person-editor__field-control">
        <span className="seg-person-editor__field-label">{label}</span>
        {children}
      </label>
      {message != null && (
        <span
          className={
            'seg-person-editor__field-hint' +
            (error != null ? ' seg-person-editor__field-hint--error' : '')
          }
        >
          {message}
        </span>
      )}
    </div>
  )
}

function mapServerError(error: unknown, t: (key: string) => string): string {
  if (isApiError(error)) {
    switch (error.problem?.code) {
      case 'firebird.person.validation':
        return t('editor.errors.validation')
      case 'firebird.person.visibility_forbidden':
        return t('editor.errors.visibilityForbidden')
      case 'firebird.catalog.unknown_reference':
        return t('editor.errors.unknownReference')
    }
    if (error.kind === 'transient' || error.kind === 'unavailable') {
      return t('editor.errors.conflict')
    }
  }
  return t('editor.errors.generic')
}

function createUsernameSchema(messages: {
  platformRequired: string
  handleRequired: string
  handleTooLong: string
  notesTooLong: string
}) {
  return z.object({
    platformId: z.string().min(1, messages.platformRequired),
    handle: z
      .string()
      .trim()
      .min(1, messages.handleRequired)
      .max(200, messages.handleTooLong),
    notes: z.string().max(1000, messages.notesTooLong),
  })
}

function createInteractionSchema(messages: {
  dateRequired: string
  dateFuture: string
  descriptionRequired: string
  descriptionTooLong: string
}) {
  return z.object({
    date: z
      .string()
      .min(1, messages.dateRequired)
      .refine((value) => value <= new Date().toISOString().slice(0, 10), {
        message: messages.dateFuture,
      }),
    description: z
      .string()
      .trim()
      .min(1, messages.descriptionRequired)
      .max(2000, messages.descriptionTooLong),
  })
}

function formatCivilDate(value: string, locale: string): string {
  const [year, month, day] = value.split('-').map(Number)
  if (!Number.isFinite(year) || !Number.isFinite(month) || !Number.isFinite(day)) {
    return value
  }
  return new Intl.DateTimeFormat(locale, {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  }).format(new Date(year, month - 1, day))
}

function mapSubEntityError(error: unknown, t: (key: string) => string): string {
  if (isApiError(error)) {
    if (error.kind === 'not-found') return t('subEntities.errors.notFound')
    if (error.kind === 'authorization-denied') return t('subEntities.errors.forbidden')
    if (error.kind === 'validation') return t('subEntities.errors.validation')
    if (error.problem?.code?.endsWith('unknown_reference') === true) {
      return t('subEntities.errors.unknownReference')
    }
    if (error.kind === 'transient' || error.kind === 'unavailable') {
      return t('subEntities.errors.transient')
    }
  }
  return t('subEntities.errors.generic')
}
