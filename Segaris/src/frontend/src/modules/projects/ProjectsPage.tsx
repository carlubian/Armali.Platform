import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  AlertTriangle,
  ClipboardList,
  ChevronDown,
  ChevronRight,
  FolderTree,
  Globe,
  Lock,
  Plus,
  ShieldAlert,
  Trash2,
} from 'lucide-react'
import { useEffect, useState, type ReactNode } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'

import { isApiError } from '@/app/api/errors'
import {
  projectStatuses,
  projectVisibilities,
  projectsApi,
  projectsStructureApi,
  type Activity,
  type AxisNode,
  type CreateActivityRequest,
  type CreateProjectRequest,
  type ProgramNode,
  type Project,
  type ProjectRisk,
  type ProjectRiskBandSummary,
  type ProjectRiskRequest,
  type ProjectTreeItem,
  type RiskBand,
} from '@/app/api/projects'
import { useSession } from '@/app/session/SessionContext'
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

import { projectRiskRequestSchema, projectsKeys } from './contracts'
import { ProjectAttachments } from './ProjectAttachments'
import { useProjectsDialogState } from './projectsState'

import './ProjectsPage.css'

type ItemMode = 'project' | 'activity'
type SaveMode = 'create' | 'edit'
type ToastKind =
  | 'projectCreated'
  | 'projectUpdated'
  | 'projectDeleted'
  | 'activityCreated'
  | 'activityUpdated'
  | 'activityDeleted'
  | 'riskSaved'
  | 'riskDeleted'

interface ToastState {
  kind: ToastKind
  name: string
}

const itemModes = ['project', 'activity'] as const

function isItemMode(value: string): value is ItemMode {
  return itemModes.some((itemMode) => itemMode === value)
}

function kindKey(itemMode: ItemMode): 'Project' | 'Activity' {
  return itemMode === 'project' ? 'Project' : 'Activity'
}

export function ProjectsPage() {
  const { t } = useTranslation('projects')
  const { session } = useSession()
  const queryClient = useQueryClient()
  const dialogState = useProjectsDialogState()
  const [expandedPrograms, setExpandedPrograms] = useState<Set<number>>(new Set())
  const [expandedAxes, setExpandedAxes] = useState<Set<number>>(new Set())
  const [toast, setToast] = useState<ToastState | null>(null)
  const programsQuery = useQuery({
    queryKey: projectsKeys.programs(),
    queryFn: ({ signal }) => projectsApi.programs(signal),
  })

  const toggleProgram = (programId: number) =>
    setExpandedPrograms((current) => toggled(current, programId))
  const toggleAxis = (axisId: number) =>
    setExpandedAxes((current) => toggled(current, axisId))

  const invalidateTree = (item?: Project | Activity) => {
    void queryClient.invalidateQueries({ queryKey: projectsKeys.tree() })
    if (item != null) {
      if ('riskSummary' in item)
        void queryClient.invalidateQueries({ queryKey: projectsKeys.project(item.id) })
      else void queryClient.invalidateQueries({ queryKey: projectsKeys.activity(item.id) })
    }
  }

  const handleSaved = (item: Project | Activity, itemMode: ItemMode, saveMode: SaveMode) => {
    invalidateTree(item)
    setToast({
      kind:
        itemMode === 'project'
          ? saveMode === 'create'
            ? 'projectCreated'
            : 'projectUpdated'
          : saveMode === 'create'
            ? 'activityCreated'
            : 'activityUpdated',
      name: item.name,
    })
    dialogState.closeDialog()
  }

  const handleDeleted = (item: Project | Activity, itemMode: ItemMode) => {
    invalidateTree()
    setToast({
      kind: itemMode === 'project' ? 'projectDeleted' : 'activityDeleted',
      name: item.name,
    })
    dialogState.closeDialog()
  }

  if (programsQuery.isError) {
    const error = programsQuery.error
    if (isApiError(error) && ['unavailable', 'transient'].includes(error.kind)) {
      return <ServiceUnavailable onRetry={() => void programsQuery.refetch()} />
    }
  }

  return (
    <main className="seg-projects armali-aurora">
      <section className="seg-projects__head">
        <div>
          <div className="armali-eyebrow">{t('page.eyebrow')}</div>
          <h1>{t('page.title')}</h1>
          <p>{t('page.description')}</p>
        </div>
        <Badge tone="neutral">{t('page.programCount', { count: programsQuery.data?.length ?? 0 })}</Badge>
      </section>

      <section className="seg-projects__workspace">
        <div className="seg-projects__tree-card">
          <div className="seg-projects__tree-head">
            <div>
              <h2>{t('tree.title')}</h2>
              <p>{t('tree.description')}</p>
            </div>
            {programsQuery.isFetching && !programsQuery.isPending && <Spinner size={18} />}
          </div>

          {programsQuery.isPending ? (
            <div className="seg-projects__loading">
              <Spinner />
              <span>{t('tree.loading')}</span>
            </div>
          ) : programsQuery.isError ? (
            <p className="seg-projects__error" role="alert">
              {t('tree.loadError')}
            </p>
          ) : (programsQuery.data ?? []).length === 0 ? (
            <p className="seg-projects__empty">{t('tree.empty')}</p>
          ) : (
            <ul className="seg-projects-tree" aria-label={t('tree.label')}>
              {(programsQuery.data ?? []).map((program) => (
                <ProgramBranch
                  key={program.id}
                  program={program}
                  expanded={expandedPrograms.has(program.id)}
                  expandedAxes={expandedAxes}
                  onToggle={() => toggleProgram(program.id)}
                  onToggleAxis={toggleAxis}
                  onCreateItem={dialogState.openCreateItem}
                  onOpenProject={dialogState.openProject}
                  onOpenProjectRisks={dialogState.openProjectRisks}
                  onOpenActivity={dialogState.openActivity}
                />
              ))}
            </ul>
          )}
        </div>
      </section>

      {dialogState.dialog.mode === 'createItem' && (
        <ProjectItemDialog
          mode="create"
          itemMode={dialogState.dialog.itemMode}
          axisId={dialogState.dialog.axisId}
          currentUserId={session?.userId ?? null}
          onClose={dialogState.closeDialog}
          onSaved={handleSaved}
          onDeleted={handleDeleted}
        />
      )}
      {dialogState.dialog.mode === 'editProject' &&
        (() => {
          const projectId = dialogState.dialog.projectId
          return (
            <>
              <ProjectItemDialog
                mode="edit"
                itemMode="project"
                itemId={projectId}
                currentUserId={session?.userId ?? null}
                onClose={dialogState.closeDialog}
                onSaved={handleSaved}
                onDeleted={handleDeleted}
                onOpenRisks={() => dialogState.openProjectRisks(projectId)}
              />
              {dialogState.dialog.risks && (
                <ProjectRiskDialog
                  projectId={projectId}
                  onClose={() => dialogState.closeProjectRisks(projectId)}
                  onChanged={(kind, name) => setToast({ kind, name })}
                />
              )}
            </>
          )
        })()}
      {dialogState.dialog.mode === 'editActivity' && (
        <ProjectItemDialog
          mode="edit"
          itemMode="activity"
          itemId={dialogState.dialog.activityId}
          currentUserId={session?.userId ?? null}
          onClose={dialogState.closeDialog}
          onSaved={handleSaved}
          onDeleted={handleDeleted}
        />
      )}

      {toast != null && (
        <div className="seg-projects__toast">
          <Toast
            tone="success"
            title={t(`toast.${toast.kind}`)}
            onClose={() => setToast(null)}
            closeLabel={t('actions.close')}
          >
            {t(`toast.${toast.kind}Body`, { name: toast.name })}
          </Toast>
        </div>
      )}
    </main>
  )
}

