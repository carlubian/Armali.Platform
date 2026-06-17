import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Download, FileText, Paperclip, RotateCw, Trash2, X } from 'lucide-react'
import { useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'

import { travelApi, type TravelAttachment } from '@/app/api/travel'
import { Button, Spinner } from '@/components/ui'

import {
  attachmentAccept,
  formatFileSize,
  rejectionFor,
  type AttachmentRejection,
} from './attachments'
import { travelKeys } from './contracts'

export type AttachmentOwner =
  | { kind: 'trip'; tripId: number }
  | { kind: 'expense'; tripId: number; expenseId: number }

export interface TravelAttachmentsProps {
  owner: AttachmentOwner
}

interface UploadTask {
  id: number
  file: File
  status: 'uploading' | 'error'
  rejection: AttachmentRejection
}

/** Resolves the API surface and cache key for the chosen owner. */
function bindOwner(owner: AttachmentOwner) {
  if (owner.kind === 'trip') {
    const { tripId } = owner
    return {
      queryKey: travelKeys.tripAttachments(tripId),
      list: (signal?: AbortSignal) => travelApi.listTripAttachments(tripId, signal),
      upload: (file: File) => travelApi.uploadTripAttachment(tripId, file),
      downloadUrl: (attachmentId: string) =>
        travelApi.tripAttachmentDownloadUrl(tripId, attachmentId),
      remove: (attachmentId: string) =>
        travelApi.deleteTripAttachment(tripId, attachmentId),
    }
  }
  const { tripId, expenseId } = owner
  return {
    queryKey: travelKeys.expenseAttachments(tripId, expenseId),
    list: (signal?: AbortSignal) =>
      travelApi.listExpenseAttachments(tripId, expenseId, signal),
    upload: (file: File) => travelApi.uploadExpenseAttachment(tripId, expenseId, file),
    downloadUrl: (attachmentId: string) =>
      travelApi.expenseAttachmentDownloadUrl(tripId, expenseId, attachmentId),
    remove: (attachmentId: string) =>
      travelApi.deleteExpenseAttachment(tripId, expenseId, attachmentId),
  }
}

export function TravelAttachments({ owner }: TravelAttachmentsProps) {
  const { t } = useTranslation('travel')
  const queryClient = useQueryClient()
  const bound = bindOwner(owner)

  const list = useQuery({
    queryKey: bound.queryKey,
    queryFn: ({ signal }) => bound.list(signal),
  })

  const [tasks, setTasks] = useState<UploadTask[]>([])
  const [removing, setRemoving] = useState<Set<string>>(new Set())
  const nextTaskId = useRef(0)
  const fileInput = useRef<HTMLInputElement>(null)

  const updateTask = (id: number, patch: Partial<UploadTask>) =>
    setTasks((current) =>
      current.map((task) => (task.id === id ? { ...task, ...patch } : task)),
    )

  const runUpload = async (taskId: number, file: File) => {
    try {
      await bound.upload(file)
      setTasks((current) => current.filter((task) => task.id !== taskId))
      await queryClient.invalidateQueries({ queryKey: bound.queryKey })
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

  const dismiss = (taskId: number) =>
    setTasks((current) => current.filter((task) => task.id !== taskId))

  const remove = async (attachment: TravelAttachment) => {
    setRemoving((current) => new Set(current).add(attachment.id))
    try {
      await bound.remove(attachment.id)
      await queryClient.invalidateQueries({ queryKey: bound.queryKey })
    } finally {
      setRemoving((current) => {
        const next = new Set(current)
        next.delete(attachment.id)
        return next
      })
    }
  }

  const attachments = list.data ?? []
  const hasContent = attachments.length > 0 || tasks.length > 0

  return (
    <div className="seg-trv-attach">
      <div className="seg-trv-attach__head">
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
          className="seg-trv-attach__input"
          tabIndex={-1}
          onChange={onFilesChosen}
          aria-label={t('editor.attachments.add')}
        />
      </div>

      {list.isPending ? (
        <div className="seg-trv-attach__status">
          <Spinner />
        </div>
      ) : list.isError ? (
        <p className="seg-trv-attach__error" role="alert">
          {t('editor.attachments.errors.loadFailed')}
        </p>
      ) : !hasContent ? (
        <p className="seg-trv-attach__empty">{t('editor.attachments.empty')}</p>
      ) : null}

      {hasContent && (
        <ul className="seg-trv-attach__list">
          {attachments.map((attachment) => (
            <li key={attachment.id} className="seg-trv-attach__item">
              <FileText size={18} aria-hidden="true" className="seg-trv-attach__icon" />
              <span className="seg-trv-attach__meta">
                <span className="seg-trv-attach__name">{attachment.fileName}</span>
                <span className="seg-trv-attach__size">
                  {formatFileSize(attachment.size)}
                </span>
              </span>
              <a
                className="seg-trv-attach__action"
                href={bound.downloadUrl(attachment.id)}
                download={attachment.fileName}
                aria-label={t('editor.attachments.download', {
                  name: attachment.fileName,
                })}
              >
                <Download size={16} aria-hidden="true" />
              </a>
              <button
                type="button"
                className="seg-trv-attach__action seg-trv-attach__action--danger"
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
                'seg-trv-attach__item' +
                (task.status === 'error' ? ' seg-trv-attach__item--error' : '')
              }
            >
              <FileText size={18} aria-hidden="true" className="seg-trv-attach__icon" />
              <span className="seg-trv-attach__meta">
                <span className="seg-trv-attach__name">{task.file.name}</span>
                <span className="seg-trv-attach__size">
                  {task.status === 'uploading'
                    ? t('editor.attachments.uploading')
                    : messageForTask(task, t)}
                </span>
              </span>
              {task.status === 'uploading' ? (
                <span className="seg-trv-attach__action" aria-hidden="true">
                  <Spinner size={16} />
                </span>
              ) : (
                <>
                  <button
                    type="button"
                    className="seg-trv-attach__action"
                    onClick={() => retry(task)}
                    disabled={task.rejection != null}
                    aria-label={t('editor.attachments.retry')}
                  >
                    <RotateCw size={16} aria-hidden="true" />
                  </button>
                  <button
                    type="button"
                    className="seg-trv-attach__action"
                    onClick={() => dismiss(task.id)}
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
