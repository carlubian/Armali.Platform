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
  BookOpen,
  CalendarDays,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  ChevronUp,
  Clock,
  Globe,
  ImagePlus,
  List,
  ListOrdered,
  Lock,
  Package,
  Plus,
  Search,
  Trash2,
  UtensilsCrossed,
} from 'lucide-react'
import { useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import { useFieldArray, useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import {
  recipeMenusRoutePath,
  recipePageSizes,
  recipesApi,
  type CreateRecipeRequest,
  type Recipe,
  type RecipeCategory,
  type RecipeDifficulty,
  type RecipePageSize,
  type RecipeSortField,
  type RecipeSummary,
  type RecipeVisibility,
} from '@/app/api/recipes'
import { isApiError } from '@/app/api/errors'
import { useSession } from '@/app/session/SessionContext'
import { EntityReferenceField } from '@/components/entity-selection'
import { ServiceUnavailable } from '@/components/feedback/SystemScreens'
import {
  Badge,
  Button,
  Dialog,
  Input,
  SegmentedControl,
  Select,
  Spinner,
  Toast,
  type SegmentTone,
} from '@/components/ui'

import {
  InventoryItemEntitySelector,
  inventoryItemReference,
} from './InventoryItemEntitySelector'
import { RecipeAttachments } from './RecipeAttachments'
import { recipesKeys, useRecipeCategories } from './queries'
import {
  activeRecipeFilterCount,
  type RecipesFilterPatch,
  type RecipesState,
  useRecipesState,
} from './recipesState'
import {
  buildDefaults,
  createRecipeFormSchema,
  fromRecipe,
  newIngredient,
  newStep,
  toRequest,
  type RecipeFormValues,
} from './recipeForm'

import './RecipesPage.css'

type ToastKind = 'created' | 'updated' | 'deleted'

interface ToastState {
  kind: ToastKind
  name: string
}

const difficulties: RecipeDifficulty[] = ['Easy', 'Medium', 'Hard']
const visibilities: RecipeVisibility[] = ['Public', 'Private']
const sortFields: RecipeSortField[] = ['name', 'category']

const visibilityMeta: Record<
  RecipeVisibility,
  { icon: ReactNode; tone: SegmentTone }
> = {
  Public: { icon: <Globe size={15} />, tone: 'accent' },
  Private: { icon: <Lock size={15} />, tone: 'neutral' },
}

const categoryTones = ['aqua', 'azure', 'gold', 'sea', 'rose'] as const

export function RecipesPage() {
  const { t } = useTranslation('recipes')
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
    closeDialog,
  } = useRecipesState(currentUserId)

  const categories = useRecipeCategories()
  const recipesQuery = useQuery({
    queryKey: recipesKeys.recipeList(listQuery),
    queryFn: ({ signal }) => recipesApi.listRecipes(listQuery, signal),
    placeholderData: keepPreviousData,
  })

  const data = recipesQuery.data
  const recipes = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = Math.max(1, Math.ceil(totalCount / state.pageSize))
  const hasFilters = activeRecipeFilterCount(state) > 0

  const invalidateRecipes = (recipeId?: number) => {
    void queryClient.invalidateQueries({ queryKey: recipesKeys.recipes() })
    if (recipeId != null)
      void queryClient.invalidateQueries({ queryKey: recipesKeys.recipe(recipeId) })
  }

  const handleSaved = (recipe: Recipe, mode: 'create' | 'edit') => {
    queryClient.setQueryData(recipesKeys.recipe(recipe.id), recipe)
    invalidateRecipes(recipe.id)
    setToast({ kind: mode === 'create' ? 'created' : 'updated', name: recipe.name })
    closeDialog()
  }

  const handleDeleted = (recipe: Recipe) => {
    invalidateRecipes()
    setToast({ kind: 'deleted', name: recipe.name })
    closeDialog()
  }

  useEffect(() => {
    if (data != null && state.page > totalPages) setPage(totalPages)
  }, [data, state.page, totalPages, setPage])

  if (recipesQuery.isError) {
    const error = recipesQuery.error
    if (isApiError(error) && ['unavailable', 'transient'].includes(error.kind)) {
      return <ServiceUnavailable onRetry={() => void recipesQuery.refetch()} />
    }
  }

  return (
    <main className="seg-recipes armali-aurora">
      <section className="seg-recipes__head">
        <div>
          <div className="armali-eyebrow">{t('page.eyebrow')}</div>
          <h1>{t('page.title')}</h1>
          <p>{t('page.description')}</p>
        </div>
        <div className="seg-recipes__head-actions">
          <Link className="seg-recipes__view-link" to={recipeMenusRoutePath}>
            <CalendarDays size={16} aria-hidden="true" />
            {t('page.menus')}
          </Link>
          <Button iconLeft={<Plus size={16} />} onClick={openCreateDialog}>
            {t('gallery.newRecipe')}
          </Button>
        </div>
      </section>

      <section className="seg-recipes__panel-head">
        <Badge tone="neutral">{t('gallery.count', { count: totalCount })}</Badge>
      </section>

      <RecipesFilters
        state={state}
        categories={categories.data ?? []}
        onChange={setFilters}
        onClear={clearFilters}
      />

      {recipesQuery.isPending ? (
        <div className="seg-recipes__loading">
          <Spinner label={t('states.loading')} />
        </div>
      ) : recipesQuery.isError ? (
        <p className="seg-recipes__error" role="alert">
          {t('states.loadError')}
        </p>
      ) : recipes.length === 0 ? (
        <p className="seg-recipes__empty">
          {hasFilters ? t('states.emptyFiltered') : t('states.empty')}
        </p>
      ) : (
        <RecipeGallery
          recipes={recipes}
          categories={categories.data ?? []}
          state={state}
          busy={recipesQuery.isFetching && !recipesQuery.isPending}
          onSort={setSort}
          onOpen={openEditDialog}
        />
      )}

      <Pager
        page={state.page}
        pageSize={state.pageSize}
        totalPages={totalPages}
        fetching={recipesQuery.isFetching}
        onPage={setPage}
        onPageSize={setPageSize}
      />

      {dialog.mode !== 'closed' && (
        <RecipeDialog
          mode={dialog.mode}
          recipeId={dialog.mode === 'edit' ? dialog.recipeId : undefined}
          currentUserId={currentUserId}
          onClose={closeDialog}
          onSaved={handleSaved}
          onDeleted={handleDeleted}
        />
      )}

      {toast != null && (
        <div className="seg-recipes__toast">
          <Toast
            tone="success"
            title={t(`toast.${toast.kind}`)}
            closeLabel={t('editor.actions.cancel')}
            onClose={() => setToast(null)}
          >
            {t(`toast.${toast.kind}Body`, { name: toast.name })}
          </Toast>
        </div>
      )}
    </main>
  )
}

interface RecipesFiltersProps {
  state: RecipesState
  categories: RecipeCategory[]
  onChange: (patch: RecipesFilterPatch) => void
  onClear: () => void
}

function RecipesFilters({
  state,
  categories,
  onChange,
  onClear,
}: RecipesFiltersProps) {
  const { t } = useTranslation('recipes')
  const count = activeRecipeFilterCount(state)

  return (
    <section className="seg-recipes__filters" aria-label={t('filters.active', { count })}>
      <div className="seg-recipes__filters-primary">
        <Input
          className="seg-recipes__search"
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
          label={t('filters.difficulty')}
          value={state.difficulty}
          allLabel={t('filters.anyDifficulty')}
          onChange={(value) =>
            onChange({ difficulty: value as RecipeDifficulty | '' })
          }
          options={difficulties.map((difficulty) => ({
            value: difficulty,
            label: t(`difficulty.${difficulty}`),
          }))}
        />
        <FilterSelect
          label={t('filters.visibility')}
          value={state.visibility}
          allLabel={t('filters.allVisibilities')}
          onChange={(value) =>
            onChange({ visibility: value as RecipeVisibility | '' })
          }
          options={visibilities.map((visibility) => ({
            value: visibility,
            label: t(`visibility.${visibility}`),
          }))}
        />
        <label className="seg-recipes__mine">
          <input
            type="checkbox"
            checked={state.mine}
            onChange={(event) => onChange({ mine: event.target.checked })}
          />
          <span>{t('filters.mine')}</span>
        </label>
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
  allLabel?: string
}

function FilterSelect({
  label,
  value,
  options,
  onChange,
  allLabel,
}: FilterSelectProps) {
  const { t } = useTranslation('recipes')
  return (
    <label className="seg-recipes__field">
      <span>{label}</span>
      <Select
        value={value}
        onChange={(event) => onChange(event.target.value)}
        options={[{ value: '', label: allLabel ?? t('filters.all') }, ...options]}
      />
    </label>
  )
}

interface RecipeGalleryProps {
  recipes: RecipeSummary[]
  categories: RecipeCategory[]
  state: RecipesState
  busy: boolean
  onSort: (sort: RecipeSortField) => void
  onOpen: (recipeId: number) => void
}

function RecipeGallery({
  recipes,
  categories,
  state,
  busy,
  onSort,
  onOpen,
}: RecipeGalleryProps) {
  const { t } = useTranslation('recipes')
  return (
    <section className="seg-recipes__gallery-wrap" aria-busy={busy}>
      <div className="seg-recipes__sortbar">
        <span className="seg-recipes__sort-label">{t('sort.label')}</span>
        {sortFields.map((field) => (
          <button
            key={field}
            type="button"
            className={
              'seg-recipes__sort' +
              (state.sort === field ? ' seg-recipes__sort--active' : '')
            }
            onClick={() => onSort(field)}
          >
            {t(`sort.${field}`)}
            {state.sort === field && (
              <span>{state.sortDirection === 'asc' ? <ArrowDownAZ /> : <ArrowUpZA />}</span>
            )}
          </button>
        ))}
      </div>
      <div className="seg-recipes__gallery">
        {recipes.map((recipe) => (
          <article key={recipe.id} className="seg-recipes-card">
            <button
              type="button"
              className="seg-recipes-card__open"
              onClick={() => onOpen(recipe.id)}
              aria-label={t('gallery.open', { name: recipe.name })}
            >
              <RecipeThumb recipe={recipe} categories={categories} />
            </button>
            <div className="seg-recipes-card__body">
              <div className="seg-recipes-card__title-row">
                <h2>{recipe.name}</h2>
                <Badge tone={recipe.visibility === 'Private' ? 'neutral' : 'aqua'}>
                  {t(`visibility.${recipe.visibility}`)}
                </Badge>
              </div>
              <div className="seg-recipes-card__chips">
                <CategoryChip recipe={recipe} categories={categories} />
                <DifficultyDots difficulty={recipe.difficulty} />
              </div>
              <p className="seg-recipes-card__meta">{t('gallery.by', { name: recipe.creatorName })}</p>
            </div>
          </article>
        ))}
      </div>
    </section>
  )
}

function RecipeThumb({
  recipe,
  categories,
}: {
  recipe: RecipeSummary
  categories: RecipeCategory[]
}) {
  const { t } = useTranslation('recipes')
  const tone = toneForCategory(recipe.categoryId, categories)
  return (
    <span className={`seg-recipes-thumb seg-recipes-tone--${tone}`}>
      {recipe.thumbnail.url != null ? (
        <img
          src={recipe.thumbnail.url}
          alt={t('gallery.thumbnailAlt', { name: recipe.name })}
        />
      ) : (
        <span className="seg-recipes-thumb__placeholder">
          <UtensilsCrossed size={34} aria-hidden="true" />
          <span>{t('gallery.placeholder')}</span>
        </span>
      )}
      <span
        className={
          'seg-recipes-thumb__vis' +
          (recipe.visibility === 'Private' ? ' seg-recipes-thumb__vis--private' : '')
        }
      >
        {recipe.visibility === 'Private' ? <Lock size={13} /> : <Globe size={13} />}
      </span>
    </span>
  )
}

function CategoryChip({
  recipe,
  categories,
}: {
  recipe: Pick<RecipeSummary, 'categoryId' | 'categoryName'>
  categories: RecipeCategory[]
}) {
  const tone = toneForCategory(recipe.categoryId, categories)
  return <span className={`seg-recipes-cat seg-recipes-tone--${tone}`}>{recipe.categoryName}</span>
}

function DifficultyDots({ difficulty }: { difficulty: RecipeDifficulty | null }) {
  const { t } = useTranslation('recipes')
  if (difficulty == null) {
    return <span className="seg-recipes-diff seg-recipes-diff--none">{t('difficulty.none')}</span>
  }
  return (
    <span className={`seg-recipes-diff seg-recipes-diff--${difficulty.toLowerCase()}`}>
      <span className="seg-recipes-diff__dots" aria-hidden="true">
        <i />
        <i />
        <i />
      </span>
      {t(`difficulty.${difficulty}`)}
    </span>
  )
}

function toneForCategory(categoryId: number, categories: RecipeCategory[]) {
  const index = Math.max(0, categories.findIndex((category) => category.id === categoryId))
  return categoryTones[index % categoryTones.length]
}

interface PagerProps {
  page: number
  pageSize: RecipePageSize
  totalPages: number
  fetching: boolean
  onPage: (page: number) => void
  onPageSize: (pageSize: RecipePageSize) => void
}

function Pager({
  page,
  pageSize,
  totalPages,
  fetching,
  onPage,
  onPageSize,
}: PagerProps) {
  const { t } = useTranslation('recipes')
  return (
    <nav className="seg-recipes__pager" aria-label={t('pagination.label')}>
      <label className="seg-recipes__rows">
        <span>{t('pagination.rowsPerPage')}</span>
        <Select
          value={String(pageSize)}
          onChange={(event) => onPageSize(Number(event.target.value) as RecipePageSize)}
          options={recipePageSizes.map((size) => ({
            value: String(size),
            label: String(size),
          }))}
        />
      </label>
      <div className="seg-recipes__pager-nav">
        <Button
          variant="ghost"
          size="sm"
          iconLeft={<ChevronLeft size={16} />}
          disabled={page <= 1 || fetching}
          onClick={() => onPage(Math.max(1, page - 1))}
        >
          {t('pagination.previous')}
        </Button>
        <span className="seg-recipes__page" aria-live="polite">
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

interface RecipeDialogProps {
  mode: 'create' | 'edit'
  recipeId?: number
  currentUserId: number | null
  onClose: () => void
  onSaved: (recipe: Recipe, mode: 'create' | 'edit') => void
  onDeleted: (recipe: Recipe) => void
}

function RecipeDialog({
  mode,
  recipeId,
  currentUserId,
  onClose,
  onSaved,
  onDeleted,
}: RecipeDialogProps) {
  const { t } = useTranslation('recipes')
  const categories = useRecipeCategories()
  const recipeQuery = useQuery({
    queryKey: recipesKeys.recipe(recipeId as number),
    queryFn: ({ signal }) => recipesApi.getRecipe(recipeId as number, signal),
    enabled: mode === 'edit' && recipeId != null,
  })

  const title = mode === 'create' ? t('editor.createTitle') : t('editor.editTitle')
  const description =
    mode === 'create' ? t('editor.createDescription') : t('editor.editDescription')

  if (categories.data == null || (mode === 'edit' && recipeQuery.isPending)) {
    return (
      <Dialog scrollable width={880} title={title} onClose={onClose} closeLabel={t('editor.actions.cancel')}>
        <div className="seg-recipes-editor__status">
          <Spinner />
          <span>{t('editor.loading')}</span>
        </div>
      </Dialog>
    )
  }

  if (mode === 'edit' && recipeQuery.isError) {
    const notFound =
      isApiError(recipeQuery.error) && recipeQuery.error.kind === 'not-found'
    return (
      <Dialog
        width={640}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
        footer={<Button onClick={onClose}>{t('editor.actions.cancel')}</Button>}
      >
        <p className="seg-recipes-editor__error" role="alert">
          {notFound ? t('editor.notFound') : t('editor.loadError')}
        </p>
      </Dialog>
    )
  }

  const recipe = mode === 'edit' ? (recipeQuery.data as Recipe) : undefined
  const initialValues =
    recipe != null ? fromRecipe(recipe) : buildDefaults(firstCatalogId(categories.data))
  const canChangeVisibility =
    recipe == null || (currentUserId != null && recipe.createdById === currentUserId)

  return (
    <RecipeEditorForm
      mode={mode}
      recipeId={recipeId}
      recipe={recipe}
      title={title}
      description={description}
      initialValues={initialValues}
      categories={categories.data}
      canChangeVisibility={canChangeVisibility}
      onClose={onClose}
      onSaved={onSaved}
      onDeleted={onDeleted}
    />
  )
}

function firstCatalogId(items: ReadonlyArray<{ id: number }> | undefined): string {
  const first = items?.[0]
  return first != null ? String(first.id) : ''
}

interface RecipeEditorFormProps {
  mode: 'create' | 'edit'
  recipeId?: number
  recipe?: Recipe
  title: string
  description: string
  initialValues: RecipeFormValues
  categories: RecipeCategory[]
  canChangeVisibility: boolean
  onClose: () => void
  onSaved: (recipe: Recipe, mode: 'create' | 'edit') => void
  onDeleted: (recipe: Recipe) => void
}

function RecipeEditorForm({
  mode,
  recipeId,
  recipe,
  title,
  description,
  initialValues,
  categories,
  canChangeVisibility,
  onClose,
  onSaved,
  onDeleted,
}: RecipeEditorFormProps) {
  const { t } = useTranslation('recipes')
  const schema = useMemo(
    () =>
      createRecipeFormSchema({
        nameRequired: t('editor.validation.nameRequired'),
        nameTooLong: t('editor.validation.nameTooLong'),
        categoryRequired: t('editor.validation.categoryRequired'),
        positiveNumber: t('editor.validation.positiveNumber'),
        nonNegativeNumber: t('editor.validation.nonNegativeNumber'),
        ingredientNameRequired: t('editor.validation.ingredientNameRequired'),
        ingredientNameTooLong: t('editor.validation.ingredientNameTooLong'),
        quantityTooLong: t('editor.validation.quantityTooLong'),
        stepRequired: t('editor.validation.stepRequired'),
        stepTooLong: t('editor.validation.stepTooLong'),
        notesTooLong: t('editor.validation.notesTooLong'),
      }),
    [t],
  )
  const form = useForm<RecipeFormValues>({
    resolver: zodResolver(schema),
    defaultValues: initialValues,
  })
  const { register, control, handleSubmit, formState, setValue } = form
  const ingredientFields = useFieldArray({ control, name: 'ingredients', keyName: 'fieldKey' })
  const stepFields = useFieldArray({ control, name: 'steps', keyName: 'fieldKey' })
  const ingredients = useWatch({ control, name: 'ingredients' }) ?? []
  const visibility = useWatch({ control, name: 'visibility' })
  const selectedCategoryId = Number(useWatch({ control, name: 'categoryId' }) || 0)
  const [serverError, setServerError] = useState<string | null>(null)
  const [confirmingClose, setConfirmingClose] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const [stagedFiles, setStagedFiles] = useState<File[]>([])
  const [createdRecipe, setCreatedRecipe] = useState<Recipe | null>(null)
  const [selectingIngredient, setSelectingIngredient] = useState<number | null>(null)
  const editedRef = useRef(false)

  const mutation = useMutation({
    mutationFn: (request: CreateRecipeRequest) =>
      mode === 'create'
        ? recipesApi.createRecipe(request)
        : recipesApi.updateRecipe(recipeId as number, request),
    onSuccess: (saved) => {
      if (mode === 'create' && stagedFiles.length > 0) {
        setCreatedRecipe(saved)
        return
      }
      onSaved(saved, mode)
    },
    onError: (error) => setServerError(mapServerError(error, t)),
  })

  const deleteMutation = useMutation({
    mutationFn: () => recipesApi.deleteRecipe(recipeId as number),
    onSuccess: () => {
      if (recipe != null) onDeleted(recipe)
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

  if (createdRecipe != null) {
    const finish = () => onSaved(createdRecipe, 'create')
    return (
      <Dialog
        scrollable
        width={820}
        title={t('editor.attachments.uploadTitle')}
        description={t('editor.attachments.uploadDescription', {
          name: createdRecipe.name,
        })}
        onClose={finish}
        closeLabel={t('editor.attachments.close')}
        footer={<Button onClick={finish}>{t('editor.attachments.done')}</Button>}
      >
        <section className="seg-recipes-editor__section">
          <h3>{t('editor.attachments.title')}</h3>
          <RecipeAttachments recipeId={createdRecipe.id} autoUpload={stagedFiles} />
        </section>
      </Dialog>
    )
  }

  const selectedIngredient =
    selectingIngredient == null ? null : ingredients[selectingIngredient]

  return (
    <>
      <Dialog
        scrollable
        width={900}
        title={title}
        description={description}
        onClose={requestClose}
        closeLabel={t('editor.actions.cancel')}
        footer={
          <>
            {mode === 'edit' && (
              <Button
                variant="ghost"
                className="seg-recipes-editor__delete"
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
            <Button type="submit" form="seg-recipes-form" disabled={submitting}>
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
          id="seg-recipes-form"
          className="seg-recipes-editor"
          onSubmit={submit}
          onChange={() => {
            editedRef.current = true
          }}
          noValidate
        >
          {serverError != null && (
            <p className="seg-recipes-editor__error" role="alert">
              {serverError}
            </p>
          )}

          <section className="seg-recipes-editor__section">
            <SectionHead icon={<BookOpen size={16} />} title={t('editor.sections.identity')} />
            <div className="seg-recipes-editor__identity">
              <div className="seg-recipes-editor__image">
                <div className={`seg-recipes-editor__imagebox seg-recipes-tone--${toneForCategory(selectedCategoryId, categories)}`}>
                  {recipe?.thumbnail.url != null ? (
                    <img src={recipe.thumbnail.url} alt="" />
                  ) : (
                    <ImagePlus size={30} aria-hidden="true" />
                  )}
                </div>
                <span>{t('editor.attachments.hint')}</span>
              </div>
              <div className="seg-recipes-editor__identity-fields">
                <Input
                  label={t('editor.fields.name')}
                  placeholder={t('editor.fields.namePlaceholder')}
                  required
                  error={formState.errors.name?.message}
                  {...register('name')}
                />
                <div className="seg-recipes-editor__grid">
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
                  <Field label={t('editor.fields.difficulty')}>
                    <Select
                      {...register('difficulty')}
                      options={[
                        { value: '', label: t('difficulty.none') },
                        ...difficulties.map((difficulty) => ({
                          value: difficulty,
                          label: t(`difficulty.${difficulty}`),
                        })),
                      ]}
                    />
                  </Field>
                  <Input
                    label={t('editor.fields.servings')}
                    inputMode="numeric"
                    error={formState.errors.servings?.message}
                    {...register('servings')}
                  />
                  <Input
                    label={t('editor.fields.preparationMinutes')}
                    inputMode="numeric"
                    error={formState.errors.preparationMinutes?.message}
                    {...register('preparationMinutes')}
                  />
                  <Input
                    label={t('editor.fields.cookMinutes')}
                    inputMode="numeric"
                    error={formState.errors.cookMinutes?.message}
                    {...register('cookMinutes')}
                  />
                  <ToggleField
                    id="recipes-field-visibility"
                    label={t('editor.fields.visibility')}
                    hint={
                      canChangeVisibility
                        ? t('editor.hints.visibility')
                        : t('editor.hints.visibilityLocked')
                    }
                  >
                    <SegmentedControl
                      aria-labelledby="recipes-field-visibility"
                      disabled={!canChangeVisibility}
                      {...register('visibility')}
                      options={visibilities.map((value) => ({
                        value,
                        label: t(`visibility.${value}`),
                        icon: visibilityMeta[value].icon,
                        tone: visibilityMeta[value].tone,
                      }))}
                    />
                  </ToggleField>
                </div>
              </div>
            </div>
          </section>

          <section className="seg-recipes-editor__section">
            <SectionHead
              icon={<List size={16} />}
              title={t('editor.sections.ingredients')}
              count={ingredientFields.fields.length}
              hint={t('editor.hints.ingredients')}
            />
            {ingredientFields.fields.length === 0 ? (
              <p className="seg-recipes-editor__subempty">{t('editor.ingredients.empty')}</p>
            ) : (
              <div className="seg-recipes-ing">
                {ingredientFields.fields.map((field, index) => {
                  const ingredient = ingredients[index]
                  return (
                    <div key={field.fieldKey} className="seg-recipes-ing__row">
                      <div className="seg-recipes-ing__move">
                        <button
                          type="button"
                          onClick={() => ingredientFields.swap(index, index - 1)}
                          disabled={index === 0}
                          aria-label={t('editor.ingredients.moveUp')}
                        >
                          <ChevronUp size={14} />
                        </button>
                        <button
                          type="button"
                          onClick={() => ingredientFields.swap(index, index + 1)}
                          disabled={index === ingredientFields.fields.length - 1}
                          aria-label={t('editor.ingredients.moveDown')}
                        >
                          <ChevronDown size={14} />
                        </button>
                      </div>
                      <Input
                        label={t('editor.fields.ingredientName')}
                        placeholder={t('editor.fields.ingredientNamePlaceholder')}
                        error={formState.errors.ingredients?.[index]?.name?.message}
                        {...register(`ingredients.${index}.name`)}
                      />
                      <Input
                        label={t('editor.fields.quantity')}
                        placeholder={t('editor.fields.quantityPlaceholder')}
                        error={formState.errors.ingredients?.[index]?.quantity?.message}
                        {...register(`ingredients.${index}.quantity`)}
                      />
                      <div className="seg-recipes-ing__item">
                        <span className="seg-recipes-editor__field-label">
                          {t('editor.itemLink.label')}
                        </span>
                        <EntityReferenceField
                          value={
                            ingredient?.itemId == null
                              ? null
                              : inventoryItemReference({
                                  name: ingredient.itemName || t('editor.itemLink.empty'),
                                })
                          }
                          icon={<Package size={18} />}
                          placeholder={t('editor.itemLink.empty')}
                          helperText={t('editor.itemLink.selectorDescription')}
                          browseLabel={t('editor.itemLink.empty')}
                          changeLabel={t('selector.select')}
                          clearLabel={t('editor.itemLink.clear')}
                          onBrowse={() => setSelectingIngredient(index)}
                          onClear={() => {
                            editedRef.current = true
                            setValue(`ingredients.${index}.itemId`, null, {
                              shouldDirty: true,
                            })
                            setValue(`ingredients.${index}.itemName`, '', {
                              shouldDirty: true,
                            })
                          }}
                        />
                      </div>
                      <button
                        type="button"
                        className="seg-recipes-editor__row-delete"
                        onClick={() => ingredientFields.remove(index)}
                        aria-label={t('editor.ingredients.remove')}
                      >
                        <Trash2 size={16} aria-hidden="true" />
                      </button>
                    </div>
                  )
                })}
              </div>
            )}
            <Button
              variant="outline"
              size="sm"
              iconLeft={<Plus size={15} />}
              onClick={() => ingredientFields.append(newIngredient())}
            >
              {t('editor.ingredients.add')}
            </Button>
          </section>

          <section className="seg-recipes-editor__section">
            <SectionHead
              icon={<ListOrdered size={16} />}
              title={t('editor.sections.steps')}
              count={stepFields.fields.length}
              hint={t('editor.hints.steps')}
            />
            {stepFields.fields.length === 0 ? (
              <p className="seg-recipes-editor__subempty">{t('editor.steps.empty')}</p>
            ) : (
              <div className="seg-recipes-steps">
                {stepFields.fields.map((field, index) => (
                  <div key={field.fieldKey} className="seg-recipes-steps__row">
                    <span className="seg-recipes-steps__num">{index + 1}</span>
                    <label className="seg-recipes-editor__textarea-field">
                      <span>{t('editor.fields.stepInstruction')}</span>
                      <textarea
                        rows={3}
                        placeholder={t('editor.fields.stepPlaceholder')}
                        aria-invalid={
                          formState.errors.steps?.[index]?.instruction != null
                        }
                        {...register(`steps.${index}.instruction`)}
                      />
                      {formState.errors.steps?.[index]?.instruction?.message != null && (
                        <span className="seg-recipes-editor__field-error" role="alert">
                          {formState.errors.steps[index]?.instruction?.message}
                        </span>
                      )}
                    </label>
                    <div className="seg-recipes-ing__move">
                      <button
                        type="button"
                        onClick={() => stepFields.swap(index, index - 1)}
                        disabled={index === 0}
                        aria-label={t('editor.steps.moveUp')}
                      >
                        <ChevronUp size={14} />
                      </button>
                      <button
                        type="button"
                        onClick={() => stepFields.swap(index, index + 1)}
                        disabled={index === stepFields.fields.length - 1}
                        aria-label={t('editor.steps.moveDown')}
                      >
                        <ChevronDown size={14} />
                      </button>
                    </div>
                    <button
                      type="button"
                      className="seg-recipes-editor__row-delete"
                      onClick={() => stepFields.remove(index)}
                      aria-label={t('editor.steps.remove')}
                    >
                      <Trash2 size={16} aria-hidden="true" />
                    </button>
                  </div>
                ))}
              </div>
            )}
            <Button
              variant="outline"
              size="sm"
              iconLeft={<Plus size={15} />}
              onClick={() => stepFields.append(newStep())}
            >
              {t('editor.steps.add')}
            </Button>
          </section>

          <section className="seg-recipes-editor__section">
            <SectionHead icon={<Clock size={16} />} title={t('editor.sections.notes')} hint={t('editor.hints.notes')} />
            <label className="seg-recipes-editor__textarea-field">
              <span>{t('editor.fields.notes')}</span>
              <textarea
                rows={4}
                placeholder={t('editor.fields.notesPlaceholder')}
                aria-invalid={formState.errors.notes != null}
                {...register('notes')}
              />
              {formState.errors.notes?.message != null && (
                <span className="seg-recipes-editor__field-error" role="alert">
                  {formState.errors.notes.message}
                </span>
              )}
            </label>
          </section>

          <section className="seg-recipes-editor__section">
            <SectionHead icon={<ImagePlus size={16} />} title={t('editor.sections.attachments')} />
            <p className="seg-recipes-editor__hint">{t('editor.attachments.hint')}</p>
            {mode === 'edit' && recipeId != null ? (
              <RecipeAttachments recipeId={recipeId} />
            ) : (
              <StagedAttachments
                files={stagedFiles}
                onChange={(files) => {
                  editedRef.current = true
                  setStagedFiles(files)
                }}
              />
            )}
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

      {selectingIngredient != null && (
        <InventoryItemEntitySelector
          currentItemId={selectedIngredient?.itemId ?? null}
          forcedVisibility={visibility === 'Public' ? 'Public' : null}
          description={t('editor.itemLink.selectorDescription')}
          onClose={() => setSelectingIngredient(null)}
          onSelect={(item) => {
            editedRef.current = true
            setValue(`ingredients.${selectingIngredient}.itemId`, item.id, {
              shouldDirty: true,
              shouldValidate: true,
            })
            setValue(`ingredients.${selectingIngredient}.itemName`, item.name, {
              shouldDirty: true,
            })
            setSelectingIngredient(null)
          }}
        />
      )}
    </>
  )
}

function SectionHead({
  icon,
  title,
  count,
  hint,
}: {
  icon: ReactNode
  title: string
  count?: number
  hint?: string
}) {
  return (
    <div className="seg-recipes-editor__section-head">
      <span className="seg-recipes-editor__section-icon">{icon}</span>
      <h3>{title}</h3>
      {count != null && <span className="seg-recipes-editor__count">{count}</span>}
      {hint != null && <span className="seg-recipes-editor__hint">{hint}</span>}
    </div>
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
    <div className="seg-recipes-editor__field">
      <label className="seg-recipes-editor__field-control">
        <span className="seg-recipes-editor__field-label">{label}</span>
        {children}
      </label>
      {message != null && (
        <span
          className={
            'seg-recipes-editor__field-hint' +
            (error != null ? ' seg-recipes-editor__field-hint--error' : '')
          }
        >
          {message}
        </span>
      )}
    </div>
  )
}

interface ToggleFieldProps {
  id: string
  label: string
  hint?: string
  children: ReactNode
}

function ToggleField({ id, label, hint, children }: ToggleFieldProps) {
  return (
    <div className="seg-recipes-editor__field seg-recipes-editor__field--wide">
      <span className="seg-recipes-editor__field-label" id={id}>
        {label}
      </span>
      {children}
      {hint != null && <span className="seg-recipes-editor__field-hint">{hint}</span>}
    </div>
  )
}

interface StagedAttachmentsProps {
  files: File[]
  onChange: (files: File[]) => void
}

function StagedAttachments({ files, onChange }: StagedAttachmentsProps) {
  const { t } = useTranslation('recipes')
  const input = useRef<HTMLInputElement>(null)
  return (
    <div className="seg-recipes-staged">
      <Button
        variant="outline"
        size="sm"
        onClick={() => input.current?.click()}
        iconLeft={<Plus size={15} />}
      >
        {t('editor.attachments.add')}
      </Button>
      <input
        ref={input}
        type="file"
        multiple
        className="seg-recipes-staged__input"
        onChange={(event) => {
          const next = event.target.files == null ? [] : Array.from(event.target.files)
          onChange([...files, ...next])
          event.target.value = ''
        }}
        aria-label={t('editor.attachments.add')}
      />
      {files.length === 0 ? (
        <p className="seg-recipes-staged__empty">{t('editor.attachments.empty')}</p>
      ) : (
        <ul className="seg-recipes-staged__list">
          {files.map((file, index) => (
            <li key={`${file.name}-${index}`} className="seg-recipes-staged__item">
              <span>{file.name}</span>
              <button
                type="button"
                onClick={() =>
                  onChange(files.filter((_, candidate) => candidate !== index))
                }
                aria-label={t('editor.attachments.remove')}
              >
                <Trash2 size={15} aria-hidden="true" />
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}

function mapServerError(error: unknown, t: (key: string) => string): string {
  if (isApiError(error)) {
    switch (error.problem?.code) {
      case 'recipes.recipe.validation':
        return t('editor.errors.validation')
      case 'recipes.recipe.visibility_forbidden':
        return t('editor.errors.visibilityForbidden')
      case 'recipes.ingredient.item_not_accessible':
        return t('editor.errors.itemNotAccessible')
      case 'recipes.ingredient.item_visibility_forbidden':
        return t('editor.errors.itemVisibilityForbidden')
      case 'recipes.catalog.unknown_reference':
        return t('editor.errors.unknownReference')
    }
    if (error.kind === 'not-found') return t('editor.notFound')
    if (error.kind === 'transient' || error.kind === 'unavailable') {
      return t('editor.errors.conflict')
    }
  }
  return t('editor.errors.generic')
}