function toggled(current: Set<number>, id: number): Set<number> {
  const next = new Set(current)
  if (next.has(id)) next.delete(id)
  else next.add(id)
  return next
}

interface ProgramBranchProps {
  program: ProgramNode
  expanded: boolean
  expandedAxes: Set<number>
  onToggle: () => void
  onToggleAxis: (axisId: number) => void
  onCreateItem: (axisId: number) => void
  onOpenProject: (projectId: number) => void
  onOpenProjectRisks: (projectId: number) => void
  onOpenActivity: (activityId: number) => void
}

function ProgramBranch({
  program,
  expanded,
  expandedAxes,
  onToggle,
  onToggleAxis,
  onCreateItem,
  onOpenProject,
  onOpenProjectRisks,
  onOpenActivity,
}: ProgramBranchProps) {
  const { t } = useTranslation('projects')
  const axesQuery = useQuery({
    queryKey: projectsKeys.axes(program.id),
    queryFn: ({ signal }) => projectsApi.axes(program.id, signal),
    enabled: expanded,
  })

  return (
    <li className="seg-projects-tree__program">
      <button
        type="button"
        className="seg-projects-tree__toggle seg-projects-tree__toggle--program"
        onClick={onToggle}
        aria-expanded={expanded}
      >
        {expanded ? <ChevronDown size={18} /> : <ChevronRight size={18} />}
        <FolderTree size={18} aria-hidden="true" />
        <span className="seg-projects-tree__node-title">{program.code}</span>
        <span className="seg-projects-tree__node-name">{program.name}</span>
      </button>

      {expanded && (
        <div className="seg-projects-tree__children">
          {axesQuery.isPending ? (
            <InlineState>{t('tree.loadingAxes')}</InlineState>
          ) : axesQuery.isError ? (
            <InlineState tone="error">{t('tree.axesLoadError')}</InlineState>
          ) : (axesQuery.data ?? []).length === 0 ? (
            <InlineState>{t('tree.noAxes')}</InlineState>
          ) : (
            <ul>
              {(axesQuery.data ?? []).map((axis) => (
                <AxisBranch
                  key={axis.id}
                  axis={axis}
                  expanded={expandedAxes.has(axis.id)}
                  onToggle={() => onToggleAxis(axis.id)}
                  onCreateItem={onCreateItem}
                  onOpenProject={onOpenProject}
                  onOpenProjectRisks={onOpenProjectRisks}
                  onOpenActivity={onOpenActivity}
                />
              ))}
            </ul>
          )}
        </div>
      )}
    </li>
  )
}

