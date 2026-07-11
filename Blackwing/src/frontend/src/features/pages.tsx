import { useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import { Check, ChevronRight } from 'lucide-react'
import { fetchReview, saveReview } from './review/api'
import type { ImageForReview, Tag } from './review/api'
import { TagEditor, tagTypes } from './tags'
function Header({ title, children }: { title: string; children: ReactNode }) { return <><div className="eyebrow">BLACKWING</div><h1>{title}</h1><p>{children}</p></> }
export function ReviewPage() {
  const [data, setData] = useState<{ pendingCount: number; image: ImageForReview | null } | null>(null); const [tags, setTags] = useState<Tag[]>([]); const [saving, setSaving] = useState(false); const [error, setError] = useState('')
  const load = () => fetchReview().then(result => { setData(result); setTags(result.image?.tags ?? []); setError('') }).catch(reason => setError(reason instanceof Error ? reason.message : 'Could not load the review queue.'))
  useEffect(() => { void load() }, [])
  const save = async () => { if (!data?.image) return; setSaving(true); try { await saveReview(data.image.id, tags); await load() } catch (reason) { setError(reason instanceof Error ? reason.message : 'Could not save the review.') } finally { setSaving(false) } }
  if (!data) return <section><Header title="Pending review">Assign tags one image at a time, then move to the next.</Header><p className="loading">Loading review queue…</p></section>
  if (!data.image) return <section><Header title="Pending review">Assign tags one image at a time, then move to the next.</Header><div className="empty-state"><div className="empty-icon success">✓</div><h2>You&apos;re all caught up</h2><p>New uploads will appear here for review.</p></div></section>
  const image = data.image
  return <section className="review-page"><div className="eyebrow">PENDING REVIEW · {data.pendingCount} LEFT</div><h1>Review image</h1><p>Assign tags, or simply mark it reviewed when there is nothing to add.</p>{error && <p role="alert" className="form-error">{error}</p>}<div className="review-layout"><div className="review-preview"><img src={`/api/images/${image.id}/preview`} alt="Image waiting for review" /><dl><div><dt>Uploaded</dt><dd>{new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' }).format(new Date(image.uploadedAt))}</dd></div><div><dt>Dimensions</dt><dd>{image.width} × {image.height}</dd></div></dl></div><div className="review-form">{tagTypes.map(config => <TagEditor key={config.type} {...config} tags={tags.filter(tag => tag.type === config.type)} onChange={next => setTags(current => [...current.filter(tag => tag.type !== config.type), ...next])} />)}<button className="review-submit" disabled={saving} onClick={() => void save()}><Check size={18} />{saving ? 'Saving…' : 'Mark reviewed & next'}<ChevronRight size={17} /></button></div></div></section>
}
export { UploadPage } from './upload/UploadPage'
export { GalleryPage } from './gallery/GalleryPage'
export { DetailPage } from './gallery/DetailPage'
