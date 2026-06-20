# Entity Link Implementation Plan

## Purpose

This plan replaces the current Maintenance asset-link dropdown with a scalable,
reusable entity-link mechanism inspired by the entity selector prototype under
`docs/ui-design/segaris/screens-entity-selector.jsx`.

The initial consumer is Maintenance linking a task to an Asset. The implementation
must remain generic enough to support future entity links between other modules
without introducing a global polymorphic association model or weakening module
ownership boundaries.

The prototype is a design reference only. Runtime implementation belongs under
`src/frontend` and must use the existing typed React, TanStack Query, React Hook
Form, i18n, and Project Armali component conventions.

## Source References

- `docs/ui-design/segaris/screens-entity-selector.jsx`
  - `A - Edit popup + picker control`: reference control for showing an empty or
    selected link and opening the selector.
  - `B - Selector - top filter bar`: preferred selector layout with search,
    filters, active chips, sortable table, pagination, empty state, and explicit
    selection.
- `docs/architecture/frontend.md`: server state through TanStack Query, temporary
  picker state as local UI state, forms through React Hook Form, and i18n from the
  beginning.
- `docs/architecture/design-system.md`: prototype assets are references, not
  runtime dependencies; overlays use shared portal-backed dialogs.
- `docs/architecture/user-experience.md`: modules may own domain-specific
  interactions while preserving shared accessibility, loading, error, and modal
  conventions.

## Current State

Maintenance currently links an optional Asset through `assetId` in
`MaintenanceFormValues`. `MaintenanceDialog.tsx` loads the first 100 matching
assets with `assetsApi.listAssets` and renders them in a native-like `Select`.

That approach does not scale because:

- the user must pick from a long dropdown rather than search and inspect a table;
- only a bounded first page is loaded, so valid assets can be absent;
- the visibility compatibility check is coupled to the currently loaded option
  page;
- the pattern cannot be reused cleanly for future entity references.

Assets already exposes the required frontend contract:

- `assetsApi.listAssets(query, signal)` with search, filters, sort, pagination,
  and visibility constraints;
- `assetsApi.getAsset(assetId, signal)` for resolving a selected or existing
  reference;
- catalog queries for categories and locations.

No backend API change is expected for the initial Maintenance to Asset selector.

## Delivery Principles

- Keep the persisted link unchanged: Maintenance still stores one optional live
  Asset identifier.
- Keep backend validation authoritative for visibility, access, and existence.
  The frontend selector improves usability but is not the integrity boundary.
- Avoid loading broad entity lists only to populate a dropdown.
- Keep selector search, filters, sort, and page as local picker state. The picker
  is a temporary dialog, not a navigable module page.
- Build reusable frontend primitives without creating a generic backend
  cross-module reference model.
- Keep domain-specific rendering in adapters. The shared selector should not know
  about Asset columns, Maintenance visibility rules, or future module semantics.
- Preserve unsaved-change behavior in Maintenance: selecting, changing, or
  clearing a link marks the editor dirty.
- Use Project Armali tokens and shared components; do not consume prototype code
  at runtime.

## Fixed Technical Contracts

### Shared Frontend Components

Create a focused shared area such as:

```text
src/frontend/src/components/entity-selection/
```

The initial component set should include:

- `EntityReferenceField`
  - Displays an empty state with icon, label, helper text, and Browse action.
  - Displays a selected state with primary label, secondary metadata, Change
    action, and Clear action when clearing is allowed.
  - Is controlled by props and does not own the selected entity.
  - Exposes accessible labels for Browse, Change, and Clear.
- `EntitySelectorDialog`
  - Renders a large portal-backed `Dialog`.
  - Owns temporary selector state: search text, filters, sort, page, and page
    size.
  - Uses caller-provided query data, columns, filters, labels, row identity, and
    selection callback.
  - Supports loading, refetching, API error, empty, and filtered-empty states.
  - Shows the currently linked entity as already selected/current rather than as
    a second selectable action.