interface AxisBranchProps {
  axis: AxisNode
  expanded: boolean
  onToggle: () => void
  onCreateItem: (axisId: number) => void
  onOpenProject: (projectId: number) => void
  onOpenProjectRisks: (projectId: number) => void
  onOpenActivity: (activityId: number) => void
}

function AxisBranch({
  axis,
  expanded,
  onToggle,
  onCreateItem,
  onOpenProject,
  onOpenProjectRisks,
  onOpenActivity,
}: AxisBranchProps) {
  const { t } = useTranslation('projects')
  const itemsQuery = useQuery({
    queryKey: projectsKeys.items(axis.id),
    queryFn: ({ signal }) => projectsApi.items(axis.id, signal),
    enabled: expanded,
  })

  return (
    <li className="seg-projects-tree__axis">
      <div className="seg-projects-tree__axis-row">
        <button
          type="button"
          className="seg-projects-tree__toggle"
          onClick={onToggle}
          aria-expanded={expanded}
        >
          {expanded ? <ChevronDown size={18} /> : <ChevronRight size={18} />}
          <span className="seg-projects-tree__node-title">{axis.code}</span>
          <span className="seg-projects-tree__node-name">{axis.name}</span>
        </button>
        <div className="seg-projects-tree__axis-actions">
          <Button
            size="sm"
            variant="ghost"
            iconLeft={<Plus size={15} />}
            onClick={() => onCreateItem(axis.id)}
          >
            {t('tree.newItem')}
          </Button>
        </div>
      </div>

      {expanded && (
        <div className="seg-projects-tree__children">
          {itemsQuery.isPending ? (
            <InlineState>{t('tree.loadingItems')}</InlineState>
          ) : itemsQuery.isError ? (
            <InlineState tone="error">{t('tree.itemsLoadError')}</InlineState>
          ) : (itemsQuery.data ?? []).length === 0 ? (
            <InlineState>{t('tree.noItems')}</InlineState>
          ) : (
            <ul className="seg-projects-tree__items">
              {(itemsQuery.data ?? []).map((item) => (
                <ProjectTreeItemRow
                  key={`${item.kind}-${item.id}`}
                  item={item}
                  onOpenProject={onOpenProject}
                  onOpenProjectRisks={onOpenProjectRisks}
                  onOpenActivity={onOpenActivity}
                />
              ))}
            </ul>
          )}
        </div>
      )}
    </li>
  )
}

interface ProjectTreeItemRowProps {
  item: ProjectTreeItem
  onOpenProject: (projectId: number) => void
  onOpenProjectRisks: (projectId: number) => void
  onOpenActivity: (activityId: number) => void
}

function ProjectTreeItemRow({
  item,
  onOpenProject,
  onOpenProjectRisks,
  onOpenActivity,
}: ProjectTreeItemRowProps) {
  const { t } = useTranslation('projects')
  const isProject = item.kind === 'Project'

  return (
    <li className="seg-projects-tree__item">
      <button
        type="button"
        className="seg-projects-tree__item-open"
        onClick={() => (isProject ? onOpenProject(item.id) : onOpenActivity(item.id))}
        aria-label={t(isProject ? 'tree.openProject' : 'tree.openActivity', {
          identifier: item.identifier,
        })}
      >
        <span className="seg-projects-tree__identifier">{item.identifier}</span>
        <span className="seg-projects-tree__meta">
          <Badge tone={isProject ? 'aqua' : 'neutral'}>{t(`kind.${item.kind}`)}</Badge>
          <Badge tone="neutral">{t(`status.${item.status}`)}</Badge>
          <Badge tone={item.visibility === 'Private' ? 'neutral' : 'success'}>
            {t(`visibility.${item.visibility}`)}
          </Badge>
        </span>
      </button>
      {isProject && item.riskSummary != null && (
        <button
          type="button"
          className="seg-projects-risk-summary"
          onClick={() => onOpenProjectRisks(item.id)}
          aria-label={t('risks.openFor', { identifier: item.identifier })}
        >
          <RiskSummary summary={item.riskSummary} />
        </button>
      )}
    </li>
  )
}

function RiskSummary({ summary }: { summary: ProjectRiskBandSummary }) {
  const { t } = useTranslation('projects')
  return (
    <span className="seg-projects-risk-summary__pills" aria-label={t('risks.summaryLabel')}>
      <span data-band="Low">{t('risks.summary.low', { count: summary.low })}</span>
      <span data-band="Medium">{t('risks.summary.medium', { count: summary.medium })}</span>
      <span data-band="High">{t('risks.summary.high', { count: summary.high })}</span>
    </span>
  )
}

