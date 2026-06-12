# Backend Module Conventions

## Purpose

This document records the implemented Wave 3 path for adding backend modules and HTTP endpoints. It complements the architecture documents with concrete repository conventions.

## Module Registration

1. Add one module class under `src/backend/Segaris.Api/Modules/<ModuleName>` that implements `ISegarisModule`.
2. Give the module a unique stable `Name`.
3. Register module-owned services in `AddServices`.
4. Map module-owned endpoints in `MapEndpoints`.
5. Add the module once to `SegarisModules.RegisteredModules`.

Duplicate module names fail during service registration. The API host remains the only composition root.

## Route Groups And Contracts

- Create module groups with `MapSegarisApiGroup`; its prefix is relative to `/api` and must use lowercase URL-safe segments.
- Use explicit request and response records. Never serialize EF Core entities or internal domain objects.
- Give endpoints summaries and response metadata so the generated OpenAPI document describes the supported contract.
- Accept and propagate the request `CancellationToken` through asynchronous application and persistence work.
- API request bodies are limited to 1 MiB by default. Use `WithRequestBodyLimit` only when an endpoint has a documented reason and validation policy for another bound.

## Current User And Visibility

- Depend on `ICurrentUser`; do not read credential or Identity internals from business modules.
- Store ownership with `UserId` where required.
- Use `VisibilityPolicy` for the shared public/private invariant, then apply any additional module authorization.
- Filter private records in queries before returning data where practical.
- Return the standard not-found problem when a record is absent or hidden. The `Admin` role does not bypass creator-only privacy.

## Errors

- Expected API failures use `ApiProblemException` with the documented HTTP status and a stable `ErrorCode`.
- Validation errors include a field-keyed `errors` extension when the mapping is stable.
- Framework binding failures and unexpected exceptions are handled centrally.
- Every problem response includes `code` and `traceId`.
- Details must never expose stack traces, exception types, SQL, secrets, or private record contents.

## Collections

- Use `PaginationRequest` and `PaginatedResponse<T>` for growing collections.
- Defaults are page `1` and page size `25`; the maximum page size is `100`.
- Use `SortRequest.Create` with module-owned allow-listed fields.
- Every sort contract names a stable tie-breaker, normally `id`.
- Invalid page, page size, sort field, or sort direction returns a structured `400` response rather than being ignored.
- Do not order by a `DateTimeOffset` column: SQLite cannot `ORDER BY` that type. Order by the auto-incrementing `id`, which is monotonic with creation, when creation order is needed.

## OpenAPI And Documentation

- OpenAPI generation is registered in every environment.
- The JSON document is exposed at `/openapi/v1.json` in Development and Testing only.
- Scalar interactive documentation is exposed at `/scalar/v1` in Development only.
- Tests validate the document, important schemas, and duplicate method/route combinations.

## Shared-Core Governance

`Segaris.Shared` may depend only on .NET base libraries and contains stable primitives or published contracts. It must not gain business entities, generic repositories, universal entity bases, generic classifications, notes, reminders, audit history, polymorphic associations, or a general Money abstraction.