- Small internal helpers may be extracted for selector pagination, sortable table
  headers, active filter chips, and result counts if this keeps the main dialog
  readable.

These components are shared application components rather than low-level
`components/ui` primitives because they model a business-facing entity selection
workflow.

### Asset Selector Adapter

Create an Asset-specific adapter, for example:

```text
src/frontend/src/modules/assets/AssetEntitySelector.tsx
```

or, if keeping the first consumer local is clearer:

```text
src/frontend/src/modules/maintenance/AssetLinkSelector.tsx
```

The adapter owns Asset-specific configuration:

- query construction for `assetsApi.listAssets`;
- category and location catalog filters;
- status and visibility filters;
- Asset table columns;
- row display metadata for `EntityReferenceField`;
- mapping between `AssetSummary` and `assetId`.

Suggested selector columns:

- Asset name;
- code;
- category;
- location;
- status;
- visibility.

Suggested filters:

- search by asset name or code;
- status;
- category;
- location;
- visibility when not forced by the caller.

For a `Public` Maintenance task, the selector query must force
`visibility: Public`. For a `Private` Maintenance task, it may list any assets the
current user can access according to the Assets API.

### Maintenance Integration

Replace the asset dropdown in `MaintenanceDialog.tsx` with the reference control
and selector dialog.

The form contract remains:

```ts
assetId: string
```

Selection behavior:

- Browse opens the Asset selector.
- Select sets `assetId` to `String(asset.id)`.
- Clear sets `assetId` to `''`.
- Change reopens the selector with the current asset marked as current.
- All three mutating actions mark the form dirty and set `editedRef.current`.

The existing `toRequest` behavior remains valid: an empty string serializes to
`assetId: null`; a selected asset serializes to its numeric identifier.

### Existing Link Resolution

The editor must not depend on a paginated list response to know whether the
current link is valid.

For edit mode with an existing `task.assetId`, resolve the selected asset through
one of these approaches:

1. Prefer `assetsApi.getAsset(task.assetId)` when the current user is allowed to
   read the asset.
2. If only `task.assetName` is available, show a degraded selected state using
   the known name and limited metadata.
3. If the asset cannot be resolved, show an explicit unavailable selected state
   consistent with the existing `Linked asset unavailable` table behavior.

The frontend may clear a selected asset automatically only when it knows the new
Maintenance visibility makes that asset incompatible. It must not clear a valid
asset merely because it is absent from the current selector page.

### Dialog And Overlay Behavior

The selector opens from inside the Maintenance editor, so the implementation must
handle stacked overlays deliberately:

- the selector is the active modal surface;
- the editor remains visible behind it where appropriate but must not receive
  interaction while the selector is open;
- Escape closes the selector before the editor;
- focus is restored to the reference control when the selector closes;
- Cancel closes the selector without changing the form.

If the current `Dialog` component needs small enhancements for stacked dialogs,
keep those changes generic and covered by tests.

## Delivery Waves

### Wave 0 - Confirm Runtime Constraints

- Re-read the current `Dialog`, `Button`, `Input`, `Select`, `Badge`, and
  `IconButton` implementations before coding.
- Confirm whether stacked dialogs can be implemented with the existing `Dialog`
  component or need a small generic extension.
- Confirm the exact Asset API response fields available for selected-state
  display.

Exit criteria:

- The implementation path is confirmed without requiring backend changes.
- Any required shared `Dialog` adjustment is identified before feature work.

### Wave 1 - Shared Reference Field

- Implement `EntityReferenceField`.
- Add CSS using existing Project Armali tokens.
- Support empty, selected, unavailable, disabled, and busy states as needed by the
  Maintenance integration.
- Add focused component tests for Browse, Change, Clear, labeling, and disabled
  behavior.

Exit criteria:

- The field can replace a dropdown visually and functionally without depending on
  Asset-specific code.

### Wave 2 - Shared Selector Dialog Shell

- Implement `EntitySelectorDialog` with the top-filter-bar layout selected from
  the prototype.
