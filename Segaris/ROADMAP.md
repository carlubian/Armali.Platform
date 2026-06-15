# Planning Roadmap

This roadmap tracks decisions that still need to be discussed or resolved. It is a living document: add new questions as they appear, and keep resolved decisions visible with a short rationale or a link to the document where they were settled.

Current phase: **Phase 2 - Functional Definition**. Capex is implemented and
accepted (see `docs/planning/CAPEX_ACCEPTANCE.md`); the remaining business modules
are still in functional definition.

## Status Legend

- `Open`: Needs discussion.
- `In discussion`: Currently being explored.
- `Resolved`: Decision made and documented.
- `Deferred`: Intentionally postponed.

## Phase 1: Architecture, Structure, And Core

### Product Shape

| Status | Decision | Notes |
| --- | --- | --- |
| Resolved | Primary application type | Internally hosted web application with separate ASP.NET Core REST backend and TypeScript frontend. Desktop and large displays are primary; mobile is outside initial scope. See `docs/architecture/product-shape.md`. |
| Resolved | Primary users | One household with two or three distinct users and `User` / `Admin` roles. Public entities are shared; private entities are creator-only, including from administrators. |
| Resolved | Offline needs | Online-only. Backend unavailability produces an explicit global error state; missing or expired sessions redirect to login. |
| Resolved | Multi-household support | One household only. Multi-household tenancy is outside the current scope. |
| Resolved | Localization needs | Spain, `Europe/Madrid`, EUR, Monday-first weeks, and localized `dd MMM yyyy` dates. Internationalization is architectural from the start; English is required and Spanish may follow. |

### Architecture

| Status | Decision | Notes |
| --- | --- | --- |
| Resolved | Frontend architecture | React, TypeScript, and Vite provide the client-rendered SPA. React Router owns navigation and URL state; TanStack Query manages REST server state; focused Context and local React state cover client-owned state. React Hook Form with Zod handles forms, and i18next provides module-oriented translations. Native React boundaries isolate root and module rendering failures, while modules are loaded lazily through Suspense. The visual component strategy remains separately deferred until screen designs are available. See `docs/architecture/frontend.md`. |
| Resolved | Backend architecture | ASP.NET Core modular monolith deployed as one process, API, container, and relational database. Modules expose grouped Minimal API routes with explicit DTOs, ProblemDetails, bounded pagination, and OpenAPI. Identity-backed cookies serve the SPA and a future API-key scheme may authenticate trusted clients as users. Persistent typed background jobs run through `BackgroundService`. One `SegarisDbContext` composes module-owned mappings, while separate PostgreSQL and SQLite migration assemblies preserve provider-specific histories. See `docs/architecture/backend.md`. |
| Resolved | Domain organization | Capabilities are divided into narrow platform modules, independent business modules, and read-only cross-domain modules. Each module owns its entities, classifications, behavior, search, privacy, and published contracts. Business modules do not depend on each other by default; cross-module references require explicit lifecycle and deletion rules. See `docs/architecture/domain-organization.md`. |
| Resolved | Shared core model | The shared core contains only stable primitives and published platform contracts: `UserId`, current-user context, public/private visibility policy, creation and modification conventions, an injectable UTC clock, ISO currency codes, API pagination/error primitives, and narrow Attachments/Jobs contracts. There is no Household entity, universal domain base class, generic classifications, notes, reminders, Money behavior, or polymorphic association model. See `docs/architecture/shared-core.md`. |
| Resolved | Integration boundaries | Modules define narrow capability contracts in application terms; provider-specific adapters own protocols, SDKs, credentials, validation, resilience, and failure translation. HTTP integrations use typed clients and bounded resilience only when implemented. External dependencies are classified as optional, operation-specific, or readiness-critical. Any external data transfer requires an explicit privacy review, and webhooks use authenticated provider-specific endpoints. See `docs/architecture/integrations.md`. |

### Data And Storage

