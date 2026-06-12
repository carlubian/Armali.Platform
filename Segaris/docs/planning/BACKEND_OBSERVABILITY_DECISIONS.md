# Backend Observability And Runtime Safety Decisions

## Purpose

This document records the concrete Wave 7 implementation choices for logging, correlation,
health reporting, frontend diagnostics, and abuse-sensitive endpoint limits.

## Logging And Seq

- Serilog is the backend logging pipeline. Console events use newline-delimited compact JSON;
  events below `Warning` go to `stdout`, while warnings and errors go to `stderr`.
- `Logging:LogLevel` remains the category-level configuration contract. Production defaults to
  `Information`, with framework categories independently overrideable.
- Seq is optional and disabled by default. When enabled, its absolute HTTP/HTTPS URL and minimum
  level are validated at startup. Its API key is optional and remains a backend secret.
- Seq delivery uses a bounded in-memory queue, bounded batches, a two-second posting period, and
  a 64 KiB event-body limit. Delivery failures never affect startup, readiness, requests, or jobs;
  local console events remain the operational fallback.
- Migration and background-job lifecycle events include structured duration and stable identifiers.
  Logs never intentionally include credentials, request/response bodies, job parameters, or file
  contents.

## Correlation

- Each HTTP request uses the active ASP.NET Core activity trace identifier, exposed as
  `HttpContext.TraceIdentifier` and returned in the `X-Trace-ID` response header.
- The same identifier is included in ProblemDetails and the structured request completion event.
- Frontend diagnostics receive a server trace identifier in their accepted response. A bounded
  client trace identifier may be submitted as a separate field and is never trusted as the server
  correlation identity.

## Health Reporting

- `/health/live` proves only that the backend process can answer HTTP.
- `/health/ready` checks database connectivity, absence of pending migrations, and writable
  attachment storage. Startup still applies migrations before accepting traffic.
- Seq is deliberately excluded from readiness because it is optional and best-effort.

## Frontend Diagnostics And Rate Limits

- `POST /api/diagnostics/frontend` requires an authenticated cookie session, antiforgery, and a
  fixed-window rate-limit policy. There is no anonymous or direct browser-to-Seq path.
- The accepted schema is closed and bounded: event code, severity, message, optional stack, route,
  component, and client trace identifier. Debug/trace events and arbitrary metadata are rejected.
- Known password, token, API-key, cookie, authorization, and connection-string shapes are redacted
  before browser-provided text enters logs. This is defense in depth; clients must still avoid
  collecting sensitive values.
- Login has a separate per-client fixed-window policy in addition to Identity account lockout.
  Diagnostics defaults to 30 requests per 60 seconds and a 16 KiB body limit. These values are
  configurable through `Segaris:Diagnostics` within validated safety bounds.

## Configuration

- `Segaris:Observability:Seq:Enabled`
- `Segaris:Observability:Seq:ServerUrl`
- `Segaris:Observability:Seq:ApiKey`
- `Segaris:Observability:Seq:MinimumLevel`
- `Segaris:Diagnostics:MaxBodyBytes`
- `Segaris:Diagnostics:PermitLimit`
- `Segaris:Diagnostics:WindowSeconds`

Tracked configuration contains no usable Seq API key.
