# MCP Implementation Plan

## Purpose

This plan delivers a Model Context Protocol (MCP) server that exposes Segaris
module capabilities to external agents, together with the user-bound API-key
authentication that such non-browser clients require.

The plan covers two capabilities that are separable but sequenced together:

1. **User API keys.** The authentication scheme reserved in
   `docs/architecture/backend.md` ("Future User API Keys") becomes implemented.
   It has value independently of MCP and is a prerequisite for it.
2. **MCP server.** A protocol host inside the backend that publishes a curated
   set of module-owned tools to agents authenticated with those keys.

The first tool surface covers Capex, Opex, and Inventory. The contribution
mechanism is deliberately generic so later plans add a module by implementing a
contract and registering it, without touching the platform.

## Accepted Decisions

These were resolved in planning and are owned by this document.

- **Placement.** The MCP server lives inside `Segaris.Api`. It is not a separate
  application or container.
- **Ownership split.** `Platform/Mcp` owns the protocol host, transport,
  authentication, and error translation. Each business module owns and registers
  its own tools.
- **Authentication.** Non-browser clients authenticate with a user-bound API key
  presented in the `Authorization` header, per the scheme already specified in
  `docs/architecture/backend.md`.
- **Identity semantics.** A key produces the same application identity as an
  interactive session and enters the same authorization policies. It reaches the
  bound user's creator-only private records, exactly as that user's own session
  does. A key never grants permissions the user lacks.
- **Exposure.** The MCP endpoint is consumed from the household local network
  only. This preserves the current plain-HTTP, no-CORS, same-origin posture. The
  open ROADMAP decision on the `Secure` cookie flag and CORS is *not* activated
  by this plan.
- **First-wave scope.** Reads plus bounded creates. No updates and no deletions
  through MCP in this plan.
- **First-wave modules.** Capex, Opex, and Inventory.

### Recorded Privacy Position

Segaris keys carry the bound user's full identity, including private records.
The consequence is explicit and accepted: an agent invoking a tool may receive
private household records, and the agent's own model provider will therefore
receive them, even though the Segaris endpoint itself is only reachable from the
local network. Restricting the endpoint to the LAN bounds who can *call*
Segaris; it does not bound where a caller forwards the response.

Mitigations retained instead of scope restriction:

- Keys are individually named, individually revocable, and record last use, so a
  key can be attributed to one agent and withdrawn.
- The MCP capability is disabled by default and enabled by explicit
  configuration.
- Key creation is self-service only. A user can only ever expose their own
  records; no user, including an administrator, can mint a key that reaches
  another user's private data.

If a future requirement needs an agent that must not read private records, the
mechanism to add is a per-key scope. This plan leaves room for it by keeping the
scope decision inside the key record rather than in the tools.

## Delivery Principles

- Tools call existing module application services (`*ReadService`,
  `*WriteService`). They never re-implement queries, validation, or domain rules,
  and never reach into another module's entities.
- No change to the 21 existing modules is required for authentication to work.
  `HttpCurrentUser` resolves identity from `HttpContext.User`, so a key handler
  that produces an equivalent `ClaimsPrincipal` makes `ICurrentUser`,
  `VisibilityPolicy`, and every module's `AccessibleTo(userId)` filter apply
  unchanged.
- Tools speak domain language, not transport language. Tool names and parameters
  describe household concepts, not REST verbs, routes, or pagination mechanics.
- The tool surface is curated, never generated. Segaris does not derive tools
  from the OpenAPI document, and does not expose a tool per endpoint.
- The MCP capability is additive. With the feature disabled, backend startup,
  readiness, and every existing behaviour are unchanged.

## Fixed Technical Contracts

### Libraries

- `ModelContextProtocol` and `ModelContextProtocol.AspNetCore`, the official C#
  SDK maintained jointly by Microsoft, Anthropic, and the MCP organization.
- The package version is pinned explicitly and upgraded deliberately, never by
  floating range. Wave 0 pins the current stable `1.4.1` release and deliberately
  does not adopt the newer `2.0.0-preview.*` line.
- Transport is **Streamable HTTP**. SSE is deprecated by recent protocol
  revisions and is not offered.

### Placement