| Status | Decision | Notes |
| --- | --- | --- |
| Resolved | Primary database | Relational persistence through Entity Framework Core. SQLite is the default for local development and PostgreSQL for production; the provider and connection string are selected through configuration. Provider compatibility is verified explicitly rather than assumed. See `docs/architecture/data-and-storage.md`. |
| Resolved | Database migration execution | The backend applies pending EF Core migrations automatically at startup in development and production, before accepting HTTP traffic. Migration failure aborts startup. The production topology permits only one backend instance, and risky or destructive migrations require backup and an explicit deployment note. See `docs/architecture/data-and-storage.md`. |
| Resolved | File and attachment storage | Files live in a persistent local Docker volume, grouped by module and named with UUIDs. Metadata and ownership remain in PostgreSQL. Deletion is immediate and uses compensating operations to keep the database and filesystem aligned. See `docs/architecture/data-and-storage.md`. |
| Resolved | Backup package generation | An asynchronous administrative API generates one latest package containing a PostgreSQL dump, attachments, and a manifest. Only one job may run at a time; output is staged before atomically replacing the previous package. An external service copies it off-server. See `docs/architecture/data-and-storage.md`. |
| Resolved | Shared data conventions | Auto-incrementing integer keys, UTC technical timestamps, date-only civil dates, fixed-precision amounts with ISO currency, last-write-wins updates, and physical deletion by default. See `docs/architecture/data-and-storage.md`. |
| Resolved | Attachment security policy | Files are limited to 25 MiB and accepted through a positive allow-list covering common images, documents, and text formats. Extension, media type, signatures or package structure, and parseable structured text are validated; original names remain metadata only. Malware scanning is deferred for the initial trusted-household deployment. See `docs/planning/BACKEND_ATTACHMENT_DECISIONS.md`. |
| Resolved | Search strategy | Search is module-specific and database-backed. The baseline is exact filtering for classifications and partial matching on entity names, with no initial global index or dedicated search service. See `docs/architecture/data-and-storage.md`. |
| Resolved | Audit and history | Entities store creation and last-modification metadata only. There is no general audit table, revision history, soft deletion, or undo after confirmation. See `docs/architecture/data-and-storage.md`. |

### Identity, Security, And Privacy

| Status | Decision | Notes |
| --- | --- | --- |
| Resolved | Authentication | Local username-and-password accounts for the initial trusted environment. Preserve an extension point for future user-bound API keys; detailed session and API-key design remains an architecture concern. See `docs/architecture/product-shape.md`. |
| Resolved | Authorization model | One household with `User` and `Admin` roles. Public entities are shared by all users; private entities are creator-only and remain hidden from administrators. |
| Resolved | Administrator credential lifecycle | The first administrator is bootstrapped idempotently from `Segaris:Identity:Bootstrap` configuration or environment variables; no credential is committed. Administrators set permanent, immediately usable passwords for new accounts and for approved credential recovery, and users may change their own password but are not forced to. Deactivation and credential recovery invalidate active sessions through security-stamp revalidation. See `docs/planning/BACKEND_IDENTITY_DECISIONS.md` and `docs/architecture/backend.md`. |
| Resolved | Identity profile and avatar contract | Users have a display name and an explicit `en-GB` language preference. Profile and avatar mutations are self-service and antiforgery-protected; JPEG, PNG, and WebP avatars use shared attachment storage. Any authenticated household user may read another user's avatar, while only the owner may replace or remove it. See `docs/planning/IDENTITY_PROFILE_DECISIONS.md`. |
| Resolved | Backup API authorization | `/api/backup-jobs` requires the `Admin` policy with antiforgery on writes; conflicts return `409` with the active job identifier. Unattended automation authenticates as an `Admin` cookie session (API keys remain out of scope). The package is not downloaded through the API; the external service reads it from the backups volume. See `docs/planning/BACKEND_BACKUP_DECISIONS.md`. |
| Open | Sensitive data policy | Expenses, documents, IDs, travel details, credentials, and private notes. |
| Open | Session cookie `Secure` flag and CORS posture | Revisit the session cookie's `Secure` attribute and the no-CORS, same-origin assumption if Segaris is ever exposed beyond the trusted local network. Tracked as Follow-Up in `docs/planning/FRONTEND_CORE_IMPLEMENTATION_PLAN.md`. |
| Resolved | Secrets management | Production secrets (database password, first-administrator bootstrap, Seq API key) are injected as Portainer stack environment variables, never committed or baked into images, and rotated by updating the stack and re-deploying. Local development uses an untracked `deploy/compose/.env` and .NET user secrets. See `docs/planning/BACKEND_DEPLOYMENT_DECISIONS.md`. |

### User Experience

