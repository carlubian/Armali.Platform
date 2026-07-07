import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation } from '@tanstack/react-query'
import {
  ArrowRight,
  Bookmark,
  ChevronsUpDown,
  Flag,
  Gamepad2,
  Globe,
  Lock,
  Map as MapIcon,
  Play,
  Trash2,
  X,
} from 'lucide-react'
import { useMemo, useRef, useState, type ReactNode } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'

import {
  gamesApi,
  playthroughStatuses,
  type Game,
  type GamePlatform,
  type GamesVisibility,
  type Playthrough,
  type PlaythroughStatus,
} from '@/app/api/games'
import { isApiError } from '@/app/api/errors'
import {
  Button,
  Dialog,
  Input,
  Select,
  SegmentedControl,
  Spinner,
  type SegmentTone,
} from '@/components/ui'

import { GameEntitySelector } from './GameEntitySelector'
import {
  buildPlaythroughDefaults,
  createPlaythroughSchema,
  fromPlaythrough,
  normalizeTags,
  toPlaythroughRequest,
  type PlaythroughFormValues,
} from './playthroughForm'
import { usePlaythrough } from './queries'

const statusMeta: Record<PlaythroughStatus, { icon: ReactNode; tone: SegmentTone }> = {
  Planning: { icon: <MapIcon size={15} />, tone: 'neutral' },
  Active: { icon: <Play size={15} />, tone: 'accent' },
  Completed: { icon: <Flag size={15} />, tone: 'success' },
}

const visibilityMeta: Record<GamesVisibility, { icon: ReactNode; tone: SegmentTone }> =
  {
    Public: { icon: <Globe size={15} />, tone: 'accent' },
    Private: { icon: <Lock size={15} />, tone: 'neutral' },
  }

interface SelectedGame {
  id: number
  name: string
  platform: GamePlatform
}

interface PlaythroughDialogProps {
  mode: 'create' | 'edit'
  playthroughId?: number
  currentUserId: number | null
  onClose: () => void
  onSaved: (playthrough: Playthrough, mode: 'create' | 'edit') => void
  onDeleted: (playthrough: Playthrough) => void
  onOpenProgress: (playthroughId: number) => void
}

export function PlaythroughDialog({
  mode,
  playthroughId,
  currentUserId,
  onClose,
  onSaved,
  onDeleted,
  onOpenProgress,
}: PlaythroughDialogProps) {
  const { t } = useTranslation('games')
  const playthroughQuery = usePlaythrough(
    playthroughId ?? 0,
    mode === 'edit' && playthroughId != null,
  )

  const title = mode === 'create' ? t('editor.createTitle') : t('editor.editTitle')
  const description =
    mode === 'create' ? t('editor.createDescription') : t('editor.editDescription')

  if (mode === 'edit' && playthroughQuery.isPending) {
    return (
      <Dialog
        scrollable
        width={580}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
      >
        <div className="seg-games-editor__status">
          <Spinner />
          <span>{t('editor.loading')}</span>
        </div>
      </Dialog>
    )
  }

  if (mode === 'edit' && playthroughQuery.isError) {
    const notFound =
      isApiError(playthroughQuery.error) && playthroughQuery.error.kind === 'not-found'
    return (
      <Dialog
        width={580}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
        footer={<Button onClick={onClose}>{t('editor.actions.cancel')}</Button>}
      >
        <p className="seg-games-editor__error" role="alert">
          {notFound ? t('editor.notFound') : t('editor.loadError')}
        </p>
      </Dialog>
    )
  }

  const playthrough =
    mode === 'edit' ? (playthroughQuery.data as Playthrough) : undefined
  const currentYear = new Date().getFullYear()
  const initialValues =
    playthrough != null
      ? fromPlaythrough(playthrough)
      : buildPlaythroughDefaults(currentYear)
  const initialGame: SelectedGame | null =
    playthrough != null
      ? {
          id: playthrough.gameId,
          name: playthrough.gameName,
          platform: playthrough.platform,
        }
      : null
  const canChangeVisibility =
    playthrough == null ||
    (currentUserId != null && playthrough.creatorId === currentUserId)

  return (
    <PlaythroughEditorForm
      mode={mode}
      playthroughId={playthroughId}
      playthrough={playthrough}
      title={title}
      description={description}
      currentYear={currentYear}
      initialValues={initialValues}
      initialGame={initialGame}
      canChangeVisibility={canChangeVisibility}
      onClose={onClose}
      onSaved={onSaved}
      onDeleted={onDeleted}
      onOpenProgress={onOpenProgress}
    />
  )
}

