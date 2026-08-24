# Identity, roles and the ownership perimeter

Blackwing is a private, multi-user application. Two properties hold the whole
product together, and both are settled here rather than in the screens that use
them: an account authenticates once and carries that identity through every
request, and **the content of an account never leaves it — not even towards an
administrator**.

## The identity module, and why it is a seam

Everything under `Blackwing.Api/Modules/Identity/` is a self-contained module
over ASP.NET Identity. It is deliberately isolated, because the household is
likely to grow a single Armali sign-on service later, and when that happens the
local user store should be replaceable without touching product code.

The module is a folder with two extension methods, not a registered module type:

- `AddBlackwingIdentity(IServiceCollection, IConfiguration)` — the user store,
  the cookie, antiforgery, the `Admin` policy and Data Protection.
- `MapBlackwingIdentityEndpoints(IEndpointRouteBuilder)` — the session and
  administration endpoints.

Segaris has a composition mechanism (`ISegarisModule`, `SegarisModules`) because
it hosts a dozen modules. Blackwing has one, so the machinery would be ceremony
that obscures the seam instead of marking it.

The seam is real and it is enforced by a test. `Blackwing.Persistence` **must not
reference `Microsoft.AspNetCore.Identity`**; it only knows
`IBlackwingModelContributor`, and the identity module contributes its own tables
(`identity_users`, `identity_roles`, `identity_user_claims`,
`identity_user_roles`, `identity_user_logins`, `identity_user_tokens`,
`identity_role_claims`) through it. `Blackwing.ArchitectureTests` fails if that
ever stops being true.

### What replacing this with an Armali SSO would touch

- `BlackwingUser` / `BlackwingRole` and `IdentityModelContributor` disappear, and
  with them the seven identity tables.
- `AddBlackwingIdentity` swaps the cookie handler for whatever the SSO speaks,
  and keeps the `Admin` policy and the antiforgery configuration.
- `SessionEndpoints` and `AdminUserEndpoints` either disappear or become thin
  proxies.
- **Nothing else moves.** `ICurrentUser`, `IOwnedByUser`, the global query filter
  and every product endpoint are written against an abstract "current account",
  not against ASP.NET Identity.

## The user model

`BlackwingUser` is `IdentityUser<int>` plus `DisplayName`, `IsActive` (defaults
to `true`) and `CreatedAt`. There is no `Language` — the interface is Spanish
only — and no avatar. Email is not required and not unique.

Roles come from the `PlatformRole` enum: `User` and `Admin`. They are seeded
idempotently at startup, so a fresh database and a restart behave the same.

## The ownership perimeter

This is the part that must not be got wrong. Every piece of user content
implements `IOwnedByUser` (`Blackwing.Shared/Ownership/`), and
`BlackwingDbContext` does three things with it:

1. **Global query filter.** For each entity type implementing `IOwnedByUser`, the
   context registers `HasQueryFilter(e => e.OwnerUserId == CurrentOwnerId)`.
   `CurrentOwnerId` is a private member of the context that reads `ICurrentUser`,
   and EF Core turns an instance member of the context into a query parameter:
   the value is re-evaluated on every query while the model is still built and
   cached exactly once.
2. **Zero is never a valid identifier.** `UserId` rejects zero and negatives, and
   `CurrentOwnerId` falls back to zero when nobody is authenticated. A context
   without a user therefore sees *absolutely nothing*, rather than seeing
   everything.
3. **Owner stamping on insert.** `SaveChanges` and `SaveChangesAsync` assign
   `OwnerUserId` to every added owned entity from the request identity, and throw
   `InvalidOperationException` if there is none. Forgetting to set the owner is
   not possible, and no request body can ever choose it.

The identity tables carry no filter: they are global by nature.

The alternative — scoping each query by hand, `.Where(x => x.OwnerUserId == me)`
— was rejected because a single omission is a silent data leak whose only signal
is somebody noticing. Here the safe path is the default one, and leaving it takes
an explicit `IgnoreQueryFilters` at the call site, which is visible in review.
`AGENTS.md` carries that rule: `IgnoreQueryFilters` is not used without a written
justification in the code itself.

`Blackwing.ArchitectureTests/OwnershipTests.cs` fails if an `IOwnedByUser` entity
reaches the model without a query filter.

### The ownership canary

`OwnedProbe` (table `ownership_probes`) and its endpoints under
`/api/platform/ownership` exist so the perimeter can be demonstrated end to end
with two real accounts *before* `Image` and `Tag` exist. It is production code
with its own migration and tests, not a test fixture — the same precedent as the
platform probes in Segaris.

Its contract carries the argument: the create endpoint **does not accept an
owner**, so impersonation cannot even be expressed, and reading another account's
probe answers **404, never 403** — a 403 would confirm that the identifier
exists, which is itself a leak.

