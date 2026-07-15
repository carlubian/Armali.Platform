# External Integrations

This document records the Phase 1 boundaries for services and systems outside Segaris Platform. Integrations remain replaceable adapters and must not leak provider-specific concepts into domain behavior.

## Integration Principle

Segaris uses a ports-and-adapters approach when a concrete external integration exists.

The module that needs an external capability defines a narrow interface in application or domain terms. An infrastructure adapter implements that interface with the provider SDK, HTTP protocol, filesystem command, or other external mechanism.

For example, a module may define an operation conceptually like:

```csharp
public interface IReceiptTextExtractor
{
    Task<ReceiptText> ExtractAsync(
        AttachmentId attachmentId,
        CancellationToken cancellationToken);
}
```

The contract describes the capability required by Segaris. It does not expose provider URLs, request DTOs, authentication methods, model names, SDK response types, or provider exceptions.

Interfaces are added when a real integration is designed. Segaris does not create speculative abstractions for every possible future service.

## Ownership And Placement

The consuming module owns:

- The capability contract expressed in its own application language.
- Authorization and privacy decisions before invoking the integration.
- Validation of the business input and interpretation of the internal result.
- The user-facing behavior when the integration succeeds, fails, or is unavailable.

The infrastructure adapter owns:

- Provider authentication and configuration.
- Protocol, SDK, serialization, and transport details.
- Provider-specific request and response mapping.
- Timeouts, resilience, and rate or concurrency limits.
- Sanitized operational logging and provider diagnostics.

An indicative location is:

```text
Modules/
`-- Archive/
    |-- Application/
    |   `-- IReceiptTextExtractor.cs
    `-- Infrastructure/
        `-- ProviderReceiptTextExtractor.cs
```

An adapter may live in a separate infrastructure project only when project-level separation provides a useful dependency boundary. The architecture does not require one project per provider.

Provider DTOs are translated at the adapter boundary and never returned to endpoints, persisted as domain records, or passed through other modules. If raw provider output must be retained for diagnosis or reproducibility, the requirement, access controls, retention, and sensitivity classification must be explicit.

## HTTP Integrations

HTTP-based adapters use typed clients configured through `IHttpClientFactory` unless a provider SDK manages HTTP lifetime safely itself.

Every client defines:

- A configured base address rather than accepting arbitrary user-supplied hosts.
- Explicit connection, attempt, and total-operation timeouts appropriate to the provider.
- Bounded request and response sizes.
- Cancellation-token propagation.
- Required authentication and non-secret request headers.
- Strict success and error response parsing.

An integration must not deserialize unbounded or polymorphic provider content without explicit limits and validation. Unexpected response shapes produce a controlled integration failure rather than partially populated domain data.

`Microsoft.Extensions.Http.Resilience` may be added when the first real HTTP integration requires standard resilience behavior. It is configured per client or integration class rather than applied indiscriminately to every outgoing request.

## Resilience Policy

Retries are limited to failures that are plausibly transient, such as selected timeouts, connection failures, `408`, `429`, and appropriate server errors. Retry delays use bounded exponential backoff with jitter when retry is justified.

Unsafe or non-idempotent operations are not retried automatically unless the provider supports an idempotency key or Segaris can prove duplicate execution is harmless. The adapter documents the idempotency behavior of every retried operation.

Circuit breakers and outbound concurrency limits are introduced when an integration's call volume and failure behavior make them useful. They are not mandatory decoration for a service called a few times per month.

All resilience strategies have finite limits. Segaris must not wait indefinitely, create unbounded retry storms, or hide a persistent provider failure behind repeated background attempts.

## Dependency Classification

Every integration is classified by its effect on application availability.

### Optional

An optional dependency enhances diagnostics or behavior but its absence does not prevent normal Segaris use.

Seq is the initial example. Delivery remains best-effort and its failure does not affect startup, readiness, requests, jobs, or local container logging.

### Required For One Operation

The dependency is necessary only for a specific command, query, or background job. Its failure produces a controlled failure for that operation while the rest of Segaris remains available.

Future OCR, AI extraction, calendar synchronization, exchange-rate retrieval, or document-processing services are expected to use this classification unless their requirements prove otherwise.

Long-running or failure-prone operations use the persistent background-job infrastructure. The initiating request returns a job identifier rather than holding an HTTP connection open for the duration of the provider operation.

### Required For Application Readiness

A dependency enters backend readiness only when Segaris cannot safely serve its core API without it. Initial required dependencies are PostgreSQL and the configured local attachment storage. External SaaS providers are not initially readiness dependencies.

Adding an external readiness dependency requires explicit architectural review because its outage would make the complete household application unavailable.

## Failure Translation

Adapters translate provider-specific failures into a small internal failure model. Modules and endpoints do not branch on SDK exception types or provider text.

