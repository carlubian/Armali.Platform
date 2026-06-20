import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Download, FileText, Paperclip, RotateCw, Trash2, X } from 'lucide-react'
import { useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'

import { projectsApi, type ProjectAttachment } from '@/app/api/projects'
import { Button, Spinner } from '@/components/ui'

import {
  attachmentAccept,
  formatFileSize,
  rejectionFor,
  type AttachmentRejection,
} from './attachments'
import { projectsKeys } from './contracts'

interface UploadTask {
  id: number
  file: File
  status: 'uploading' | 'error'
  rejection: AttachmentRejection
}

interface ProjectAttachmentsProps {
  projectId: number
  onChanged?: () => void
}

export function ProjectAttachments({ projectId, onChanged }: ProjectAttachmentsProps) {
  const { t } = useTranslation('projects')
  const queryClient = useQueryClient()
  const queryKey = projectsKeys.projectAttachments(projectId)
  const list = useQuery({
    queryKey,
    queryFn: ({ signal }) => projectsApi.listAttachments(projectId, signal),
  })
  const [tasks, setTasks] = useState<UploadTask[]>([])
  const [removing, setRemoving] = useState<Set<string>>(new Set())
  const nextTaskId = useRef(0)
  const fileInput = useRef<HTMLInputElement>(null)

  const invalidate = async () => {
    await queryClient.invalidateQueries({ queryKey })
    await queryClient.invalidateQueries({ queryKey: projectsKeys.project(projectId) })
    await queryClient.invalidateQueries({ queryKey: projectsKeys.tree() })
    onChanged?.()
  }

  const updateTask = (id: number, patch: Partial<UploadTask>) =>
    setTasks((current) =>
      current.map((task) => (task.id === id ? { ...task, ...patch } : task)),
    )

  const runUpload = async (taskId: number, file: File) => {
    try {
      await projectsApi.uploadAttachment(projectId, file)
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

  const retry = (task: UploadTask) => {
    const rejection = rejectionFor(task.file)
    if (rejection != null) {
      updateTask(task.id, { status: 'error', rejection })
      return
    }
    updateTask(task.id, { status: 'uploading', rejection: null })
    void runUpload(task.id, task.file)
  }

  const remove = async (attachment: ProjectAttachment) => {
    setRemoving((current) => new Set(current).add(attachment.id))
    try {
      await projectsApi.deleteAttachment(projectId, attachment.id)
      await invalidate()
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
    <div className="seg-projects-attach">
      <div className="seg-projects-attach__head">
        <Button
          variant="outline"
          size="sm"
          iconLeft={<Paperclip size={15} />}
          onClick={() => fileInput.current?.click()}
        >
          {t('attachments.add')}
        </Button>
        <input
          ref={fileInput}
          type="file"
          multiple
          accept={attachmentAccept}
          className="seg-projects-attach__input"
          tabIndex={-1}
          onChange={(event) => {
            const chosen = event.target.files
            if (chosen != null) {
              for (const file of Array.from(chosen)) enqueue(file)
            }
            event.target.value = ''
          }}
          aria-label={t('attachments.add')}
        />
      </div>

      {list.isPending ? (
        <div className="seg-projects-attach__status">
          <Spinner />
        </div>
      ) : list.isError ? (
        <p className="seg-projects-attach__error" role="alert">
          {t('attachments.errors.loadFailed')}
        </p>
      ) : !hasContent ? (
        <p className="seg-projects-attach__empty">{t('attachments.empty')}</p>
      ) : null}

      {hasContent && (
        <ul className="seg-projects-attach__list">
          {attachments.map((attachment) => (
            <li key={attachment.id} className="seg-projects-attach__item">
              <FileText
                size={18}
                aria-hidden="true"
                className="seg-projects-attach__icon"
              />
              <span className="seg-projects-attach__meta">
                <span className="seg-projects-attach__name">{attachment.fileName}</span>
                <span className="seg-projects-attach__size">
                  {formatFileSize(attachment.size)}
                </span>
              </span>
              <a
                className="seg-projects-attach__action"
                href={projectsApi.attachmentDownloadUrl(projectId, attachment.id)}
                download={attachment.fileName}
                aria-label={t('attachments.download', { name: attachment.fileName })}
              >
                <Download size={16} aria-hidden="true" />
              </a>
              <button
                type="button"
                className="seg-projects-attach__action seg-projects-attach__action--danger"
                onClick={() => void remove(attachment)}
                disabled={removing.has(attachment.id)}
                aria-label={t('attachments.remove')}
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
                'seg-projects-attach__item' +
                (task.status === 'error' ? ' seg-projects-attach__item--error' : '')
              }
            >
              <FileText
                size={18}
                aria-hidden="true"
                className="seg-projects-attach__icon"
              />
              <span className="seg-projects-attach__meta">
                <span className="seg-projects-attach__name">{task.file.name}</span>
                <span className="seg-projects-attach__size">
                  {task.status === 'uploading'
                    ? t('attachments.uploading')
                    : messageForTask(task, t)}
                </span>
              </span>
              {task.status === 'uploading' ? (
                <span className="seg-projects-attach__action" aria-hidden="true">
                  <Spinner size={16} />
                </span>
              ) : (
                <>
                  <button
                    type="button"
                    className="seg-projects-attach__action"
                    onClick={() => retry(task)}
                    disabled={task.rejection != null}
                    aria-label={t('attachments.retry')}
                  >
                    <RotateCw size={16} aria-hidden="true" />
                  </button>
                  <button
                    type="button"
                    className="seg-projects-attach__action"
                    onClick={() =>
                      setTasks((current) =>
                        current.filter((currentTask) => currentTask.id !== task.id),
                      )
                    }
                    aria-label={t('attachments.dismiss')}
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

function messageForTask(
  task: UploadTask,
  t: (key: string, options?: Record<string, unknown>) => string,
): string {
  switch (task.rejection) {
    case 'tooLarge':
      return t('attachments.errors.tooLarge')
    case 'type':
      return t('attachments.errors.type')
    default:
      return t('attachments.errors.failed')
  }
}
