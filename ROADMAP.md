# Planning Roadmap

This roadmap tracks decisions that still need to be discussed or resolved. It is a living document: add new questions as they appear, and keep resolved decisions visible with a short rationale or a link to the document where they were settled.

Current phase: **Phase 1 - Architecture, Structure, And Core**.

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
| Resolved | Backend architecture | ASP.NET Core modular monolith deployed as one process, API, container, and relational database. Modules expose grouped Minimal API routes with explicit DTOs, ProblemDetails, bounded pagination, and OpenAPI. Identity-backed cookies serve the SPA and a future API-key scheme may authenticate trusted clients as users. Persistent typed background jobs run through `BackgroundService`. One `ArmaliDbContext` composes module-owned mappings, while separate PostgreSQL and SQLite migration assemblies preserve provider-specific histories. See `docs/architecture/backend.md`. |
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
| Open | Data import, export, and restoration | Define user-facing portability, package restoration, restore verification, and module-specific behavior for externally referenced records. |
| Resolved | Search strategy | Search is module-specific and database-backed. The baseline is exact filtering for classifications and partial matching on entity names, with no initial global index or dedicated search service. See `docs/architecture/data-and-storage.md`. |
| Resolved | Audit and history | Entities store creation and last-modification metadata only. There is no general audit table, revision history, soft deletion, or undo after confirmation. See `docs/architecture/data-and-storage.md`. |

### Identity, Security, And Privacy

| Status | Decision | Notes |
| --- | --- | --- |
| Resolved | Authentication | Local username-and-password accounts for the initial trusted environment. Preserve an extension point for future user-bound API keys; detailed session and API-key design remains an architecture concern. See `docs/architecture/product-shape.md`. |
| Resolved | Authorization model | One household with `User` and `Admin` roles. Public entities are shared by all users; private entities are creator-only and remain hidden from administrators. |
| Open | Sensitive data policy | Expenses, documents, IDs, travel details, credentials, and private notes. |
| Open | Secrets management | Where API keys, tokens, and service credentials live. |

### User Experience

| Status | Decision | Notes |
| --- | --- | --- |
| Resolved | Navigation model | Armali uses a central dashboard as a module launcher. Modules are immersive, self-contained experiences with their own internal navigation; switching modules requires returning to the launcher rather than using persistent global navigation. The launcher contains no summaries or aggregated domain data, although each module may expose a simple current-user attention indicator on its card. See `docs/architecture/user-experience.md`. |
| Deferred | Design system | Screen designs will be added to the project documentation before frontend implementation and used as the primary visual inspiration. Review them first, then select the UI library and define shared components, module variation, density, and accessibility requirements. See `docs/architecture/user-experience.md`. |
| Resolved | Attention and feedback model | Armali uses three distinct mechanisms: module-owned attention indicators on launcher cards, transient toast feedback for actions and background processes, and persistent events or due dates shown through a calendar view. There is no initial unified notification inbox, email, or push delivery. Calendar ownership and detailed event rules remain Phase 2 functional decisions. See `docs/architecture/user-experience.md`. |
| Open | Reporting model | Dashboards, charts, exports, household summaries. |

### Automation And Intelligence

| Status | Decision | Notes |
| --- | --- | --- |
| Open | AI usage | Whether to include AI-assisted extraction, categorization, planning, or summaries. |
| Open | Document ingestion | Receipts, invoices, tickets, bookings, warranties, and OCR needs. |
| Open | Rule automation | Recurring tasks, budget alerts, low-stock alerts, travel reminders. |

### Development And Operations

