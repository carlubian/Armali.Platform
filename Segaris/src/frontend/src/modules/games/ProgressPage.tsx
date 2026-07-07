import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  ArrowDown,
  ArrowLeft,
  ArrowUp,
  Calendar,
  Check,
  Dices,
  Gamepad2,
  Globe,
  Layers3,
  Lock,
  Monitor,
  Pencil,
  Plus,
  Shapes,
  Smartphone,
  Swords,
  Tag,
  Target,
  Trash2,
} from 'lucide-react'
import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router-dom'

import {
  gamesApi,
  sectionColors,
  type GamePlatform,
  type GamesVisibility,
  type Goal,
  type Playthrough,
  type PlaythroughStatus,
  type Section,
  type SectionColor,
} from '@/app/api/games'
import { isApiError } from '@/app/api/errors'
import {
  Badge,
  Button,
  Checkbox,
  Dialog,
  IconButton,
  Input,
  Spinner,
  Toast,
  type BadgeTone,
} from '@/components/ui'

import { goalRequestSchema, gamesKeys, sectionRequestSchema } from './contracts'
import { useProgressPageState } from './gamesState'
import { useGoals, usePlaythrough, useSections } from './queries'

import './GamesPage.css'

const platformIcon: Record<GamePlatform, ReactNode> = {
  PC: <Monitor size={24} aria-hidden="true" />,
  Console: <Gamepad2 size={24} aria-hidden="true" />,
  Mobile: <Smartphone size={24} aria-hidden="true" />,
  BoardGame: <Dices size={24} aria-hidden="true" />,
  TabletopRpg: <Swords size={24} aria-hidden="true" />,
  Other: <Shapes size={24} aria-hidden="true" />,
}

const statusTone: Record<PlaythroughStatus, BadgeTone> = {
  Planning: 'neutral',
  Active: 'aqua',
  Completed: 'success',
}

type SectionDialogState =
  | { mode: 'closed' }
  | { mode: 'create' }
  | { mode: 'edit'; section: Section }

type GoalDialogState =
  | { mode: 'closed' }
  | { mode: 'edit'; section: Section; goal: Goal }

type ConfirmState =
  | { mode: 'closed' }
  | { mode: 'deleteSection'; section: Section }
  | { mode: 'deleteGoal'; section: Section; goal: Goal }

type ToastState = 'sectionSaved' | 'sectionDeleted' | 'goalSaved' | 'goalDeleted'

function pct(done: number, total: number): number {
  return total > 0 ? Math.round((done / total) * 100) : 0
}

function colorClass(color: SectionColor): string {
  return `gm-sec--${color}`
}