| Status | Decision | Notes |
| --- | --- | --- |
| Resolved | Navigation model | Segaris uses a central dashboard as a module launcher. Modules are immersive, self-contained experiences with their own internal navigation; switching modules requires returning to the launcher rather than using persistent global navigation. The launcher contains no summaries or aggregated domain data, although each module may expose a simple current-user attention indicator on its card. See `docs/architecture/user-experience.md`. |
| Resolved | Design system | Segaris adopts the Project Armali design system (tokens, fonts, and shared components) and the `segaris/` prototype screens under `docs/ui-design/` as the frontend's visual foundation and shared shell, with Login variant A (centered card) and User management variant B (cards), and the supplied raster logo replacing the prototype's inline brand mark. See `docs/architecture/design-system.md`. |
| Resolved | Attention and feedback model | Segaris uses three distinct mechanisms: module-owned attention indicators on launcher cards, transient toast feedback for actions and background processes, and persistent events or due dates shown through a calendar view. There is no initial unified notification inbox, email, or push delivery. Calendar ownership and detailed event rules remain Phase 2 functional decisions. See `docs/architecture/user-experience.md`. |
| Open | Reporting model | Dashboards, charts, exports, household summaries. |

### Development And Operations

| Status | Decision | Notes |
| --- | --- | --- |
| Resolved | Repository structure | Single monorepo containing `src/backend`, `src/frontend`, cross-application `tests`, `deploy/compose`, documentation, and shared operational scripts. Frontend and backend retain independent dependencies, builds, tests, and images, with the REST API as their integration boundary. See `docs/architecture/development-and-operations.md`. |
| Resolved | Backend runtime and naming | .NET 10 LTS with a repository-root SDK policy. The solution, production assemblies, root namespaces, migration projects, and five backend test projects use the documented `Segaris.*` names. See `docs/planning/BACKEND_FOUNDATION_DECISIONS.md`. |
| Resolved | Backend configuration contract | ASP.NET Core source precedence is preserved; project-owned environment variables use `SEGARIS__` and nested double-underscore mapping. The complete known foundation schema, validation rules, and secret restrictions are documented. See `docs/planning/BACKEND_FOUNDATION_DECISIONS.md`. |
| Resolved | Development database reset and seed | Reset is an explicit, confirmed, Development-only command with provider-specific safety checks. Seeding is deterministic, idempotent, and excludes committed credentials. See `docs/planning/BACKEND_FOUNDATION_DECISIONS.md`. |
| Resolved | Runtime and deployment | Local Ubuntu server using Docker. Frontend and backend have separate images and containers. Persistent state lives in Docker-managed or host-mounted volumes where needed. See `docs/architecture/deployment.md`. |
| Resolved | Local development and application configuration | Frontend and backend run natively during normal development, with PostgreSQL and supporting services in Docker Compose; a fully containerized local mode validates production behavior. The backend uses an untracked `appsettings.json`, and the frontend an untracked `.env`, each created from a versioned example file. Frontend configuration must never contain secrets. See `docs/architecture/development-and-operations.md`. |
| Resolved | Container topology and ingress | Compose definitions support both local containerized execution and production deployment as a Portainer stack. A dedicated Caddy container is the only household-facing service and publishes `SEGARIS_HTTP_PORT`, defaulting to `5525`; it routes `/api/` to the backend and all other traffic to the frontend. Backend and PostgreSQL remain on the internal Docker network. TLS and application-managed DNS are outside initial scope; the server IP and optional UniFi DNS record are infrastructure concerns. See `docs/architecture/deployment.md`. |
| Resolved | Persistence and backup operations | PostgreSQL uses a Docker-managed named volume. Attachments and generated backup packages use bind mounts rooted at `SEGARIS_DATA_PATH`, defaulting to `/data/volumes/segaris`, with separate `attachments/` and `backups/` directories. Segaris exposes an administrative API that generates the latest package; an external service owns scheduling, transfer, external storage, encryption, retention, and lifecycle management. Concrete UID/GID provisioning and restoration procedures remain implementation details. See `docs/architecture/deployment.md` and `docs/architecture/data-and-storage.md`. |
| Resolved | Testing strategy | Backend unit tests use xUnit; integration tests use `WebApplicationFactory` and PostgreSQL through Testcontainers. Frontend tests use Vitest and Testing Library. Playwright covers critical end-to-end journeys against the Compose stack, with limited visual checks. PostgreSQL migration tests cover fresh creation and upgrades. GitHub Actions reports these suites on pull requests; exact required checks remain a CI/CD decision. See `docs/architecture/development-and-operations.md`. |
| Resolved | Observability | Backend events are structured and always written to container `stdout`/`stderr`, with optional best-effort delivery to a household Seq server. Seq failure never affects startup, readiness, requests, or jobs. Selected frontend failures are sent to a protected and rate-limited backend diagnostics endpoint, then enter the same logging pipeline; the browser never connects directly to Seq. Liveness and readiness cover required application dependencies but exclude Seq. No metrics or tracing stack is initially required. See `docs/architecture/development-and-operations.md`. |
| Resolved | CI/CD | GitHub Actions validates pull requests with build, lint, unit, integration, PostgreSQL migration, and Playwright suites. Trusted main-branch workflows publish independently built frontend and backend images to a private Azure Container Registry, tagged immutably by commit SHA. GitHub credentials use repository/environment secrets or preferably Azure OIDC federation; Portainer keeps its existing pull credentials. Production deployment and rollback are manual Portainer operations, and database migrations are not automatically reversed. See `docs/architecture/development-and-operations.md`. |
| Resolved | Production host and filesystem baseline | Ubuntu 24.04 LTS, x64 6–8 cores, ≥8 GB RAM, ≥256 GB SSD. Backend runs as UID:GID 5525:5525; attachments, backups, and Data Protection keys are bind mounts under `/data/volumes/segaris` owned by that identity, provisioned by `scripts/host-provision.sh`. See `docs/planning/BACKEND_DEPLOYMENT_DECISIONS.md`. |
| Resolved | Restore procedure | `scripts/restore.sh` restores a `segaris-backup.tar` package (pg_restore plus attachment mirror) into the running stack and requires explicit confirmation; the household administrator owns recovery and rehearses it at least quarterly. See `docs/operations/backup-and-restore.md` and `docs/operations/rollback.md`. |
| Resolved | Required CI checks | `Segaris Backend`, `Segaris PostgreSQL`, and `Segaris Compose` are required checks for `main`; branches must be current, review conversations resolved, and force pushes/deletion blocked. No approval is required while the repository has one regular maintainer. See `docs/planning/BACKEND_CI_DECISIONS.md`. |
| Resolved | Frontend core implementation plan | A dependency-ordered, wave-based plan covering the frontend scaffold, design-system port, shared shell, login, self-service profile (including avatar), administrative user management, a minimal launcher, a scoped backend Identity-profile extension, and the container/Compose/CI changes needed to serve the real frontend image. See `docs/planning/FRONTEND_CORE_IMPLEMENTATION_PLAN.md`. |
| Resolved | Frontend foundation conventions | Node 24.16.0 and pnpm 11.6.0 are pinned at repository level. ESLint/Prettier/TypeScript, Vitest/Testing Library, and Playwright have explicit ownership and placement conventions; local API access uses a same-origin Vite `/api` proxy; public build-time environment values and the module-oriented source tree are fixed for the Wave 2 scaffold. See `docs/planning/FRONTEND_FOUNDATION_DECISIONS.md`. |
| Resolved | Capex implementation plan | A dependency-ordered plan covers Configuration catalogs, Capex persistence and APIs, Launcher attention aggregation, the Entries table and popup editor, attachments, migrations, and end-to-end acceptance. See `docs/planning/CAPEX_IMPLEMENTATION_PLAN.md`. |
| Resolved | Configuration management implementation plan | A dependency-ordered plan covers administrator catalog CRUD and ordering, one-time initialization, safe deletion and reference migration, the unified Configuration frontend, advanced currency conversion, provider migrations, and acceptance. See `docs/planning/CONFIGURATION_IMPLEMENTATION_PLAN.md`. |