```text
src/backend/Segaris.Api/
|-- Platform/
|   `-- Mcp/                      host, transport, options, error translation
|-- Modules/
|   |-- Identity/
|   |   |-- ApiKeys/              key entity, issuing, verification, endpoints
|   |   `-- Security/             authentication handler, scheme selection
|   |-- Capex/Mcp/CapexMcpTools.cs
|   |-- Opex/Mcp/OpexMcpTools.cs
|   `-- Inventory/Mcp/InventoryMcpTools.cs
```

The Identity module owns API keys because a key is user-bound state, and Identity
already owns the user, its roles, and its security stamp.

### API Key Scheme

- Table `identity_api_keys`, following the module-prefixed `snake_case`
  convention.
- Record fields: owning user, name, creation time, optional expiration, last-use
  time, revocation state, key identifier, and secret verifier.
- Token format `segaris_<keyId>_<secret>`. The `keyId` is the lookup index; the
  `secret` is high-entropy random. Only a hash of the secret is persisted, never
  the usable token.
- The complete token is returned exactly once, at creation.
- Authentication scheme name `Segaris.ApiKey`, presented as
  `Authorization: Bearer segaris_...`.
- A policy scheme selects the key handler when an `Authorization` header is
  present and the cookie handler otherwise. Cookie behaviour is untouched.
- The handler produces a `ClaimsPrincipal` equivalent to the cookie session's,
  including the role claims, so existing policies apply without modification.
- **Security stamp validation must cover keys.** Deactivating a user, changing a
  password, or an administrative security change invalidates that user's keys
  through the same central mechanism that invalidates sessions.
- **Antiforgery is bypassed for the key scheme only.** Cookie-authenticated
  writes keep antiforgery validation unconditionally. The bypass is keyed on the
  authenticated scheme, never on the endpoint.
- Self-service management endpoints under `/api/profile/api-keys`
  (`POST`, `GET`, `DELETE` to revoke). Keys are a profile concern, not a session
  lifecycle concern; `ProfileEndpoints` already establishes the self-service
  pattern.
- Expired, revoked, unknown, and malformed tokens all produce the same generic
  `401`, consistent with the existing login-failure position.

### Tool Contribution Contract

The platform defines a contract; modules implement it. This mirrors
`ILauncherAttentionContributor`, which Capex, Opex, and Inventory already
implement and register in their own `AddServices`.

- The contract is owned by `Platform/Mcp` and depends on no module.
- Each contributing module registers its implementation in its own
  `ISegarisModule.AddServices`, exactly as it registers attention contributors,
  model contributors, and catalog handlers.
- The platform host enumerates the registered contributors from DI.
- **Assembly-wide attribute scanning (`WithToolsFromAssembly`) is not used.** It
  would bypass the module registration convention and the
  `SegarisModules.RegisteredModules` gate, and would silently publish tools from
  any module that happened to carry an attribute.
- Adding a module to the MCP surface must require: implement the contributor,
  register one line in the module class. Nothing in `Platform/Mcp` changes.

### Tool Surface Design

- Names are `<module>_<domain_verb>`, lowercase with underscores.
- Every module exposes a search/list tool returning **compact summaries** and a
  detail tool returning one full record by identifier. Agents locate first and
  read detail second; list tools never return full records.
- List tools accept domain filters and a bounded result count. The API's page
  size maximum of `100` is *not* the MCP maximum; MCP list tools use a
  substantially lower cap because agent context is the binding constraint, not
  bandwidth.
- Tools declare whether they are read-only and whether they are idempotent.
- Cancellation tokens propagate into the module services, per existing
  convention.

Indicative first surface (final names fixed in Wave 0):

```text
capex_search_entries        capex_get_entry        capex_list_categories
opex_search_entries         opex_get_entry         opex_list_categories
inventory_search_items      inventory_get_item     inventory_list_locations
```

### Errors

`ApiProblemException` and its stable `ErrorCode` are the existing failure model.
The MCP host translates them into protocol tool errors centrally, preserving the
stable code and never leaking stack traces, SQL, secrets, or private record
contents. Modules throw exactly what they already throw; they do not learn about
MCP error shapes.

A hidden or absent record returns the module's standard not-found problem. The
creator-only privacy rule is unchanged: the `Admin` role gets no bypass through
MCP.

## Waves

### Wave 0: Spike And Contracts

Resolves the unknowns that would otherwise be discovered late.

- Verify the SDK's mechanism for registering tools resolved from DI rather than
  by assembly attribute scanning. If the SDK cannot express module-owned
  registration cleanly, decide between an adapter layer and a documented
  deviation **before** any tool is written.
- Verify that `IHttpContextAccessor`, and therefore `HttpCurrentUser`, resolves
  correctly inside a tool invocation under Streamable HTTP, including the scope
  and lifetime of a long-lived stream. This is load-bearing: the entire
  zero-change authorization story depends on it. If it does not hold, the
  identity abstraction for tools must be designed here.
- Pin the SDK version.
- Fix the final tool names and parameter shapes.
- Add the test skeleton.

Exit: both risks closed or explicitly re-planned.

### Wave 1: API Key Infrastructure

Independently valuable; ships without any MCP surface.

- Key entity, model contributor, and paired migrations for PostgreSQL and
  SQLite.
- Issuing, hashing, and verification.
- `Segaris.ApiKey` authentication handler and policy-scheme selection.
- Security-stamp invalidation coverage.
- Antiforgery bypass keyed on the authenticated scheme.
- `/api/profile/api-keys` endpoints with OpenAPI metadata.
- Tests: issuing, verification, expiration, revocation, deactivated user, cookie
  path unaffected, antiforgery still enforced for cookies, admin key cannot read
  another user's private records.

### Wave 2: MCP Host Skeleton

- `Platform/Mcp` host, options, and feature flag, disabled by default.
- Streamable HTTP endpoint at `/mcp`, authenticated by the key scheme.
- The tool contribution contract.
- Central error translation.
- One trivial identity tool reporting the calling user, to validate the full
  circuit against a real MCP client before investing in surface.
- No CORS is added. MCP clients are not browsers, so the same-origin posture
  stands.

Exit: a real MCP client on the LAN authenticates with a key and calls the
identity tool, returning the correct user.

### Wave 3: Read Tools For Capex, Opex, And Inventory

- Contributor implementations in each of the three modules, registered in their
  module classes.
- Search and detail tools delegating to the existing `*ReadService` types.
- Tests per module: visibility respected, private records of other users never
  returned, result caps enforced, filters validated.
- The generality of the contract is proved by the third module requiring no
  platform change.

### Wave 4: Bounded Creates

- Creation tools for the agreed subset, delegating to existing `*WriteService`
  types.
- No update and no delete tools.
- Idempotency declared per tool. Where a create is not idempotent, that is stated
  in the tool description so the agent does not retry blindly.
- Tests: validation failures surface as stable codes, ownership is set to the key's
  user, writes are rejected for a revoked key.

### Wave 5: Hardening And Acceptance

- Rate limiting for the MCP endpoint, independent of normal authenticated API
  traffic.
- Observability: structured logging of tool invocation, outcome, and calling
  user, with no secrets, tokens, or private record contents. Segaris has no
  general audit table by decision, so what is retained about agent-driven writes
  is decided here explicitly rather than inherited.
- Frontend surface for key management under the profile.
- Operational documentation.
- Acceptance document.

## Suggested Pull Request Boundaries

1. Wave 0 spike and contracts.
2. Wave 1 API keys, backend only.
3. Wave 2 MCP host and identity tool.
4. Wave 3 read tools.
5. Wave 4 create tools.
6. Wave 5 hardening, key-management frontend, and acceptance.

Waves 1 and 2 are the meaningful review gates. Wave 1 changes the authentication
surface of the whole application; Wave 2 admits a prerelease dependency into the
main process.

## Documentation Updates

- `docs/architecture/backend.md`: the "Future User API Keys" section becomes
  implemented behaviour; add the MCP host, transport, and ownership split.
- `docs/architecture/integrations.md`: the document covers outbound adapters and
  inbound webhooks, but not an inbound agent-driven integration where an external
  caller pulls household data. Add that boundary, with the privacy position
  recorded above.
- `ROADMAP.md`: resolve the authentication extension-point note; revisit the
  backup-authorization note, which currently states that API keys are out of
  scope and that unattended automation uses an `Admin` cookie session.
- `README.md`: only if repository-wide commands or setup change.

## Open Questions

- Which creates belong in Wave 4. Capex and Opex entries are the obvious
  candidates; Inventory items, stock adjustments, and orders are separable and
  not all equally safe.
- Whether the backup automation client migrates to a key once one exists, or
  stays on its `Admin` cookie session. Out of scope here, but this plan removes
  the blocker.
- Whether key expiration is required at creation or remains optional. The
  accepted design allows a key with no automatic expiration.

## Plan Completion Criteria

- A LAN MCP client authenticates with a user-bound key and reads Capex, Opex, and
  Inventory data under that user's exact visibility.
- Bounded creates work and are attributed to the key's user.
- Revoking a key, or deactivating its user, immediately stops access.
- The MCP feature disabled by default leaves startup, readiness, and all existing
  behaviour unchanged.
- Adding a fourth module needs no change to `Platform/Mcp`.