export function ProgressPage() {
  const { t } = useTranslation('games')
  const navigate = useNavigate()
  const params = useParams()
  const queryClient = useQueryClient()
  const {
    selectedSectionId: selectedSectionFromUrl,
    manageSections,
    selectSection,
    openManageSections,
    closeManageSections,
  } = useProgressPageState()
  const [sectionDialog, setSectionDialog] = useState<SectionDialogState>({
    mode: 'closed',
  })
  const [goalDialog, setGoalDialog] = useState<GoalDialogState>({ mode: 'closed' })
  const [confirming, setConfirming] = useState<ConfirmState>({ mode: 'closed' })
  const [toast, setToast] = useState<ToastState | null>(null)

  const playthroughId = Number.parseInt(params.playthroughId ?? '', 10)
  const valid = Number.isFinite(playthroughId) && playthroughId > 0
  const playthroughQuery = usePlaythrough(valid ? playthroughId : 0, valid)
  const sectionsQuery = useSections(valid ? playthroughId : 0, valid)
  const sections = useMemo(
    () => [...(sectionsQuery.data ?? [])].sort((a, b) => a.sortOrder - b.sortOrder),
    [sectionsQuery.data],
  )

  const selectedSection =
    sections.find((section) => section.id === selectedSectionFromUrl) ?? null
  const selectedSectionId = selectedSection?.id ?? 0
  const goalsQuery = useGoals(playthroughId, selectedSectionId, selectedSection != null)

  useEffect(() => {
    if (!sectionsQuery.isSuccess) return
    const selectedExists = sections.some(
      (section) => section.id === selectedSectionFromUrl,
    )
    if (sections.length === 0 && selectedSectionFromUrl != null) {
      selectSection(null)
    } else if (sections.length > 0 && !selectedExists) {
      selectSection(sections[0].id)
    }
  }, [sections, sectionsQuery.isSuccess, selectedSectionFromUrl, selectSection])

  const invalidateProgress = (sectionId?: number) => {
    void queryClient.invalidateQueries({
      queryKey: gamesKeys.playthrough(playthroughId),
    })
    void queryClient.invalidateQueries({ queryKey: gamesKeys.sections(playthroughId) })
    if (sectionId != null) {
      void queryClient.invalidateQueries({
        queryKey: gamesKeys.goals(playthroughId, sectionId),
      })
    }
    void queryClient.invalidateQueries({ queryKey: gamesKeys.playthroughs() })
  }

  const back = (
    <Button
      variant="outline"
      size="sm"
      iconLeft={<ArrowLeft size={15} />}
      onClick={() => void navigate('/games')}
    >
      {t('progressPage.back')}
    </Button>
  )

  if (!valid || playthroughQuery.isError) {
    const notFound =
      !valid ||
      (isApiError(playthroughQuery.error) &&
        playthroughQuery.error.kind === 'not-found')
    return (
      <main className="seg-games-progress armali-aurora">
        <div className="seg-games-progress__back">{back}</div>
        <p className="seg-games__error" role="alert">
          {notFound ? t('progressPage.notFound') : t('progressPage.loadError')}
        </p>
      </main>
    )
  }

  if (playthroughQuery.isPending) {
    return (
      <main className="seg-games-progress armali-aurora">
        <div className="seg-games-progress__back">{back}</div>
        <div className="seg-games-progress__status">
          <Spinner />
          <span>{t('progressPage.loading')}</span>
        </div>
      </main>
    )
  }

  const playthrough = playthroughQuery.data

  return (
    <main className="seg-games-progress armali-aurora">
      <div className="seg-games-progress__back">{back}</div>
      <ProgressHeader playthrough={playthrough} />

      {sectionsQuery.isPending ? (
        <div className="seg-games-progress__status">
          <Spinner />
          <span>{t('progressPage.sections.loading')}</span>
        </div>
      ) : sectionsQuery.isError ? (
        <p className="seg-games__error" role="alert">
          {t('progressPage.sections.loadError')}
        </p>
      ) : (
        <section
          className="seg-games-progress__body"
          aria-label={t('progressPage.title')}
        >
          <SectionRail
            sections={sections}
            selectedSectionId={selectedSection?.id ?? null}
            onSelect={selectSection}
            onCreate={() => setSectionDialog({ mode: 'create' })}
            onManage={openManageSections}
          />
          <GoalsPane
            playthroughId={playthroughId}
            section={selectedSection}
            goals={goalsQuery.data ?? []}
            loading={goalsQuery.isPending && selectedSection != null}
            error={goalsQuery.isError}
            onCreateSection={() => setSectionDialog({ mode: 'create' })}
            onEditGoal={(section, goal) =>
              setGoalDialog({ mode: 'edit', section, goal })
            }
            onDeleteGoal={(section, goal) =>
              setConfirming({ mode: 'deleteGoal', section, goal })
            }
            onChanged={(sectionId) => invalidateProgress(sectionId)}
          />
        </section>
      )}

      {manageSections && (
        <ManageSectionsDialog
          playthroughId={playthroughId}
          playthroughName={playthrough.name}
          sections={sections}
          onClose={closeManageSections}
          onCreate={() => setSectionDialog({ mode: 'create' })}
          onEdit={(section) => setSectionDialog({ mode: 'edit', section })}
          onDelete={(section) => setConfirming({ mode: 'deleteSection', section })}
          onChanged={(selectedId) => {
            invalidateProgress()
            if (selectedId != null) selectSection(selectedId)
          }}
        />
      )}

      {sectionDialog.mode !== 'closed' && (
        <SectionDialog
          mode={sectionDialog.mode}
          playthroughId={playthroughId}
          section={sectionDialog.mode === 'edit' ? sectionDialog.section : undefined}
          onClose={() => setSectionDialog({ mode: 'closed' })}
          onSaved={(section) => {
            setSectionDialog({ mode: 'closed' })
            selectSection(section.id)
            invalidateProgress(section.id)
            setToast('sectionSaved')
          }}
        />
      )}

      {goalDialog.mode === 'edit' && (
        <GoalDialog
          playthroughId={playthroughId}
          section={goalDialog.section}
          goal={goalDialog.goal}
          onClose={() => setGoalDialog({ mode: 'closed' })}
          onSaved={(sectionId) => {
            setGoalDialog({ mode: 'closed' })
            invalidateProgress(sectionId)
            setToast('goalSaved')
          }}
        />
      )}

      {confirming.mode !== 'closed' && (
        <DeleteConfirmDialog
          playthroughId={playthroughId}
          confirming={confirming}
          onClose={() => setConfirming({ mode: 'closed' })}
          onDeleted={(deletedSectionId) => {
            setConfirming({ mode: 'closed' })
            if (
              deletedSectionId != null &&
              selectedSectionFromUrl === deletedSectionId
            ) {
              selectSection(null)
            }
            invalidateProgress(
              confirming.mode === 'deleteGoal' ? confirming.section.id : undefined,
            )
            setToast(
              confirming.mode === 'deleteSection' ? 'sectionDeleted' : 'goalDeleted',
            )
          }}
        />
      )}

      {toast != null && (
        <div className="seg-games__toast">
          <Toast
            tone="success"
            title={t(`progressPage.toast.${toast}`)}
            closeLabel={t('editor.actions.cancel')}
            onClose={() => setToast(null)}
          />
        </div>
      )}
    </main>
  )
}