function InlineState({
  tone = 'neutral',
  children,
}: {
  tone?: 'neutral' | 'error'
  children: ReactNode
}) {
  return (
    <p className={`seg-projects-tree__state seg-projects-tree__state--${tone}`}>
      {children}
    </p>
  )
}

interface ProjectItemDialogProps {
  mode: SaveMode
  itemMode: ItemMode
  axisId?: number
  itemId?: number
  currentUserId: number | null
  onClose: () => void
  onSaved: (item: Project | Activity, itemMode: ItemMode, saveMode: SaveMode) => void
  onDeleted: (item: Project | Activity, itemMode: ItemMode) => void
  onOpenRisks?: () => void
}

function ProjectItemDialog({
  mode,
  itemMode,
  axisId,
  itemId,
  currentUserId,
  onClose,
  onSaved,
  onDeleted,
  onOpenRisks,
}: ProjectItemDialogProps) {
  const { t } = useTranslation('projects')
  const [createItemMode, setCreateItemMode] = useState<ItemMode>(itemMode)
  useEffect(() => {
    if (mode === 'create') setCreateItemMode(itemMode)
  }, [axisId, itemMode, mode])

  const effectiveItemMode = mode === 'create' ? createItemMode : itemMode
  const title = t(`${effectiveItemMode}Editor.${mode}Title`)
  const detailQuery = useQuery({
    queryKey:
      effectiveItemMode === 'project'
        ? projectsKeys.project(itemId as number)
        : projectsKeys.activity(itemId as number),
    queryFn: ({ signal }) =>
      effectiveItemMode === 'project'
        ? projectsApi.getProject(itemId as number, signal)
        : projectsApi.getActivity(itemId as number, signal),
    enabled: mode === 'edit' && itemId != null,
  })
  const programsQuery = useQuery({
    queryKey: projectsKeys.structure(),
    queryFn: ({ signal }) => projectsStructureApi.listPrograms(signal),
  })
  const axesQuery = useQuery({
    queryKey: [...projectsKeys.structure(), 'axes'] as const,
    queryFn: ({ signal }) => projectsStructureApi.listAxes(signal),
  })

  const loading =
    axesQuery.isPending ||
    programsQuery.isPending ||
    (mode === 'edit' && detailQuery.isPending)
  if (loading) {
    return (
      <Dialog
        scrollable
        width={760}
        title={title}
        onClose={onClose}
        closeLabel={t('actions.close')}
      >
        <div className="seg-projects-editor__status">
          <Spinner />
          <span>{t(`${effectiveItemMode}Editor.loading`)}</span>
        </div>
      </Dialog>
    )
  }

  const failed =
    axesQuery.isError ||
    programsQuery.isError ||
    (mode === 'edit' && detailQuery.isError)
  if (failed) {
    const notFound =
      mode === 'edit' &&
      detailQuery.isError &&
      isApiError(detailQuery.error) &&
      detailQuery.error.kind === 'not-found'
    return (
      <Dialog
        width={760}
        title={title}
        onClose={onClose}
        closeLabel={t('actions.close')}
        footer={<Button onClick={onClose}>{t('actions.close')}</Button>}
      >
        <p className="seg-projects-editor__error" role="alert">
          {notFound
            ? t(`${effectiveItemMode}Editor.notFound`)
            : t(`${effectiveItemMode}Editor.loadError`)}
        </p>
      </Dialog>
    )
  }

  const item = mode === 'edit' ? (detailQuery.data as Project | Activity) : undefined
  const fallbackAxisId = axisId ?? axesQuery.data?.[0]?.id ?? 0
  const initialValues: ItemFormValues = {
    axisId: String(item?.axisId ?? fallbackAxisId),
    name: item?.name ?? '',
    status: item?.status ?? 'Planning',
    visibility: item?.visibility ?? 'Public',
  }
  const canChangeVisibility =
    item == null || (currentUserId != null && item.createdById === currentUserId)
  const axisOptions = buildAxisOptions(axesQuery.data ?? [], programsQuery.data ?? [])

  return (
    <ProjectItemForm
      mode={mode}
      itemMode={effectiveItemMode}
      itemId={itemId}
      item={item}
      title={title}
      description={t(`${effectiveItemMode}Editor.${mode}Description`)}
      initialValues={initialValues}
      axisOptions={axisOptions}
      canChangeVisibility={canChangeVisibility}
      onItemModeChange={setCreateItemMode}
      onClose={onClose}
      onSaved={onSaved}
      onDeleted={onDeleted}
      onOpenRisks={onOpenRisks}
    />
  )
}

interface AxisOption {
  value: string
  label: string
}

