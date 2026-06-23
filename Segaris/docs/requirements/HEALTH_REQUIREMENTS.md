# Health Requirements

## Status

Phase 2 functional definition is complete. This document is the functional
source of truth for the initial Health implementation plan.

## Purpose

Health records the household's medical reference knowledge as durable concepts,
not as individual occurrences. It maintains a catalogue of `Disease` concepts
(an illness or condition as a general idea, never a dated episode) and a
catalogue of `Medicine` concepts (a medication as a general idea, never a
specific package or dose taken), together with a many-to-many relationship that
records which medicines are used for which diseases.

`Health` is the interface name of the module. It owns two peer top-level REST
resources, `diseases` (`/api/health/diseases`) and `medicines`
(`/api/health/medicines`), and the association between them.

Health is an independent business module. It introduces one cross-business-module
reference: a `Medicine` may optionally point at one Inventory item through the
shared Entity Link mechanism. The dependency direction is Health to Inventory,
following the same contract-inversion pattern used by Recipes so Inventory never
depends on Health.

## Initial Scope

- Maintain a `Disease` collection with a name, a required category, optional
  free-text symptoms, an optional average duration in days, free-text notes, and
  visibility. A disease has no attachments and no lifecycle status.
- Maintain a `Medicine` collection with a name, a required category, optional
  free-text posology, a "requires prescription" flag, an optional live link to one
  Inventory item, free-text notes, attachments with an optional primary image, and
  visibility. A medicine has no lifecycle status.
- Record a many-to-many relationship between diseases and medicines as a pure
  link with no attributes of its own, editable symmetrically from either side.
- Own the `DiseaseCategory` and `MedicineCategory` catalogues, managed through
  Configuration with replace-only deletion.
- Present Health as a single module surface with two tabs, the disease list and
  the medicine gallery, with URL-aware popups.
- Let a medicine optionally reference one Inventory item, selected through the
  shared entity-selection components, under the standard link visibility rule.

## Excluded Scope

The initial Health implementation excludes:

- Disease occurrences or episodes: dated illness events, symptom logs, durations
  of an actual illness, recovery tracking, or any per-occurrence record. Disease
  is a concept only.
- Medicine occurrences: stock of a medication, doses actually taken, intake
  schedules, reminders, treatment courses, or expiry of a specific package. The
  optional Inventory link is the only connection to real stock, and it is
  Inventory's concern, not Health's.
- Attributes on the disease-to-medicine association (such as a per-pair posology
  or per-pair notes); the link is a pure association. General posology lives on
  the medicine itself.
- Attachments on diseases; only the medicine carries attachments and a primary
  image.
- People or patients: Health records household-level medical reference knowledge,
  not who is affected. There is no link to Firebird or to household members.
- Dosage calculators, drug-interaction checking, medical coding systems (such as
  ICD or ATC), prescriptions as documents, or any clinical decision support.
- Any launcher attention signal for the module.
- Analytics or Calendar integration.
- Spanish translations; the module ships English strings under an i18n namespace
  prepared for future translation.

## Disease

A `Disease` contains:

- A required name.
- A required `DiseaseCategory` reference.
- Optional free-text symptoms.
- An optional average duration in whole days.
- Optional free-text notes.
- Visibility.
- Standard ownership and audit metadata.

A disease has no lifecycle status and no attachments. Diseases that are no longer
wanted are deleted.

### Average Duration

`AverageDurationDays` is an optional positive whole number describing, as
reference knowledge, how long the illness typically lasts in days. It is purely
descriptive, drives no attention and no calculation, and is absent by default. It
is not a duration of any actual episode, because Health records concepts and not
occurrences.

## Medicine

A `Medicine` contains:

- A required name.
- A required `MedicineCategory` reference.
- Optional free-text posology describing the general dosing guidance for the
  medicine.
- A required `RequiresPrescription` boolean flag, defaulting to `false`.
- An optional live link to one Inventory item.
- Optional free-text notes.
- Attachments with an optional primary image.
- Visibility.
- Standard ownership and audit metadata.

A medicine has no lifecycle status. Medicines that are no longer wanted are
deleted.

### Posology

