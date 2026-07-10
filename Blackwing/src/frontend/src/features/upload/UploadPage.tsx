import { useCallback, useEffect, useRef, useState } from 'react'
import type { ChangeEvent, DragEvent } from 'react'
import { AlertTriangle, CheckCircle2, Copy, RotateCw, UploadCloud } from 'lucide-react'
import { ACCEPTED_TYPES, MAX_FILE_BYTES, csrf, fetchJobs, retryJob, uploadFile } from './api'

type ItemStatus = 'uploading' | 'processing' | 'done' | 'duplicate' | 'rejected' | 'failed'

type Item = {
  id: string
  name: string
  sizeLabel: string
  previewUrl: string
  status: ItemStatus
  progress: number
  jobId?: string
  reason?: string | null
  recoverable?: boolean
}

const REASON_LABELS: Record<string, string> = {
  too_large: 'Too large — the limit is 100 MB',
  unsupported_format: 'Unsupported format — use JPEG, PNG or WebP',
  invalid_image: 'The file could not be read as an image',
  processing_error: 'Processing failed — you can retry',
  storage_error: 'Could not be stored — you can retry',
  staging_missing: 'The upload expired before it was processed',
}

function reasonLabel(reason?: string | null) {
  return (reason && REASON_LABELS[reason]) || 'Upload failed'
}

function formatSize(bytes: number) {
  if (bytes >= 1_048_576) return `${(bytes / 1_048_576).toFixed(1)} MB`
  return `${Math.max(1, Math.round(bytes / 1024))} KB`
}

