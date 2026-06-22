import { useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Download,
  FileText,
  Image,
  Paperclip,
  RotateCw,
  Star,
  Trash2,
  X,
} from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'

import { recipesApi, type RecipeAttachment } from '@/app/api/recipes'
import { Button, Spinner } from '@/components/ui'
import {
  attachmentAccept,
  formatFileSize,
  rejectionFor,
  type AttachmentRejection,
} from '@/modules/clothes/attachments'

import { recipesKeys } from './contracts'

interface UploadTask {
  id: number
  file: File
  status: 'uploading' | 'error'
  rejection: AttachmentRejection
}

export interface RecipeAttachmentsProps {
  recipeId: number
  autoUpload?: File[]
}

export function RecipeAttachments({ recipeId, autoUpload }: RecipeAttachmentsProps) {
  const { t } = useTranslation('recipes')
  const queryClient = useQueryClient()
  const queryKey = recipesKeys.recipeAttachments(recipeId)
  const list = useQuery({
    queryKey,
    queryFn: ({ signal }) => recipesApi.listRecipeAttachments(recipeId, signal),
  })

  const [tasks, setTasks] = useState<UploadTask[]>([])
  const [removing, setRemoving] = useState<Set<string>>(new Set())
  const [primaryBusy, setPrimaryBusy] = useState<string | null>(null)
  const [primaryError, setPrimaryError] = useState(false)
  const nextTaskId = useRef(0)
  const fileInput = useRef<HTMLInputElement>(null)

  const invalidate = async () => {
    await queryClient.invalidateQueries({ queryKey })
    await queryClient.invalidateQueries({ queryKey: recipesKeys.recipe(recipeId) })
    await queryClient.invalidateQueries({ queryKey: recipesKeys.recipes() })
  }

  const updateTask = (id: number, patch: Partial<UploadTask>) =>
    setTasks((current) =>
      current.map((task) => (task.id === id ? { ...task, ...patch } : task)),
    )

  const runUpload = async (taskId: number, file: File) => {
    try {
      await recipesApi.uploadRecipeAttachment(recipeId, file)
      setTasks((current) => current.filter((task) => task.id !== taskId))
      await invalidate()
    } catch {
      updateTask(taskId, { status: 'error', rejection: null })
    }
  }

  const enqueue = (file: File) => {
    const id = nextTaskId.current++
    const rejection = rejectionFor(file)
    setTasks((current) => [
      ...current,
      { id, file, status: rejection == null ? 'uploading' : 'error', rejection },
    ])
    if (rejection == null) void runUpload(id, file)
  }

  const onFilesChosen = (event: React.ChangeEvent<HTMLInputElement>) => {
    const chosen = event.target.files
    if (chosen != null) {
      for (const file of Array.from(chosen)) enqueue(file)
    }
    event.target.value = ''
  }

  const retry = (task: UploadTask) => {
    const rejection = rejectionFor(task.file)
    if (rejection != null) {
      updateTask(task.id, { status: 'error', rejection })
      return
    }
    updateTask(task.id, { status: 'uploading', rejection: null })
    void runUpload(task.id, task.file)
  }

  const remove = async (attachment: RecipeAttachment) => {
    setRemoving((current) => new Set(current).add(attachment.id))
    try {
      await recipesApi.deleteRecipeAttachment(recipeId, attachment.id)
      await invalidate()
    } finally {
      setRemoving((current) => {
        const next = new Set(current)
        next.delete(attachment.id)
        return next
      })
    }
  }

  const makePrimary = async (attachment: RecipeAttachment) => {
    setPrimaryError(false)
    setPrimaryBusy(attachment.id)
    try {
      await recipesApi.setPrimaryRecipeAttachment(recipeId, attachment.id)
      await invalidate()
    } catch {
      setPrimaryError(true)
    } finally {
      setPrimaryBusy(null)
    }
  }

  const autoUploaded = useRef(false)
  useEffect(() => {
    if (autoUploaded.current || autoUpload == null || autoUpload.length === 0) return
    autoUploaded.current = true
    for (const file of autoUpload) enqueue(file)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const attachments = list.data ?? []
  const hasContent = attachments.length > 0 || tasks.length > 0

  return (
    <div className="seg-rec-attach">
      <div className="seg-rec-attach__head">
        <Button
          variant="outline"
          size="sm"
          iconLeft={<Paperclip size={15} />}
          onClick={() => fileInput.current?.click()}
        >
          {t('editor.attachments.add')}
        </Button>
        <input
          ref={fileInput}
          type="file"
          multiple
          accept={attachmentAccept}
          className="seg-rec-attach__input"
          tabIndex={-1}
          onChange={onFilesChosen}
          aria-label={t('editor.attachments.add')}
        />
      </div>

      {primaryError && (
        <p className="seg-rec-attach__error" role="alert">
          {t('editor.attachments.errors.primaryFailed')}
        </p>
      )}

      {list.isPending ? (
        <div className="seg-rec-attach__status">
          <Spinner />
        </div>
      ) : list.isError ? (
        <p className="seg-rec-attach__error" role="alert">
          {t('editor.attachments.errors.loadFailed')}
        </p>
      ) : !hasContent ? (
        <p className="seg-rec-attach__empty">{t('editor.attachments.empty')}</p>
      ) : null}

      {hasContent && (
        <ul className="seg-rec-attach__list">
          {attachments.map((attachment) => (
            <li key={attachment.id} className="seg-rec-attach__item">
              {attachment.contentType.startsWith('image/') ? (
                <Image size={18} aria-hidden="true" />
              ) : (
                <FileText size={18} aria-hidden="true" />
              )}
              <span className="seg-rec-attach__meta">
                <span className="seg-rec-attach__name">{attachment.fileName}</span>
                <span className="seg-rec-attach__size">
                  {formatFileSize(attachment.size)}
                </span>
              </span>
              {attachment.isPrimary ? (
                <span className="seg-rec-attach__primary">
                  <Star size={14} aria-hidden="true" />
                  {t('editor.attachments.primary')}
                </span>
              ) : attachment.contentType.startsWith('image/') ? (
                <button
                  type="button"
                  className="seg-rec-attach__action"
                  onClick={() => void makePrimary(attachment)}
                  disabled={primaryBusy != null}
                  aria-label={t('editor.attachments.makePrimary')}
                >
                  {primaryBusy === attachment.id ? (
                    <Spinner size={16} />
                  ) : (
                    <Star size={16} aria-hidden="true" />
                  )}
                </button>
              ) : null}
              <a
                className="seg-rec-attach__action"
                href={recipesApi.recipeAttachmentDownloadUrl(recipeId, attachment.id)}
                download={attachment.fileName}
                aria-label={t('editor.attachments.download', {
                  name: attachment.fileName,
                })}
              >
                <Download size={16} aria-hidden="true" />
              </a>
              <button
                type="button"
                className="seg-rec-attach__action seg-rec-attach__action--danger"
                onClick={() => void remove(attachment)}
                disabled={removing.has(attachment.id)}
                aria-label={t('editor.attachments.remove')}
              >
                {removing.has(attachment.id) ? (
                  <Spinner size={16} />
                ) : (
                  <Trash2 size={16} aria-hidden="true" />
                )}
              </button>
            </li>
          ))}

          {tasks.map((task) => (
            <li
              key={`task-${task.id}`}
              className={
                'seg-rec-attach__item' +
                (task.status === 'error' ? ' seg-rec-attach__item--error' : '')
              }
            >
              <FileText size={18} aria-hidden="true" />
              <span className="seg-rec-attach__meta">
                <span className="seg-rec-attach__name">{task.file.name}</span>
                <span className="seg-rec-attach__size">
                  {task.status === 'uploading'
                    ? t('editor.attachments.uploading')
                    : messageForTask(task, t)}
                </span>
              </span>
              {task.status === 'uploading' ? (
                <span className="seg-rec-attach__action" aria-hidden="true">
                  <Spinner size={16} />
                </span>
              ) : (
                <>
                  <button
                    type="button"
                    className="seg-rec-attach__action"
                    onClick={() => retry(task)}
                    disabled={task.rejection != null}
                    aria-label={t('editor.attachments.retry')}
                  >
                    <RotateCw size={16} aria-hidden="true" />
                  </button>
                  <button
                    type="button"
                    className="seg-rec-attach__action"
                    onClick={() =>
                      setTasks((current) =>
                        current.filter((candidate) => candidate.id !== task.id),
                      )
                    }
                    aria-label={t('editor.attachments.dismiss')}
                  >
                    <X size={16} aria-hidden="true" />
                  </button>
                </>
              )}
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}

function messageForTask(task: UploadTask, t: (key: string) => string): string {
  switch (task.rejection) {
    case 'tooLarge':
      return t('editor.attachments.errors.tooLarge')
    case 'type':
      return t('editor.attachments.errors.type')
    default:
      return t('editor.attachments.errors.failed')
  }
}