## Phase 2: Functional Definition Backlog

These items should be expanded into detailed requirements after the Phase 1 foundation is clear.

### Configuration

Module purpose: Administrator management of shared reference catalogs and
module-owned classifications through one unified experience.

| Status | Decision | Notes |
| --- | --- | --- |
| Resolved | Initial catalogs and ownership | Global Supplier, CostCenter, and Currency remain owned by Configuration; CapexCategory remains owned by Capex and is presented in the same frontend. See `docs/requirements/CONFIGURATION_REQUIREMENTS.md`. |
| Resolved | User workflow | An administrator-only launcher card opens flat Global and Capex sections, with tabs for multiple catalogs, complete tables, popup editors, and accessible move controls. |
| Resolved | Deletion and migration | Unreferenced values delete directly; referenced values are atomically replaced, optional Supplier/CostCenter references may be cleared, and private records are never disclosed. |
| Resolved | Currency replacement | Referenced currencies require a manual source-to-target exchange rate and authoritative two-decimal Capex recalculation. Delivery is isolated in an advanced plan wave. |
| Resolved | Implementation plan | Seven waves cover contracts, schema upgrade and one-time initialization, CRUD, reference migration, frontend delivery, currency conversion, and acceptance. See `docs/planning/CONFIGURATION_IMPLEMENTATION_PLAN.md`. |
| Resolved | Implementation and acceptance | The Configuration plan is delivered through Wave 6. All thirteen requirement acceptance criteria are mapped to covering code and tests in `docs/planning/CONFIGURATION_ACCEPTANCE.md`. |
| Deferred | Browser-level currency conversion E2E journey | Referenced-currency conversion and deletion are covered by API integration and PostgreSQL parity tests (`ConfigurationManagementEndpointTests`, `PostgresPersistenceTests`) and the conversion-dialog component tests. The administrator Playwright journey exercises the non-currency surface; the irreversible conversion journey is left out of the browser run to keep the seeded catalogs intact. |
| Deferred | Non-administrator Configuration browser journey | Non-admin enforcement (hidden launcher card, Access Denied route, normal-user API rejection) is covered by router/component tests (`ConfigurationPage.test.tsx`) and API tests (`Management_routes_reject_normal_users`). The browser-level non-admin guard is authored in `configuration.spec.ts` but skipped until multi-account Playwright infrastructure seeds a second account. |

