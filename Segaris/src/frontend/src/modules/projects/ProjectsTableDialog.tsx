import { keepPreviousData, useQuery } from '@tanstack/react-query'
import {
  CircleDot,
  ClipboardList,
  Globe,
  Lock,
  PanelRightOpen,
  Pencil,
  RotateCcw,
  Search,
  ShieldAlert,
} from 'lucide-react'
import { useState, type MouseEvent, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

import {
  projectItemKinds,
  projectStatuses,
  projectsApi,
  projectsStructureApi,
  type AxisNode,
  type ProgramNode,
  type ProjectItemKind,
  type ProjectListFilters,
  type ProjectListItem,
  type ProjectRiskBandSummary,
  type ProjectStatus,
} from '@/app/api/projects'
import {
  Badge,
  Button,
  Dialog,
  IconButton,
  Input,
  Select,
  Spinner,
} from '@/components/ui'
import type { BadgeTone } from '@/components/ui'

import { projectsKeys } from './contracts'

/** The table opens on active work from any program or axis. */
export const defaultProjectListFilters: ProjectListFilters = {
  programId: null,
  axisId: null,
  status: 'Active',
  kind: null,
}

const statusTone: Record<ProjectStatus, BadgeTone> = {
  Planning: 'azure',
  Active: 'aqua',
  Completed: 'success',
  OnHold: 'gold',
  Cancelled: 'danger',
}

interface ProjectsTableDialogProps {
  programs: ProgramNode[]
  onClose: () => void
  onEditProject: (projectId: number) => void
  onEditActivity: (activityId: number) => void
  onOpenProjectRisks: (projectId: number) => void
  onShowDetails: (item: ProjectListItem) => void
}

/**
 * Secondary, filterable table of every accessible project and activity. The tree stays
 * the primary navigation; this view only lists work across programs and axes and hands
 * rows off to the existing edit, risk, and detail workflows.
 */
export function ProjectsTableDialog({
  programs,
  onClose,
  onEditProject,
  onEditActivity,
  onOpenProjectRisks,
  onShowDetails,
}: ProjectsTableDialogProps) {
  const { t } = useTranslation('projects')
  const [filters, setFilters] = useState<ProjectListFilters>(defaultProjectListFilters)
  const [search, setSearch] = useState('')
  const axesQuery = useQuery({
    queryKey: projectsKeys.structureAxes(),
    queryFn: ({ signal }) => projectsStructureApi.listAxes(signal),
  })
  const itemsQuery = useQuery({
    queryKey: projectsKeys.list(filters),
    queryFn: ({ signal }) => projectsApi.listItems(filters, signal),
    placeholderData: keepPreviousData,
  })

  const axes = axesQuery.data ?? []
  const axisOptions = axisFilterOptions(axes, programs, filters.programId)
  const needle = search.trim().toLocaleLowerCase()
  const rows = (itemsQuery.data ?? []).filter(
    (item) =>
      needle.length === 0 ||
      item.name.toLocaleLowerCase().includes(needle) ||
      item.identifier.toLocaleLowerCase().includes(needle),
  )
  const filtersAreDefault =
    search.length === 0 &&
    filters.programId === defaultProjectListFilters.programId &&
    filters.axisId === defaultProjectListFilters.axisId &&
    filters.status === defaultProjectListFilters.status &&
    filters.kind === defaultProjectListFilters.kind

  const changeProgram = (value: string) => {
    const programId = idOrNull(value)
    setFilters((current) => {
      const axis = axes.find((candidate) => candidate.id === current.axisId)
      const keepAxis = programId == null || axis == null || axis.programId === programId
      return { ...current, programId, axisId: keepAxis ? current.axisId : null }
    })
  }
  const changeAxis = (value: string) =>
    setFilters((current) => ({ ...current, axisId: idOrNull(value) }))
  const changeStatus = (value: string) =>
    setFilters((current) => ({ ...current, status: statusOrNull(value) }))
  const changeKind = (value: string) =>
    setFilters((current) => ({ ...current, kind: kindOrNull(value) }))
  const resetFilters = () => {
    setFilters(defaultProjectListFilters)
    setSearch('')
  }
  const edit = (item: ProjectListItem) =>
    item.kind === 'Project' ? onEditProject(item.id) : onEditActivity(item.id)

  return (
    <Dialog
      scrollable
      width={1200}
      title={t('table.title')}
      description={t('table.description')}
      onClose={onClose}
      closeLabel={t('actions.close')}
      footer={<Button onClick={onClose}>{t('actions.close')}</Button>}
    >
      <div className="seg-projects-table">
        <div
          className="seg-projects-table__filters"
          role="group"
          aria-label={t('table.filters.label')}
        >
          <Input
            className="seg-projects-table__search"
            label={t('table.filters.search')}
            placeholder={t('table.filters.searchPlaceholder')}
            iconLeft={<Search size={15} />}
            value={search}
            onChange={(event) => setSearch(event.currentTarget.value)}
          />
          <FilterField label={t('table.filters.program')}>
            <Select
              aria-label={t('table.filters.program')}
              value={filters.programId == null ? '' : String(filters.programId)}
              onChange={(event) => changeProgram(event.currentTarget.value)}
              options={[
                { value: '', label: t('table.filters.allPrograms') },
                ...programs.map((program) => ({
                  value: String(program.id),
                  label: `${program.code} - ${program.name}`,
                })),
              ]}
            />
          </FilterField>
          <FilterField label={t('table.filters.axis')}>
            <Select
              aria-label={t('table.filters.axis')}
              value={filters.axisId == null ? '' : String(filters.axisId)}
              onChange={(event) => changeAxis(event.currentTarget.value)}
              options={[
                { value: '', label: t('table.filters.allAxes') },
                ...axisOptions,
              ]}
            />
          </FilterField>
          <FilterField label={t('table.filters.status')}>
            <Select
              aria-label={t('table.filters.status')}
              value={filters.status ?? ''}
              onChange={(event) => changeStatus(event.currentTarget.value)}
              options={[
                { value: '', label: t('table.filters.allStatuses') },
                ...projectStatuses.map((status) => ({
                  value: status,
                  label: t(`status.${status}`),
                })),
              ]}
            />
          </FilterField>
          <FilterField label={t('table.filters.kind')}>
            <Select
              aria-label={t('table.filters.kind')}
              value={filters.kind ?? ''}
              onChange={(event) => changeKind(event.currentTarget.value)}
              options={[
                { value: '', label: t('table.filters.allKinds') },
                ...projectItemKinds.map((kind) => ({
                  value: kind,
                  label: t(`kind.${kind}`),
                })),
              ]}
            />
          </FilterField>
        </div>

        <div className="seg-projects-table__summary">
          <span>
            {itemsQuery.isPending ? '' : t('table.count', { count: rows.length })}
          </span>
          <div className="seg-projects-table__summary-actions">
            {itemsQuery.isFetching && !itemsQuery.isPending && <Spinner size={16} />}
            <Button
              size="sm"
              variant="ghost"
              iconLeft={<RotateCcw size={14} />}
              onClick={resetFilters}
              disabled={filtersAreDefault}
            >
              {t('table.filters.reset')}
            </Button>
          </div>
        </div>

        {itemsQuery.isPending ? (
          <div className="seg-projects__loading">
            <Spinner />
            <span>{t('table.loading')}</span>
          </div>
        ) : itemsQuery.isError ? (
          <p className="seg-projects__error" role="alert">
            {t('table.loadError')}
          </p>
        ) : rows.length === 0 ? (
          <p className="seg-projects__empty">{t('table.empty')}</p>
        ) : (
          <div className="seg-projects-table__wrap" aria-busy={itemsQuery.isFetching}>
            <table className="seg-projects-table__table" aria-label={t('table.label')}>
              <thead>
                <tr>
                  <th>{t('table.columns.item')}</th>
                  <th>{t('table.columns.kind')}</th>
                  <th>{t('table.columns.program')}</th>
                  <th>{t('table.columns.axis')}</th>
                  <th>{t('table.columns.status')}</th>
                  <th>{t('table.columns.visibility')}</th>
                  <th>{t('table.columns.risks')}</th>
                  <th>
                    <span className="seg-projects-tree__sr-status">
                      {t('table.columns.actions')}
                    </span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {rows.map((item) => (
                  <ProjectsTableRow
                    key={`${item.kind}-${item.id}`}
                    item={item}
                    onEdit={() => edit(item)}
                    onOpenRisks={() => onOpenProjectRisks(item.id)}
                    onShowDetails={() => onShowDetails(item)}
                  />
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </Dialog>
  )
}

function ProjectsTableRow({
  item,
  onEdit,
  onOpenRisks,
  onShowDetails,
}: {
  item: ProjectListItem
  onEdit: () => void
  onOpenRisks: () => void
  onShowDetails: () => void
}) {
  const { t } = useTranslation('projects')
  const isProject = item.kind === 'Project'

  return (
    <tr className="seg-projects-table__row" onClick={onEdit}>
      <td className="seg-projects-table__item">
        <button
          type="button"
          className="seg-projects-table__open"
          onClick={withoutRowClick(onEdit)}
          aria-label={t('table.actions.edit', { identifier: item.identifier })}
        >
          {item.name}
        </button>
        <code>{item.identifier}</code>
      </td>
      <td>
        <span className="seg-projects-table__kind">
          {isProject ? (
            <ClipboardList size={15} aria-hidden="true" />
          ) : (
            <CircleDot size={15} aria-hidden="true" />
          )}
          {t(`kind.${item.kind}`)}
        </span>
      </td>
      <td>
        <NodeCell code={item.programCode} name={item.programName} />
      </td>
      <td>
        <NodeCell code={item.axisCode} name={item.axisName} />
      </td>
      <td>
        <Badge tone={statusTone[item.status]} dot>
          {t(`status.${item.status}`)}
        </Badge>
      </td>
      <td>
        <span className="seg-projects-table__visibility">
          {item.visibility === 'Private' ? (
            <Lock size={14} aria-hidden="true" />
          ) : (
            <Globe size={14} aria-hidden="true" />
          )}
          {t(`visibility.${item.visibility}`)}
        </span>
      </td>
      <td>
        {item.riskSummary == null ? (
          <span className="seg-projects-table__muted">-</span>
        ) : (
          <CompactRiskSummary summary={item.riskSummary} />
        )}
      </td>
      <td>
        <div className="seg-projects-table__actions">
          {isProject && (
            <IconButton
              size="sm"
              variant="bare"
              icon={<ShieldAlert size={16} />}
              label={t('risks.openFor', { identifier: item.identifier })}
              onClick={withoutRowClick(onOpenRisks)}
            />
          )}
          <IconButton
            size="sm"
            variant="bare"
            icon={<PanelRightOpen size={16} />}
            label={t('table.actions.details', { identifier: item.identifier })}
            onClick={withoutRowClick(onShowDetails)}
          />
          <IconButton
            size="sm"
            variant="bare"
            icon={<Pencil size={16} />}
            label={t('table.actions.edit', { identifier: item.identifier })}
            onClick={withoutRowClick(onEdit)}
          />
        </div>
      </td>
    </tr>
  )
}

/** Row-level buttons run their own action without also triggering the row edit. */
function withoutRowClick(action: () => void) {
  return (event: MouseEvent<HTMLButtonElement>) => {
    event.stopPropagation()
    action()
  }
}

function NodeCell({ code, name }: { code: string; name: string }) {
  return (
    <span className="seg-projects-table__node">
      <span className="seg-projects-tree__code-pill">{code}</span>
      <span>{name}</span>
    </span>
  )
}

function CompactRiskSummary({ summary }: { summary: ProjectRiskBandSummary }) {
  const { t } = useTranslation('projects')
  const bands = [
    { band: 'High', count: summary.high, key: 'risks.summary.high' },
    { band: 'Medium', count: summary.medium, key: 'risks.summary.medium' },
    { band: 'Low', count: summary.low, key: 'risks.summary.low' },
  ].filter((entry) => entry.count > 0)

  if (bands.length === 0)
    return <span className="seg-projects-table__muted">{t('table.noRisks')}</span>
  return (
    <span
      className="seg-projects-risk-summary__pills"
      aria-label={t('risks.summaryLabel')}
    >
      {bands.map((entry) => (
        <span key={entry.band} data-band={entry.band}>
          {t(entry.key, { count: entry.count })}
        </span>
      ))}
    </span>
  )
}

function FilterField({ label, children }: { label: string; children: ReactNode }) {
  return (
    <label className="arm-field seg-projects-table__filter">
      <span className="arm-field__label">{label}</span>
      {children}
    </label>
  )
}

function axisFilterOptions(
  axes: AxisNode[],
  programs: ProgramNode[],
  programId: number | null,
) {
  const programCodes = new Map(programs.map((program) => [program.id, program.code]))
  return axes
    .filter((axis) => programId == null || axis.programId === programId)
    .map((axis) => ({
      value: String(axis.id),
      label:
        programId == null
          ? `${programCodes.get(axis.programId) ?? '----'}${axis.code} - ${axis.name}`
          : `${axis.code} - ${axis.name}`,
    }))
}

function idOrNull(value: string): number | null {
  const parsed = Number.parseInt(value, 10)
  return Number.isFinite(parsed) && parsed > 0 ? parsed : null
}

function statusOrNull(value: string): ProjectStatus | null {
  return projectStatuses.find((status) => status === value) ?? null
}

function kindOrNull(value: string): ProjectItemKind | null {
  return projectItemKinds.find((kind) => kind === value) ?? null
}