export function UploadPage() {
  const [items, setItems] = useState<Item[]>([])
  const [dragActive, setDragActive] = useState(false)
  const inputRef = useRef<HTMLInputElement>(null)

  const update = useCallback((id: string, patch: Partial<Item>) => {
    setItems((current) => current.map((item) => (item.id === id ? { ...item, ...patch } : item)))
  }, [])

  const addFiles = useCallback(
    async (fileList: FileList | null) => {
      const files = Array.from(fileList ?? [])
      if (files.length === 0) return

      const accepted: { id: string; file: File }[] = []
      const created: Item[] = files.map((file) => {
        const id = crypto.randomUUID()
        const base: Item = {
          id,
          name: file.name,
          sizeLabel: formatSize(file.size),
          previewUrl: URL.createObjectURL(file),
          status: 'uploading',
          progress: 0,
        }
        if (!ACCEPTED_TYPES.includes(file.type as (typeof ACCEPTED_TYPES)[number]))
          return { ...base, status: 'rejected', reason: 'unsupported_format' }
        if (file.size > MAX_FILE_BYTES) return { ...base, status: 'rejected', reason: 'too_large' }
        accepted.push({ id, file })
        return base
      })

      setItems((current) => [...created, ...current])
      if (accepted.length === 0) return

      const token = await csrf()
      accepted.forEach(({ id, file }) => {
        uploadFile(file, token, (percent) => update(id, { progress: percent }))
          .then((result) => {
            if (result.status === 'accepted') update(id, { status: 'processing', progress: 100, jobId: result.jobId ?? undefined })
            else if (result.status === 'duplicate') update(id, { status: 'duplicate', progress: 100 })
            else update(id, { status: 'rejected', reason: result.reason })
          })
          .catch(() => update(id, { status: 'failed', reason: 'storage_error', recoverable: false }))
      })
    },
    [update],
  )

  // Poll the worker while any file is still being processed, reconciling each item
  // with its job's outcome. The interval tears down once nothing is in flight.
  useEffect(() => {
    if (!items.some((item) => item.status === 'processing')) return
    const timer = window.setInterval(async () => {
      let jobs
      try {
        jobs = await fetchJobs()
      } catch {
        return
      }
      setItems((current) =>
        current.map((item) => {
          if (item.status !== 'processing' || !item.jobId) return item
          const job = jobs.find((candidate) => candidate.id === item.jobId)
          if (!job) return item
          if (job.status === 'Completed') return { ...item, status: 'done' }
          if (job.status === 'Duplicate') return { ...item, status: 'duplicate' }
          if (job.status === 'Failed') return { ...item, status: 'failed', reason: job.failureCode, recoverable: job.recoverable }
          return item
        }),
      )
    }, 1200)
    return () => window.clearInterval(timer)
  }, [items])

  useEffect(() => () => items.forEach((item) => URL.revokeObjectURL(item.previewUrl)), [items])

  const retry = useCallback(
    async (item: Item) => {
      if (!item.jobId) return
      update(item.id, { status: 'processing', progress: 100, reason: null })
      try {
        await retryJob(item.jobId)
      } catch {
        update(item.id, { status: 'failed', reason: 'processing_error', recoverable: true })
      }
    },
    [update],
  )

  function onDrop(event: DragEvent<HTMLDivElement>) {
    event.preventDefault()
    setDragActive(false)
    void addFiles(event.dataTransfer.files)
  }

  function onInputChange(event: ChangeEvent<HTMLInputElement>) {
    void addFiles(event.target.files)
    event.target.value = ''
  }

  return (
    <section>
      <div className="eyebrow">BLACKWING</div>
      <h1>Upload</h1>
      <p>Images land in pending review, whether you add one or hundreds at once. Tags are assigned afterward, in Review.</p>

      <div
        className={dragActive ? 'upload-zone drag-active' : 'upload-zone'}
        onDragOver={(event) => {
          event.preventDefault()
          setDragActive(true)
        }}
        onDragLeave={(event) => {
          event.preventDefault()
          setDragActive(false)
        }}
        onDrop={onDrop}
      >
        <span className="upload-badge">
          <UploadCloud size={26} />
        </span>
        <h2>Drag images here</h2>
        <p>or browse your device — single or bulk</p>
        <button type="button" className="button upload-browse" onClick={() => inputRef.current?.click()}>
          Browse files
        </button>
        <input
          ref={inputRef}
          type="file"
          multiple
          accept="image/jpeg,image/png,image/webp"
          onChange={onInputChange}
        />
      </div>
      <p className="hint">JPEG, PNG and WebP · maximum 100 MB per file</p>

      {items.length > 0 && (
        <ul className="upload-list">
          {items.map((item) => (
            <li key={item.id} className="upload-item">
              <img className="upload-thumb" src={item.previewUrl} alt="" />
              <div className="upload-meta">
                <div className="upload-name">{item.name}</div>
                <div className="upload-sub">{item.sizeLabel}</div>
                {(item.status === 'uploading' || item.status === 'processing') && (
                  <div className="upload-track">
                    <div
                      className={item.status === 'processing' ? 'upload-fill indeterminate' : 'upload-fill'}
                      style={{ width: `${item.progress}%` }}
                    />
                  </div>
                )}
              </div>
              <div className="upload-status">
                <StatusLabel item={item} onRetry={() => retry(item)} />
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}

function StatusLabel({ item, onRetry }: { item: Item; onRetry: () => void }) {
  switch (item.status) {
    case 'uploading':
      return <span className="status-muted">Uploading… {item.progress}%</span>
    case 'processing':
      return <span className="status-muted">Processing…</span>
    case 'done':
      return (
        <span className="status-ok">
          <CheckCircle2 size={15} /> Added to pending review
        </span>
      )
    case 'duplicate':
      return (
        <span className="status-muted">
          <Copy size={15} /> Already in your gallery
        </span>
      )
    case 'rejected':
      return (
        <span className="status-error">
          <AlertTriangle size={15} /> {reasonLabel(item.reason)}
        </span>
      )
    case 'failed':
      return (
        <span className="status-error">
          <AlertTriangle size={15} /> {reasonLabel(item.reason)}
          {item.recoverable && (
            <button type="button" className="link-button" onClick={onRetry}>
              <RotateCw size={14} /> Retry
            </button>
          )}
        </span>
      )
  }
}
