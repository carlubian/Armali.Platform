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
| Open | Primary application type | Web app, desktop app, mobile-first web app, native mobile, or hybrid. |
| Open | Primary users | Single household owner, multiple household members, guests, administrators, or role-based access. |
| Open | Offline needs | Whether core workflows must work offline or only online. |
| Open | Multi-household support | Whether the system should support one household only or multiple independent households. |
| Open | Localization needs | Languages, currency, date formats, and regional assumptions. |

### Architecture

| Status | Decision | Notes |
| --- | --- | --- |
| Open | Frontend architecture | Framework, routing, state management, UI component strategy. |
| Open | Backend architecture | Monolith, modular monolith, service boundaries, API style. |
| Open | Domain organization | How to structure domains such as purchases, expenses, inventory, and travel. |
| Open | Shared core model | Common entities such as users, household, tags, attachments, notes, audit history, and reminders. |
| Open | Integration boundaries | How external services should be wrapped or isolated. |

### Data And Storage

| Status | Decision | Notes |
| --- | --- | --- |
| Open | Primary database | Relational, document, local-first, or other storage model. |
| Open | File and attachment storage | Receipts, invoices, documents, images, travel files, warranties. |
| Open | Data ownership and export | Backup, import/export, portability, and deletion requirements. |
| Open | Search strategy | Global search, domain search, filters, indexing. |
| Open | Audit and history | Whether changes require history, undo, or activity logs. |

### Identity, Security, And Privacy

| Status | Decision | Notes |
| --- | --- | --- |
| Open | Authentication | Local accounts, OAuth, passkeys, single-user mode, or external identity provider. |
| Open | Authorization model | Roles, permissions, household membership, shared views. |
| Open | Sensitive data policy | Expenses, documents, IDs, travel details, credentials, and private notes. |
| Open | Secrets management | Where API keys, tokens, and service credentials live. |

### User Experience

| Status | Decision | Notes |
| --- | --- | --- |
| Open | Navigation model | Dashboard, domain modules, command palette, search-first, or task-first structure. |
| Open | Design system | UI library, custom components, density, accessibility baseline. |
| Open | Notification model | In-app reminders, email, push, calendar integration, or no notifications initially. |
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
| Open | Repository structure | Single app, monorepo, packages, shared libraries. |
| Open | Runtime and deployment | Local server, cloud deployment, containerization, hosting target. |
| Open | Testing strategy | Unit, integration, end-to-end, visual, data migration tests. |
| Open | Observability | Logging, metrics, error reporting, audit diagnostics. |
| Open | CI/CD | Build, test, release, deployment workflow. |

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