function buildAxisOptions(axes: AxisNode[], programs: ProgramNode[]): AxisOption[] {
  const programCodes = new Map(programs.map((program) => [program.id, program.code]))
  return axes.map((axis) => ({
    value: String(axis.id),
    label: `${programCodes.get(axis.programId) ?? '----'}${axis.code} - ${axis.name}`,
  }))
}

const itemFormSchema = z.object({
  axisId: z.string().regex(/^[1-9]\d*$/),
  name: z.string().trim().min(1).max(200),
  status: z.enum(projectStatuses),
  visibility: z.enum(projectVisibilities),
})

type ItemFormValues = z.input<typeof itemFormSchema>

interface ProjectItemFormProps {
  mode: SaveMode
  itemMode: ItemMode
  itemId?: number
  item?: Project | Activity
  title: string
  description: string
  initialValues: ItemFormValues
  axisOptions: AxisOption[]
  canChangeVisibility: boolean
  onItemModeChange: (itemMode: ItemMode) => void
  onClose: () => void
  onSaved: (item: Project | Activity, itemMode: ItemMode, saveMode: SaveMode) => void
  onDeleted: (item: Project | Activity, itemMode: ItemMode) => void
  onOpenRisks?: () => void
}

const visibilityMeta: Record<
  'Public' | 'Private',
  { icon: ReactNode; tone: SegmentTone }
> = {
  Public: { icon: <Globe size={15} />, tone: 'accent' },
  Private: { icon: <Lock size={15} />, tone: 'neutral' },
}

