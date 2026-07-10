import type { ReactNode } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { UploadCloud } from 'lucide-react'
const uploadSchema = z.object({ files: z.instanceof(FileList).optional() }); type UploadForm = z.infer<typeof uploadSchema>
function Header({ title, children }: { title: string; children: ReactNode }) { return <><div className="eyebrow">BLACKWING</div><h1>{title}</h1><p>{children}</p></> }
export function GalleryPage() { return <section><Header title="Gallery">Your private gallery will appear here once images are imported.</Header><div className="empty-state"><div className="empty-icon">⌑</div><h2>Your gallery is ready</h2><p>Upload images to start building your collection. Tags and filters arrive in the next product phases.</p></div></section> }
export function ReviewPage() { return <section><Header title="Pending review">Assign tags one image at a time, then move to the next.</Header><div className="empty-state"><div className="empty-icon success">✓</div><h2>You&apos;re all caught up</h2><p>New uploads will appear here for review.</p></div></section> }
export function UploadPage() { const { register, handleSubmit } = useForm<UploadForm>({ resolver: zodResolver(uploadSchema) }); return <section><Header title="Upload">Images land in pending review, whether you add one or hundreds at once.</Header><form onSubmit={handleSubmit(() => undefined)} className="upload-zone"><UploadCloud size={42} /><h2>Drag images here</h2><p>or browse your device — single or bulk</p><label className="button" htmlFor="files">Choose images</label><input id="files" type="file" multiple accept="image/jpeg,image/png,image/webp" {...register('files')} /></form><p className="hint">JPEG, PNG and WebP · maximum 100 MB per file</p></section> }
