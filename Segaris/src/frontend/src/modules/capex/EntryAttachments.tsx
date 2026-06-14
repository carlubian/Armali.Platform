import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Download, FileText, Paperclip, RotateCw, Trash2, X } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'

import { capexApi, type CapexAttachment } from '@/app/api/capex'
import { Button, Spinner } from '@/components/ui'

import {
  attachmentAccept,
  formatFileSize,
  rejectionFor,
  type AttachmentRejection,
} from './attachments'
import { capexKeys } from './queries'

export interface EntryAttachmentsProps {
  /** The owning entry. Attachments act against this entry immediately. */
  entryId: number
  /**
   * Files to upload automatically the first time the component mounts. Used by
   * the post-create flow to upload files staged before the entry existed.
   */
  autoUpload?: File[]
}

/** A client-side upload in flight or failed. Successful uploads leave the list. */
interface UploadTask {
  id: number
  file: File
  status: 'uploading' | 'error'
  /** Set when the file was rejected before any request was made. */
  rejection: AttachmentRejection
}

/**
 * Live attachment manager for an existing entry. It lists the entry's
 * attachments, uploads new files with per-file state and retry, offers
 * authenticated downloads, and removes attachments. Each upload is an
 * independent task so a single failure never blocks the others (Wave 7).
 */
export function EntryAttachments({ entryId, autoUpload }: EntryAttachmentsProps) {
  const { t } = useTranslation('capex')
  const queryClient = useQueryClient()

  const list = useQuery({
    queryKey: capexKeys.attachments(entryId),
    queryFn: ({ signal }) => capexApi.listAttachments(entryId, signal),
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
      await capexApi.uploadAttachment(entryId, file)
      // Drop the task and let the refreshed list surface the new attachment.
      setTasks((current) => current.filter((task) => task.id !== taskId))
      await queryClient.invalidateQueries({ queryKey: capexKeys.attachments(entryId) })
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
    // Reset so choosing the same file again re-triggers the change event.
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

  const remove = async (attachment: CapexAttachment) => {
    setRemoving((current) => new Set(current).add(attachment.id))
    try {
      await capexApi.deleteAttachment(entryId, attachment.id)
      await queryClient.invalidateQueries({ queryKey: capexKeys.attachments(entryId) })
    } finally {
      setRemoving((current) => {
        const next = new Set(current)
        next.delete(attachment.id)
        return next
      })
    }
  }

  // Auto-upload the staged files exactly once on mount (post-create flow).
  const autoUploaded = useRef(false)
  useEffect(() => {
    if (autoUploaded.current || autoUpload == null || autoUpload.length === 0) return
    autoUploaded.current = true
    for (const file of autoUpload) enqueue(file)
    // enqueue is stable for our purposes; this effect must run only once.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const attachments = list.data ?? []
  const hasContent = attachments.length > 0 || tasks.length > 0

  return (
    <div className="seg-capex-attach">
      <div className="seg-capex-attach__head">
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
          className="seg-capex-attach__input"
          // The visible button is the keyboard trigger; keep the hidden input
          // out of the tab order while still reachable for its accessible name.
          tabIndex={-1}
          onChange={onFilesChosen}
          aria-label={t('editor.attachments.add')}
        />
      </div>

      {list.isPending ? (
        <div className="seg-capex-attach__status">
          <Spinner />
        </div>
      ) : list.isError ? (
        <p className="seg-capex-attach__error" role="alert">
          {t('editor.attachments.errors.loadFailed')}
        </p>
      ) : !hasContent ? (
        <p className="seg-capex-attach__empty">{t('editor.attachments.empty')}</p>
      ) : null}

      {hasContent && (
        <ul className="seg-capex-attach__list">
          {attachments.map((attachment) => (
            <li key={attachment.id} className="seg-capex-attach__item">
              <FileText
                size={18}
                aria-hidden="true"
                className="seg-capex-attach__icon"
              />
              <span className="seg-capex-attach__meta">
                <span className="seg-capex-attach__name">{attachment.fileName}</span>
                <span className="seg-capex-attach__size">
                  {formatFileSize(attachment.size)}
                </span>
              </span>
              <a
                className="seg-capex-attach__action"
                href={capexApi.attachmentDownloadUrl(entryId, attachment.id)}
                download={attachment.fileName}
                aria-label={t('editor.attachments.download', {
                  name: attachment.fileName,
                })}
              >
                <Download size={16} aria-hidden="true" />
              </a>
              <button
                type="button"
                className="seg-capex-attach__action seg-capex-attach__action--danger"
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
                'seg-capex-attach__item' +
                (task.status === 'error' ? ' seg-capex-attach__item--error' : '')
              }
            >
              <FileText
                size={18}
                aria-hidden="true"
                className="seg-capex-attach__icon"
              />
              <span className="seg-capex-attach__meta">
                <span className="seg-capex-attach__name">{task.file.name}</span>
                <span className="seg-capex-attach__size">
                  {task.status === 'uploading'
                    ? t('editor.attachments.uploading')
                    : messageForTask(task, t)}
                </span>
              </span>
              {task.status === 'uploading' ? (
                <span className="seg-capex-attach__action" aria-hidden="true">
                  <Spinner size={16} />
                </span>
              ) : (
                <>
                  <button
                    type="button"
                    className="seg-capex-attach__action"
                    onClick={() => retry(task)}
                    disabled={task.rejection != null}
                    aria-label={t('editor.attachments.retry')}
                  >
                    <RotateCw size={16} aria-hidden="true" />
                  </button>
                  <button
                    type="button"
                    className="seg-capex-attach__action"
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
