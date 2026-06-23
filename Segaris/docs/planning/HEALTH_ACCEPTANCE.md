# Health Acceptance Record (Wave 9)

This document records the Wave 9 end-to-end, hardening, and acceptance pass for
the Health module against `docs/requirements/HEALTH_REQUIREMENTS.md` and the
exit criteria in `docs/planning/HEALTH_IMPLEMENTATION_PLAN.md`.

## Verification Approach

Wave 9 was executed as a focused hardening and acceptance pass:

- Functional behaviour is covered by the automated suites delivered in Waves 0-8
  and gated through the repository validation scripts and CI checks.
- The repeatable validation entry points remain the repository scripts: backend
  format verification, build, unit, API integration, architecture, provider
  migration coverage, PostgreSQL integration, frontend format, lint, type-check,
  unit, production build, and Playwright.
- The representative Playwright journey added below compiles with the frontend
  test suite and runs against the Compose stack when seeded
  `SEGARIS_E2E_USERNAME` / `SEGARIS_E2E_PASSWORD` credentials are present.
- The OpenAPI surface and database indexes/query shape were verified statically
  against the implemented endpoints, module contracts, and paired provider
  migrations.

## End-To-End Journey

`tests/frontend/e2e/health.spec.ts` adds a single-user critical journey against
the full stack: sign in, create a disposable public Inventory item, open Health
from the launcher, exercise a diseases filter, create a disease with category,
symptoms, and average duration, create a medicine with category, posology,
prescription flag, a staged primary image, an associated disease, and an
Inventory item link, verify the symmetric association from the disease side,
delete the referenced Inventory item through the privacy-neutral impact dialog,
verify the medicine link is cleared while the disease association remains, then
delete the safe medicine and disease test data. It is skipped without seeded
credentials, matching the other E2E specs.

## Static Verification Results

### HTTP / OpenAPI Surface

All frozen Health routes are mapped under `/api/health` with explicit Minimal
API metadata and DTO contracts; EF Core entities are not exposed. The group
requires authentication, write routes apply antiforgery, and missing or
inaccessible records share the module not-found behaviour.

- **Diseases**: `GET /api/health/diseases`, `POST /api/health/diseases`,
  `GET /api/health/diseases/{diseaseId}`,
  `PUT /api/health/diseases/{diseaseId}`, and
  `DELETE /api/health/diseases/{diseaseId}` expose list/detail/mutation
  contracts with validation, visibility, category, sorting, pagination, and
  privacy-safe problem responses.
- **Medicines**: `GET /api/health/medicines`, `POST /api/health/medicines`,
  `GET /api/health/medicines/{medicineId}`,
  `PUT /api/health/medicines/{medicineId}`, and
  `DELETE /api/health/medicines/{medicineId}` expose list/detail/mutation
  contracts including prescription filtering, Inventory item reference
  validation, thumbnail projection, and privacy-safe problem responses.
- **Associations**:
  `GET|POST|DELETE /api/health/diseases/{diseaseId}/medicines[...]` and
  `GET|POST|DELETE /api/health/medicines/{medicineId}/diseases[...]` operate on
  the same `DiseaseMedicine` join row, enforce accessible-only creation,
  viewer-filtered reads, idempotent creation, no-op-safe removal, and the publish
  guard.
- **Medicine attachments**:
  `GET|POST /api/health/medicines/{medicineId}/attachments`,
  `GET|DELETE /api/health/medicines/{medicineId}/attachments/{attachmentId}`,
  and
  `PUT /api/health/medicines/{medicineId}/attachments/{attachmentId}/primary`
  follow the shared attachment pattern and inherit medicine authorization.
- **Catalogues**: `GET /api/health/disease-categories` and
  `GET /api/health/medicine-categories` are authenticated reads.
  Administrator catalog management routes are exposed through Configuration with
  create, update, move, deletion-impact, direct delete, and replace-and-delete.
