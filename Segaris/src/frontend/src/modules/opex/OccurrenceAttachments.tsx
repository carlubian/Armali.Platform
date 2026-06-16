import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Download, FileText, Paperclip, RotateCw, Trash2, X } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'

import { opexApi, type OpexAttachment } from '@/app/api/opex'
import { Button, Spinner } from '@/components/ui'

import {
  attachmentAccept,
  formatFileSize,
  rejectionFor,
  type AttachmentRejection,
} from './attachments'
import { opexKeys } from './queries'

export interface OccurrenceAttachmentsProps {
  contractId: number
  occurrenceId: number
  autoUpload?: File[]
}

interface UploadTask {
  id: number
  file: File
  status: 'uploading' | 'error'
  rejection: AttachmentRejection
}

export function OccurrenceAttachments({
  contractId,
  occurrenceId,
  autoUpload,
}: OccurrenceAttachmentsProps) {
  const { t } = useTranslation('opex')
  const queryClient = useQueryClient()

  const list = useQuery({
    queryKey: opexKeys.occurrenceAttachments(contractId, occurrenceId),
    queryFn: ({ signal }) =>
      opexApi.listOccurrenceAttachments(contractId, occurrenceId, signal),
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
      await opexApi.uploadOccurrenceAttachment(contractId, occurrenceId, file)
      setTasks((current) => current.filter((task) => task.id !== taskId))
      await queryClient.invalidateQueries({
        queryKey: opexKeys.occurrenceAttachments(contractId, occurrenceId),
      })
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

  const remove = async (attachment: OpexAttachment) => {
    setRemoving((current) => new Set(current).add(attachment.id))
    try {
      await opexApi.deleteOccurrenceAttachment(contractId, occurrenceId, attachment.id)
      await queryClient.invalidateQueries({
        queryKey: opexKeys.occurrenceAttachments(contractId, occurrenceId),
      })
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
    <div className="seg-opex-attach">
      <div className="seg-opex-attach__head">
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
          className="seg-opex-attach__input"
          tabIndex={-1}
          onChange={onFilesChosen}
          aria-label={t('editor.attachments.add')}
        />
      </div>

      {list.isPending ? (
        <div className="seg-opex-attach__status">
          <Spinner />
        </div>
      ) : list.isError ? (
        <p className="seg-opex-attach__error" role="alert">
          {t('editor.attachments.errors.loadFailed')}
        </p>
      ) : !hasContent ? (
        <p className="seg-opex-attach__empty">{t('editor.attachments.empty')}</p>
      ) : null}

      {hasContent && (
        <ul className="seg-opex-attach__list">
          {attachments.map((attachment) => (
            <li key={attachment.id} className="seg-opex-attach__item">
              <FileText size={18} aria-hidden="true" className="seg-opex-attach__icon" />
              <span className="seg-opex-attach__meta">
                <span className="seg-opex-attach__name">{attachment.fileName}</span>
                <span className="seg-opex-attach__size">
                  {formatFileSize(attachment.size)}
                </span>
              </span>
              <a
                className="seg-opex-attach__action"
                href={opexApi.occurrenceAttachmentDownloadUrl(
                  contractId,
                  occurrenceId,
                  attachment.id,
                )}
                download={attachment.fileName}
                aria-label={t('editor.attachments.download', {
                  name: attachment.fileName,
                })}
              >
                <Download size={16} aria-hidden="true" />
              </a>
              <button
                type="button"
                className="seg-opex-attach__action seg-opex-attach__action--danger"
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
                'seg-opex-attach__item' +
                (task.status === 'error' ? ' seg-opex-attach__item--error' : '')
              }
            >
              <FileText size={18} aria-hidden="true" className="seg-opex-attach__icon" />
              <span className="seg-opex-attach__meta">
                <span className="seg-opex-attach__name">{task.file.name}</span>
                <span className="seg-opex-attach__size">
                  {task.status === 'uploading'
                    ? t('editor.attachments.uploading')
                    : messageForTask(task, t)}
                </span>
              </span>
              {task.status === 'uploading' ? (
                <span className="seg-opex-attach__action" aria-hidden="true">
                  <Spinner size={16} />
                </span>
              ) : (
                <>
                  <button
                    type="button"
                    className="seg-opex-attach__action"
                    onClick={() => retry(task)}
                    disabled={task.rejection != null}
                    aria-label={t('editor.attachments.retry')}
                  >
                    <RotateCw size={16} aria-hidden="true" />
                  </button>
                  <button
                    type="button"
                    className="seg-opex-attach__action"
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
