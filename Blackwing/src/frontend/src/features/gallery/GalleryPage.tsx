import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { ImageOff, X } from 'lucide-react'
import { tagTypes } from '@/features/tags'
import type { TagType } from '@/features/review/api'
import { browseGallery, fetchFacets, thumbUrl } from './api'
import type { GalleryItem, ReviewStatus, TagFacet } from './api'

const statuses: { value: ReviewStatus; label: string }[] = [
  { value: 'All', label: 'All' },
  { value: 'Pending', label: 'Pending' },
  { value: 'Reviewed', label: 'Reviewed' },
]
const dateLabel = (iso: string) => new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' }).format(new Date(iso))

/// Sidebar filter for one tag type: selected chips plus an autocomplete over the
/// owner's tags of that type. Adding a tag narrows results with AND semantics.
function TagFilter({ type, label, facets, selected, onToggle }: { type: TagType; label: string; facets: TagFacet[]; selected: Set<string>; onToggle: (id: string) => void }) {
  const [search, setSearch] = useState('')
  const mine = facets.filter(facet => facet.type === type)
  const query = search.trim().toLowerCase()
  const suggestions = query ? mine.filter(facet => !selected.has(facet.id) && facet.value.toLowerCase().includes(query)).slice(0, 8) : []
  const chosen = mine.filter(facet => selected.has(facet.id))
  return (
    <div className={`filter-group tag-${type.toLowerCase()}`}>
      <div className="filter-head"><span className="tag-dot" /><strong>{label}</strong><span className="filter-count">{mine.length}</span></div>
      {chosen.length > 0 && <div className="filter-chips">{chosen.map(facet => <span className="tag-chip" key={facet.id}>{facet.value}<button onClick={() => onToggle(facet.id)} aria-label={`Remove ${facet.value}`}><X size={12} /></button></span>)}</div>}
      <div className="filter-search">
        <input value={search} onChange={event => setSearch(event.target.value)} placeholder={`Search ${label.toLowerCase()}…`} disabled={mine.length === 0} />
        {suggestions.length > 0 && <div className="tag-suggestions">{suggestions.map(facet => <button key={facet.id} onMouseDown={event => { event.preventDefault(); onToggle(facet.id); setSearch('') }}><span>{facet.value}</span><span className="filter-count">{facet.count}</span></button>)}</div>}
      </div>
    </div>
  )
}

export function GalleryPage() {
  const navigate = useNavigate()
  const [params, setParams] = useSearchParams()
  const [facets, setFacets] = useState<TagFacet[]>([])
  const [items, setItems] = useState<GalleryItem[]>([])
  const [cursor, setCursor] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [more, setMore] = useState(false)
  const [error, setError] = useState('')

  const selected = useMemo(() => new Set(params.getAll('tag')), [params])
  const status = (params.get('status') as ReviewStatus | null) ?? 'All'
  const filterKey = `${[...selected].sort().join(',')}|${status}`

  useEffect(() => { void fetchFacets().then(setFacets).catch(() => undefined) }, [])

  useEffect(() => {
    let cancelled = false
    const run = async () => {
      setLoading(true); setError('')
      try { const page = await browseGallery({ tags: [...selected], status }); if (!cancelled) { setItems(page.items); setCursor(page.nextCursor) } }
      catch (reason) { if (!cancelled) setError(reason instanceof Error ? reason.message : 'Could not load the gallery.') }
      finally { if (!cancelled) setLoading(false) }
    }
    void run()
    return () => { cancelled = true }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filterKey])

  const loadMore = () => {
    if (!cursor) return
    setMore(true)
    browseGallery({ tags: [...selected], status, cursor })
      .then(page => { setItems(current => [...current, ...page.items]); setCursor(page.nextCursor) })
      .catch(reason => setError(reason instanceof Error ? reason.message : 'Could not load more images.'))
      .finally(() => setMore(false))
  }

  const setStatus = (next: ReviewStatus) => setParams(previous => { const copy = new URLSearchParams(previous); copy.delete('status'); if (next !== 'All') copy.set('status', next); return copy }, { replace: true })
  const toggleTag = (id: string) => setParams(previous => { const copy = new URLSearchParams(previous); const current = copy.getAll('tag'); copy.delete('tag'); const next = current.includes(id) ? current.filter(value => value !== id) : [...current, id]; for (const value of next) copy.append('tag', value); return copy }, { replace: true })
  const clearFilters = () => setParams(previous => { const copy = new URLSearchParams(previous); copy.delete('tag'); return copy }, { replace: true })

  const chosenFacets = facets.filter(facet => selected.has(facet.id))
  const countLabel = loading ? 'Loading…' : `${items.length}${cursor ? '+' : ''} ${items.length === 1 ? 'image' : 'images'}`

  return (
    <section className="gallery-page">
      <div className="eyebrow">BLACKWING</div>
      <h1>Gallery</h1>
      <p>Browse your private collection. Combine tags to narrow it down — an image must match every tag you pick.</p>
      <div className="gallery-layout">
        <aside className="gallery-filters">
          <div className="eyebrow">Filter by tag</div>
          {chosenFacets.length > 0 && (
            <div className="filter-selected">
              <div className="filter-chips">{chosenFacets.map(facet => <span className={`tag-chip tag-${facet.type.toLowerCase()}`} key={facet.id}>{facet.value}<button onClick={() => toggleTag(facet.id)} aria-label={`Remove ${facet.value}`}><X size={12} /></button></span>)}</div>
              <button className="link-button" onClick={clearFilters}>Clear all</button>
            </div>
          )}
          {tagTypes.map(config => <TagFilter key={config.type} type={config.type} label={config.label} facets={facets} selected={selected} onToggle={toggleTag} />)}
        </aside>
        <div className="gallery-main">
          <div className="gallery-toolbar">
            <span className="result-count">{countLabel}</span>
            <div className="segmented">{statuses.map(option => <button key={option.value} className={status === option.value ? 'active' : ''} onClick={() => setStatus(option.value)}>{option.label}</button>)}</div>
          </div>
          {error && <p role="alert" className="form-error">{error}</p>}
          {!loading && items.length === 0 && !error && (
            <div className="empty-state"><div className="empty-icon"><ImageOff size={32} /></div><h2>{selected.size > 0 || status !== 'All' ? 'No images match these filters' : 'Your gallery is empty'}</h2><p>{selected.size > 0 || status !== 'All' ? 'Try removing a filter or choosing a different combination.' : 'Upload images to start building your collection.'}</p></div>
          )}
          {items.length > 0 && (
            <>
              <div className="gallery-grid">
                {items.map(item => (
                  <button key={item.id} className="gallery-tile" onClick={() => navigate(`/gallery/${item.id}`)}>
                    <img src={thumbUrl(item.id)} alt="" loading="lazy" />
                    {!item.reviewed && <span className="tile-badge">Pending</span>}
                    <span className="tile-date">{dateLabel(item.effectiveCapturedAt)}</span>
                  </button>
                ))}
              </div>
              {cursor && <div className="gallery-more"><button className="button-outline" disabled={more} onClick={loadMore}>{more ? 'Loading…' : 'Load more'}</button></div>}
            </>
          )}
        </div>
      </div>
    </section>
  )
}
