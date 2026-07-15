# MCP Wave 0 Spike Notes

Date: 2026-07-15

## SDK Version

Segaris pins `ModelContextProtocol.AspNetCore` to `1.4.1` through Central
Package Management. The package restores net10 assets for
`ModelContextProtocol.AspNetCore`, `ModelContextProtocol`, and
`ModelContextProtocol.Core`.

## Registration Mechanism

The SDK supports explicit tool registration with `.WithTools<T>()` for ASP.NET
Core hosts, so Segaris can avoid assembly-wide attribute scanning. The first
Segaris contract is `ISegarisMcpToolContributor`, registered by each module
through its own `ISegarisModule.AddServices` method. Wave 2 can enumerate these
contributors from DI and register only the module-owned tool types surfaced by
registered modules.

The SDK also supports creating `McpServerTool` instances directly from
delegates, `MethodInfo`, or `AIFunction`. If the explicit generic registration
does not fit the final dynamic host shape, the fallback is an adapter that
creates `McpServerTool` instances from the contributor metadata. No documented
deviation from the module registration rule is currently needed.

## Current User And DI

The SDK documents that tool method parameters can be resolved from dependency
injection and that `CancellationToken` is bound to the request. Segaris'
`HttpCurrentUser` reads `IHttpContextAccessor.HttpContext.User`, so the planned
identity path remains viable for Streamable HTTP POST tool invocations.

The full end-to-end proof still belongs to Wave 2, after the API-key scheme
exists: the identity probe tool must be called through `/mcp` by a real
Streamable HTTP client and must return the authenticated user. If that client
test fails, Wave 2 must add a tool-specific identity adapter before any business
tool ships.

## Wave 0 Tool Surface

MCP list/search tools use a lower result cap than REST pagination:

- Default limit: `10`
- Maximum limit: `20`

Final read tool names:

- `capex_search_entries`
- `capex_get_entry`
- `capex_list_categories`
- `opex_search_entries`
- `opex_get_entry`
- `opex_list_categories`
- `inventory_search_items`
- `inventory_get_item`
- `inventory_list_locations`

Search/list tools return compact summaries; detail tools return one full record
by identifier. All Wave 0 contracts are read-only and idempotent.