**Phase 3 decides** whether this resource is retired when `Image` arrives, or
kept permanently. The case for keeping it is that an isolation test which depends
on no product entity stays green, and stays meaningful, however much the model
changes.

### Administrators are not exempt

`Admin` grants exactly five endpoints, all under `/api/admin/users`: list, create,
reset password, activate, deactivate. That is the entire administrative surface.
An administrator has **no route at all** to another account's content, and an
integration test asserts it against the canary.

If a review ever turns up an admin endpoint returning images, tags, or any
`IOwnedByUser` entity, it is wrong by definition — not a design trade-off.

Two safeguards, both of them answers to real failures:

- An administrator cannot deactivate themselves.
- The last *active* administrator cannot be deactivated: the house would be left
  with no way to manage accounts at all.

## Sessions

Login is a session cookie, `blackwing.session`: `HttpOnly`, `SameSite=Strict`,
sliding expiration of 12 hours. `SecurePolicy` is `None` because the deployment
is plain HTTP on a trusted household network; **HTTPS and `Secure` cookies become
mandatory before any remote exposure.**

The cookie handler's redirect events are replaced: unauthenticated answers
**401** and forbidden answers **403**. This is an API, not a site with login
pages, and a redirect to HTML is useless to the client.

`SecurityStampValidatorOptions.ValidationInterval` is `TimeSpan.Zero`. That is
what makes deactivating an account or resetting its password cut its live
sessions on the very next request, instead of up to thirty minutes later.

Login refuses an unknown user, a wrong password and a deactivated account with
**the same status and the same body**. The shape of the failure can never be used
to prove that an account exists. `PasswordSignInAsync` runs with
`lockoutOnFailure: true`; five failures lock the account for fifteen minutes.
Passwords require twelve characters and nothing else: length beats composition
rules.

Every state-changing request carries an antiforgery token in `X-CSRF-TOKEN`,
validated by `AntiforgeryEndpointFilter`; the token is issued anonymously by
`GET /api/session/antiforgery`.

### Data Protection

The key ring is persisted to `Blackwing:Storage:DataProtectionKeysPath` and the
application name is pinned to `Blackwing`. Without this, a container restart
silently invalidates every session cookie in the house — the failure looks like
"everyone got logged out for no reason", which is exactly the kind of thing
nobody debugs quickly. An integration test restarts the host and asserts the
cookie still works.

## Forwarded headers, and why the rate limiter depends on them

The login endpoint is rate limited to **10 attempts per minute per client IP**
(fixed window, no queue, `429` in `problem+json`).

Partitioning by client IP is only meaningful if the client IP is the real one.
Behind the Caddy ingress, `HttpContext.Connection.RemoteIpAddress` is *always the
address of the Caddy container*, so without forwarded headers every caller in the
house shares one partition and any single client can exhaust everyone's quota —
the limiter looks like it works and protects nothing.

`app.UseForwardedHeaders()` therefore runs first in the pipeline, before anything
reads the client address, configured with:

- `ForwardedHeaders.XForwardedFor` only. Scheme and host are not taken from
  headers.
- `ForwardLimit = 1`. Exactly one proxy sits in front of the backend. A longer
  chain would let a client prepend entries and choose which address is read as
  its own.
- `KnownIPNetworks` and `KnownProxies` **cleared and repopulated**, never left
  empty. The defaults trust loopback only, which is never the Compose case; empty
  collections trust everyone, which turns the limiter into decoration because
  then the *client* picks its own partition. Both are wrong. Blackwing trusts
  `172.16.0.0/12` — the Docker bridge pool Compose allocates its internal network
  from — plus loopback, so local runs and in-process test hosts can exercise the
  partitioning.

Caddy's `reverse_proxy` already sends `X-Forwarded-For`, so the `Caddyfile` needs
no change.

**Segaris does not have this configuration** and partitions by the connection
address behind the same kind of ingress, so its login rate limiting is
effectively disabled. It has not been touched — it is a different project — but
it is worth raising there.

## Bootstrapping the first administrator

`Blackwing:Identity:Bootstrap:UserName` and `:Password` create the first `Admin`
at startup if it does not already exist. The operation is idempotent: a second
start creates nothing.

The validator treats an **empty section as valid** and **one of the two values
present as a failure at startup**, so a half-written configuration fails loudly
instead of silently leaving the house without an administrator.

In Compose the values come from `BLACKWING_BOOTSTRAP_USERNAME` and
`BLACKWING_BOOTSTRAP_PASSWORD`, both empty in `.env.example`, needed only on the
very first start, and treated as a secret. Unlike Segaris, **no default
credentials are committed**: a versioned `admin/admin` is a publicly known
administrator account, not a convenience.
