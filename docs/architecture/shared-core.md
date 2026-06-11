# Shared Core

This document records the deliberately small shared core for Armali Platform. Shared code exists only for stable technical or cross-cutting semantics. Domain concepts remain owned by their modules unless functional evidence establishes one coherent shared lifecycle.

## Design Principle

Armali shares contracts and primitives, not a universal domain model.

The shared core must remain small enough that business modules can evolve independently. It must not become a dependency destination for code with unclear ownership or a shortcut around module boundaries.

Before adding a shared type, the change must establish:

- The semantics are genuinely identical across consumers.
- One owner can define and evolve the contract.
- The lifecycle is stable and not merely similar by name.
- Sharing reduces meaningful duplication without coupling unrelated domain behavior.

If those conditions are uncertain, the concept remains local to each module until real implementations provide evidence for extraction.

## User Identity Primitive

Identity publishes a stable strongly typed user identifier for references from other modules, conceptually:

```csharp
public readonly record struct UserId(int Value);
```

Business modules may store `UserId` values for ownership and creation or modification metadata. They do not reference the internal Identity user entity or its EF Core navigation properties.

A shared current-user contract exposes only the authenticated identity and platform capabilities required for authorization, such as:

- Whether a user is authenticated.
- The current `UserId`.
- The current platform role or stable capability checks.

The contract does not expose password, session, API-key, lockout, or other credential internals.

## Record Visibility

Modules that support the platform's public and creator-only private model use a shared visibility value with two initial states:

```csharp
public enum RecordVisibility
{
    Public,
    Private
}
```

A shared visibility policy may express the invariant:

- A public record is available to users who are otherwise authorized to use the owning module.
- A private record is available only to its creator.
- The `Admin` role does not override creator-only privacy.

The shared primitive does not automatically add visibility to every entity. Each module decides which records support privacy, stores the owner and visibility fields, applies the policy in every query and command, and defines any additional module permissions.

Modules must enforce visibility before loading or returning protected domain data where practical. Private records requested by another user normally produce `404 Not Found` so their existence is not disclosed.

## Creation And Modification Metadata

The shared data convention consists of:

- `CreatedAt` as a UTC technical instant.
- `CreatedBy` as a `UserId` when creation is attributable to a user.
- `UpdatedAt` as a UTC technical instant.
- `UpdatedBy` as a `UserId` when modification is attributable to a user.

These fields are a convention, not a mandatory inheritance hierarchy. Armali does not introduce a universal `Entity`, `AuditableEntity`, or aggregate-root base class.

Each entity includes only the metadata meaningful for its lifecycle. Immutable records, join tables, migration state, background infrastructure, and system-created technical records may use a different subset or explicit system attribution.

Entity Framework Core may apply shared mapping or save-interceptor behavior for the convention when that behavior remains transparent and testable. Domain code must still control modifications that have business meaning; a persistence interceptor must not silently invent domain transitions.

## Time

Application and domain code obtain the current instant through an injectable clock abstraction rather than directly calling the system clock throughout the codebase.

The shared time capability provides UTC instants. Conversion to `Europe/Madrid`, civil-date interpretation, business deadlines, and calendar rules remain explicit application or domain concerns.

Tests can substitute a deterministic clock. Persisted technical timestamps follow the UTC conventions in `docs/architecture/data-and-storage.md`.

## Currency Code

The shared core may provide a validated ISO 4217 currency-code value or equivalent validation helper. It guarantees canonical code representation such as `EUR` but does not implement monetary arithmetic, exchange rates, rounding policy, or formatting.

A general `Money` domain type is deferred until Capex, Opex, Travel, and Analytics requirements establish compatible semantics for amount precision, currency conversion, aggregation, and rounding. Modules may initially use their own amount value objects while following the common persistence convention of fixed-precision decimals plus an ISO currency code.

## API Primitives

The backend may share stable transport and endpoint primitives such as:

- Validated page and page-size values.
- Standard paginated response metadata.
- Stable application error-code representation.
- Correlation and trace identifier helpers.
- Common ProblemDetails extensions.

These primitives standardize the API conventions in `docs/architecture/backend.md`. They must not contain domain-specific filters, resource DTOs, or business error catalogs that belong to a module.

## Published Platform Contracts

Attachments and Jobs expose their shared contracts through their owning platform modules.

The small shared project may contain an interface or immutable contract when consumers require it, but ownership remains with the relevant platform module:

- Attachment identifiers, upload or deletion requests, and authorized access contracts belong to Attachments.
- Job identifiers, job registration, progress, and handler contracts belong to Jobs.
- Current-user and user-reference contracts belong to Identity.

Physical storage records, background-job database entities, and Identity entities are not shared domain types.

## Explicit Exclusions

The shared core does not initially contain:

- A `Household` entity or tenant identifier.
- Generic `Category`, `Status`, `Tag`, `Property`, or classification entities.
- Generic `Note`, `Comment`, activity timeline, or history entities.
- A general `Reminder` or notification entity.
- A universal `Money` type with domain behavior.
- Base `Entity`, `AggregateRoot`, or auditable-entity classes.
- Generic repositories, services, managers, or CRUD handlers.
- A general domain-event base hierarchy or mediator contracts.
- A polymorphic `EntityType` plus `EntityId` association that can attach arbitrary records to arbitrary modules.
- A general audit-event or entity-revision model.

Categories, statuses, tags, notes, comments, reminders, and similar concepts remain inside their owning business module. They may be extracted only after Phase 2 or implementation demonstrates shared semantics and a clear owner.

## Reference Shape

An indicative shared-code structure is:

```text
Armali.Shared/
|-- Identity/
|   |-- UserId.cs
|   `-- ICurrentUser.cs
|-- Authorization/
|   |-- RecordVisibility.cs
|   `-- VisibilityPolicy.cs
|-- Time/
|   `-- IClock.cs
|-- Money/
|   `-- CurrencyCode.cs
`-- Api/
    |-- Pagination.cs
    `-- ErrorCode.cs
```

Published Attachments and Jobs contracts may live in this project or in a narrow contracts namespace owned by their module. The implementation layout is less important than preserving ownership and preventing dependencies on internal module types.

## Testing And Governance

- Shared primitives require focused unit tests because defects affect several modules.
- Architecture tests prevent the shared project from referencing business modules.
- The shared project may depend only on the .NET base libraries and narrowly justified technical abstractions.
- New shared dependencies and domain-looking types require explicit architectural rationale during review.
- A shared abstraction that accumulates module-specific conditionals should be split or returned to its owning modules.

The size of the shared core is treated as a constraint, not a measure of architectural maturity.