function ProgressHeader({ playthrough }: { playthrough: Playthrough }) {
  const { t } = useTranslation('games')
  const done = playthrough.progress.completedGoals
  const total = playthrough.progress.totalGoals
  const percent = pct(done, total)
  const shownTags = playthrough.tags.slice(0, 4)
  const extraTags = playthrough.tags.length - shownTags.length

  return (
    <section className="seg-games-progress__head">
      <span className="seg-games-progress__cover">
        {platformIcon[playthrough.platform]}
      </span>
      <div className="seg-games-progress__identity">
        <div className="armali-eyebrow">{playthrough.gameName}</div>
        <h1>{playthrough.name}</h1>
        <div className="seg-games-progress__meta">
          <Badge tone={statusTone[playthrough.status]} dot>
            {t(`status.${playthrough.status}`)}
          </Badge>
          <span className="seg-games-plat">
            {t(`platform.${playthrough.platform}`)}
          </span>
          <VisibilityMark visibility={playthrough.visibility} />
          <span className="seg-games-card__date">
            <Calendar size={13} aria-hidden="true" />
            {t('collection.started', {
              date: formatStart(playthrough.startMonth, playthrough.startYear, t),
            })}
          </span>
        </div>
        {shownTags.length > 0 && (
          <div className="seg-games-tags">
            {shownTags.map((tag) => (
              <span key={tag} className="seg-games-tag">
                <Tag size={11} aria-hidden="true" />
                {tag}
              </span>
            ))}
            {extraTags > 0 && (
              <span className="seg-games-tag">
                {t('collection.moreTags', { count: extraTags })}
              </span>
            )}
          </div>
        )}
      </div>
      <ProgressBlock done={done} total={total} percent={percent} />
    </section>
  )
}

