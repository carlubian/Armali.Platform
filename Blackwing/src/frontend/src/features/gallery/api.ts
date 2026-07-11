import { csrf } from '@/features/upload/api'
import type { Tag, TagType } from '@/features/review/api'

export type ReviewStatus = 'All' | 'Pending' | 'Reviewed'
export type GalleryItem = { id: string; width: number; height: number; capturedAt: string | null; uploadedAt: string; effectiveCapturedAt: string; reviewed: boolean }
export type GalleryPage = { items: GalleryItem[]; nextCursor: string | null }
export type TagFacet = { id: string; type: TagType; value: string; count: number }
export type ImageDetail = { id: string; width: number; height: number; bytes: number; capturedAt: string | null; uploadedAt: string; reviewedAt: string | null; tags: Tag[] }

export async function browseGallery(params: { tags: string[]; status: ReviewStatus; cursor?: string | null }): Promise<GalleryPage> {
  const query = new URLSearchParams()
  for (const tag of params.tags) query.append('tag', tag)
  if (params.status !== 'All') query.set('status', params.status)
  if (params.cursor) query.set('cursor', params.cursor)
  const response = await fetch(`/api/images/?${query.toString()}`)
  if (!response.ok) throw new Error('Could not load the gallery.')
  const page = (await response.json()) as Partial<GalleryPage>
  return { items: page.items ?? [], nextCursor: page.nextCursor ?? null }
}

export async function fetchFacets(): Promise<TagFacet[]> {
  const response = await fetch('/api/tags/facets')
  if (!response.ok) return []
  return ((await response.json()) as { tags?: TagFacet[] }).tags ?? []
}

export async function fetchImage(id: string): Promise<ImageDetail> {
  const response = await fetch(`/api/images/${id}`)
  if (!response.ok) throw new Error('Could not load the image.')
  return response.json() as Promise<ImageDetail>
}

export async function saveImageTags(id: string, tags: Pick<Tag, 'type' | 'value'>[]): Promise<void> {
  const response = await fetch(`/api/images/${id}/tags`, { method: 'PUT', headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': await csrf() }, body: JSON.stringify({ tags, markReviewed: false }) })
  if (!response.ok) throw new Error('Could not save the tags.')
}

export async function deleteImage(id: string): Promise<void> {
  const response = await fetch(`/api/images/${id}`, { method: 'DELETE', headers: { 'X-CSRF-TOKEN': await csrf() } })
  if (!response.ok) throw new Error('Could not delete the image.')
}

export const thumbUrl = (id: string) => `/api/images/${id}/thumb`
export const previewUrl = (id: string) => `/api/images/${id}/preview`
export const originalUrl = (id: string) => `/api/images/${id}/original`
