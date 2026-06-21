import {
  Car,
  Folder,
  GraduationCap,
  HeartPulse,
  House,
  Landmark,
  ListChecks,
  Scale,
  Stamp,
  type LucideIcon,
} from 'lucide-react'

/**
 * Maps the seeded category names to their reference icons. Categories are
 * configurable, so any unrecognised name falls back to the neutral checklist
 * icon rather than failing.
 */
const byName: Record<string, LucideIcon> = {
  Administrative: Stamp,
  Legal: Scale,
  Tax: Landmark,
  Health: HeartPulse,
  Education: GraduationCap,
  Vehicle: Car,
  Housing: House,
  Other: Folder,
}

/** Renders the icon for a category name, resolved at render time. */
export function CategoryGlyph({ name, size = 16 }: { name: string; size?: number }) {
  const Icon = byName[name] ?? ListChecks
  return <Icon size={size} aria-hidden="true" />
}