interface PlaythroughEditorFormProps {
  mode: 'create' | 'edit'
  playthroughId?: number
  playthrough?: Playthrough
  title: string
  description: string
  currentYear: number
  initialValues: PlaythroughFormValues
  initialGame: SelectedGame | null
  canChangeVisibility: boolean
  onClose: () => void
  onSaved: (playthrough: Playthrough, mode: 'create' | 'edit') => void
  onDeleted: (playthrough: Playthrough) => void
  onOpenProgress: (playthroughId: number) => void
}

function PlaythroughEditorForm({
  mode,
  playthroughId,
  playthrough,
  title,
  description,
  currentYear,
  initialValues,
  initialGame,
  canChangeVisibility,
  onClose,
  onSaved,
  onDeleted,
  onOpenProgress,
}: PlaythroughEditorFormProps) {
  const { t } = useTranslation('games')
  const schema = useMemo(
    () =>
      createPlaythroughSchema({
        nameRequired: t('editor.validation.nameRequired'),
        nameTooLong: t('editor.validation.nameTooLong'),
        gameRequired: t('editor.validation.gameRequired'),
        startMonthRequired: t('editor.validation.startMonthRequired'),
        startYearRequired: t('editor.validation.startYearRequired'),
        tagTooLong: t('editor.validation.tagTooLong'),
      }),
    [t],
  )

  const form = useForm<PlaythroughFormValues>({
    resolver: zodResolver(schema),
    defaultValues: initialValues,
  })
  const { register, handleSubmit, formState, setValue, control } = form

  const [selectedGame, setSelectedGame] = useState<SelectedGame | null>(initialGame)
  const [picking, setPicking] = useState(false)
  const [draftTag, setDraftTag] = useState('')
  const [serverError, setServerError] = useState<string | null>(null)
  const [confirmingClose, setConfirmingClose] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const editedRef = useRef(false)

  const tags = useWatch({ control, name: 'tags' })

  const mutation = useMutation({
    mutationFn: (values: PlaythroughFormValues) => {
      const request = toPlaythroughRequest(values)
      return mode === 'create'
        ? gamesApi.createPlaythrough(request)
        : gamesApi.updatePlaythrough(playthroughId as number, request)
    },
    onSuccess: (saved) => onSaved(saved, mode),
    onError: (error) => setServerError(mapServerError(error, t)),
  })

  const deleteMutation = useMutation({
    mutationFn: () => gamesApi.deletePlaythrough(playthroughId as number),
    onSuccess: () => {
      if (playthrough != null) onDeleted(playthrough)
    },
    onError: (error) => {
      setConfirmingDelete(false)
      setServerError(mapDeleteError(error, t))
    },
  })

  const submit = (event: React.FormEvent<HTMLFormElement>) => {
    void handleSubmit((values) => {
      setServerError(null)
      mutation.mutate(values)
    })(event)
  }

  const requestClose = () => {
    if (editedRef.current && !mutation.isSuccess) {
      setConfirmingClose(true)
      return
    }
    onClose()
  }

  const markEdited = () => {
    editedRef.current = true
  }

  const pickGame = (game: Game) => {
    setSelectedGame({ id: game.id, name: game.name, platform: game.platform })
    setValue('gameId', game.id, { shouldValidate: true, shouldDirty: true })
    markEdited()
    setPicking(false)
  }

  const addTag = () => {
    const value = draftTag.trim()
    if (value === '') return
    const next = normalizeTags([...tags, value])
    if (next.length !== tags.length) {
      setValue('tags', next, { shouldValidate: true, shouldDirty: true })
      markEdited()
    }
    setDraftTag('')
  }

  const removeTag = (tag: string) => {
    setValue(
      'tags',
      tags.filter((current) => current !== tag),
      { shouldValidate: true, shouldDirty: true },
    )
    markEdited()
  }

  const onTagKeyDown = (event: React.KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Enter' || event.key === ',') {
      event.preventDefault()
      addTag()
    } else if (event.key === 'Backspace' && draftTag === '' && tags.length > 0) {
      removeTag(tags[tags.length - 1])
    }
  }

  const years = useMemo(() => {
    const result: number[] = []
    for (let year = currentYear + 1; year >= 2005; year -= 1) result.push(year)
    return result
  }, [currentYear])

  const submitting = mutation.isPending
  const gameError = formState.errors.gameId?.message

  return (
    <>
      <Dialog
        scrollable
        width={580}
        title={title}
        description={description}
        onClose={requestClose}
        closeLabel={t('editor.actions.cancel')}
        footer={
          <>
            {mode === 'edit' && playthroughId != null && (
              <>
                <Button
                  variant="outline"
                  iconLeft={<ArrowRight size={15} />}
                  onClick={() => onOpenProgress(playthroughId)}
                  disabled={submitting}
                >
                  {t('editor.openProgress')}
                </Button>
                <Button
                  variant="ghost"
                  className="seg-games-editor__delete"
                  iconLeft={<Trash2 size={15} />}
                  onClick={() => setConfirmingDelete(true)}
                  disabled={submitting || deleteMutation.isPending}
                >
                  {t('editor.delete.action')}
                </Button>
              </>
            )}
            <Button variant="ghost" onClick={requestClose} disabled={submitting}>
              {t('editor.actions.cancel')}
            </Button>
            <Button type="submit" form="seg-games-form" disabled={submitting}>
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
          id="seg-games-form"
          className="seg-games-editor"
          onSubmit={submit}
          onChange={markEdited}
          noValidate
        >
          {serverError != null && (
            <p className="seg-games-editor__error" role="alert">
              {serverError}
            </p>
          )}

          <Input
            label={t('editor.fields.name')}
            placeholder={t('editor.fields.namePlaceholder')}
            required
            iconLeft={<Bookmark size={16} />}
            error={formState.errors.name?.message}
            {...register('name')}
          />

          <div className="seg-games-editor__field">
            <span className="seg-games-editor__label seg-games-editor__label--req">
              {t('editor.fields.game')}
            </span>
            <button
              type="button"
              className={
                'seg-games-gameref' +
                (selectedGame != null ? ' is-filled' : ' is-empty') +
                (gameError != null ? ' is-invalid' : '')
              }
              onClick={() => setPicking(true)}
              aria-describedby="seg-games-game-hint"
            >
              <span className="seg-games-gameref__icon">
                <Gamepad2 size={19} aria-hidden="true" />
              </span>
              <span className="seg-games-gameref__body">
                {selectedGame != null ? (
                  <>
                    <span className="seg-games-gameref__name">{selectedGame.name}</span>
                    <span className="seg-games-gameref__meta">
                      {t(`platform.${selectedGame.platform}`)}
                    </span>
                  </>
                ) : (
                  <span className="seg-games-gameref__placeholder">
                    {t('editor.fields.gamePlaceholder')}
                  </span>
                )}
              </span>
              <span className="seg-games-gameref__chev">
                <ChevronsUpDown size={16} aria-hidden="true" />
              </span>
            </button>
            {gameError != null ? (
              <span
                id="seg-games-game-hint"
                className="seg-games-editor__hint seg-games-editor__hint--error"
                role="alert"
              >
                {gameError}
              </span>
            ) : (
              <span id="seg-games-game-hint" className="seg-games-editor__hint">
                {t('editor.fields.gameHint')}
              </span>
            )}
          </div>

          <div className="seg-games-editor__grid2">
            <label className="seg-games-editor__field">
              <span className="seg-games-editor__label seg-games-editor__label--req">
                {t('editor.fields.startMonth')}
              </span>
              <Select
                {...register('startMonth')}
                aria-invalid={formState.errors.startMonth != null}
                options={Array.from({ length: 12 }, (_, index) => ({
                  value: String(index + 1),
                  label: t(`editor.months.${index + 1}`),
                }))}
              />
            </label>
            <label className="seg-games-editor__field">
              <span className="seg-games-editor__label seg-games-editor__label--req">
                {t('editor.fields.startYear')}
              </span>
              <Select
                {...register('startYear')}
                aria-invalid={formState.errors.startYear != null}
                options={years.map((year) => ({
                  value: String(year),
                  label: String(year),
                }))}
              />
            </label>
          </div>

          <div className="seg-games-editor__field">
            <span className="seg-games-editor__label" id="seg-games-status">
              {t('editor.fields.status')}
            </span>
            <SegmentedControl
              aria-labelledby="seg-games-status"
              {...register('status')}
              options={playthroughStatuses.map((value) => ({
                value,
                label: t(`status.${value}`),
                icon: statusMeta[value].icon,
                tone: statusMeta[value].tone,
              }))}
            />
            <span className="seg-games-editor__hint">
              {t('editor.fields.statusHint')}
            </span>
          </div>

          <div className="seg-games-editor__field">
            <span className="seg-games-editor__label">{t('editor.fields.tags')}</span>
            <div className="seg-games-taginput">
              {tags.map((tag) => (
                <span key={tag} className="seg-games-tagchip">
                  {tag}
                  <button
                    type="button"
                    className="seg-games-tagchip__x"
                    aria-label={t('editor.fields.removeTag', { tag })}
                    onClick={() => removeTag(tag)}
                  >
                    <X size={11} aria-hidden="true" />
                  </button>
                </span>
              ))}
              <input
                className="seg-games-taginput__input"
                value={draftTag}
                placeholder={
                  tags.length > 0
                    ? t('editor.fields.tagsPlaceholderMore')
                    : t('editor.fields.tagsPlaceholder')
                }
                onChange={(event) => setDraftTag(event.target.value)}
                onKeyDown={onTagKeyDown}
                onBlur={addTag}
                aria-label={t('editor.fields.tags')}
              />
            </div>
            {formState.errors.tags != null ? (
              <span className="seg-games-editor__hint seg-games-editor__hint--error">
                {t('editor.validation.tagTooLong')}
              </span>
            ) : (
              <span className="seg-games-editor__hint">
                {t('editor.fields.tagsHint')}
              </span>
            )}
          </div>

          <div className="seg-games-editor__field">
            <span className="seg-games-editor__label" id="seg-games-visibility">
              {t('editor.fields.visibility')}
            </span>
            <SegmentedControl
              aria-labelledby="seg-games-visibility"
              disabled={!canChangeVisibility}
              {...register('visibility')}
              options={(['Public', 'Private'] as GamesVisibility[]).map((value) => ({
                value,
                label: t(`visibility.${value}`),
                icon: visibilityMeta[value].icon,
                tone: visibilityMeta[value].tone,
              }))}
            />
            <span className="seg-games-editor__hint">
              {t('editor.fields.visibilityHint')}
            </span>
          </div>
        </form>
      </Dialog>

      {picking && (
        <GameEntitySelector
          currentGameId={selectedGame?.id ?? null}
          onSelect={pickGame}
          onClose={() => setPicking(false)}
        />
      )}

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
          width={440}
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
                iconLeft={<Trash2 size={15} />}
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

function mapServerError(error: unknown, t: (key: string) => string): string {
  if (isApiError(error)) {
    switch (error.problem?.code) {
      case 'games.playthrough.validation':
        return t('editor.errors.validation')
      case 'games.playthrough.unknown_game':
        return t('editor.errors.unknownGame')
      case 'games.playthrough.visibility_forbidden':
        return t('editor.errors.visibilityForbidden')
    }
    if (error.kind === 'not-found') return t('editor.notFound')
    if (error.kind === 'transient' || error.kind === 'unavailable') {
      return t('editor.errors.conflict')
    }
  }
  return t('editor.errors.generic')
}

function mapDeleteError(error: unknown, t: (key: string) => string): string {
  if (isApiError(error) && error.kind === 'not-found') return t('editor.notFound')
  return t('editor.delete.error')
}
