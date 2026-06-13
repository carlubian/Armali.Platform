# Identity Profile Decisions

## Status

This record closes Wave 1 of the frontend and application-core implementation
plan. It defines the backend contract consumed by the profile, shared shell, and
administrative user-management frontend work.

## Persisted Profile

`SegarisUser` stores:

- `DisplayName`: required, trimmed, 1 to 200 characters.
- `Language`: required and validated against an explicit allow-list. The only
  supported value is currently `en-GB`.

The paired `IdentityProfile` SQLite and PostgreSQL migrations add both columns.
Existing users receive their current user name as the display name and `en-GB`
as the language. New bootstrap and administratively created users also start
with their user name as the display name.

## Profile Contract

```text
GET    /api/session/profile          Returns displayName, language, avatarUrl
PUT    /api/session/profile          Updates displayName and language
PUT    /api/session/profile/avatar   Uploads or replaces the current avatar
GET    /api/session/profile/avatar   Streams the current avatar
DELETE /api/session/profile/avatar   Removes the current avatar
GET    /api/users/{id}/avatar        Streams a household user's avatar
```

The profile update request is:

```json
{
  "displayName": "Household Admin",
  "language": "en-GB"
}
```

Unsupported languages return `400` with a field error for `language`; they are
never coerced. Profile and avatar mutations require the normal authenticated
antiforgery token.

The current-session response additionally exposes `displayName`, `language`,
and nullable `avatarUrl`. Administrative user-list items expose `displayName`
and nullable `avatarUrl`. Avatar references use `/api/users/{id}/avatar` so the
same URL works in the shared shell and administrative cards.

## Avatar Policy

Avatars use the shared attachment service with this owner identity:

```text
Module: Identity
EntityType: UserAvatar
EntityId: current integer user ID
```

Only JPEG, PNG, and WebP are accepted. Extension, declared media type, and file
signature are validated by the existing attachment policy, and the existing
25 MiB attachment limit remains authoritative.

An upload creates the new attachment before deleting the previous one, so a
failed new upload preserves the existing avatar. Each user has at most one
avatar after a successful request. Deletion is immediate.

Any authenticated household user may read another user's avatar. Only the
owner may upload, replace, or remove their avatar because all mutations are
available exclusively through the current-session routes. Missing users and
missing avatars return `404`.

## Administrative User Editing (Wave 8)

Wave 8 closes the Wave 7 gap where administrators could create, reset,
activate, and deactivate accounts but could not edit an existing member's
display name or role.

### Endpoint Contract

```text
PUT /api/admin/users/{id}   Updates a member's display name and role
```

The request is an explicit DTO (no EF entity serialization):

```json
{
  "displayName": "Household Admin",
  "role": "Admin"
}
```

The endpoint is `Admin`-only and antiforgery-gated, like the other
`/api/admin/users` mutations. It returns the standard `AdminUserResponse`
(id, userName, displayName, roles, isActive, createdAt, avatarUrl). A missing
user returns `404`.

`displayName` reuses the self-service profile rule (trimmed, 1 to 200
characters) through `IdentityProfilePolicy.TryNormalizeDisplayName`. `role`
reuses the create-flow `User`/`Admin` allow-list. Invalid values return `400`
with field-keyed errors (`displayName`, `role`).

The role is changed through `UserManager`: when the requested role differs from
the current one, the previous role assignments are removed and the new role is
assigned. When the role is unchanged, only the display name is updated.

### Role-Change Rules

- **No self role change.** An administrator cannot change their own role; the
  request is rejected with a `400` `role` field error. A different
  administrator must perform the change. Administrators may still edit their own
  display name.
- **Last-administrator guard.** Demoting a user who is the only remaining
  administrator is rejected with a `400` `role` field error. With the
  self-role-change prohibition in place this guard is effectively a
  defence-in-depth measure — the actor is always an administrator, so any other
  administrative target implies at least two administrators — but it is retained
  so the lock-out invariant does not depend solely on the self-change rule.

### Create-Time Display Name

The create flow is deliberately left unchanged: a new account's display name is
still derived from its username. Administrators set a distinct display name
afterwards through the new edit endpoint. No schema change is introduced, since
`DisplayName` already exists.