function ProjectItemForm({
  mode,
  itemMode,
  itemId,
  item,
  title,
  description,
  initialValues,
  axisOptions,
  canChangeVisibility,
  onItemModeChange,
  onClose,
  onSaved,
  onDeleted,
  onOpenRisks,
}: ProjectItemFormProps) {
  const { t } = useTranslation('projects')
  const [serverError, setServerError] = useState<string | null>(null)
  const [confirmingClose, setConfirmingClose] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const form = useForm<ItemFormValues>({
    resolver: zodResolver(itemFormSchema),
    defaultValues: initialValues,
  })
  const { register, handleSubmit, formState } = form
  const mutation = useMutation({
    mutationFn: (request: CreateProjectRequest | CreateActivityRequest) => {
      if (itemMode === 'project') {
        return mode === 'create'
          ? projectsApi.createProject(request)
          : projectsApi.updateProject(itemId as number, request)
      }
      return mode === 'create'
        ? projectsApi.createActivity(request)
        : projectsApi.updateActivity(itemId as number, request)
    },
    onSuccess: (saved) => onSaved(saved, itemMode, mode),
    onError: (error) => setServerError(mapSaveError(error, t)),
  })
  const deleteMutation = useMutation({
    mutationFn: () =>
      itemMode === 'project'
        ? projectsApi.deleteProject(itemId as number)
        : projectsApi.deleteActivity(itemId as number),
    onSuccess: () => {
      if (item != null) onDeleted(item, itemMode)
    },
    onError: (error) => {
      setConfirmingDelete(false)
      setServerError(mapSaveError(error, t))
    },
  })

  const closeWithGuard = () => {
    if (formState.isDirty) setConfirmingClose(true)
    else onClose()
  }
  const submit = handleSubmit((values) => {
    setServerError(null)
    const request = {
      axisId: Number(values.axisId),
      name: values.name.trim(),
      status: values.status,
      visibility: canChangeVisibility ? values.visibility : initialValues.visibility,
    }
    mutation.mutate(request)
  })
  const changeItemMode = (value: string) => {
    if (isItemMode(value)) onItemModeChange(value)
  }

  const footer = (
    <>
      {mode === 'edit' && item != null && (
        <Button
          variant="ghost"
          className="seg-projects-editor__delete"
          iconLeft={<Trash2 size={16} />}
          onClick={() => setConfirmingDelete(true)}
        >
          {t(`${itemMode}Editor.delete.action`)}
        </Button>
      )}
      <Button variant="ghost" onClick={closeWithGuard}>
        {t('actions.cancel')}
      </Button>
      <Button onClick={() => void submit()} disabled={mutation.isPending}>
        {mutation.isPending
          ? t(mode === 'create' ? 'actions.creating' : 'actions.saving')
          : t(mode === 'create' ? 'actions.create' : 'actions.save')}
      </Button>
    </>
  )

  return (
    <>
      <Dialog
        scrollable
        width={800}
        title={title}
        description={description}
        onClose={closeWithGuard}
        closeLabel={t('actions.close')}
        footer={footer}
      >
        <form className="seg-projects-editor" onSubmit={(event) => void submit(event)}>
          {serverError != null && (
            <p className="seg-projects-editor__error" role="alert">
              {serverError}
            </p>
          )}
          {item != null && (
            <div className="seg-projects-editor__identifier">
              <span>{t(`${itemMode}Editor.identifier`)}</span>
              <strong>{item.identifier}</strong>
            </div>
          )}
          {mode === 'create' && (
            <Field label={t('itemEditor.fields.type')}>
              <SegmentedControl
                aria-label={t('itemEditor.fields.type')}
                name="itemMode"
                value={itemMode}
                onChange={(event) => changeItemMode(event.currentTarget.value)}
                options={itemModes.map((option) => ({
                  value: option,
                  label: t(`kind.${kindKey(option)}`),
                  icon:
                    option === 'project' ? (
                      <FolderTree size={15} />
                    ) : (
                      <ClipboardList size={15} />
                    ),
                  tone: option === 'project' ? 'accent' : 'neutral',
                }))}
              />
            </Field>
          )}
          <section className="seg-projects-editor__section">
            <h3>{t(`${itemMode}Editor.sections.general`)}</h3>
            <div className="seg-projects-editor__grid">
              <Input
                label={t(`${itemMode}Editor.fields.name`)}
                required
                error={formState.errors.name != null ? t('validation.nameRequired') : null}
                {...register('name')}
              />
              <Field
                label={t(`${itemMode}Editor.fields.axis`)}
                error={formState.errors.axisId != null ? t('validation.axisRequired') : null}
              >
                <Select
                  aria-label={t(`${itemMode}Editor.fields.axis`)}
                  options={axisOptions}
                  {...register('axisId')}
                />
              </Field>
              <Field label={t(`${itemMode}Editor.fields.status`)}>
                <Select
                  aria-label={t(`${itemMode}Editor.fields.status`)}
                  options={projectStatuses.map((status) => ({
                    value: status,
                    label: t(`status.${status}`),
                  }))}
                  {...register('status')}
                />
              </Field>
              <Field
                label={t(`${itemMode}Editor.fields.visibility`)}
                hint={!canChangeVisibility ? t('visibility.locked') : undefined}
              >
                <SegmentedControl
                  aria-label={t(`${itemMode}Editor.fields.visibility`)}
                  options={projectVisibilities.map((visibility) => ({
                    value: visibility,
                    label: t(`visibility.${visibility}`),
                    ...visibilityMeta[visibility],
                  }))}
                  disabled={!canChangeVisibility}
                  {...register('visibility')}
                />
              </Field>
            </div>
          </section>

          {itemMode === 'project' && mode === 'edit' && item != null && 'riskSummary' in item && (
            <section className="seg-projects-editor__section">
              <div className="seg-projects-editor__section-head">
                <h3>{t('projectEditor.sections.risks')}</h3>
                <Button
                  type="button"
                  size="sm"
                  variant="outline"
                  iconLeft={<ShieldAlert size={15} />}
                  onClick={onOpenRisks}
                >
                  {t('risks.open')}
                </Button>
              </div>
              <RiskSummary summary={item.riskSummary} />
            </section>
          )}

          {itemMode === 'project' && mode === 'edit' && item != null && (
            <section className="seg-projects-editor__section">
              <h3>{t('projectEditor.sections.attachments')}</h3>
              <p className="seg-projects-editor__hint">{t('attachments.hint')}</p>
              <ProjectAttachments projectId={item.id} />
            </section>
          )}

          <button type="submit" hidden />
        </form>
      </Dialog>

      {confirmingClose && (
        <Dialog
          width={420}
          title={t('unsaved.title')}
          description={t('unsaved.description')}
          onClose={() => setConfirmingClose(false)}
          closeLabel={t('actions.close')}
          footer={
            <>
              <Button variant="ghost" onClick={() => setConfirmingClose(false)}>
                {t('unsaved.stay')}
              </Button>
              <Button variant="danger" onClick={onClose}>
                {t('unsaved.leave')}
              </Button>
            </>
          }
        />
      )}

      {confirmingDelete && item != null && (
        <Dialog
          width={460}
          title={t(`${itemMode}Editor.delete.title`, { name: item.name })}
          description={t(`${itemMode}Editor.delete.description`)}
          onClose={() => setConfirmingDelete(false)}
          closeLabel={t('actions.close')}
          footer={
            <>
              <Button variant="ghost" onClick={() => setConfirmingDelete(false)}>
                {t('actions.cancel')}
              </Button>
              <Button
                variant="danger"
                onClick={() => deleteMutation.mutate()}
                disabled={deleteMutation.isPending}
              >
                {deleteMutation.isPending
                  ? t('actions.deleting')
                  : t(`${itemMode}Editor.delete.confirm`)}
              </Button>
            </>
          }
        />
      )}
    </>
  )
}

function Field({
  label,
  hint,
  error,
  children,
}: {
  label: string
  hint?: string
  error?: string | null
  children: ReactNode
}) {
  return (
    <label className="seg-projects-editor__field">
      <span className="seg-projects-editor__field-label">{label}</span>
      {children}
      {(error ?? hint) != null && (
        <span
          className={
            error != null
              ? 'seg-projects-editor__field-error'
              : 'seg-projects-editor__field-hint'
          }
        >
          {error ?? hint}
        </span>
      )}
    </label>
  )
}

