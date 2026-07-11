import { useEffect, useState } from 'react'
import type { KeyboardEvent } from 'react'
import { Plus, X } from 'lucide-react'
import { findTags } from './review/api'
import type { Tag, TagType } from './review/api'

export const tagTypes: { type: TagType; label: string; placeholder: string }[] = [
  { type: 'Person', label: 'Person', placeholder: 'Add a person…' },
  { type: 'Place', label: 'Place', placeholder: 'Add a place…' },
  { type: 'Topic', label: 'Topic', placeholder: 'Add a topic…' },
]

/// A single-type tag input: shows the assigned chips, autocompletes owner-scoped
/// suggestions, and creates a tag on the fly when none matches.
export function TagEditor({ type, label, placeholder, tags, onChange }: { type: TagType; label: string; placeholder: string; tags: Tag[]; onChange: (tags: Tag[]) => void }) {
  const [input, setInput] = useState('')
  const [suggestions, setSuggestions] = useState<Tag[]>([])
  useEffect(() => { const value = input.trim(); if (!value) return; const timer = window.setTimeout(() => { void findTags(type, value).then(setSuggestions) }, 180); return () => window.clearTimeout(timer) }, [input, type])
  const add = (value: string) => { const clean = value.trim(); if (!clean || tags.some(tag => tag.value.localeCompare(clean, undefined, { sensitivity: 'accent' }) === 0)) return; onChange([...tags, { id: crypto.randomUUID(), type, value: clean }]); setInput(''); setSuggestions([]) }
  const keyDown = (event: KeyboardEvent<HTMLInputElement>) => { if (event.key === 'Enter') { event.preventDefault(); add(input) } }
  return <div className={`tag-editor tag-${type.toLowerCase()}`}><div className="tag-title"><span /><strong>{label}</strong></div><div className="tag-chips">{tags.map(tag => <span className="tag-chip" key={tag.id}>{tag.value}<button onClick={() => onChange(tags.filter(item => item.id !== tag.id))} aria-label={`Remove ${tag.value}`}><X size={13} /></button></span>)}</div><div className="tag-entry"><input value={input} onChange={event => { setInput(event.target.value); if (!event.target.value.trim()) setSuggestions([]) }} onKeyDown={keyDown} placeholder={placeholder} maxLength={128} /><button className="add-tag" onClick={() => add(input)} title={`Add ${label}`}><Plus size={16} /></button>{input.trim() && suggestions.length > 0 && <div className="tag-suggestions">{suggestions.map(tag => <button key={tag.id} onMouseDown={event => { event.preventDefault(); add(tag.value) }}>{tag.value}</button>)}</div>}</div></div>
}
