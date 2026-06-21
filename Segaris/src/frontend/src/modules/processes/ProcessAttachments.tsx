import { Download, FileText, Image, Paperclip, RotateCw, Trash2, X } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery, useQueryClient } from '@tanstack/react-query'

import { processesApi, type ProcessAttachment } from '@/app/api/processes'
import { Button, Spinner } from '@/components/ui'

import {
  attachmentAccept,
  formatFileSize,
  isImageContentType,
  rejectionFor,
  type AttachmentRejection,
} from './attachments'
import { processesKeys } from './contracts'

export interface ProcessAttachmentsProps {
  processId: number
  autoUpload?: File[]
  onChanged?: () => void
}

interface UploadTask {
  id: number
  file: File
  status: 'uploading' | 'error'
  rejection: AttachmentRejection
}

export function ProcessAttachments({
  processId,
  autoUpload,
  onChanged,
}: ProcessAttachmentsProps) {
  const { t } = useTranslation('processes')
  const queryClient = useQueryClient()
  const queryKey = processesKeys.attachments(processId)

  const list = useQuery({
    queryKey,
    queryFn: ({ signal }) => processesApi.listAttachments(processId, signal),
  })

  const [tasks, setTasks] = useState<UploadTask[]>([])
  const [removing, setRemoving] = useState<Set<string>>(new Set())
  const nextTaskId = useRef(0)
  const fileInput = useRef<HTMLInputElement>(null)

  const invalidate = async () => {
    await queryClient.invalidateQueries({ queryKey })
    await queryClient.invalidateQueries({ queryKey: processesKeys.process(processId) })
    await queryClient.invalidateQueries({ queryKey: processesKeys.all })
    onChanged?.()
  }

  const updateTask = (id: number, patch: Partial<UploadTask>) =>
    setTasks((current) =>
      current.map((task) => (task.id === id ? { ...task, ...patch } : task)),
    )

  const runUpload = async (taskUploadId: number, file: File) => {
    try {
      await processesApi.uploadAttachment(processId, file)
      setTasks((current) => current.filter((task) => task.id !== taskUploadId))
      await invalidate()
    } catch {
      updateTask(taskUploadId, { status: 'error', rejection: null })
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

  const remove = async (attachment: ProcessAttachment) => {
    setRemoving((current) => new Set(current).add(attachment.id))
    try {
      await processesApi.deleteAttachment(processId, attachment.id)
      await invalidate()
    } finally {
      setRemoving((current) => {
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
    <div className="seg-proc-attach">
      <div className="seg-proc-attach__head">
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
          className="seg-proc-attach__input"
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
        <div className="seg-proc-attach__status">
          <Spinner />
        </div>
      ) : list.isError ? (
        <p className="seg-proc-attach__error" role="alert">
          {t('editor.attachments.errors.loadFailed')}
        </p>
      ) : !hasContent ? (
        <p className="seg-proc-attach__empty">{t('editor.attachments.empty')}</p>
      ) : null}

      {hasContent && (
        <ul className="seg-proc-attach__list">
          {attachments.map((attachment) => {
            const image = isImageContentType(attachment.contentType)
            return (
              <li key={attachment.id} className="seg-proc-attach__item">
                {image ? (
                  <Image
                    size={18}
                    aria-hidden="true"
                    className="seg-proc-attach__icon"
                  />
                ) : (
                  <FileText
                    size={18}
                    aria-hidden="true"
                    className="seg-proc-attach__icon"
                  />
                )}
                <span className="seg-proc-attach__meta">
                  <span className="seg-proc-attach__name">{attachment.fileName}</span>
                  <span className="seg-proc-attach__size">
                    {formatFileSize(attachment.size)}
                  </span>
                </span>
                <a
                  className="seg-proc-attach__action"
                  href={processesApi.attachmentDownloadUrl(processId, attachment.id)}
                  download={attachment.fileName}
                  aria-label={t('editor.attachments.download', {
                    name: attachment.fileName,
                  })}
                >
                  <Download size={16} aria-hidden="true" />
                </a>
                <button
                  type="button"
                  className="seg-proc-attach__action seg-proc-attach__action--danger"
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
                'seg-proc-attach__item' +
                (task.status === 'error' ? ' seg-proc-attach__item--error' : '')
              }
            >
              <FileText
                size={18}
                aria-hidden="true"
                className="seg-proc-attach__icon"
              />
              <span className="seg-proc-attach__meta">
                <span className="seg-proc-attach__name">{task.file.name}</span>
                <span className="seg-proc-attach__size">
                  {task.status === 'uploading'
                    ? t('editor.attachments.uploading')
                    : messageForTask(task, t)}
                </span>
              </span>
              {task.status === 'uploading' ? (
                <span className="seg-proc-attach__action" aria-hidden="true">
                  <Spinner size={16} />
                </span>
              ) : (
                <>
                  <button
                    type="button"
                    className="seg-proc-attach__action"
                    onClick={() => retry(task)}
                    disabled={task.rejection != null}
                    aria-label={t('editor.attachments.retry')}
                  >
                    <RotateCw size={16} aria-hidden="true" />
                  </button>
                  <button
                    type="button"
                    className="seg-proc-attach__action"
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
