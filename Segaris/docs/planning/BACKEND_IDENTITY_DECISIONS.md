# Backend Identity Decisions

## Status

This record closes Wave 4 of the backend implementation plan. It documents the implemented identity, session, and administrative-user behavior and the decisions taken during implementation.

## Identity Integration

- ASP.NET Core Identity is integrated into the single `SegarisDbContext`. The context is **not** derived from `IdentityDbContext`; instead the Identity module contributes the Identity model through an `ISegarisModelContributor`, so `Segaris.Persistence` stays provider-neutral and free of Identity dependencies.
- Identity uses `int` keys (`IdentityUser<int>`, `IdentityRole<int>`) to align with the shared `UserId` primitive and the auto-incrementing integer key convention.
- `SegarisUser` adds `IsActive` (administrative activation state) and `CreatedAt` (UTC `DateTimeOffset`) to the standard Identity user.
- Identity tables use the documented `identity_*` snake_case table names with default Identity column names, matching the column convention established in Wave 2.

## Password Policy And Lockout

- Minimum length 12 characters; no required character-class composition.
- Five failed attempts trigger a 15-minute lockout, enabled for new users.
- Hashing uses the framework password hasher; no custom hashing is implemented.

## Credential Lifecycle

This resolves the previously open "Administrator credential lifecycle" decision and the plan's "Temporary-password and forced-change behavior" item.

- **First administrator (bootstrap):** created idempotently at startup from `Segaris:Identity:Bootstrap:UserName` and `:Password`. If no matching account exists it is created and assigned the `Admin` role. The password is a secret provided only through user secrets or environment/mounted configuration; nothing is committed. If the section is absent, only the platform roles are seeded.
- **Account creation and recovery:** administrators set a **permanent, immediately usable** password. There is no temporary-password or forced-change flow; users may change their own password but are not required to. This was chosen for operational simplicity in a small trusted household.
- **Self password change** (`POST /api/session/password`) rotates the security stamp but refreshes the current session so the active user is not signed out by their own change.
- **Administrative credential recovery** (`POST /api/admin/users/{id}/password`) resets the password and, by rotating the security stamp, invalidates the target account's active sessions.

## Sessions And Cookies

- Authentication uses the ASP.NET Core application cookie `segaris.session`: `HttpOnly`, `SameSite=Strict`, 12-hour sliding expiration, no persistent "remember me".
- The cookie is **not** `Secure` because the documented deployment serves plain HTTP on a trusted household network. HTTPS and `Secure` become mandatory before any remote exposure.
- Unauthenticated and unauthorized API requests return `401` and `403` instead of redirecting to a login or access-denied page.
- The security-stamp validation interval is zero, so deactivation and credential recovery take effect on the account's next request.

## Antiforgery

- Antiforgery is required for cookie-authenticated state-changing requests (`POST`, `PUT`, `PATCH`, `DELETE`) under the session and admin groups, enforced by a shared endpoint filter. Login is not antiforgery-gated because it is not yet cookie-authenticated and `SameSite=Strict` mitigates login CSRF.
- The SPA obtains the request token from `GET /api/session/antiforgery` (after authenticating, so the token is bound to the user) and sends it in the `X-CSRF-TOKEN` header. The antiforgery cookie `segaris.antiforgery` is `HttpOnly` and `SameSite=Strict`.

## Data Protection Keys

- Data Protection uses a fixed application name and persists keys to `Segaris:Storage:DataProtectionKeysPath` when configured. In Production the path is required and startup fails if it is missing; in Development the default key location is acceptable.

## Endpoints

```text
GET    /api/session/antiforgery   (anonymous)        Issues an antiforgery token
POST   /api/session               (anonymous)        Login
GET    /api/session               (authenticated)    Current session (id, userName, roles)
DELETE /api/session               (authenticated)    Logout
POST   /api/session/password      (authenticated)    Change own password
GET    /api/admin/users           (Admin)            Paginated account list
POST   /api/admin/users           (Admin)            Create account
POST   /api/admin/users/{id}/activate    (Admin)     Activate account
POST   /api/admin/users/{id}/deactivate  (Admin)     Deactivate and invalidate sessions
POST   /api/admin/users/{id}/password    (Admin)     Recover credentials and invalidate sessions
```

The Wave 1 frontend-core extension adds display names, language preference, and
avatar endpoints. Its complete contract and visibility policy are recorded in
`docs/planning/IDENTITY_PROFILE_DECISIONS.md`.

Login failures return a single generic `401` that does not reveal whether the account exists, is inactive, or had the wrong password. The `Admin` role does not bypass creator-only privacy.

## Implementation Notes For Future Modules

- **SQLite cannot `ORDER BY` a `DateTimeOffset`.** Because technical timestamps use `DateTimeOffset`, deterministic sorting must not order by a timestamp column when SQLite must remain usable. Auto-incrementing integer keys are monotonic with creation, so ordering by `id` reproduces creation order on both providers; the admin user list maps a `createdAt` sort to `id` ordering. A future provider-specific value converter could enable timestamp ordering if a module genuinely needs it.
- In-memory SQLite is held open for the host lifetime by a shared connection so startup migrations and seeded data survive across `DbContext` connections. This is a test convenience only; the file and PostgreSQL providers never use it.
- Roles are seeded as essential platform data at startup in every environment; the development `database seed`/`reset` commands also run this seed.
