import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { ArrowLeft, Download, Trash2 } from 'lucide-react'
import { TagEditor, tagTypes } from '@/features/tags'
import type { Tag } from '@/features/review/api'
import { deleteImage, fetchImage, originalUrl, previewUrl, saveImageTags } from './api'
import type { ImageDetail } from './api'

const formatBytes = (bytes: number) => {
  if (bytes < 1024) return `${bytes} B`
  const units = ['KB', 'MB', 'GB']
  let value = bytes / 1024
  let unit = 0
  while (value >= 1024 && unit < units.length - 1) { value /= 1024; unit += 1 }
  return `${value.toFixed(value >= 10 ? 0 : 1)} ${units[unit]}`
}
const dateLabel = (iso: string | null) => iso ? new Intl.DateTimeFormat(undefined, { dateStyle: 'long' }).format(new Date(iso)) : '—'

export function DetailPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const [image, setImage] = useState<ImageDetail | null>(null)
  const [tags, setTags] = useState<Tag[]>([])
  const [saving, setSaving] = useState(false)
  const [deleting, setDeleting] = useState(false)
  const [confirming, setConfirming] = useState(false)
  const [error, setError] = useState('')
  const [notFound, setNotFound] = useState(false)

  useEffect(() => {
    let cancelled = false
    const run = async () => {
      setError(''); setNotFound(false)
      try { const result = await fetchImage(id); if (!cancelled) { setImage(result); setTags(result.tags) } }
      catch { if (!cancelled) setNotFound(true) }
    }
    void run()
    return () => { cancelled = true }
  }, [id])

  const save = async () => {
    setSaving(true); setError('')
    try { await saveImageTags(id, tags); const fresh = await fetchImage(id); setImage(fresh); setTags(fresh.tags) }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'Could not save the tags.') }
    finally { setSaving(false) }
  }

  const remove = async () => {
    setDeleting(true); setError('')
    try { await deleteImage(id); navigate('/gallery') }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'Could not delete the image.'); setDeleting(false); setConfirming(false) }
  }

  if (notFound) return <section><div className="eyebrow">BLACKWING</div><h1>Image not found</h1><p>This image may have been deleted.</p><button className="link-button" onClick={() => navigate('/gallery')}><ArrowLeft size={16} /> Back to gallery</button></section>
  if (!image) return <section><div className="eyebrow">BLACKWING</div><h1>Image</h1><p className="loading">Loading…</p></section>

  return (
    <section className="detail-page">
      <button className="link-button detail-back" onClick={() => navigate(-1)}><ArrowLeft size={16} /> Back to gallery</button>
      {error && <p role="alert" className="form-error">{error}</p>}
      <div className="detail-layout">
        <div className="detail-canvas"><img src={previewUrl(image.id)} alt="Image preview" /></div>
        <aside className="detail-panel">
          <div>
            <div className="eyebrow">Image details</div>
            <span className={image.reviewedAt ? 'badge badge-reviewed' : 'badge badge-pending'}>{image.reviewedAt ? 'Reviewed' : 'Pending review'}</span>
          </div>
          <dl className="detail-meta">
            <div><dt>Captured</dt><dd>{dateLabel(image.capturedAt ?? image.uploadedAt)}</dd></div>
            <div><dt>Dimensions</dt><dd>{image.width} × {image.height}</dd></div>
            <div><dt>File size</dt><dd>{formatBytes(image.bytes)}</dd></div>
          </dl>
          <div className="detail-divider" />
          {tagTypes.map(config => <TagEditor key={config.type} {...config} tags={tags.filter(tag => tag.type === config.type)} onChange={next => setTags(current => [...current.filter(tag => tag.type !== config.type), ...next])} />)}
          <div className="detail-actions">
            <button className="button" disabled={saving} onClick={() => void save()}>{saving ? 'Saving…' : 'Save changes'}</button>
            <a className="button-outline" href={originalUrl(image.id)} download><Download size={16} /> Download original</a>
            {confirming ? (
              <div className="confirm-row">
                <span>Delete this image?</span>
                <button className="button-danger" disabled={deleting} onClick={() => void remove()}>{deleting ? 'Deleting…' : 'Delete'}</button>
                <button className="link-button" onClick={() => setConfirming(false)}>Cancel</button>
              </div>
            ) : (
              <button className="button-outline danger" onClick={() => setConfirming(true)}><Trash2 size={16} /> Delete image</button>
            )}
          </div>
        </aside>
      </div>
    </section>
  )
}