- **Inventory deletion impact**: Inventory owns its item deletion reference
  contract and exposes privacy-neutral impact reporting; Health implements the
  handler that clears medicine `InventoryItemId` references transactionally
  without creating an Inventory-to-Health dependency.

### Indexes And Query Shape

The Health persistence indexes exist in both SQLite and PostgreSQL migrations
and match the implemented query shapes:

| Index                                       | Query that uses it                                      |
| ------------------------------------------- | ------------------------------------------------------- |
| `diseases (Name, Id)`                       | Default deterministic ordering and name sorting         |
| `diseases (CategoryId)`                     | Exact category filter and category reference migration  |
| `diseases (Visibility)`                     | Exact visibility filter and selector constraints        |
| `diseases (CreatedBy, Visibility, Id)`      | Visibility/accessibility filter and creator filter      |
| `medicines (Name, Id)`                      | Default deterministic ordering and name sorting         |
| `medicines (CategoryId)`                    | Exact category filter and category reference migration  |
| `medicines (RequiresPrescription)`          | Exact prescription filter                               |
| `medicines (Visibility)`                    | Exact visibility filter and selector constraints        |
| `medicines (CreatedBy, Visibility, Id)`     | Visibility/accessibility filter and creator filter      |
| `medicines (InventoryItemId)` filtered      | Inventory item-deletion reference lookup                |
| `disease_medicines (DiseaseId, MedicineId)` | Disease-side lookup and unique association pair         |
| `disease_medicines (MedicineId, DiseaseId)` | Medicine-side lookup                                   |
| Health category normalized-name indexes     | Catalog name uniqueness                                 |
| Health category sort-order indexes          | Default catalog ordering                                |

List filtering, sorting, pagination, association counts, and detail projection
run as `IQueryable` queries translated to SQL; the client never loads the full
result set. Partial search remains the accepted database-backed `LIKE` scan
across disease and medicine names.

## Acceptance Criteria

Each criterion from `HEALTH_REQUIREMENTS.md` and its primary covering evidence:

| #   | Criterion                                                                                                                                                                     | Status                | Primary evidence                                                                                                                                                                                                                     |
| --- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 1   | Disease fields, required category, optional symptoms/duration/notes, visibility, ownership/audit, no lifecycle status, and no attachments                                     | Met                   | `Disease`, `HealthModelContributor`, provider migrations, `HealthDomainTests`, `HealthDiseaseEndpointTests`, `HealthPage.test.tsx`, `health.spec.ts`                                                                                |
| 2   | Medicine fields, required category, posology, required prescription flag defaulting false, optional Inventory link, notes, attachments, visibility, ownership/audit, no status | Met                   | `Medicine`, `HealthModelContributor`, `HealthDomainTests`, `HealthMedicineEndpointTests`, `HealthMedicineAttachmentTests`, `HealthMedicineInventoryLinkTests`, `HealthPage.test.tsx`, `health.spec.ts`                             |
| 3   | Diseases and medicines use a pure symmetric many-to-many association, editable from either side, with individual add/remove operations and cleanup on deletion                 | Met                   | `DiseaseMedicine`, `HealthAssociationService`, `HealthAssociationEndpointTests`, `DiseaseEntitySelector`, `MedicineEntitySelector`, `HealthPage.test.tsx`, `health.spec.ts`                                                         |
| 4   | Association visibility rule: accessible-only creation, viewer-filtered reads, privacy-safe public records, and publish guard with blocking count                               | Met                   | `HealthAssociationEndpointTests`, `HealthDiseaseEndpointTests`, `HealthMedicineEndpointTests`, `HealthAssociationService`, `HealthDiseasePolicies`, `HealthMedicinePolicies`                                                        |
| 5   | Medicine attachments follow shared policy with at most one primary image, thumbnail fallback, cleanup on medicine deletion, and no disease attachments                         | Met                   | `HealthMedicineAttachmentTests`, `MedicineAttachments`, `MedicineThumbnailResolver`, `HealthAttachments`, provider migrations, `health.spec.ts`                                                                                      |
| 6   | Health owns required replace-only `DiseaseCategory` and `MedicineCategory` catalogues through Configuration, seeded with accepted values; prescription remains fixed           | Met                   | `HealthCatalogEndpointTests`, `HealthCatalog`, Health category management services, `ConfigurationPage` integration, `HealthPage.test.tsx`                                                                                           |
| 7   | Visibility follows public-collaboration/private-isolation, public defaults, creator-only visibility changes, inherited attachment authorization, and privacy-safe not-found    | Met                   | `HealthDiseaseEndpointTests`, `HealthMedicineEndpointTests`, `HealthAssociationEndpointTests`, `HealthMedicineAttachmentTests`, `HealthDiseasePolicies`, `HealthMedicinePolicies`                                                    |
| 8   | Medicine optionally references one Inventory item under the visibility rule and resolves item names with neutral placeholders when unresolved                                  | Met                   | `HealthMedicineInventoryLinkTests`, `MedicineReadService`, `InventoryItemEntitySelector`, `HealthPage.test.tsx`, `health.spec.ts`                                                                                                   |
| 9   | Inventory item deletion clears every medicine link atomically, never blocks deletion, reports privacy-neutrally, and preserves dependency inversion                            | Met                   | `InventoryItemDeletionReferenceTests`, `HealthInventoryItemDeletionReferenceHandler`, `IInventoryItemDeletionReferenceHandler`, `ModuleBoundaryTests`, `InventoryPage`, `health.spec.ts`                                             |
| 10  | Launcher card never requests attention                                                                                                                                        | Met                   | `HealthModule`, `ModuleRegistrationTests`, `LauncherPage`, `LauncherPage.test.tsx`                                                                                                                                                   |
| 11  | Frontend opens on one URL-backed Health surface with disease list and medicine gallery, filters/sorting/pagination, dialogs, selectors, badges, counts, and resolved item name | Met                   | `AppRouter`, `HealthPage`, `HealthMedicineTab`, `healthState.ts`, `contracts.test.ts`, `HealthPage.test.tsx`, `health.spec.ts`                                                                                                      |
| 12  | SQLite/PostgreSQL migrations, backend unit/integration/architecture tests, frontend component tests, and a representative Playwright journey verify behaviour/privacy          | Met (single-user E2E) | `Segaris.Migrations.IntegrationTests`, `Segaris.Postgres.IntegrationTests`, `ModuleBoundaryTests`, Health unit/API suites, `contracts.test.ts`, `HealthPage.test.tsx`, `health.spec.ts`                                             |

## Deferred Items

Recorded in `ROADMAP.md`:

- **Second-user Health privacy E2E journey.** Public-collaboration,
  private-isolation, association visibility, and Inventory-link privacy are
  covered by API integration tests (`HealthDiseaseEndpointTests`,
  `HealthMedicineEndpointTests`, `HealthAssociationEndpointTests`,
  `HealthMedicineAttachmentTests`, `HealthMedicineInventoryLinkTests`,
  `InventoryItemDeletionReferenceTests`); the browser-level multi-session
  journey waits on multi-account Playwright infrastructure, matching the
  deferred patterns for earlier modules.
- **PostgreSQL representative-volume query-plan benchmark.** Index existence and
  database-level query shape are verified; a large-dataset `EXPLAIN ANALYZE`
  benchmark waits on a representative seeding/benchmark harness.
- **Future Health scope.** Disease and medicine occurrences, intake schedules,
  reminders, treatment courses, association attributes, disease attachments,
  standard medical coding or drug catalogues, links to people, and Analytics or
  Calendar integration remain future versions.

## Documentation Outcomes

- `README.md`: no change; repository-wide commands and setup were unaffected by
  the Health waves.
- `ROADMAP.md`: Health implementation marked accepted; the intentional
  deferrals above recorded.
- `docs/planning/HEALTH_IMPLEMENTATION_PLAN.md`: Wave 9 acceptance is recorded
  in this document.