`Posology` is optional free text describing the general dosing guidance for the
medicine (for example "1 tablet every 8 hours after meals"). Because the
disease-to-medicine link is a pure association with no attributes, posology
belongs to the medicine as a whole and is not specialised per disease.

### Inventory Item Reference

A medicine optionally references one Inventory item through a live identifier (no
snapshot), reusing the shared Entity Link mechanism.

- Health consumes a narrow Inventory read contract to validate the referenced
  item, resolve its display name, and evaluate accessibility for the current user.
  Inventory owns this contract.
- A `Public` medicine may reference only `Public` items; a `Private` medicine may
  reference any item its creator can access. This mirrors the Maintenance-to-Assets,
  Recipes-to-Inventory, and Travel-to-Destinations visibility rule.
- When the item cannot be resolved for the viewer, the medicine shows a neutral
  placeholder instead of disclosing a private item.
- The reference is optional, so a medicine without an item link is valid.

### Image And Attachments

- A medicine carries zero or more attachments through the shared platform
  attachment storage with the owner kind `Medicine`, under the shared attachment
  size and policy bounds.
- One attachment may be marked as the primary image and is shown as the gallery
  thumbnail; a medicine without a primary image falls back to its first image and
  then to a placeholder, following the Clothes, Assets, Recipes, and Destinations
  pattern.
- Attachments are removed when their medicine is deleted.

## Disease-To-Medicine Association

Health records which medicines are used for which diseases as a many-to-many
relationship.

- The association is a **pure link** with no attributes of its own. It records
  only that a given disease and a given medicine are related.
- A disease may be associated with any number of medicines, and a medicine with
  any number of diseases. Either may have none.
- The association is **symmetric** and is the same join whichever side creates it:
  associating disease D with medicine M is identical to associating medicine M
  with disease D.
- The association is editable from **both sides**: from a disease's editor the
  user manages its medicines, and from a medicine's editor the user manages its
  diseases.
- The association is managed through individual add and remove operations, not
  whole-set replacement, so a mutation never disturbs association links that the
  acting user cannot see (see the visibility rule below).
- Deleting either a disease or a medicine removes all of its association links.
  This is an intra-module cascade and never blocks the deletion.

### Association Visibility Rule

Both diseases and medicines carry their own independent `Public`/`Private`
visibility. Because the link is symmetric, the rule is expressed over the
unordered pair rather than a direction.

- **Creation.** A user may create an association only between a disease and a
  medicine that the user can access. Any `Private` endpoint of the pair must
  therefore be owned by the acting user.
- **Read (viewer-filtered).** An association is shown to a viewer only when the
  viewer can access **both** linked records. As a result, a `Public` record never
  exposes another user's `Private` associations: from a public medicine, a viewer
  sees only the diseases they can access, and vice versa. A link that touches a
  `Private` record is effectively visible only to that record's owner.
