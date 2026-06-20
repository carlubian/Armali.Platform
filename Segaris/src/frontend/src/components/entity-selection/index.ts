/**
 * Shared entity-selection components.
 *
 * Business-facing building blocks for linking a record to another module's
 * entity: a reference control plus (later) a portal-backed selector dialog.
 * Domain modules wrap these in thin adapters rather than forking the layout.
 */

export { EntityReferenceField } from './EntityReferenceField'
export type { EntityReference, EntityReferenceFieldProps } from './EntityReferenceField'

export { EntitySelectorDialog } from './EntitySelectorDialog'
export type {
  EntityQueryResult,
  EntitySelectorColumn,
  EntitySelectorDialogProps,
  EntitySelectorFilter,
  EntitySelectorFilterOption,
  EntitySelectorLabels,
  EntitySelectorState,
  SortDirection,
} from './EntitySelectorDialog'
