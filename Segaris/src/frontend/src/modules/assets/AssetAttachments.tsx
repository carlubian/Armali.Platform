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

import { assetsApi, type AssetAttachment } from '@/app/api/assets'
import { Button, Spinner } from '@/components/ui'

import {
  attachmentAccept,
  formatFileSize,
  isImageContentType,
  rejectionFor,
  type AttachmentRejection,
} from './attachments'
import { assetsKeys } from './queries'

export interface AssetAttachmentsProps {
  assetId: number
  autoUpload?: File[]
  onChanged?: () => void
}

interface UploadTask {
  id: number
  file: File
  status: 'uploading' | 'error'
  rejection: AttachmentRejection
}

export function AssetAttachments({
  assetId,
  autoUpload,
  onChanged,
}: AssetAttachmentsProps) {
  const { t } = useTranslation('assets')
  const queryClient = useQueryClient()
  const queryKey = assetsKeys.assetAttachments(assetId)

  const list = useQuery({
    queryKey,
    queryFn: ({ signal }) => assetsApi.listAssetAttachments(assetId, signal),
  })

  const [tasks, setTasks] = useState<UploadTask[]>([])
  const [removing, setRemoving] = useState<Set<string>>(new Set())
  const [markingPrimary, setMarkingPrimary] = useState<Set<string>>(new Set())
  const nextTaskId = useRef(0)
  const fileInput = useRef<HTMLInputElement>(null)

  const invalidate = async () => {
    await queryClient.invalidateQueries({ queryKey })
    await queryClient.invalidateQueries({ queryKey: assetsKeys.asset(assetId) })
    await queryClient.invalidateQueries({ queryKey: assetsKeys.assets() })
    onChanged?.()
  }

  const updateTask = (id: number, patch: Partial<UploadTask>) =>
    setTasks((current) =>
      current.map((task) => (task.id === id ? { ...task, ...patch } : task)),
    )

  const runUpload = async (taskId: number, file: File) => {
    try {
      await assetsApi.uploadAssetAttachment(assetId, file)
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
      { id, file, status: rejection != null ? 'error' : 'uploading', rejection },
    ])
    if (rejection == null) void runUpload(id, file)
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

  const remove = async (attachment: AssetAttachment) => {
    setRemoving((current) => new Set(current).add(attachment.id))
    try {
      await assetsApi.deleteAssetAttachment(assetId, attachment.id)
      await invalidate()
    } finally {
      setRemoving((current) => {
        const next = new Set(current)
        next.delete(attachment.id)
        return next
      })
    }
  }

  const setPrimary = async (attachment: AssetAttachment) => {
    setMarkingPrimary((current) => new Set(current).add(attachment.id))
    try {
      await assetsApi.setPrimaryAssetAttachment(assetId, attachment.id)
      await invalidate()
    } finally {
      setMarkingPrimary((current) => {
        const next = new Set(current)
        next.delete(attachment.id)
        return next
      })
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
    <div className="seg-assets-attach">
      <div className="seg-assets-attach__head">
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
          className="seg-assets-attach__input"
          tabIndex={-1}
          onChange={(event) => {
            const chosen = event.target.files
            if (chosen != null) {
              for (const file of Array.from(chosen)) enqueue(file)
            }
            event.target.value = ''
          }}
          aria-label={t('editor.attachments.add')}
        />
      </div>

      {list.isPending ? (
        <div className="seg-assets-attach__status">
          <Spinner />
        </div>
      ) : list.isError ? (
        <p className="seg-assets-attach__error" role="alert">
          {t('editor.attachments.errors.loadFailed')}
        </p>
      ) : !hasContent ? (
        <p className="seg-assets-attach__empty">{t('editor.attachments.empty')}</p>
      ) : null}

      {hasContent && (
        <ul className="seg-assets-attach__list">
          {attachments.map((attachment) => {
            const image = isImageContentType(attachment.contentType)
            return (
              <li key={attachment.id} className="seg-assets-attach__item">
                {image ? (
                  <Image
                    size={18}
                    aria-hidden="true"
                    className="seg-assets-attach__icon"
                  />
                ) : (
                  <FileText
                    size={18}
                    aria-hidden="true"
                    className="seg-assets-attach__icon"
                  />
                )}
                <span className="seg-assets-attach__meta">
                  <span className="seg-assets-attach__name">{attachment.fileName}</span>
                  <span className="seg-assets-attach__size">
                    {formatFileSize(attachment.size)}
                    {attachment.isPrimary
                      ? ` · ${t('editor.attachments.primary')}`
                      : ''}
                  </span>
                </span>
                {image && (
                  <button
                    type="button"
                    className="seg-assets-attach__action"
                    onClick={() => void setPrimary(attachment)}
                    disabled={attachment.isPrimary || markingPrimary.has(attachment.id)}
                    aria-label={t('editor.attachments.makePrimary', {
                      name: attachment.fileName,
                    })}
                  >
                    {markingPrimary.has(attachment.id) ? (
                      <Spinner size={16} />
                    ) : (
                      <Star size={16} aria-hidden="true" />
                    )}
                  </button>
                )}
                <a
                  className="seg-assets-attach__action"
                  href={assetsApi.assetAttachmentDownloadUrl(assetId, attachment.id)}
                  download={attachment.fileName}
                  aria-label={t('editor.attachments.download', {
                    name: attachment.fileName,
                  })}
                >
                  <Download size={16} aria-hidden="true" />
                </a>
                <button
                  type="button"
                  className="seg-assets-attach__action seg-assets-attach__action--danger"
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
            )
          })}

          {tasks.map((task) => (
            <li
              key={`task-${task.id}`}
              className={
                'seg-assets-attach__item' +
                (task.status === 'error' ? ' seg-assets-attach__item--error' : '')
              }
            >
              <FileText
                size={18}
                aria-hidden="true"
                className="seg-assets-attach__icon"
              />
              <span className="seg-assets-attach__meta">
                <span className="seg-assets-attach__name">{task.file.name}</span>
                <span className="seg-assets-attach__size">
                  {task.status === 'uploading'
                    ? t('editor.attachments.uploading')
                    : messageForTask(task, t)}
                </span>
              </span>
              {task.status === 'uploading' ? (
                <span className="seg-assets-attach__action" aria-hidden="true">
                  <Spinner size={16} />
                </span>
              ) : (
                <>
                  <button
                    type="button"
                    className="seg-assets-attach__action"
                    onClick={() => retry(task)}
                    disabled={task.rejection != null}
                    aria-label={t('editor.attachments.retry')}
                  >
                    <RotateCw size={16} aria-hidden="true" />
                  </button>
                  <button
                    type="button"
                    className="seg-assets-attach__action"
                    onClick={() =>
                      setTasks((current) =>
                        current.filter((currentTask) => currentTask.id !== task.id),
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
