import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  AlertTriangle,
  ClipboardList,
  ChevronDown,
  ChevronRight,
  CircleDot,
  GitBranch,
  FolderTree,
  Globe,
  Hash,
  Lock,
  Pencil,
  Plus,
  Settings2,
  ShieldAlert,
  Trash2,
} from 'lucide-react'
import { useState, type ReactNode } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'

import { isApiError } from '@/app/api/errors'
import {
  projectStatuses,
  projectVisibilities,
  projectsApi,
  projectsStructureApi,
  type ProjectStatus,
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
import {
  useProjectsDialogState,
  useProjectsSelectionState,
  type ProjectsSelectionState,
} from './projectsState'

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
const treeItemStatusClass: Record<ProjectStatus, string> = {
  Planning: 'planning',
  Active: 'active',
  Completed: 'completed',
  OnHold: 'on-hold',
  Cancelled: 'cancelled',
}

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
  const selectionState = useProjectsSelectionState()
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
      else
        void queryClient.invalidateQueries({ queryKey: projectsKeys.activity(item.id) })
    }
  }

  const handleSaved = (
    item: Project | Activity,
    itemMode: ItemMode,
    saveMode: SaveMode,
  ) => {
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
    if (itemMode === 'project') selectionState.selectProject(item.id)
    else selectionState.selectActivity(item.id)
    dialogState.closeDialog()
  }

  const handleDeleted = (item: Project | Activity, itemMode: ItemMode) => {
    invalidateTree()
    setToast({
      kind: itemMode === 'project' ? 'projectDeleted' : 'activityDeleted',
      name: item.name,
    })
    if (
      (itemMode === 'project' &&
        selectionState.selection.kind === 'project' &&
        selectionState.selection.projectId === item.id) ||
      (itemMode === 'activity' &&
        selectionState.selection.kind === 'activity' &&
        selectionState.selection.activityId === item.id)
    ) {
      selectionState.clearSelection()
    }
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
        <Badge tone="neutral">
          {t('page.programCount', { count: programsQuery.data?.length ?? 0 })}
        </Badge>
      </section>

      <section className="seg-projects__workspace">
        <div className="seg-projects__tree-card">
          <div className="seg-projects__tree-head">
            <div>
              <h2>{t('tree.title')}</h2>
              <p>{t('tree.description')}</p>
            </div>
            {programsQuery.isFetching && !programsQuery.isPending && (
              <Spinner size={18} />
            )}
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
                  selection={selectionState.selection}
                  onSelectProgram={selectionState.selectProgram}
                  onSelectAxis={selectionState.selectAxis}
                  onSelectProject={selectionState.selectProject}
                  onSelectActivity={selectionState.selectActivity}
                />
              ))}
            </ul>
          )}
        </div>
        <ProjectDetailsPane
          selection={selectionState.selection}
          programs={programsQuery.data ?? []}
          onSelectAxis={selectionState.selectAxis}
          onSelectProject={selectionState.selectProject}
          onSelectActivity={selectionState.selectActivity}
          onCreateItem={dialogState.openCreateItem}
          onEditProject={dialogState.openProject}
          onEditActivity={dialogState.openActivity}
          onOpenProjectRisks={dialogState.openProjectRisks}
        />
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
      {dialogState.dialog.mode === 'editProject' && (
        <ProjectItemDialog
          mode="edit"
          itemMode="project"
          itemId={dialogState.dialog.projectId}
          currentUserId={session?.userId ?? null}
          onClose={dialogState.closeDialog}
          onSaved={handleSaved}
          onDeleted={handleDeleted}
        />
      )}
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
      {dialogState.dialog.mode === 'projectRisks' && (
        <ProjectRiskDialog
          projectId={dialogState.dialog.projectId}
          onClose={dialogState.closeDialog}
          onChanged={(kind, name) => setToast({ kind, name })}
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
  selection: ProjectsSelectionState
  onSelectProgram: (programId: number) => void
  onSelectAxis: (axisId: number) => void
  onSelectProject: (projectId: number) => void
  onSelectActivity: (activityId: number) => void
}

function ProgramBranch({
  program,
  expanded,
  expandedAxes,
  onToggle,
  onToggleAxis,
  onCreateItem,
  selection,
  onSelectProgram,
  onSelectAxis,
  onSelectProject,
  onSelectActivity,
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
        onClick={() => {
          onSelectProgram(program.id)
          onToggle()
        }}
        aria-expanded={expanded}
        aria-current={
          selection.kind === 'program' && selection.programId === program.id
            ? 'true'
            : undefined
        }
      >
        {expanded ? <ChevronDown size={18} /> : <ChevronRight size={18} />}
        <FolderTree size={18} aria-hidden="true" />
        <span className="seg-projects-tree__code-pill">{program.code}</span>
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
                  selection={selection}
                  onSelectAxis={onSelectAxis}
                  onSelectProject={onSelectProject}
                  onSelectActivity={onSelectActivity}
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
  selection: ProjectsSelectionState
  onSelectAxis: (axisId: number) => void
  onSelectProject: (projectId: number) => void
  onSelectActivity: (activityId: number) => void
}

function AxisBranch({
  axis,
  expanded,
  onToggle,
  onCreateItem,
  selection,
  onSelectAxis,
  onSelectProject,
  onSelectActivity,
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
          onClick={() => {
            onSelectAxis(axis.id)
            onToggle()
          }}
          aria-expanded={expanded}
          aria-current={
            selection.kind === 'axis' && selection.axisId === axis.id
              ? 'true'
              : undefined
          }
        >
          {expanded ? <ChevronDown size={18} /> : <ChevronRight size={18} />}
          <GitBranch size={17} aria-hidden="true" />
          <span className="seg-projects-tree__code-pill">{axis.code}</span>
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
                  selected={
                    (item.kind === 'Project' &&
                      selection.kind === 'project' &&
                      selection.projectId === item.id) ||
                    (item.kind === 'Activity' &&
                      selection.kind === 'activity' &&
                      selection.activityId === item.id)
                  }
                  onSelectProject={onSelectProject}
                  onSelectActivity={onSelectActivity}
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
  selected: boolean
  onSelectProject: (projectId: number) => void
  onSelectActivity: (activityId: number) => void
}

function ProjectTreeItemRow({
  item,
  selected,
  onSelectProject,
  onSelectActivity,
}: ProjectTreeItemRowProps) {
  const { t } = useTranslation('projects')
  const isProject = item.kind === 'Project'
  const statusClass = treeItemStatusClass[item.status]

  return (
    <li className="seg-projects-tree__item">
      <button
        type="button"
        className="seg-projects-tree__item-open seg-projects-tree__leaf-row"
        onClick={() =>
          isProject ? onSelectProject(item.id) : onSelectActivity(item.id)
        }
        aria-label={t(isProject ? 'tree.selectProject' : 'tree.selectActivity', {
          identifier: item.identifier,
        })}
        aria-current={selected ? 'true' : undefined}
      >
        <span
          className={`seg-projects-tree__type-icon seg-projects-tree__type-icon--${statusClass}`}
          title={`${t(`kind.${item.kind}`)} · ${t(`status.${item.status}`)}`}
          aria-hidden="true"
        >
          {isProject ? <ClipboardList size={15} /> : <CircleDot size={15} />}
        </span>
        <span className="seg-projects-tree__code-pill">{item.identifier}</span>
        <span className="seg-projects-tree__leaf-name">{item.name}</span>
        <span className="seg-projects-tree__sr-status">
          {t(`kind.${item.kind}`)} · {t(`status.${item.status}`)}
        </span>
      </button>
    </li>
  )
}

function RiskSummary({ summary }: { summary: ProjectRiskBandSummary }) {
  const { t } = useTranslation('projects')
  return (
    <span
      className="seg-projects-risk-summary__pills"
      aria-label={t('risks.summaryLabel')}
    >
      <span data-band="Low">{t('risks.summary.low', { count: summary.low })}</span>
      <span data-band="Medium">
        {t('risks.summary.medium', { count: summary.medium })}
      </span>
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

interface ProjectDetailsPaneProps {
  selection: ProjectsSelectionState
  programs: ProgramNode[]
  onSelectAxis: (axisId: number) => void
  onSelectProject: (projectId: number) => void
  onSelectActivity: (activityId: number) => void
  onCreateItem: (axisId: number) => void
  onEditProject: (projectId: number) => void
  onEditActivity: (activityId: number) => void
  onOpenProjectRisks: (projectId: number) => void
}

function ProjectDetailsPane({
  selection,
  programs,
  onSelectAxis,
  onSelectProject,
  onSelectActivity,
  onCreateItem,
  onEditProject,
  onEditActivity,
  onOpenProjectRisks,
}: ProjectDetailsPaneProps) {
  const { t } = useTranslation('projects')
  const selectedProgramId = selection.kind === 'program' ? selection.programId : null
  const selectedAxisId = selection.kind === 'axis' ? selection.axisId : null
  const selectedProjectId = selection.kind === 'project' ? selection.projectId : null
  const selectedActivityId = selection.kind === 'activity' ? selection.activityId : null
  const programAxesQuery = useQuery({
    queryKey: projectsKeys.axes(selectedProgramId ?? 0),
    queryFn: ({ signal }) => projectsApi.axes(selectedProgramId as number, signal),
    enabled: selectedProgramId != null,
  })
  const axisItemsQuery = useQuery({
    queryKey: projectsKeys.items(selectedAxisId ?? 0),
    queryFn: ({ signal }) => projectsApi.items(selectedAxisId as number, signal),
    enabled: selectedAxisId != null,
  })
  const structureAxesQuery = useQuery({
    queryKey: projectsKeys.structureAxes(),
    queryFn: ({ signal }) => projectsStructureApi.listAxes(signal),
    enabled:
      selection.kind === 'axis' ||
      selection.kind === 'project' ||
      selection.kind === 'activity',
  })
  const projectQuery = useQuery({
    queryKey: projectsKeys.project(selectedProjectId ?? 0),
    queryFn: ({ signal }) =>
      projectsApi.getProject(selectedProjectId as number, signal),
    enabled: selectedProjectId != null,
  })
  const activityQuery = useQuery({
    queryKey: projectsKeys.activity(selectedActivityId ?? 0),
    queryFn: ({ signal }) =>
      projectsApi.getActivity(selectedActivityId as number, signal),
    enabled: selectedActivityId != null,
  })

  const axes = structureAxesQuery.data ?? []
  const selectedProgram =
    programs.find((program) => program.id === selectedProgramId) ?? null
  const selectedAxis = axes.find((axis) => axis.id === selectedAxisId) ?? null
  const selectedAxisProgram =
    selectedAxis == null
      ? null
      : (programs.find((program) => program.id === selectedAxis.programId) ?? null)
  const selectedItem = projectQuery.data ?? activityQuery.data ?? null
  const itemAxis =
    selectedItem == null
      ? null
      : (axes.find((axis) => axis.id === selectedItem.axisId) ?? null)
  const itemProgram =
    itemAxis == null
      ? null
      : (programs.find((program) => program.id === itemAxis.programId) ?? null)

  if (selection.kind === 'none') {
    return (
      <aside className="seg-projects-detail seg-projects-detail--empty">
        <FolderTree size={34} aria-hidden="true" />
        <h2>{t('details.empty.title')}</h2>
        <p>{t('details.empty.description')}</p>
      </aside>
    )
  }

  if (selection.kind === 'program') {
    if (selectedProgram == null)
      return <DetailsError message={t('details.program.notFound')} />
    return (
      <aside className="seg-projects-detail">
        <DetailHeader
          eyebrow={t('details.program.eyebrow')}
          title={selectedProgram.name}
          badges={[selectedProgram.code, t('details.structure.alwaysPublic')]}
        />
        <ConfigurationNote />
        <section className="seg-projects-detail__card">
          <DetailCardHeader
            title={t('details.program.axes')}
            count={programAxesQuery.data?.length ?? 0}
          />
          {programAxesQuery.isPending ? (
            <InlineState>{t('tree.loadingAxes')}</InlineState>
          ) : programAxesQuery.isError ? (
            <InlineState tone="error">{t('tree.axesLoadError')}</InlineState>
          ) : (
            <ChildList
              empty={t('tree.noAxes')}
              items={(programAxesQuery.data ?? []).map((axis) => ({
                id: axis.id,
                label: axis.name,
                meta: axis.code,
                icon: <ChevronRight size={15} />,
                onClick: () => onSelectAxis(axis.id),
              }))}
            />
          )}
        </section>
      </aside>
    )
  }

  if (selection.kind === 'axis') {
    if (structureAxesQuery.isPending)
      return <DetailsLoading message={t('details.loading')} />
    if (structureAxesQuery.isError)
      return <DetailsError message={t('details.loadError')} />
    if (selectedAxis == null)
      return <DetailsError message={t('details.axis.notFound')} />
    return (
      <aside className="seg-projects-detail">
        <DetailHeader
          eyebrow={t('details.axis.eyebrow')}
          title={selectedAxis.name}
          badges={[
            selectedAxis.code,
            selectedAxisProgram?.name ?? t('details.structure.unknownParent'),
          ]}
        />
        <ConfigurationNote />
        <section className="seg-projects-detail__card">
          <div className="seg-projects-detail__card-head">
            <DetailCardHeader
              title={t('details.axis.items')}
              count={axisItemsQuery.data?.length ?? 0}
            />
            <Button
              size="sm"
              variant="outline"
              iconLeft={<Plus size={15} />}
              onClick={() => onCreateItem(selectedAxis.id)}
            >
              {t('tree.newItem')}
            </Button>
          </div>
          {axisItemsQuery.isPending ? (
            <InlineState>{t('tree.loadingItems')}</InlineState>
          ) : axisItemsQuery.isError ? (
            <InlineState tone="error">{t('tree.itemsLoadError')}</InlineState>
          ) : (
            <ChildList
              empty={t('tree.noItems')}
              items={(axisItemsQuery.data ?? []).map((item) => ({
                id: `${item.kind}-${item.id}`,
                label: item.name,
                meta: item.identifier,
                icon:
                  item.kind === 'Project' ? (
                    <FolderTree size={15} />
                  ) : (
                    <CircleDot size={15} />
                  ),
                onClick: () =>
                  item.kind === 'Project'
                    ? onSelectProject(item.id)
                    : onSelectActivity(item.id),
              }))}
            />
          )}
        </section>
      </aside>
    )
  }

  if (selection.kind === 'project') {
    if (projectQuery.isPending || structureAxesQuery.isPending)
      return <DetailsLoading message={t('projectEditor.loading')} />
    if (projectQuery.isError || structureAxesQuery.isError)
      return <DetailsError message={t('projectEditor.loadError')} />
    if (projectQuery.data == null)
      return <DetailsError message={t('projectEditor.notFound')} />
    return (
      <ItemDetails
        item={projectQuery.data}
        itemMode="project"
        axis={itemAxis}
        program={itemProgram}
        onEdit={() => onEditProject(projectQuery.data.id)}
        onOpenProjectRisks={() => onOpenProjectRisks(projectQuery.data.id)}
      />
    )
  }

  if (activityQuery.isPending || structureAxesQuery.isPending)
    return <DetailsLoading message={t('activityEditor.loading')} />
  if (activityQuery.isError || structureAxesQuery.isError)
    return <DetailsError message={t('activityEditor.loadError')} />
  if (activityQuery.data == null)
    return <DetailsError message={t('activityEditor.notFound')} />
  return (
    <ItemDetails
      item={activityQuery.data}
      itemMode="activity"
      axis={itemAxis}
      program={itemProgram}
      onEdit={() => onEditActivity(activityQuery.data.id)}
    />
  )
}

function DetailHeader({
  eyebrow,
  title,
  badges,
}: {
  eyebrow: string
  title: string
  badges: string[]
}) {
  return (
    <header className="seg-projects-detail__head">
      <div className="armali-eyebrow">{eyebrow}</div>
      <h2>{title}</h2>
      <div className="seg-projects-detail__badges">
        {badges.map((badge) => (
          <Badge key={badge} tone="neutral">
            {badge}
          </Badge>
        ))}
      </div>
    </header>
  )
}

function DetailCardHeader({ title, count }: { title: string; count: number }) {
  return (
    <div>
      <h3>{title}</h3>
      <span className="seg-projects-detail__subtle">{count}</span>
    </div>
  )
}

function ConfigurationNote() {
  const { t } = useTranslation('projects')
  return (
    <section className="seg-projects-detail__card seg-projects-detail__note">
      <Settings2 size={18} aria-hidden="true" />
      <div>
        <strong>{t('details.structure.configurationTitle')}</strong>
        <p>{t('details.structure.configurationBody')}</p>
      </div>
    </section>
  )
}

function ChildList({
  empty,
  items,
}: {
  empty: string
  items: Array<{
    id: string | number
    label: string
    meta: string
    icon: ReactNode
    onClick: () => void
  }>
}) {
  if (items.length === 0) return <p className="seg-projects-detail__empty">{empty}</p>
  return (
    <ul className="seg-projects-detail__children">
      {items.map((item) => (
        <li key={item.id}>
          <button type="button" onClick={item.onClick}>
            {item.icon}
            <span>{item.label}</span>
            <small>{item.meta}</small>
          </button>
        </li>
      ))}
    </ul>
  )
}

function ItemDetails({
  item,
  itemMode,
  axis,
  program,
  onEdit,
  onOpenProjectRisks,
}: {
  item: Project | Activity
  itemMode: ItemMode
  axis: AxisNode | null
  program: ProgramNode | null
  onEdit: () => void
  onOpenProjectRisks?: () => void
}) {
  const { t, i18n } = useTranslation('projects')
  const project = itemMode === 'project' && 'riskSummary' in item ? item : null
  const isProject = project != null
  return (
    <aside className="seg-projects-detail">
      <header className="seg-projects-detail__head">
        <div className="seg-projects-detail__crumbs">
          <span>{program?.name ?? t('details.structure.unknownParent')}</span>
          <ChevronRight size={13} aria-hidden="true" />
          <span>{axis?.name ?? t('details.structure.unknownParent')}</span>
        </div>
        <div className="seg-projects-detail__title-row">
          <h2>{item.name}</h2>
          <Badge tone={isProject ? 'aqua' : 'neutral'}>
            {t(`kind.${isProject ? 'Project' : 'Activity'}`)}
          </Badge>
        </div>
        <div className="seg-projects-detail__identifier">
          <Hash size={15} aria-hidden="true" />
          <code>{item.identifier}</code>
        </div>
        <div className="seg-projects-detail__badges">
          <Badge tone="neutral" dot>
            {t(`status.${item.status}`)}
          </Badge>
          <Badge tone={item.visibility === 'Private' ? 'neutral' : 'success'}>
            {t(`visibility.${item.visibility}`)}
          </Badge>
        </div>
        <div className="seg-projects-detail__actions">
          {isProject && onOpenProjectRisks != null && (
            <Button
              variant="outline"
              iconLeft={<ShieldAlert size={16} />}
              onClick={onOpenProjectRisks}
            >
              {t('risks.open')}
            </Button>
          )}
          <Button iconLeft={<Pencil size={16} />} onClick={onEdit}>
            {t('actions.edit')}
          </Button>
        </div>
      </header>

      <section className="seg-projects-detail__card">
        <h3>{t('details.item.context')}</h3>
        <div className="seg-projects-detail__meta">
          <MetaCell
            label={t('details.item.number')}
            value={String(item.number).padStart(6, '0')}
          />
          <MetaCell label={t('details.item.axis')} value={axis?.name ?? '-'} />
          <MetaCell label={t('details.item.program')} value={program?.name ?? '-'} />
          <MetaCell label={t('details.item.owner')} value={item.createdByName} />
          <MetaCell
            label={t('details.item.created')}
            value={formatTimestamp(item.createdAt, i18n.language)}
          />
          <MetaCell
            label={t('details.item.updated')}
            value={
              item.updatedAt == null
                ? t('details.item.neverUpdated')
                : formatTimestamp(item.updatedAt, i18n.language)
            }
          />
        </div>
      </section>

      {isProject ? (
        <>
          <section className="seg-projects-detail__card">
            <div className="seg-projects-detail__card-head">
              <div>
                <h3>{t('projectEditor.sections.risks')}</h3>
                <p>{t('risks.description')}</p>
              </div>
              <Button
                size="sm"
                variant="outline"
                iconLeft={<ShieldAlert size={15} />}
                onClick={onOpenProjectRisks}
              >
                {t('risks.open')}
              </Button>
            </div>
            <RiskSummary summary={project.riskSummary} />
          </section>
          <section className="seg-projects-detail__card">
            <h3>{t('projectEditor.sections.attachments')}</h3>
            <p>{t('attachments.hint')}</p>
            <ProjectAttachments projectId={project.id} />
          </section>
        </>
      ) : (
        <section className="seg-projects-detail__card seg-projects-detail__note">
          <CircleDot size={18} aria-hidden="true" />
          <div>
            <strong>{t('details.activity.title')}</strong>
            <p>{t('details.activity.description')}</p>
          </div>
        </section>
      )}
    </aside>
  )
}

function MetaCell({ label, value }: { label: string; value: string }) {
  return (
    <div className="seg-projects-detail__meta-cell">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  )
}

function DetailsLoading({ message }: { message: string }) {
  return (
    <aside className="seg-projects-detail">
      <div className="seg-projects__loading">
        <Spinner />
        <span>{message}</span>
      </div>
    </aside>
  )
}

function DetailsError({ message }: { message: string }) {
  return (
    <aside className="seg-projects-detail">
      <p className="seg-projects__error" role="alert">
        {message}
      </p>
    </aside>
  )
}

function formatTimestamp(value: string, language: string): string {
  return new Intl.DateTimeFormat(language, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
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
}: ProjectItemDialogProps) {
  const { t } = useTranslation('projects')
  const [createItemMode, setCreateItemMode] = useState<ItemMode>(itemMode)

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
                error={
                  formState.errors.name != null ? t('validation.nameRequired') : null
                }
                {...register('name')}
              />
              <Field
                label={t(`${itemMode}Editor.fields.axis`)}
                error={
                  formState.errors.axisId != null ? t('validation.axisRequired') : null
                }
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
    await queryClient.invalidateQueries({
      queryKey: projectsKeys.projectRisks(projectId),
    })
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
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() => setEditingRisk(risk)}
                >
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
                  form.formState.errors[field] != null
                    ? t('validation.factorRange')
                    : null
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