### Capex

Module purpose: Atomic income or expense, like buying furniture or appliances, eating out or a lottery prize.

| Status | Decision | Notes |
| --- | --- | --- |
| Resolved | Entities and properties | Entry, item, lifecycle, amount, category, shared catalog, privacy, attachment, deletion, and attention rules are defined in `docs/requirements/CAPEX_REQUIREMENTS.md`. |
| Resolved | User workflow | Capex opens on a paginated Entries table and uses a URL-aware popup editor that preserves table state. Initial and deferred behaviors are defined in `docs/requirements/CAPEX_REQUIREMENTS.md`. |
| Resolved | Implementation and acceptance | The Capex implementation plan is delivered through Wave 8. All thirteen requirement acceptance criteria are mapped to covering code and tests in `docs/planning/CAPEX_ACCEPTANCE.md`. |
| Deferred | Second-user Capex privacy E2E journey | Public-collaboration and private-isolation behavior is covered by API integration tests (`CapexEntryAuthorizationTests`). The browser-level multi-session journey waits on multi-account Playwright infrastructure. |
| Deferred | PostgreSQL representative-volume query-plan benchmark | The recommended indexes exist in both providers and the queries run at the database level. A large-dataset `EXPLAIN ANALYZE` benchmark waits on a representative seeding/benchmark harness. |

### Opex

Module purpose: Recurrent income/expenses, grouped inside Contracts, like subscriptions or payroll.

| Status | Decision | Notes |
| --- | --- | --- |
| Resolved | Entities and properties | Contracts, effective occurrences, classifications, lifecycle, amounts, privacy, attachments, deletion, and Configuration migration behavior are defined in `docs/requirements/OPEX_REQUIREMENTS.md`. |
| Resolved | User workflow | Opex opens on a paginated Contracts table and uses a URL-aware contract popup with subordinate occurrence management. Initial and deferred behaviors are defined in `docs/requirements/OPEX_REQUIREMENTS.md`. |
| Resolved | Implementation plan | Delivery is divided into Waves 0-8 in `docs/planning/OPEX_IMPLEMENTATION_PLAN.md`. |
| Deferred | Current-year activity filter | Filtering contracts by whether they contain occurrences in the current year is a future usability improvement. |

### Inventory

Module purpose: Manage items with stock that are spent and bought.