function ProgressBlock({
  done,
  total,
  percent,
}: {
  done: number
  total: number
  percent: number
}) {
  const { t } = useTranslation('games')
  const empty = total === 0
  return (
    <div className={'seg-games-progress__overall' + (empty ? ' is-empty' : '')}>
      <span className="seg-games-progbar__pct">
        {empty ? '—' : t('progress.percent', { percent })}
      </span>
      <span className="seg-games-progbar__count">
        {empty ? t('progress.none') : t('progress.count', { done, total })}
      </span>
      <div className="seg-games-progbar__track">
        <div
          className="seg-games-progbar__fill"
          style={{ width: `${empty ? 0 : percent}%` }}
        />
      </div>
    </div>
  )
}

function SectionRail({
  sections,
  selectedSectionId,
  onSelect,
  onCreate,
  onManage,
}: {
  sections: Section[]
  selectedSectionId: number | null
  onSelect: (sectionId: number | null) => void
  onCreate: () => void
  onManage: () => void
}) {
  const { t } = useTranslation('games')
  return (
    <aside className="seg-games-sections" aria-label={t('progressPage.sections.label')}>
      <div className="seg-games-sections__head">
        <h2>{t('progressPage.sections.title')}</h2>
        <Badge tone="neutral">{sections.length}</Badge>
      </div>
      <div className="seg-games-sections__list">
        {sections.length === 0 ? (
          <div className="seg-games-sections__empty">
            <span className="seg-games-empty__icon">
              <Layers3 size={24} aria-hidden="true" />
            </span>
            <p>{t('progressPage.sections.empty')}</p>
          </div>
        ) : (
          sections.map((section) => {
            const done = section.progress.completedGoals
            const total = section.progress.totalGoals
            const percent = pct(done, total)
            const active = section.id === selectedSectionId
            return (
              <button
                key={section.id}
                type="button"
                className={
                  'seg-games-section ' +
                  colorClass(section.color) +
                  (active ? ' is-active' : '')
                }
                onClick={() => onSelect(section.id)}
                aria-current={active ? 'true' : undefined}
              >
                <span className="seg-games-section__swatch" />
                <span className="seg-games-section__text">
                  <span className="seg-games-section__name">{section.name}</span>
                  <span className="seg-games-section__sub">
                    {total === 0
                      ? t('progressPage.goals.none')
                      : t('progressPage.sections.progress', {
                          done,
                          total,
                          percent,
                        })}
                  </span>
                </span>
                <span className="seg-games-section__mini">
                  <span
                    className="seg-games-section__minifill"
                    style={{ width: `${total === 0 ? 0 : percent}%` }}
                  />
                </span>
              </button>
            )
          })
        )}
      </div>
      <div className="seg-games-sections__foot">
        <Button
          variant="ghost"
          size="sm"
          iconLeft={<Plus size={15} />}
          onClick={onCreate}
        >
          {t('progressPage.sections.add')}
        </Button>
        <Button variant="outline" size="sm" onClick={onManage}>
          {t('progressPage.sections.manage')}
        </Button>
      </div>
    </aside>
  )
}