- **Visibility change (publish guard).** Making a `Private` record `Public` is
  **blocked** while it still has any association to a record that is not `Public`
  (that is, to one of the owner's own `Private` records). The backend rejects the
  change and reports how many associations prevent it; the owner must first remove
  those associations or make the partner `Public`. Making a `Public` record
  `Private` is always allowed, because it only narrows who can see the record and
  its links and never exposes private data.

These constraints are enforced by the backend regardless of the client. Missing
and inaccessible records share the platform not-found behaviour so private data is
not disclosed.

## Visibility And Authorization

Diseases and medicines use the platform-standard visibility values:

- `Public`
- `Private`

New diseases and medicines default to `Public`. The standard Segaris baseline
applies independently to each record:

- A user can view and edit their own records and public records.
- A private record remains creator-only, including from administrators.
- Any authenticated user may edit a public record.
- Only the creator may change a record's visibility.

Medicine attachments inherit the visibility and authorization of their owning
medicine. The disease-to-medicine association follows the association visibility
rule above. These constraints are enforced by the backend regardless of the
client. Missing and inaccessible records share the platform not-found behaviour so
private data is not disclosed.

## Catalogues And Configuration Integration

Health owns two catalogues, presented alongside the other module-owned catalogues
through the established Configuration presentation boundary:

- `DiseaseCategory`: a required name and an order. Because every disease requires a
  category, a referenced value may only be **replaced**; replacement re-points the
  affected diseases to the target value.
- `MedicineCategory`: a required name and an order. Because every medicine requires
  a category, a referenced value may only be **replaced**; replacement re-points
  the affected medicines to the target value.

Administrator CRUD, ordering, final-row protection, and the replacement dialog
with a privacy-neutral impact summary follow the established module-owned
catalogue pattern. The `RequiresPrescription` flag is fixed, not managed through
Configuration.

Accepted initial catalogue values, seeded once:

- `DiseaseCategory`: `Chronic`, `Acute`, `Infection`, `Allergy`, `Injury`,
  `Other`.
- `MedicineCategory`: `Analgesic`, `Antibiotic`, `Antihistamine`,
  `Anti-inflammatory`, `Supplement`, `Topical`, `Other`.

The one-time initialization behaviour matches the established Configuration
catalogue pattern: values are initialized once and are not reimposed after
administrative changes.

## Attention

Health contributes no launcher attention signal. The launcher card never requests
attention.

## Module Entry And Navigation

Opening Health shows a single module surface with **two tabs**, the disease list
and the medicine gallery. The active tab, the list state of each tab, and any open
dialog are reflected in the URL so that tab and list state survive dialog open and
close without a reload.

### Diseases Tab

The diseases tab is a server-paginated **list** of accessible diseases.

- Search matches the disease name.
- Filters cover category.
- Sorting covers name and category.
- Each row surfaces the name, the category, and the count of associated medicines
  the viewer can access.
- Diseases are created, viewed, edited, and deleted through the established
  Segaris URL-aware popup pattern. The disease editor manages the disease's fields
  and its associated medicines through the shared entity selector.

### Medicines Tab

The medicines tab is a server-paginated **thumbnail gallery** of accessible
medicines.

- Search matches the medicine name.
- Filters cover category and the prescription flag.
- Sorting covers name and category.
- Each card shows the primary image (or a placeholder), the name, the category, a
  prescription badge when `RequiresPrescription` is true, and the resolved
  Inventory item name when linked and resolvable.
- Medicines are created, viewed, edited, and deleted through the established
  Segaris URL-aware popup pattern. The medicine editor manages the medicine's
  fields, its attachments and primary image, its Inventory item link through the
  shared entity selector, and its associated diseases through the shared entity
  selector.

Indicative frontend route shapes:

```text
/health
/health?tab=diseases
/health?tab=medicines
/health?tab=diseases&diseaseId=12
/health?tab=diseases&newDisease=true
/health?tab=medicines&medicineId=8
/health?tab=medicines&newMedicine=true
```

If the shared shell or router ergonomics suggest a slightly different parameter
layout, preserve the same behaviour: the active tab and both lists' state must
survive dialog open and close without a reload.

## Validation

- Disease name is required, trimmed, not whitespace-only, and at most 200
  characters.
- The disease category reference is required and must exist.
- Disease symptoms are optional and at most 2,000 characters.
- The average duration, when present, is a whole number from 1 to 100,000 days.
- Disease notes are optional and at most 2,000 characters.
- Disease visibility is a known value.
- Medicine name is required, trimmed, not whitespace-only, and at most 200
  characters.
- The medicine category reference is required and must exist.
- Medicine posology is optional and at most 2,000 characters.
- `RequiresPrescription` is a required boolean.
- The medicine's Inventory item reference, when present, must exist and satisfy
  the visibility rule.
- Medicine notes are optional and at most 2,000 characters.
- Medicine attachments, when present, are within the shared attachment policy
  bounds; at most one attachment is the primary image.
- Medicine visibility is a known value.
- An association may be created only between a disease and a medicine the acting
  user can access, and a visibility change that would leave a now-`Public` record
  linked to a `Private` record is rejected.
- Catalogue names are required, trimmed, not whitespace-only, and at most 200
  characters.

## Creation Defaults

A new disease starts with:

- Visibility `Public`.
- The first available disease category by `SortOrder`, then `Id`.
- No symptoms, average duration, notes, or associated medicines.

A new medicine starts with:

- Visibility `Public`.
- The first available medicine category by `SortOrder`, then `Id`.
- `RequiresPrescription` equal to `false`.
- No posology, Inventory item link, notes, attachments, or associated diseases.

## Deletion

Deletion is physical, immediate, and irreversible.

### Disease Deletion

Deleting a disease removes the disease together with all of its association links
to medicines, in one operation. It does not delete any medicine.

### Medicine Deletion

Deleting a medicine removes the medicine together with all of its association
links to diseases and every attachment it owns, in one operation. It does not
delete any disease. The Inventory item link is a stored reference only; deleting a
medicine never affects the Inventory item.

### Inventory Item Deletion

Deleting an Inventory item referenced by medicines is **not** blocked. Inventory
defines a deletion reference contract that consumers implement to report and
resolve references when an item is deleted. Health registers an implementation
that clears the item link on every affected medicine within the deletion
transaction. Inventory enumerates registered implementations during deletion; it
never queries Health entities. The behaviour is implemented by contract inversion
so the dependency direction stays Health to Inventory. Impact reporting is
privacy-neutral and never discloses another user's private medicines.

## Acceptance Criteria

The initial Health definition is satisfied when:

1. A `Disease` carries a required name, a required `DiseaseCategory`, optional
   free-text symptoms, an optional 1-100,000 average duration in days, optional
   notes, and visibility, with standard ownership and audit metadata, and has no
   lifecycle status and no attachments.
2. A `Medicine` carries a required name, a required `MedicineCategory`, optional
   free-text posology, a required `RequiresPrescription` flag defaulting to
   `false`, an optional live Inventory item link, optional notes, attachments with
   an optional primary image, and visibility, with standard ownership and audit
   metadata, and has no lifecycle status.
3. Diseases and medicines are related through a pure many-to-many association with
   no attributes, editable symmetrically from either side through individual
   add/remove operations, where deleting either side removes its links without
   blocking.
4. The association visibility rule holds: a user may associate only records they
   can access; associations are read viewer-filtered so a public record never
   exposes another user's private associations; and publishing a record is blocked
   while it still links to a private record, reporting the blocking count.
5. A medicine carries zero or more attachments accepted under the shared policy,
   with at most one primary image used as the gallery thumbnail and a fallback to
   the first image and then a placeholder, all removed on medicine deletion;
   diseases carry no attachments.
6. Health owns the `DiseaseCategory` and `MedicineCategory` catalogues through
   Configuration, both required and replace-only, seeded with the accepted initial
   values, while the prescription flag remains fixed.
7. Visibility follows the Segaris public-collaboration and private-isolation
   baseline, defaults to `Public`, is changed only by the creator, and is
   inherited by medicine attachments; inaccessible records return the standard
   not-found behaviour.
8. A medicine optionally references one Inventory item under the visibility rule (a
   public medicine references only public items; a private medicine references any
   accessible item), resolving the item name with a neutral placeholder when it is
   not resolvable.
9. Deleting an Inventory item referenced by medicines clears the link on every
   affected medicine within the deletion transaction, never blocks deletion,
   reports impact privacy-neutrally, and is implemented by contract inversion so
   Inventory does not depend on Health.
10. The launcher card never requests attention.
11. Health opens on a single surface with a diseases tab (server-paginated list,
    name search, category filter, name/category sorting, associated-medicine
    count) and a medicines tab (server-paginated thumbnail gallery, name search,
    category and prescription filters, name/category sorting, prescription badge,
    resolved item name), using URL-aware dialogs that preserve tab and list state.
12. SQLite and PostgreSQL migrations, backend unit/integration/architecture tests,
    frontend component tests, and a representative Playwright journey verify the
    supported behaviour and privacy boundaries.

## Deferred Decisions

- Whether Health should later record disease occurrences or episodes (dated
  illness events, symptom logs, recovery tracking) alongside the concept
  catalogue.
- Whether medicines should record intake schedules, reminders, treatment courses,
  or expiry, and whether any of these should drive launcher attention.
- Whether the disease-to-medicine association should later gain attributes (such
  as a per-pair posology or notes), promoting the pure link to a join entity.
- Whether diseases should gain their own attachments.
- Whether Health should adopt a standard medical coding system (ICD, ATC) or a
  curated drug catalogue rather than free-text concepts.
- Whether Health should relate diseases or medicines to people (household members
  or Firebird) to record who is affected.
- Whether Health should publish read contracts to Analytics or a future Calendar
  module.