Internal failure categories may include:

- Invalid integration configuration.
- Authentication or permission rejection by the provider.
- Rate limiting or quota exhaustion.
- Temporary provider or network unavailability.
- Timeout.
- Unsupported or rejected input.
- Malformed or incompatible provider response.
- Permanent provider-side failure.

User-facing API errors use stable Segaris error codes and safe parameters. Provider request identifiers may be retained in structured logs and administrative diagnostics when they contain no sensitive data. Provider response bodies, tokens, document contents, and private input are not written to normal logs.

## Privacy Review

No integration may send household data to an external provider until its privacy decision is documented.

The review records at least:

- The exact fields, files, or derived content leaving the household server.
- Whether public records, private records, credentials, identity data, or uploaded documents are included.
- The provider, processing region, subprocessors where known, and transport security.
- Provider retention, deletion, model-training, and human-review behavior.
- The Segaris configuration or administrative action that enables the integration.
- Which users may invoke it and whether additional per-operation confirmation is required.
- What is stored locally about requests, results, consent, and failures.
- How users can stop future transfer and delete retained provider-side data where supported.

External transfer is disabled by default until configuration and authorization are complete. Private records and attachments receive no weaker protection than public records merely because an integration is convenient.

AI and OCR providers must not receive private documents or text under a generic platform permission. Their data scope and user experience require a dedicated Phase 2 decision.

## Secrets And Configuration

Provider credentials, API keys, client secrets, webhook secrets, and private certificates are backend secrets. They are never stored in frontend configuration, committed files, image layers, logs, job parameters, or API responses.

Adapters receive validated typed options from the backend configuration system. Startup validates required configuration for enabled integrations. An optional disabled integration must not prevent startup merely because its credentials are absent.

The concrete production secret-injection, storage, rotation, and recovery mechanism is defined by the separate secrets-management decision. Integration code must support credential replacement without rebuilding the application image.

## Incoming Webhooks

Future provider webhooks use dedicated provider-specific endpoints rather than a generic webhook receiver.

Each webhook integration defines:

- Signature or message-authentication verification using the raw request body where required.
- Timestamp validation and replay protection.
- Strict content type, payload-size, and schema validation.
- Provider event identifiers and idempotent duplicate handling.
- A minimal synchronous response path.
- Background-job handoff when processing can be slow or failure-prone.
- Safe logging that excludes secrets and sensitive payload contents.

Webhook claims, user identifiers, roles, record IDs, and filenames are untrusted input. The adapter resolves them through Segaris-owned mappings and authorization rules before changing domain state.

Webhook endpoints are rate limited independently from normal authenticated API traffic and do not use browser cookies or antiforgery tokens. Their provider authentication mechanism is mandatory.

## Current Integration Boundaries

### Seq

Seq is an optional technical adapter owned by observability. It receives sanitized structured events on a best-effort basis and never becomes a domain dependency.

### External Backup Automation

The household backup service is an external client of Segaris's authenticated administrative API. Segaris does not call it and does not store its external-storage credentials. It starts backup generation, observes the persistent job, and copies the completed package according to the operational contract.

This client will eventually authenticate through an explicitly provisioned user-bound API key or another approved machine credential with the minimum administrative capability required for backups.

### Model Context Protocol Clients

MCP clients are incoming trusted automation clients, not outbound provider integrations. Segaris exposes MCP from the backend process at `/mcp` only when `Segaris:Mcp:Enabled` is true, and the endpoint accepts user-bound API keys rather than browser cookies. A tool call therefore runs with the permissions and privacy boundaries of the API-key owner.

MCP tools must not create a second authorization model or bypass module APIs. They remain a transport surface over Segaris-owned behavior, with provider-specific client concerns kept outside the domain modules.

### Future OCR, AI, And Calendar Providers

No provider is selected. If these capabilities are approved, each consuming module defines its contract and receives an adapter with its own privacy review, configuration, resilience, and failure behavior.

## Testing

- Module unit tests use deterministic fake implementations of integration contracts.
- Adapter tests verify request mapping, response parsing, timeout, cancellation, and failure translation.
- HTTP adapter integration tests use controlled local test servers or provider sandboxes; normal CI must not depend on public provider availability.
- Contract fixtures remove or synthesize private household data.
- Tests verify that retries do not duplicate unsafe operations.
- Disabled or unavailable optional integrations must not make readiness fail.

## Explicit Non-Goals

The initial architecture does not require:

- A central enterprise service bus or integration platform.
- A universal provider abstraction covering unrelated services.
- Runtime provider plugins loaded from unknown assemblies.
- Automatic fallback between providers without domain-specific reconciliation rules.
- External integrations as prerequisites for core application startup.
- Generic webhook processing.
- Secrets stored in the database merely for adapter convenience.