interface ProjectRiskDialogProps {
  projectId: number
  onClose: () => void
  onChanged: (kind: 'riskSaved' | 'riskDeleted', name: string) => void
}

function ProjectRiskDialog({ projectId, onClose, onChanged }: ProjectRiskDialogProps) {
  const { t } = useTranslation('projects')
  const queryClient = useQueryClient()
  const [editingRisk, setEditingRisk] = useState<ProjectRisk | null>(null)
  const risksQuery = useQuery({
    queryKey: projectsKeys.projectRisks(projectId),
    queryFn: ({ signal }) => projectsApi.listRisks(projectId, signal),
  })

  const risks = risksQuery.data ?? []
  const summary = summarizeRisks(risks)
  const invalidate = async () => {
    await queryClient.invalidateQueries({ queryKey: projectsKeys.projectRisks(projectId) })
    await queryClient.invalidateQueries({ queryKey: projectsKeys.project(projectId) })
    await queryClient.invalidateQueries({ queryKey: projectsKeys.tree() })
  }

  return (
    <Dialog
      scrollable
      width={900}
      title={t('risks.title')}
      description={t('risks.description')}
      onClose={onClose}
      closeLabel={t('actions.close')}
      footer={<Button onClick={onClose}>{t('actions.close')}</Button>}
    >
      <div className="seg-projects-risks">
        <div className="seg-projects-risks__head">
          <RiskSummary summary={summary} />
          <Button
            size="sm"
            iconLeft={<Plus size={15} />}
            onClick={() =>
              setEditingRisk({
                id: 0,
                description: '',
                probability: 1,
                impact: 1,
                mitigation: 1,
                score: 1,
                band: 'Low',
              })
            }
          >
            {t('risks.new')}
          </Button>
        </div>

        {risksQuery.isPending ? (
          <div className="seg-projects__loading">
            <Spinner />
            <span>{t('risks.loading')}</span>
          </div>
        ) : risksQuery.isError ? (
          <p className="seg-projects-editor__error" role="alert">
            {t('risks.loadError')}
          </p>
        ) : risks.length === 0 ? (
          <p className="seg-projects__empty">{t('risks.empty')}</p>
        ) : (
          <ul className="seg-projects-risks__list">
            {risks.map((risk) => (
              <li key={risk.id} className="seg-projects-risks__item">
                <div>
                  <strong>{risk.description}</strong>
                  <span>
                    {t('risks.factors', {
                      probability: risk.probability,
                      impact: risk.impact,
                      mitigation: risk.mitigation,
                    })}
                  </span>
                </div>
                <RiskScore score={risk.score} band={risk.band} />
                <Button size="sm" variant="outline" onClick={() => setEditingRisk(risk)}>
                  {t('actions.edit')}
                </Button>
              </li>
            ))}
          </ul>
        )}

        {editingRisk != null && (
          <RiskEditor
            projectId={projectId}
            risk={editingRisk.id === 0 ? null : editingRisk}
            initialValues={{
              description: editingRisk.description,
              probability: String(editingRisk.probability),
              impact: String(editingRisk.impact),
              mitigation: String(editingRisk.mitigation),
            }}
            onClose={() => setEditingRisk(null)}
            onSaved={async (risk) => {
              setEditingRisk(null)
              await invalidate()
              onChanged('riskSaved', risk.description)
            }}
            onDeleted={async (risk) => {
              setEditingRisk(null)
              await invalidate()
              onChanged('riskDeleted', risk.description)
            }}
          />
        )}
      </div>
    </Dialog>
  )
}

function summarizeRisks(risks: ProjectRisk[]): ProjectRiskBandSummary {
  return risks.reduce(
    (summary, risk) => {
      if (risk.band === 'High') summary.high += 1
      else if (risk.band === 'Medium') summary.medium += 1
      else summary.low += 1
      return summary
    },
    { low: 0, medium: 0, high: 0 },
  )
}

const riskFormSchema = z.object({
  description: z.string().trim().min(1).max(1000),
  probability: z.string().regex(/^[1-5]$/),
  impact: z.string().regex(/^[1-5]$/),
  mitigation: z.string().regex(/^[1-5]$/),
})

type RiskFormValues = z.input<typeof riskFormSchema>

interface RiskEditorProps {
  projectId: number
  risk: ProjectRisk | null
  initialValues: RiskFormValues
  onClose: () => void
  onSaved: (risk: ProjectRisk) => Promise<void>
  onDeleted: (risk: ProjectRisk) => Promise<void>
}