function GoalsPane({
  playthroughId,
  section,
  goals,
  loading,
  error,
  onCreateSection,
  onEditGoal,
  onDeleteGoal,
  onChanged,
}: {
  playthroughId: number
  section: Section | null
  goals: Goal[]
  loading: boolean
  error: boolean
  onCreateSection: () => void
  onEditGoal: (section: Section, goal: Goal) => void
  onDeleteGoal: (section: Section, goal: Goal) => void
  onChanged: (sectionId: number) => void
}) {
  const { t } = useTranslation('games')
  const [draft, setDraft] = useState('')
  const [draftError, setDraftError] = useState<string | null>(null)
  const sortedGoals = useMemo(
    () => [...goals].sort((a, b) => a.position - b.position),
    [goals],
  )

  const createGoal = useMutation({
    mutationFn: (text: string) =>
      gamesApi.createGoal(playthroughId, section?.id ?? 0, { text }),
    onSuccess: () => {
      setDraft('')
      setDraftError(null)
      if (section != null) onChanged(section.id)
    },
    onError: () => setDraftError(t('progressPage.goals.errors.save')),
  })

  const completion = useMutation({
    mutationFn: (goal: Goal) =>
      gamesApi.setGoalCompletion(playthroughId, section?.id ?? 0, goal.id, {
        completed: !goal.completed,
      }),
    onSuccess: () => {
      if (section != null) onChanged(section.id)
    },
  })

  const submitDraft = () => {
    const parsed = goalRequestSchema.safeParse({ text: draft })
    if (!parsed.success) {
      setDraftError(t('progressPage.goals.validation.textRequired'))
      return
    }
    createGoal.mutate(parsed.data.text)
  }

  if (section == null) {
    return (
      <section className="seg-games-goals" aria-label={t('progressPage.goals.label')}>
        <div className="seg-games-goals__none">
          <span className="seg-games-empty__icon">
            <Target size={26} aria-hidden="true" />
          </span>
          <h2>{t('progressPage.empty.title')}</h2>
          <p>{t('progressPage.empty.body')}</p>
          <Button iconLeft={<Plus size={16} />} onClick={onCreateSection}>
            {t('progressPage.sections.add')}
          </Button>
        </div>
      </section>
    )
  }

  const done = section.progress.completedGoals
  const total = section.progress.totalGoals
  const percent = pct(done, total)
  const empty = total === 0

  return (
    <section
      className={`seg-games-goals ${colorClass(section.color)}`}
      aria-label={t('progressPage.goals.label')}
    >
      <div className="seg-games-goals__head">
        <span className="seg-games-goals__swatch">
          <Target size={17} aria-hidden="true" />
        </span>
        <div className="seg-games-goals__identity">
          <h2>{section.name}</h2>
          <p>
            {empty
              ? t('progressPage.goals.emptySection')
              : t('progressPage.goals.progress', { done, total })}
          </p>
        </div>
        <div className="seg-games-goals__progress">
          <div className="seg-games-goals__bar">
            <div
              className="seg-games-goals__fill"
              style={{ width: `${empty ? 0 : percent}%` }}
            />
          </div>
          <span>{empty ? '—' : t('progress.percent', { percent })}</span>
        </div>
      </div>

      <div className="seg-games-goals__list" aria-busy={loading}>
        {loading ? (
          <div className="seg-games-progress__status">
            <Spinner />
            <span>{t('progressPage.goals.loading')}</span>
          </div>
        ) : error ? (
          <p className="seg-games__error" role="alert">
            {t('progressPage.goals.loadError')}
          </p>
        ) : sortedGoals.length === 0 ? (
          <div className="seg-games-goals__empty">
            <span className="seg-games-empty__icon">
              <Target size={24} aria-hidden="true" />
            </span>
            <p>{t('progressPage.goals.empty')}</p>
          </div>
        ) : (
          sortedGoals.map((goal) => (
            <div
              key={goal.id}
              className={'seg-games-goal' + (goal.completed ? ' is-done' : '')}
            >
              <Checkbox
                checked={goal.completed}
                onChange={() => completion.mutate(goal)}
                disabled={completion.isPending}
                aria-label={t('progressPage.goals.toggle', { text: goal.text })}
              />
              <span className="seg-games-goal__text">{goal.text}</span>
              <span className="seg-games-goal__actions">
                <IconButton
                  variant="bare"
                  size="sm"
                  label={t('progressPage.goals.edit')}
                  icon={<Pencil size={14} />}
                  onClick={() => onEditGoal(section, goal)}
                />
                <IconButton
                  variant="bare"
                  size="sm"
                  label={t('progressPage.goals.delete')}
                  icon={<Trash2 size={14} />}
                  onClick={() => onDeleteGoal(section, goal)}
                />
              </span>
            </div>
          ))
        )}
      </div>

      <form
        className="seg-games-goaladd"
        onSubmit={(event) => {
          event.preventDefault()
          submitDraft()
        }}
        noValidate
      >
        <Plus size={16} aria-hidden="true" />
        <input
          value={draft}
          placeholder={t('progressPage.goals.addPlaceholder')}
          onChange={(event) => {
            setDraft(event.target.value)
            setDraftError(null)
          }}
          aria-label={t('progressPage.goals.addLabel')}
          aria-invalid={draftError != null}
        />
        <IconButton
          type="submit"
          variant="bare"
          size="sm"
          label={t('progressPage.goals.add')}
          icon={<Check size={15} />}
          disabled={createGoal.isPending}
        />
        {draftError != null && (
          <span className="seg-games-goaladd__error" role="alert">
            {draftError}
          </span>
        )}
      </form>
    </section>
  )
}

