import { csrf } from '@/features/upload/api'

export type TagType = 'Person' | 'Place' | 'Topic'
export type Tag = { id: string; type: TagType; value: string }
export type ImageForReview = { id: string; width: number; height: number; bytes: number; capturedAt: string | null; uploadedAt: string; reviewedAt: string | null; tags: Tag[] }
export type ReviewResponse = { pendingCount: number; image: ImageForReview | null }
export async function fetchReview(): Promise<ReviewResponse> { const response = await fetch('/api/images/review'); if (!response.ok) throw new Error('Could not load the review queue.'); return response.json() as Promise<ReviewResponse> }
export async function saveReview(id: string, tags: Pick<Tag, 'type' | 'value'>[]): Promise<void> { const response = await fetch(`/api/images/${id}/tags`, { method: 'PUT', headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': await csrf() }, body: JSON.stringify({ tags, markReviewed: true }) }); if (!response.ok) throw new Error('Could not save the image review.') }
export async function findTags(type: TagType, query: string): Promise<Tag[]> { const response = await fetch(`/api/tags?type=${type}&query=${encodeURIComponent(query)}`); if (!response.ok) return []; return ((await response.json()) as { tags: Tag[] }).tags }
