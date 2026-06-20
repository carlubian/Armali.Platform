import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Pencil, Plus, Trash2 } from 'lucide-react'
import { useMemo, useRef, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'

import { isApiError } from '@/app/api/errors'
import {
  projectsStructureApi,
  type AxisNode,
  type ProgramNode,
} from '@/app/api/projects'
import { ServiceUnavailable } from '@/components/feedback/SystemScreens'
import { Button, Dialog, Input, Select, Spinner, Tabs } from '@/components/ui'
import { projectsKeys } from '@/modules/projects/contracts'

import { invalidateProjectsStructure } from './queries'

export type ProjectsStructureToastKind =
  | 'created'
  | 'updated'
  | 'removed'
  | 'reassigned'

interface ProjectsStructureSectionProps {
  onToast: (kind: ProjectsStructureToastKind, name: string) => void
}

type StructureTab = 'programs' | 'axes'
type StructureKind = 'program' | 'axis'
type FormState =
  | { mode: 'create'; kind: StructureKind }
  | { mode: 'edit'; kind: 'program'; row: ProgramNode }
  | { mode: 'edit'; kind: 'axis'; row: AxisNode }
type DeleteState =
  | { kind: 'program'; row: ProgramNode }
  | { kind: 'axis'; row: AxisNode }

type FormValues = {
  name: string
  code: string
  programId: string
}

const codePattern = /^[A-Z]{4}$/

const byCode = <T extends { code: string }>(left: T, right: T) =>
  left.code.localeCompare(right.code, 'en-GB')

export function ProjectsStructureSection({ onToast }: ProjectsStructureSectionProps) {
  const { t } = useTranslation('configuration')
  const [tab, setTab] = useState<StructureTab>('programs')
  const [formState, setFormState] = useState<FormState | null>(null)
  const [deleteState, setDeleteState] = useState<DeleteState | null>(null)

  const programsQuery = useQuery({
    queryKey: projectsKeys.structurePrograms(),
    queryFn: ({ signal }) => projectsStructureApi.listPrograms(signal),
  })
  const axesQuery = useQuery({
    queryKey: projectsKeys.structureAxes(),
    queryFn: ({ signal }) => projectsStructureApi.listAxes(signal),
  })

  const programs = useMemo(
    () => [...(programsQuery.data ?? [])].sort(byCode),
    [programsQuery.data],
  )
  const axes = useMemo(() => [...(axesQuery.data ?? [])].sort(byCode), [axesQuery.data])
  const programNames = useMemo(
    () =>
      new Map(
        programs.map((program) => [program.id, `${program.code} - ${program.name}`]),
      ),
    [programs],
  )

  const loadError = programsQuery.isError || axesQuery.isError
  const transientError = [programsQuery.error, axesQuery.error].some(
    (error) => isApiError(error) && ['unavailable', 'transient'].includes(error.kind),
  )
  if (transientError) {
    return (
      <ServiceUnavailable
        onRetry={() => {
          void programsQuery.refetch()
          void axesQuery.refetch()
        }}
      />
    )
  }

  const rows = tab === 'programs' ? programs : axes
  const loading =
    tab === 'programs'
      ? programsQuery.isPending
      : axesQuery.isPending || programsQuery.isPending

  return (
    <section className="seg-catalog" aria-label={t(`projectsStructure.${tab}.title`)}>
      <Tabs
        aria-label={t('projectsStructure.tabsLabel')}
        value={tab}
        onChange={(next) => setTab(next as StructureTab)}
        tabs={[
          { value: 'programs', label: t('projectsStructure.programsTab') },
          { value: 'axes', label: t('projectsStructure.axesTab') },
        ]}
      />

      <header className="seg-catalog__head seg-catalog__head--tabbed">
        <div>
          <h2>{t(`projectsStructure.${tab}.title`)}</h2>
          <p>{t(`projectsStructure.${tab}.description`)}</p>
        </div>
        <Button
          iconLeft={<Plus size={16} />}
          onClick={() =>
            setFormState({
              mode: 'create',
              kind: tab === 'programs' ? 'program' : 'axis',
            })
          }
        >
          {t(`projectsStructure.${tab}.addAction`)}
        </Button>
      </header>

      {loading ? (
        <div className="seg-catalog__loading">
          <Spinner />
        </div>
      ) : loadError ? (
        <div className="seg-catalog__error" role="alert">
          <p>{t('table.loadError')}</p>
          <Button
            variant="outline"
            size="sm"
            onClick={() => {
              void programsQuery.refetch()
              void axesQuery.refetch()
            }}
          >
            {t('table.retry')}
          </Button>
        </div>
      ) : rows.length === 0 ? (
        <p className="seg-catalog__empty">{t(`projectsStructure.${tab}.empty`)}</p>
      ) : tab === 'programs' ? (
        <ProjectsStructureTable
          tab="programs"
          rows={programs}
          programNames={programNames}
          onEdit={(row) => setFormState({ mode: 'edit', kind: 'program', row })}
          onDelete={(row) => setDeleteState({ kind: 'program', row })}
        />
      ) : (
        <ProjectsStructureTable
          tab="axes"
          rows={axes}
          programNames={programNames}
          onEdit={(row) => setFormState({ mode: 'edit', kind: 'axis', row })}
          onDelete={(row) => setDeleteState({ kind: 'axis', row })}
        />
      )}

      {formState != null && (
        <ProjectsStructureFormDialog
          state={formState}
          programs={programs}
          onClose={() => setFormState(null)}
          onSaved={(row, mode) => {
            setFormState(null)
            onToast(mode === 'create' ? 'created' : 'updated', row.name)
          }}
        />
      )}

      {deleteState != null && (
        <ProjectsStructureDeleteDialog
          state={deleteState}
          programs={programs}
          axes={axes}
          programNames={programNames}
          onClose={() => setDeleteState(null)}
          onDeleted={(kind, row, reassigned) => {
            setDeleteState(null)
            onToast(reassigned ? 'reassigned' : 'removed', row.name)
            if (kind === 'program') void axesQuery.refetch()
          }}
        />
      )}
    </section>
  )
}

type ProjectsStructureTableProps =
  | {
      tab: 'programs'
      rows: readonly ProgramNode[]
      programNames: ReadonlyMap<number, string>
      onEdit: (row: ProgramNode) => void
      onDelete: (row: ProgramNode) => void
    }
  | {
      tab: 'axes'
      rows: readonly AxisNode[]
      programNames: ReadonlyMap<number, string>
      onEdit: (row: AxisNode) => void
      onDelete: (row: AxisNode) => void
    }

function ProjectsStructureTable(props: ProjectsStructureTableProps) {
  if (props.tab === 'programs') return <ProgramStructureTable {...props} />
  return <AxisStructureTable {...props} />
}

function ProgramStructureTable({
  rows,
  onEdit,
  onDelete,
}: Extract<ProjectsStructureTableProps, { tab: 'programs' }>) {
  const { t } = useTranslation('configuration')

  return (
    <div className="seg-catalog__table-wrap">
      <table className="seg-catalog__table">
        <thead>
          <tr>
            <th scope="col" className="seg-catalog__col-code">
              {t('table.columns.code')}
            </th>
            <th scope="col">{t('table.columns.name')}</th>
            <th scope="col" className="seg-catalog__col-actions">
              {t('table.columns.actions')}
            </th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={row.id}>
              <td className="seg-catalog__code">{row.code}</td>
              <td className="seg-catalog__name">{row.name}</td>
              <td className="seg-catalog__col-actions">
                <div className="seg-catalog__row-actions">
                  <button
                    type="button"
                    className="seg-catalog__icon"
                    aria-label={t('projectsStructure.table.edit', { name: row.name })}
                    onClick={() => onEdit(row)}
                  >
                    <Pencil size={16} aria-hidden="true" />
                  </button>
                  <button
                    type="button"
                    className="seg-catalog__icon seg-catalog__icon--danger"
                    aria-label={t('projectsStructure.table.delete', { name: row.name })}
                    onClick={() => onDelete(row)}
                  >
                    <Trash2 size={16} aria-hidden="true" />
                  </button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function AxisStructureTable({
  rows,
  programNames,
  onEdit,
  onDelete,
}: Extract<ProjectsStructureTableProps, { tab: 'axes' }>) {
  const { t } = useTranslation('configuration')

  return (
    <div className="seg-catalog__table-wrap">
      <table className="seg-catalog__table">
        <thead>
          <tr>
            <th scope="col" className="seg-catalog__col-code">
              {t('table.columns.code')}
            </th>
            <th scope="col">{t('table.columns.name')}</th>
            <th scope="col">{t('projectsStructure.table.parentProgram')}</th>
            <th scope="col" className="seg-catalog__col-actions">
              {t('table.columns.actions')}
            </th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={row.id}>
              <td className="seg-catalog__code">{row.code}</td>
              <td className="seg-catalog__name">{row.name}</td>
              <td className="seg-catalog__muted">
                {programNames.get(row.programId) ?? '—'}
              </td>
              <td className="seg-catalog__col-actions">
                <div className="seg-catalog__row-actions">
                  <button
                    type="button"
                    className="seg-catalog__icon"
                    aria-label={t('projectsStructure.table.edit', { name: row.name })}
                    onClick={() => onEdit(row)}
                  >
                    <Pencil size={16} aria-hidden="true" />
                  </button>
                  <button
                    type="button"
                    className="seg-catalog__icon seg-catalog__icon--danger"
                    aria-label={t('projectsStructure.table.delete', { name: row.name })}
                    onClick={() => onDelete(row)}
                  >
                    <Trash2 size={16} aria-hidden="true" />
                  </button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

interface ProjectsStructureFormDialogProps {
  state: FormState
  programs: readonly ProgramNode[]
  onClose: () => void
  onSaved: (row: ProgramNode | AxisNode, mode: 'create' | 'edit') => void
}

function ProjectsStructureFormDialog({
  state,
  programs,
  onClose,
  onSaved,
}: ProjectsStructureFormDialogProps) {
  const { t } = useTranslation('configuration')
  const queryClient = useQueryClient()
  const [confirmingClose, setConfirmingClose] = useState(false)
  const [serverError, setServerError] = useState<string | null>(null)
  const editedRef = useRef(false)
  const labels =
    `projectsStructure.${state.kind === 'program' ? 'programs' : 'axes'}` as const

  const schema = z.object({
    name: z
      .string()
      .trim()
      .min(1, t('projectsStructure.form.nameRequired'))
      .max(200, t('projectsStructure.form.nameTooLong')),
    code: z
      .string()
      .trim()
      .transform((value) => value.toUpperCase())
      .pipe(z.string().regex(codePattern, t('projectsStructure.form.codeInvalid'))),
    programId:
      state.kind === 'axis'
        ? z.string().min(1, t('projectsStructure.form.programRequired'))
        : z.string(),
  })

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      name: state.mode === 'edit' ? state.row.name : '',
      code: state.mode === 'edit' ? state.row.code : '',
      programId:
        state.kind === 'axis' && state.mode === 'edit'
          ? String(state.row.programId)
          : state.kind === 'axis' && programs.length === 1
            ? String(programs[0].id)
            : '',
    },
  })
  const { register, handleSubmit, formState, setError } = form

  const mutation = useMutation({
    mutationFn: (values: FormValues) => {
      const name = values.name.trim()
      const code = values.code.trim().toUpperCase()
      if (state.kind === 'program') {
        return state.mode === 'create'
          ? projectsStructureApi.createProgram({ name, code })
          : projectsStructureApi.updateProgram(state.row.id, { name, code })
      }
      const body = { name, code, programId: Number(values.programId) }
      return state.mode === 'create'
        ? projectsStructureApi.createAxis(body)
        : projectsStructureApi.updateAxis(state.row.id, body)
    },
    onSuccess: async (saved) => {
      await invalidateProjectsStructure(queryClient)
      onSaved(saved, state.mode)
    },
    onError: (error) => applyServerError(error),
  })

  const applyServerError = (error: unknown) => {
    if (isApiError(error)) {
      const code = error.problem?.code ?? ''
      if (code.endsWith('duplicate_name')) {
        setError('name', { message: t('projectsStructure.form.duplicateName') })
        return
      }
      if (code.endsWith('duplicate_code')) {
        setError('code', { message: t('projectsStructure.form.duplicateCode') })
        return
      }
      if (code.endsWith('invalid_code')) {
        setError('code', { message: t('projectsStructure.form.codeInvalid') })
        return
      }
      if (code.endsWith('invalid_program')) {
        setError('programId', { message: t('projectsStructure.form.programRequired') })
        return
      }
    }
    setServerError(t('projectsStructure.form.genericError'))
  }

  const submit = handleSubmit((values) => {
    setServerError(null)
    mutation.mutate(values)
  })
  const requestClose = () => {
    if (editedRef.current && !mutation.isSuccess) {
      setConfirmingClose(true)
      return
    }
    onClose()
  }

  return (
    <>
      <Dialog
        width={460}
        title={t(`${labels}.${state.mode === 'create' ? 'createTitle' : 'editTitle'}`)}
        onClose={requestClose}
        closeLabel={t('form.close')}
        footer={
          <>
            <Button
              variant="ghost"
              onClick={requestClose}
              disabled={mutation.isPending}
            >
              {t('form.cancel')}
            </Button>
            <Button
              type="submit"
              form="seg-projects-structure-form"
              variant="primary"
              disabled={mutation.isPending}
            >
              {state.mode === 'create'
                ? mutation.isPending
                  ? t('form.creating')
                  : t('form.create')
                : mutation.isPending
                  ? t('form.saving')
                  : t('form.save')}
            </Button>
          </>
        }
      >
        <form
          id="seg-projects-structure-form"
          className="seg-catalog__form"
          noValidate
          onSubmit={(event) => void submit(event)}
          onChange={() => {
            editedRef.current = true
          }}
        >
          {serverError != null && (
            <p className="seg-catalog__form-error" role="alert">
              {serverError}
            </p>
          )}
          <Input
            label={t(`${labels}.nameLabel`)}
            placeholder={t(`${labels}.namePlaceholder`)}
            autoComplete="off"
            required
            error={formState.errors.name?.message}
            {...register('name')}
          />
          <Input
            label={t(`${labels}.codeLabel`)}
            placeholder={t(`${labels}.codePlaceholder`)}
            autoComplete="off"
            maxLength={4}
            required
            error={formState.errors.code?.message}
            {...register('code')}
          />
          {state.kind === 'axis' && (
            <div className="seg-catalog__field">
              <label className="seg-catalog__field-control">
                <span className="seg-catalog__field-label">
                  {t('projectsStructure.axes.programLabel')}
                </span>
                <Select
                  aria-invalid={formState.errors.programId != null}
                  {...register('programId')}
                  options={[
                    {
                      value: '',
                      label: t('projectsStructure.axes.programPlaceholder'),
                    },
                    ...programs.map((program) => ({
                      value: String(program.id),
                      label: `${program.code} - ${program.name}`,
                    })),
                  ]}
                />
              </label>
              {formState.errors.programId?.message != null && (
                <span className="seg-catalog__field-error" role="alert">
                  {formState.errors.programId.message}
                </span>
              )}
            </div>
          )}
        </form>
      </Dialog>

      {confirmingClose && (
        <Dialog
          width={420}
          title={t('unsaved.title')}
          description={t('unsaved.description')}
          onClose={() => setConfirmingClose(false)}
          closeLabel={t('unsaved.stay')}
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
    </>
  )
}

interface ProjectsStructureDeleteDialogProps {
  state: DeleteState
  programs: readonly ProgramNode[]
  axes: readonly AxisNode[]
  programNames: ReadonlyMap<number, string>
  onClose: () => void
  onDeleted: (
    kind: StructureKind,
    row: ProgramNode | AxisNode,
    reassigned: boolean,
  ) => void
}

function ProjectsStructureDeleteDialog({
  state,
  programs,
  axes,
  programNames,
  onClose,
  onDeleted,
}: ProjectsStructureDeleteDialogProps) {
  const { t } = useTranslation('configuration')
  const queryClient = useQueryClient()
  const [targetId, setTargetId] = useState('')
  const [targetError, setTargetError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const labels =
    `projectsStructure.${state.kind === 'program' ? 'programs' : 'axes'}` as const
  const kindName = t(`${labels}.itemName`)
  const targetKindName = t(
    `projectsStructure.${state.kind === 'program' ? 'programs' : 'axes'}.itemName`,
  )

  const impactQuery = useQuery({
    queryKey: [
      'configuration',
      'projects-structure-impact',
      state.kind,
      state.row.id,
    ] as const,
    queryFn: ({ signal }) =>
      state.kind === 'program'
        ? projectsStructureApi.programDeletionImpact(state.row.id, signal)
        : projectsStructureApi.axisDeletionImpact(state.row.id, signal),
    staleTime: 0,
    gcTime: 0,
  })

  const candidates =
    state.kind === 'program'
      ? programs.filter((program) => program.id !== state.row.id)
      : axes.filter((axis) => axis.id !== state.row.id)

  const removeMutation = useMutation({
    mutationFn: ({ reassigned }: { reassigned: boolean }) => {
      if (!reassigned) {
        return state.kind === 'program'
          ? projectsStructureApi.deleteProgram(state.row.id)
          : projectsStructureApi.deleteAxis(state.row.id)
      }
      const body = { targetNodeId: Number(targetId) }
      return state.kind === 'program'
        ? projectsStructureApi.reassignAndDeleteProgram(state.row.id, body)
        : projectsStructureApi.reassignAndDeleteAxis(state.row.id, body)
    },
    onSuccess: async (_, variables) => {
      await invalidateProjectsStructure(queryClient)
      onDeleted(state.kind, state.row, variables.reassigned)
    },
    onError: () => setActionError(t('projectsStructure.remove.error')),
  })

  if (impactQuery.isPending) {
    return (
      <Dialog
        width={460}
        title={t('projectsStructure.remove.directTitle', { name: state.row.name })}
        onClose={onClose}
        closeLabel={t('projectsStructure.remove.cancel')}
      >
        <div className="seg-catalog__dialog-status">
          <Spinner />
        </div>
      </Dialog>
    )
  }

  if (impactQuery.isError) {
    return (
      <Dialog
        width={460}
        title={t('projectsStructure.remove.directTitle', { name: state.row.name })}
        onClose={onClose}
        closeLabel={t('projectsStructure.remove.cancel')}
        footer={
          <Button onClick={onClose}>{t('projectsStructure.remove.close')}</Button>
        }
      >
        <p className="seg-catalog__form-error" role="alert">
          {t('projectsStructure.remove.error')}
        </p>
      </Dialog>
    )
  }

  const impact = impactQuery.data
  const hasChildren = impact.childCount > 0
  const canReassign = hasChildren && impact.hasCompatibleTarget && candidates.length > 0
  const blocked = hasChildren && !canReassign

  if (!hasChildren) {
    return (
      <Dialog
        width={460}
        title={t('projectsStructure.remove.directTitle', { name: state.row.name })}
        description={t('projectsStructure.remove.directDescription', {
          kind: kindName,
        })}
        onClose={onClose}
        closeLabel={t('projectsStructure.remove.cancel')}
        footer={
          <>
            <Button
              variant="ghost"
              onClick={onClose}
              disabled={removeMutation.isPending}
            >
              {t('projectsStructure.remove.cancel')}
            </Button>
            <Button
              variant="danger"
              disabled={removeMutation.isPending}
              onClick={() => {
                setActionError(null)
                removeMutation.mutate({ reassigned: false })
              }}
            >
              {removeMutation.isPending
                ? t('projectsStructure.remove.deleting')
                : t('projectsStructure.remove.confirm')}
            </Button>
          </>
        }
      >
        {actionError != null && (
          <p className="seg-catalog__form-error" role="alert">
            {actionError}
          </p>
        )}
      </Dialog>
    )
  }

  if (blocked) {
    return (
      <Dialog
        width={500}
        title={t('projectsStructure.remove.blockedTitle', { name: state.row.name })}
        description={t('projectsStructure.remove.blockedDescription', {
          count: impact.childCount,
          kind: kindName,
          targetKind: targetKindName,
        })}
        onClose={onClose}
        closeLabel={t('projectsStructure.remove.close')}
        footer={
          <Button onClick={onClose}>{t('projectsStructure.remove.close')}</Button>
        }
      >
        <p className="seg-catalog__form-error" role="alert">
          {t('projectsStructure.remove.impactSummary', { count: impact.childCount })}
        </p>
      </Dialog>
    )
  }

  const submit = () => {
    setActionError(null)
    setTargetError(null)
    if (targetId === '') {
      setTargetError(t('projectsStructure.remove.targetRequired'))
      return
    }
    removeMutation.mutate({ reassigned: true })
  }

  return (
    <Dialog
      width={500}
      title={t('projectsStructure.remove.referencedTitle', { name: state.row.name })}
      description={t('projectsStructure.remove.referencedDescription', {
        count: impact.childCount,
        kind: kindName,
      })}
      onClose={onClose}
      closeLabel={t('projectsStructure.remove.cancel')}
      footer={
        <>
          <Button variant="ghost" onClick={onClose} disabled={removeMutation.isPending}>
            {t('projectsStructure.remove.cancel')}
          </Button>
          <Button variant="danger" disabled={removeMutation.isPending} onClick={submit}>
            {removeMutation.isPending
              ? t('projectsStructure.remove.deleting')
              : t('projectsStructure.remove.reassignConfirm')}
          </Button>
        </>
      }
    >
      {actionError != null && (
        <p className="seg-catalog__form-error" role="alert">
          {actionError}
        </p>
      )}
      <p className="seg-catalog__form-hint">
        {t('projectsStructure.remove.impactSummary', { count: impact.childCount })}
      </p>
      <div className="seg-catalog__field">
        <label className="seg-catalog__field-control">
          <span className="seg-catalog__field-label">
            {t('projectsStructure.remove.targetLabel')}
          </span>
          <Select
            value={targetId}
            aria-invalid={targetError != null}
            onChange={(event) => {
              setTargetId(event.target.value)
              setTargetError(null)
            }}
            options={[
              { value: '', label: t('projectsStructure.remove.targetPlaceholder') },
              ...candidates.map((candidate) => ({
                value: String(candidate.id),
                label:
                  state.kind === 'program'
                    ? `${candidate.code} - ${candidate.name}`
                    : `${candidate.code} - ${candidate.name} (${programNames.get((candidate as AxisNode).programId) ?? '—'})`,
              })),
            ]}
          />
        </label>
        {targetError != null && (
          <span className="seg-catalog__field-error" role="alert">
            {targetError}
          </span>
        )}
      </div>
    </Dialog>
  )
}