| Status | Decision | Notes |
| --- | --- | --- |
| Resolved | Repository structure | Single monorepo containing `src/backend`, `src/frontend`, cross-application `tests`, `deploy/compose`, documentation, and shared operational scripts. Frontend and backend retain independent dependencies, builds, tests, and images, with the REST API as their integration boundary. See `docs/architecture/development-and-operations.md`. |
| Resolved | Runtime and deployment | Local Ubuntu server using Docker. Frontend and backend have separate images and containers. Persistent state lives in Docker-managed or host-mounted volumes where needed. See `docs/architecture/deployment.md`. |
| Resolved | Local development and application configuration | Frontend and backend run natively during normal development, with PostgreSQL and supporting services in Docker Compose; a fully containerized local mode validates production behavior. The backend uses an untracked `appsettings.json`, and the frontend an untracked `.env`, each created from a versioned example file. Frontend configuration must never contain secrets. See `docs/architecture/development-and-operations.md`. |
| Resolved | Container topology and ingress | Compose definitions support both local containerized execution and production deployment as a Portainer stack. A dedicated Caddy container is the only household-facing service and publishes `ARMALI_HTTP_PORT`, defaulting to `5525`; it routes `/api/` to the backend and all other traffic to the frontend. Backend and PostgreSQL remain on the internal Docker network. TLS and application-managed DNS are outside initial scope; the server IP and optional UniFi DNS record are infrastructure concerns. See `docs/architecture/deployment.md`. |
| Resolved | Persistence and backup operations | PostgreSQL uses a Docker-managed named volume. Attachments and generated backup packages use bind mounts rooted at `ARMALI_DATA_PATH`, defaulting to `/data/volumes/armali`, with separate `attachments/` and `backups/` directories. Armali exposes an administrative API that generates the latest package; an external service owns scheduling, transfer, external storage, encryption, retention, and lifecycle management. Concrete UID/GID provisioning and restoration procedures remain implementation details. See `docs/architecture/deployment.md` and `docs/architecture/data-and-storage.md`. |
| Resolved | Testing strategy | Backend unit tests use xUnit; integration tests use `WebApplicationFactory` and PostgreSQL through Testcontainers. Frontend tests use Vitest and Testing Library. Playwright covers critical end-to-end journeys against the Compose stack, with limited visual checks. PostgreSQL migration tests cover fresh creation and upgrades. GitHub Actions reports these suites on pull requests; exact required checks remain a CI/CD decision. See `docs/architecture/development-and-operations.md`. |
| Resolved | Observability | Backend events are structured and always written to container `stdout`/`stderr`, with optional best-effort delivery to a household Seq server. Seq failure never affects startup, readiness, requests, or jobs. Selected frontend failures are sent to a protected and rate-limited backend diagnostics endpoint, then enter the same logging pipeline; the browser never connects directly to Seq. Liveness and readiness cover required application dependencies but exclude Seq. No metrics or tracing stack is initially required. See `docs/architecture/development-and-operations.md`. |
| Resolved | CI/CD | GitHub Actions validates pull requests with build, lint, unit, integration, PostgreSQL migration, and Playwright suites. Trusted main-branch workflows publish independently built frontend and backend images to a private Azure Container Registry, tagged immutably by commit SHA. GitHub credentials use repository/environment secrets or preferably Azure OIDC federation; Portainer keeps its existing pull credentials. Production deployment and rollback are manual Portainer operations, and database migrations are not automatically reversed. See `docs/architecture/development-and-operations.md`. |

## Phase 2: Functional Definition Backlog

These items should be expanded into detailed requirements after the Phase 1 foundation is clear.

### Capex

Module purpose: Atomic income or expense, like buying furniture or appliances, eating out or a lottery prize.

| Status | Decision | Notes |
| --- | --- | --- |
| Open | Entities and properties | Categories, statuses, properties, income/expense discrimination. |
| Open | User workflow | How to interact with the module, entry point, layout. |

### Opex

Module purpose: Recurrent income/expenses, grouped inside Contracts, like subscriptions or payroll.

| Status | Decision | Notes |
| --- | --- | --- |
| Open | Entities and properties | Categories, statuses, properties, income/expense discrimination. |
| Open | User workflow | How to interact with the module, entry point, layout. |

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

## Phase 3: Version Planning Backlog

These decisions should wait until requirements are clearer.

| Status | Decision | Notes |
| --- | --- | --- |
| Deferred | MVP version scope | Define the smallest useful implementation slice. |
| Deferred | Version sequencing | Decide how to split architecture, core, and domain features. |
| Deferred | Acceptance criteria format | Standardize what each version document must include. |
| Deferred | Implementation agent handoff format | Define the context package for future implementation agents. |