- Include:
  - search input;
  - caller-provided filter controls;
  - active filter chips and clear-all action;
  - sortable table headers;
  - result count;
  - paginated footer;
  - loading, refetching, error, and empty states;
  - current-selection treatment.
- Keep state local to the selector instance.

Exit criteria:

- A caller can provide entity-specific query data, filters, columns, and
  selection behavior without copying selector layout code.

### Wave 3 - Asset Selector Adapter

- Implement the Asset-specific selector adapter.
- Wire TanStack Query to `assetsApi.listAssets`.
- Reuse Asset catalogs for category and location filters.
- Map Asset sorting fields to `AssetSortField`.
- Force `visibility: Public` when requested by Maintenance.
- Keep selected/current Asset visible as current if it appears in the result page.

Exit criteria:

- The Asset selector can browse, filter, sort, paginate, and select assets using
  server-side data.

### Wave 4 - Maintenance Editor Integration

- Replace the current asset `Select` in `MaintenanceDialog.tsx`.
- Remove the first-100-assets dropdown query from the editor.
- Resolve the current asset independently for edit mode.
- Preserve React Hook Form integration, dirty tracking, validation, submit
  serialization, and unsaved-change behavior.
- Preserve the existing public/private compatibility rule in the UI while leaving
  backend validation authoritative.
- Add or update maintenance i18n keys.

Exit criteria:

- Users can create and edit Maintenance tasks with no linked asset, a selected
  asset, or a changed/cleared asset through the new picker mechanism.

### Wave 5 - Styling And Accessibility Pass

- Adapt the prototype visual language to real CSS classes and existing tokens.
- Verify keyboard operation:
  - open selector from the field;
  - tab through filters, rows, pagination, Select, Cancel, and Close;
  - close with Escape;
  - clear selected link without requiring a mouse.
- Ensure labels, button names, result counts, busy states, and empty/error states
  are accessible and translatable.
- Ensure the selector works at the target desktop widths used by the application.

Exit criteria:

- The new mechanism matches the application visual system and is usable without a
  pointer.

### Wave 6 - Tests And Regression Coverage

Update `MaintenancePage.test.tsx` and add focused component tests where useful.

Coverage should include:

- opening the selector from a new Maintenance task;
- public tasks requesting only public assets;
- private tasks allowing the broader accessible asset set;
- searching/filtering and selecting an asset;
- submitted Maintenance payload containing the selected `assetId`;
- clearing a selected asset and submitting `assetId: null`;
- preserving an existing linked asset even when it is not on the current selector
  page;
- showing an unavailable/degraded selected state when an existing link cannot be
  resolved;
- canceling the selector without changing the form.

Exit criteria:

- The targeted frontend tests pass and cover the dropdown-to-selector behavior
  change.

### Wave 7 - Documentation And Cleanup

- Remove obsolete helper code such as dropdown option builders that are no longer
  used.
- Keep README unchanged unless development commands or repository-wide setup
  change.
- Update focused architecture or UX documentation only if the implementation
  establishes a lasting reusable pattern beyond this plan.
- Record any unresolved cross-entity-link decisions in `ROADMAP.md` rather than
  burying them in code comments.

Exit criteria:

- The implementation leaves a reusable frontend pattern and no stale Maintenance
  dropdown path.

## Initial Non-Goals

- No backend polymorphic entity-link table.
- No global entity search service.
- No cross-module selector registry in the first delivery.
- No changes to the persisted Maintenance task contract.
- No changes to Assets deletion-guard behavior.
- No URL persistence for selector-local search/filter/page state.
- No adoption of the left-rail selector variant from the prototype unless a
  future domain proves it more appropriate.

## Future Reuse Notes

Future modules should add thin adapters around the shared selector components
rather than forking the full dialog. A future adapter should define:

- entity type and identifier;
- list query and query key;
- default sort and page size;
- available filters;
- table columns;
- selected-state display metadata;
- compatibility constraints imposed by the source entity.

If multiple future links require common lifecycle behavior beyond frontend
selection, that backend behavior should be planned separately as explicit
module-owned contracts, not as a hidden generic association model.