function ManageSectionsDialog({
  playthroughId,
  playthroughName,
  sections,
  onClose,
  onCreate,
  onEdit,
  onDelete,
  onChanged,
}: {
  playthroughId: number
  playthroughName: string
  sections: Section[]
  onClose: () => void
  onCreate: () => void
  onEdit: (section: Section) => void
  onDelete: (section: Section) => void
  onChanged: (selectedId?: number) => void
}) {
  const { t } = useTranslation('games')
  const reorder = useMutation({
    mutationFn: (sectionIds: number[]) =>
      gamesApi.reorderSections(playthroughId, { sectionIds }),
    onSuccess: () => onChanged(),
  })
  const recolor = useMutation({
    mutationFn: ({ section, color }: { section: Section; color: SectionColor }) =>
      gamesApi.updateSection(playthroughId, section.id, {
        name: section.name,
        color,
      }),
    onSuccess: (section) => onChanged(section.id),
  })

  const move = (index: number, direction: -1 | 1) => {
    const next = [...sections]
    const target = index + direction
    if (target < 0 || target >= next.length) return
    const [item] = next.splice(index, 1)
    next.splice(target, 0, item)
    reorder.mutate(next.map((section) => section.id))
  }

  return (
    <Dialog
      scrollable
      width={680}
      title={t('progressPage.sectionManager.title')}
      description={t('progressPage.sectionManager.description', {
        name: playthroughName,
      })}
      onClose={onClose}
      closeLabel={t('editor.actions.cancel')}
      footer={
        <>
          <Button variant="ghost" onClick={onClose}>
            {t('editor.actions.cancel')}
          </Button>
          <Button iconLeft={<Plus size={15} />} onClick={onCreate}>
            {t('progressPage.sections.add')}
          </Button>
        </>
      }
    >
      <div className="seg-games-sectionmgr">
        {sections.length === 0 ? (
          <p className="seg-games-sections__empty">
            {t('progressPage.sectionManager.empty')}
          </p>
        ) : (
          sections.map((section, index) => (
            <div
              key={section.id}
              className={`seg-games-sectionrow ${colorClass(section.color)}`}
            >
              <div className="seg-games-sectionrow__move">
                <IconButton
                  variant="bare"
                  size="sm"
                  label={t('progressPage.sectionManager.moveUp')}
                  icon={<ArrowUp size={14} />}
                  onClick={() => move(index, -1)}
                  disabled={index === 0 || reorder.isPending}
                />
                <IconButton
                  variant="bare"
                  size="sm"
                  label={t('progressPage.sectionManager.moveDown')}
                  icon={<ArrowDown size={14} />}
                  onClick={() => move(index, 1)}
                  disabled={index === sections.length - 1 || reorder.isPending}
                />
              </div>
              <span className="seg-games-sectionrow__swatch" />
              <div className="seg-games-sectionrow__main">
                <strong>{section.name}</strong>
                <span>
                  {t('progressPage.goals.progress', {
                    done: section.progress.completedGoals,
                    total: section.progress.totalGoals,
                  })}
                </span>
              </div>
              <div
                className="seg-games-swatches"
                role="group"
                aria-label={t('progressPage.sectionManager.colorLabel', {
                  name: section.name,
                })}
              >
                {sectionColors.map((color) => (
                  <button
                    key={color}
                    type="button"
                    className={
                      `seg-games-swatch ${colorClass(color)}` +
                      (section.color === color ? ' is-active' : '')
                    }
                    onClick={() => recolor.mutate({ section, color })}
                    aria-label={t('progressPage.sectionManager.color', {
                      color: t(`progressPage.colors.${color}`),
                    })}
                    aria-pressed={section.color === color}
                  />
                ))}
              </div>
              <IconButton
                variant="bare"
                size="sm"
                label={t('progressPage.sections.edit')}
                icon={<Pencil size={14} />}
                onClick={() => onEdit(section)}
              />
              <IconButton
                variant="bare"
                size="sm"
                label={t('progressPage.sections.delete')}
                icon={<Trash2 size={14} />}
                onClick={() => onDelete(section)}
              />
            </div>
          ))
        )}
      </div>
    </Dialog>
  )
}

