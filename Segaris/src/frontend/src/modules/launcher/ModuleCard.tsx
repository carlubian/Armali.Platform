import { ArrowRight } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import type { ReactNode } from 'react'

export type ModuleCardTone = 'aqua' | 'gold' | 'azure' | 'sea' | 'rose'

export interface ModuleCardModel {
  key: string
  title: string
  description: string
  actionLabel: string
  href: string
  icon: LucideIcon
  tone: ModuleCardTone
  requiresRole?: string
  attention?: boolean
  attentionLabel?: string
}

interface ModuleCardProps {
  module: ModuleCardModel
  onOpen: (href: string) => void
  attentionIndicator?: ReactNode
}

export function ModuleCard({ module, onOpen, attentionIndicator }: ModuleCardProps) {
  const Icon = module.icon

  return (
    <button
      type="button"
      className={`seg-mod seg-mod--${module.tone}`}
      onClick={() => onOpen(module.href)}
    >
      {module.attention &&
        (attentionIndicator ?? (
          <span
            className="seg-mod__attn"
            role="status"
            aria-label={module.attentionLabel}
          />
        ))}
      <span className="seg-mod__icon" aria-hidden="true">
        <Icon size={24} />
      </span>
      <span className="seg-mod__name">{module.title}</span>
      <span className="seg-mod__desc">{module.description}</span>
      <span className="seg-mod__foot">
        {module.actionLabel} <ArrowRight size={13} aria-hidden="true" />
      </span>
    </button>
  )
}