| Status | Decision | Notes |
| --- | --- | --- |
| Open | Entities and properties | Categories, statuses, vendors, items, orders. |
| Open | Inventory scope | Food, supplies, documents, assets, appliances, warranties, medicines. |
| Open | Stock behavior | Quantities, expiration dates, locations, low-stock alerts. |

### Travel

Module purpose: Manage travels and expenses, for both holidays and work.

| Status | Decision | Notes |
| --- | --- | --- |
| Open | Trip model | Itineraries, bookings, packing lists, expenses, documents. |
| Open | Calendar integration | Whether trips and reminders should sync with calendars. |

### Assets

Module purpose: Manage objects where stock doesn't apply, like furniture, appliances, vehicles or computers.

| Status | Decision | Notes |
| --- | --- | --- |
| Open | Entities and properties | Categories, statuses, properties, asset code. |
| Open | User workflow | How to interact with the module, entry point, layout. |

### Maintenance

Module purpose: Record repairs and other maintenance tasks over physical elements.

| Status | Decision | Notes |
| --- | --- | --- |
| Open | Entities and properties | Categories, statuses, properties, lifecycle. |
| Open | User workflow | How to interact with the module, entry point, layout. |

### Projects

Module purpose: Manage a tree structure to organize personal projects, each with files/results, tasks and a risk analysis.

| Status | Decision | Notes |
| --- | --- | --- |
| Open | Entities and properties | Categories, statuses, properties, program/axis/project hierarchy. |
| Open | User workflow | How to interact with the module, entry point, layout. |

### Processes

Module purpose: Multi-step tasks that need to be completed in order by a given date, like bureaucracy.

| Status | Decision | Notes |
| --- | --- | --- |
| Open | Entities and properties | Categories, statuses, properties, step model. |
| Open | User workflow | How to interact with the module, entry point, layout. |

### Archive

Module purpose: Long term storage of documents for reference, like contracts, bills and receipts.

| Status | Decision | Notes |
| --- | --- | --- |
| Open | Entities and properties | Categories, statuses, properties, storage. |
| Open | User workflow | How to interact with the module, entry point, layout. |

### Firebird

Module purpose: Manage people, contacts and interactions with them.

| Status | Decision | Notes |
| --- | --- | --- |
| Open | Entities and properties | Categories, statuses, properties. |
| Open | User workflow | How to interact with the module, entry point, layout. |

### Clothes

Module purpose: Manage the wardrobe, with clothes and accesories.

| Status | Decision | Notes |
| --- | --- | --- |
| Open | Entities and properties | Categories, statuses, properties, wash types. |
| Open | User workflow | How to interact with the module, entry point, layout. |

### Mood

Module purpose: Record moods or emotions for long term trend analysis.

| Status | Decision | Notes |
| --- | --- | --- |
| Open | Entities and properties | Energy, alignment, source, other discriminators, privacy model. |
| Open | User workflow | How to interact with the module, entry point, layout. |

### Analytics

Module purpose: Module to see aggregated trends of the financial modules.

| Status | Decision | Notes |
| --- | --- | --- |
| Open | Entities and properties | Targeted modules, charts and statistics, date filtering. |
| Open | User workflow | How to interact with the module, entry point, layout. |

### Cross-Domain Features

| Status | Decision | Notes |
| --- | --- | --- |
| Open | Tags and categories | Whether tags are global, per-domain, hierarchical, or flat. |
| Open | Attachments | Common attachment model across all domains. |
| Open | Notes and comments | Whether records support notes, comments, or activity timelines. |
| Open | Search and filters | What global and per-domain search should support. |
| Resolved | Shared configuration catalogs | The platform Configuration module owns Supplier, CostCenter, and Currency persistence and read contracts. Administrator management, one-time initialization, ordering, and mandatory migration before referenced deletion are defined in `docs/requirements/CONFIGURATION_REQUIREMENTS.md`; module-specific classifications remain domain-owned. |

## Phase 3: Version Planning Backlog

These decisions should wait until requirements are clearer.

| Status | Decision | Notes |
| --- | --- | --- |
| Deferred | MVP version scope | Define the smallest useful implementation slice. |
| Deferred | Version sequencing | Decide how to split architecture, core, and domain features. |
| Deferred | Acceptance criteria format | Standardize what each version document must include. |
| Deferred | Implementation agent handoff format | Define the context package for future implementation agents. |