function SectionDialog({
  mode,
  playthroughId,
  section,
  onClose,
  onSaved,
}: {
  mode: 'create' | 'edit'
  playthroughId: number
  section?: Section
  onClose: () => void
  onSaved: (section: Section) => void
}) {
  const { t } = useTranslation('games')
  const [name, setName] = useState(section?.name ?? '')
  const [color, setColor] = useState<SectionColor>(section?.color ?? 'Blue')
  const [error, setError] = useState<string | null>(null)

  const mutation = useMutation({
    mutationFn: () => {
      const parsed = sectionRequestSchema.safeParse({ name, color })
      if (!parsed.success) throw new Error('client-validation')
      return mode === 'create'
        ? gamesApi.createSection(playthroughId, parsed.data)
        : gamesApi.updateSection(playthroughId, section?.id ?? 0, parsed.data)
    },
    onSuccess: onSaved,
    onError: (err) =>
      setError(
        err instanceof Error && err.message === 'client-validation'
          ? t('progressPage.sections.validation.nameRequired')
          : t('progressPage.sections.errors.save'),
      ),
  })

  return (
    <Dialog
      width={460}
      title={
        mode === 'create'
          ? t('progressPage.sections.createTitle')
          : t('progressPage.sections.editTitle')
      }
      onClose={onClose}
      closeLabel={t('editor.actions.cancel')}
      footer={
        <>
          <Button variant="ghost" onClick={onClose} disabled={mutation.isPending}>
            {t('editor.actions.cancel')}
          </Button>
          <Button onClick={() => mutation.mutate()} disabled={mutation.isPending}>
            {mutation.isPending
              ? t('editor.actions.saving')
              : t('progressPage.sections.save')}
          </Button>
        </>
      }
    >
      <div className="seg-games-sectionform">
        {error != null && (
          <p className="seg-games-editor__error" role="alert">
            {error}
          </p>
        )}
        <Input
          label={t('progressPage.sections.name')}
          value={name}
          onChange={(event) => {
            setName(event.target.value)
            setError(null)
          }}
          required
          error={
            error === t('progressPage.sections.validation.nameRequired')
              ? error
              : undefined
          }
        />
        <div className="seg-games-editor__field">
          <span className="seg-games-editor__label">
            {t('progressPage.sections.color')}
          </span>
          <div className="seg-games-swatches">
            {sectionColors.map((candidate) => (
              <button
                key={candidate}
                type="button"
                className={
                  `seg-games-swatch ${colorClass(candidate)}` +
                  (color === candidate ? ' is-active' : '')
                }
                onClick={() => setColor(candidate)}
                aria-label={t('progressPage.sectionManager.color', {
                  color: t(`progressPage.colors.${candidate}`),
                })}
                aria-pressed={color === candidate}
              />
            ))}
          </div>
        </div>
      </div>
    </Dialog>
  )
}