function RiskEditor({
  projectId,
  risk,
  initialValues,
  onClose,
  onSaved,
  onDeleted,
}: RiskEditorProps) {
  const { t } = useTranslation('projects')
  const [serverError, setServerError] = useState<string | null>(null)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const form = useForm<RiskFormValues>({
    resolver: zodResolver(riskFormSchema),
    defaultValues: initialValues,
  })
  const values = useWatch({ control: form.control })
  const liveScore =
    Number(values.probability) * Number(values.impact) * Number(values.mitigation)
  const liveBand = bandForScore(Number.isFinite(liveScore) ? liveScore : 0)

  const mutation = useMutation({
    mutationFn: (request: ProjectRiskRequest) =>
      risk == null
        ? projectsApi.createRisk(projectId, request)
        : projectsApi.updateRisk(projectId, risk.id, request),
    onSuccess: (saved) => void onSaved(saved),
    onError: (error) => setServerError(mapSaveError(error, t)),
  })
  const deleteMutation = useMutation({
    mutationFn: () => projectsApi.deleteRisk(projectId, risk?.id as number),
    onSuccess: () => {
      if (risk != null) void onDeleted(risk)
    },
    onError: (error) => {
      setConfirmingDelete(false)
      setServerError(mapSaveError(error, t))
    },
  })

  const submit = form.handleSubmit((values) => {
    setServerError(null)
    const request = projectRiskRequestSchema.parse({
      description: values.description,
      probability: Number(values.probability),
      impact: Number(values.impact),
      mitigation: Number(values.mitigation),
    })
    mutation.mutate(request)
  })

  return (
    <>
      <Dialog
        width={640}
        title={risk == null ? t('riskEditor.createTitle') : t('riskEditor.editTitle')}
        description={t('riskEditor.description')}
        onClose={onClose}
        closeLabel={t('actions.close')}
        footer={
          <>
            {risk != null && (
              <Button
                variant="ghost"
                className="seg-projects-editor__delete"
                iconLeft={<Trash2 size={16} />}
                onClick={() => setConfirmingDelete(true)}
              >
                {t('riskEditor.delete.action')}
              </Button>
            )}
            <Button variant="ghost" onClick={onClose}>
              {t('actions.cancel')}
            </Button>
            <Button onClick={() => void submit()} disabled={mutation.isPending}>
              {mutation.isPending ? t('actions.saving') : t('actions.save')}
            </Button>
          </>
        }
      >
        <form className="seg-projects-editor" onSubmit={(event) => void submit(event)}>
          {serverError != null && (
            <p className="seg-projects-editor__error" role="alert">
              {serverError}
            </p>
          )}
          <Field
            label={t('riskEditor.fields.description')}
            error={
              form.formState.errors.description != null
                ? t('validation.descriptionRequired')
                : null
            }
          >
            <textarea
              className="seg-projects-editor__textarea"
              rows={4}
              aria-label={t('riskEditor.fields.description')}
              {...form.register('description')}
            />
          </Field>
          <div className="seg-projects-editor__grid">
            {(['probability', 'impact', 'mitigation'] as const).map((field) => (
              <Field
                key={field}
                label={t(`riskEditor.fields.${field}`)}
                error={
                  form.formState.errors[field] != null ? t('validation.factorRange') : null
                }
              >
                <Select
                  aria-label={t(`riskEditor.fields.${field}`)}
                  options={[1, 2, 3, 4, 5].map((value) => String(value))}
                  {...form.register(field)}
                />
              </Field>
            ))}
          </div>
          <div className="seg-projects-risks__live-score">
            <AlertTriangle size={18} aria-hidden="true" />
            <span>{t('riskEditor.liveScore')}</span>
            <RiskScore score={liveScore} band={liveBand} />
          </div>
          <button type="submit" hidden />
        </form>
      </Dialog>

      {confirmingDelete && risk != null && (
        <Dialog
          width={440}
          title={t('riskEditor.delete.title')}
          description={t('riskEditor.delete.description')}
          onClose={() => setConfirmingDelete(false)}
          closeLabel={t('actions.close')}
          footer={
            <>
              <Button variant="ghost" onClick={() => setConfirmingDelete(false)}>
                {t('actions.cancel')}
              </Button>
              <Button
                variant="danger"
                onClick={() => deleteMutation.mutate()}
                disabled={deleteMutation.isPending}
              >
                {deleteMutation.isPending
                  ? t('actions.deleting')
                  : t('riskEditor.delete.confirm')}
              </Button>
            </>
          }
        />
      )}
    </>
  )
}

function RiskScore({ score, band }: { score: number; band: RiskBand }) {
  const { t } = useTranslation('projects')
  return (
    <span className="seg-projects-risk-score" data-band={band}>
      {t('risks.score', { score, band: t(`risks.band.${band}`) })}
    </span>
  )
}

function bandForScore(score: number): RiskBand {
  if (score >= 100) return 'High'
  if (score >= 60) return 'Medium'
  return 'Low'
}

function mapSaveError(
  error: unknown,
  t: (key: string, options?: Record<string, unknown>) => string,
): string {
  if (isApiError(error)) {
    if (error.kind === 'not-found') return t('errors.notFound')
    if (error.kind === 'authorization-denied') return t('errors.forbidden')
    if (error.kind === 'validation') return t('errors.validation')
    if (['transient', 'unavailable'].includes(error.kind)) return t('errors.transient')
  }
  return t('errors.generic')
}
