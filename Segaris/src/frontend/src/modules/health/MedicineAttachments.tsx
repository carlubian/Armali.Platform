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

import { healthApi, type MedicineAttachment } from '@/app/api/health'
import { Button, Spinner } from '@/components/ui'
import {
  attachmentAccept,
  formatFileSize,
  rejectionFor,
  type AttachmentRejection,
} from '@/modules/clothes/attachments'

import { healthKeys } from './contracts'

interface UploadTask {
  id: number
  file: File
  status: 'uploading' | 'error'
  rejection: AttachmentRejection
}

export interface MedicineAttachmentsProps {
  medicineId: number
  autoUpload?: File[]
}

export function MedicineAttachments({
  medicineId,
  autoUpload,
}: MedicineAttachmentsProps) {
  const { t } = useTranslation('health')
  const queryClient = useQueryClient()
  const queryKey = healthKeys.medicineAttachments(medicineId)
  const list = useQuery({
    queryKey,
    queryFn: ({ signal }) => healthApi.listMedicineAttachments(medicineId, signal),
  })

  const [tasks, setTasks] = useState<UploadTask[]>([])
  const [removing, setRemoving] = useState<Set<string>>(new Set())
  const [primaryBusy, setPrimaryBusy] = useState<string | null>(null)
  const [primaryError, setPrimaryError] = useState(false)
  const nextTaskId = useRef(0)
  const fileInput = useRef<HTMLInputElement>(null)

  const invalidate = async () => {
    await queryClient.invalidateQueries({ queryKey })
    await queryClient.invalidateQueries({ queryKey: healthKeys.medicine(medicineId) })
    await queryClient.invalidateQueries({ queryKey: healthKeys.medicines() })
  }

  const updateTask = (id: number, patch: Partial<UploadTask>) =>
    setTasks((current) =>
      current.map((task) => (task.id === id ? { ...task, ...patch } : task)),
    )

  const runUpload = async (taskId: number, file: File) => {
    try {
      await healthApi.uploadMedicineAttachment(medicineId, file)
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

  const remove = async (attachment: MedicineAttachment) => {
    setRemoving((current) => new Set(current).add(attachment.id))
    try {
      await healthApi.deleteMedicineAttachment(medicineId, attachment.id)
      await invalidate()
    } finally {
      setRemoving((current) => {
        const next = new Set(current)
        next.delete(attachment.id)
        return next
      })
    }
  }

  const makePrimary = async (attachment: MedicineAttachment) => {
    setPrimaryError(false)
    setPrimaryBusy(attachment.id)
    try {
      await healthApi.setPrimaryMedicineAttachment(medicineId, attachment.id)
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

  const onFilesChosen = (event: React.ChangeEvent<HTMLInputElement>) => {
    const chosen = event.target.files
    if (chosen != null) {
      for (const file of Array.from(chosen)) enqueue(file)
    }
    event.target.value = ''
  }

  const dismiss = (taskId: number) =>
    setTasks((current) => current.filter((task) => task.id !== taskId))

  const attachments = list.data ?? []
  const hasContent = attachments.length > 0 || tasks.length > 0

  return (
    <div className="seg-health-attach">
      <div className="seg-health-attach__head">
        <Button
          variant="outline"
          size="sm"
          iconLeft={<Paperclip size={15} />}
          onClick={() => fileInput.current?.click()}
        >
          {t('medicineEditor.attachments.add')}
        </Button>
        <input
          ref={fileInput}
          type="file"
          multiple
          accept={attachmentAccept}
          className="seg-health-attach__input"
          tabIndex={-1}
          onChange={onFilesChosen}
          aria-label={t('medicineEditor.attachments.add')}
        />
      </div>

      {primaryError && (
        <p className="seg-health-attach__error" role="alert">
          {t('medicineEditor.attachments.errors.primaryFailed')}
        </p>
      )}

      {list.isPending ? (
        <div className="seg-health-attach__status">
          <Spinner />
        </div>
      ) : list.isError ? (
        <p className="seg-health-attach__error" role="alert">
          {t('medicineEditor.attachments.errors.loadFailed')}
        </p>
      ) : !hasContent ? (
        <p className="seg-health-attach__empty">
          {t('medicineEditor.attachments.empty')}
        </p>
      ) : null}

      {hasContent && (
        <ul className="seg-health-attach__list">
          {attachments.map((attachment) => (
            <li key={attachment.id} className="seg-health-attach__item">
              {attachment.contentType.startsWith('image/') ? (
                <Image size={18} aria-hidden="true" />
              ) : (
                <FileText size={18} aria-hidden="true" />
              )}
              <span className="seg-health-attach__meta">
                <span className="seg-health-attach__name">{attachment.fileName}</span>
                <span className="seg-health-attach__size">
                  {formatFileSize(attachment.size)}
                </span>
              </span>
              {attachment.isPrimary ? (
                <span className="seg-health-attach__primary">
                  <Star size={14} aria-hidden="true" />
                  {t('medicineEditor.attachments.primary')}
                </span>
              ) : attachment.contentType.startsWith('image/') ? (
                <button
                  type="button"
                  className="seg-health-attach__action"
                  onClick={() => void makePrimary(attachment)}
                  disabled={primaryBusy != null}
                  aria-label={t('medicineEditor.attachments.makePrimary')}
                >
                  {primaryBusy === attachment.id ? (
                    <Spinner size={16} />
                  ) : (
                    <Star size={16} aria-hidden="true" />
                  )}
                </button>
              ) : null}
              <a
                className="seg-health-attach__action"
                href={healthApi.medicineAttachmentDownloadUrl(
                  medicineId,
                  attachment.id,
                )}
                download={attachment.fileName}
                aria-label={t('medicineEditor.attachments.download', {
                  name: attachment.fileName,
                })}
              >
                <Download size={16} aria-hidden="true" />
              </a>
              <button
                type="button"
                className="seg-health-attach__action seg-health-attach__action--danger"
                onClick={() => void remove(attachment)}
                disabled={removing.has(attachment.id)}
                aria-label={t('medicineEditor.attachments.remove')}
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
                'seg-health-attach__item' +
                (task.status === 'error' ? ' seg-health-attach__item--error' : '')
              }
            >
              <FileText size={18} aria-hidden="true" />
              <span className="seg-health-attach__meta">
                <span className="seg-health-attach__name">{task.file.name}</span>
                <span className="seg-health-attach__size">
                  {task.status === 'uploading'
                    ? t('medicineEditor.attachments.uploading')
                    : messageForTask(task, t)}
                </span>
              </span>
              {task.status === 'uploading' ? (
                <span className="seg-health-attach__action" aria-hidden="true">
                  <Spinner size={16} />
                </span>
              ) : (
                <>
                  <button
                    type="button"
                    className="seg-health-attach__action"
                    onClick={() => retry(task)}
                    disabled={task.rejection != null}
                    aria-label={t('medicineEditor.attachments.retry')}
                  >
                    <RotateCw size={16} aria-hidden="true" />
                  </button>
                  <button
                    type="button"
                    className="seg-health-attach__action"
                    onClick={() => dismiss(task.id)}
                    aria-label={t('medicineEditor.attachments.dismiss')}
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
      return t('medicineEditor.attachments.errors.tooLarge')
    case 'type':
      return t('medicineEditor.attachments.errors.type')
    default:
      return t('medicineEditor.attachments.errors.failed')
  }
}