function GoalDialog({
  playthroughId,
  section,
  goal,
  onClose,
  onSaved,
}: {
  playthroughId: number
  section: Section
  goal: Goal
  onClose: () => void
  onSaved: (sectionId: number) => void
}) {
  const { t } = useTranslation('games')
  const [text, setText] = useState(goal.text)
  const [error, setError] = useState<string | null>(null)
  const mutation = useMutation({
    mutationFn: () => {
      const parsed = goalRequestSchema.safeParse({ text })
      if (!parsed.success) throw new Error('client-validation')
      return gamesApi.updateGoal(playthroughId, section.id, goal.id, parsed.data)
    },
    onSuccess: () => onSaved(section.id),
    onError: (err) =>
      setError(
        err instanceof Error && err.message === 'client-validation'
          ? t('progressPage.goals.validation.textRequired')
          : t('progressPage.goals.errors.save'),
      ),
  })

  return (
    <Dialog
      width={460}
      title={t('progressPage.goals.editTitle')}
      onClose={onClose}
      closeLabel={t('editor.actions.cancel')}
      footer={
        <>
          <Button variant="ghost" onClick={onClose} disabled={mutation.isPending}>
            {t('editor.actions.cancel')}
          </Button>
          <Button onClick={() => mutation.mutate()} disabled={mutation.isPending}>
            {mutation.isPending
              ? t('editor.actions.saving')
              : t('progressPage.goals.save')}
          </Button>
        </>
      }
    >
      <Input
        label={t('progressPage.goals.text')}
        value={text}
        onChange={(event) => {
          setText(event.target.value)
          setError(null)
        }}
        required
        error={error}
      />
    </Dialog>
  )
}

function DeleteConfirmDialog({
  playthroughId,
  confirming,
  onClose,
  onDeleted,
}: {
  playthroughId: number
  confirming: Exclude<ConfirmState, { mode: 'closed' }>
  onClose: () => void
  onDeleted: (deletedSectionId?: number) => void
}) {
  const { t } = useTranslation('games')
  const mutation = useMutation({
    mutationFn: () =>
      confirming.mode === 'deleteSection'
        ? gamesApi.deleteSection(playthroughId, confirming.section.id)
        : gamesApi.deleteGoal(playthroughId, confirming.section.id, confirming.goal.id),
    onSuccess: () =>
      onDeleted(
        confirming.mode === 'deleteSection' ? confirming.section.id : undefined,
      ),
  })
  const sectionMode = confirming.mode === 'deleteSection'
  return (
    <Dialog
      width={430}
      title={
        sectionMode
          ? t('progressPage.sections.deleteTitle')
          : t('progressPage.goals.deleteTitle')
      }
      description={
        sectionMode
          ? t('progressPage.sections.deleteDescription')
          : t('progressPage.goals.deleteDescription')
      }
      onClose={onClose}
      closeLabel={t('editor.delete.cancel')}
      footer={
        <>
          <Button variant="ghost" onClick={onClose} disabled={mutation.isPending}>
            {t('editor.delete.cancel')}
          </Button>
          <Button
            variant="danger"
            iconLeft={<Trash2 size={15} />}
            onClick={() => mutation.mutate()}
            disabled={mutation.isPending}
          >
            {mutation.isPending
              ? t('editor.delete.deleting')
              : sectionMode
                ? t('progressPage.sections.deleteConfirm')
                : t('progressPage.goals.deleteConfirm')}
          </Button>
        </>
      }
    />
  )
}

function VisibilityMark({ visibility }: { visibility: GamesVisibility }) {
  const { t } = useTranslation('games')
  const isPrivate = visibility === 'Private'
  return (
    <span className={'seg-games-vis' + (isPrivate ? ' is-private' : '')}>
      {isPrivate ? (
        <Lock size={13} aria-hidden="true" />
      ) : (
        <Globe size={13} aria-hidden="true" />
      )}
      {t(`visibility.${visibility}`)}
    </span>
  )
}

function formatStart(month: number, year: number, t: (key: string) => string): string {
  const safeMonth = Math.min(Math.max(month, 1), 12)
  return `${t(`editor.months.${safeMonth}`)} ${year}`
}
