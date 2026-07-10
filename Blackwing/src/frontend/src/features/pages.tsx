import type { ReactNode } from 'react'
function Header({ title, children }: { title: string; children: ReactNode }) { return <><div className="eyebrow">BLACKWING</div><h1>{title}</h1><p>{children}</p></> }
export function GalleryPage() { return <section><Header title="Gallery">Your private gallery will appear here once images are imported.</Header><div className="empty-state"><div className="empty-icon">⌑</div><h2>Your gallery is ready</h2><p>Upload images to start building your collection. Tags and filters arrive in the next product phases.</p></div></section> }
export function ReviewPage() { return <section><Header title="Pending review">Assign tags one image at a time, then move to the next.</Header><div className="empty-state"><div className="empty-icon success">✓</div><h2>You&apos;re all caught up</h2><p>New uploads will appear here for review.</p></div></section> }
export { UploadPage } from './upload/UploadPage'
