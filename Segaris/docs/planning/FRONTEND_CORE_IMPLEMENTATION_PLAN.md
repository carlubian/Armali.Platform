# Frontend And Application Core Implementation Plan

## Purpose

This plan translates the Phase 1 frontend and design-system decisions into an executable technical backlog for the Segaris frontend, a small scoped backend extension, automated tests, and deployment assets.

It is a foundation plan, not a Phase 3 product-version plan. It deliberately avoids detailed business-module behavior until the Phase 2 functional requirements exist. It produces a functional **application core**: sign-in, self-service profile management (including an avatar photo), and administrative user management, presented inside the shared shell and a minimal launcher described in `docs/architecture/user-experience.md`.

This plan builds directly on:

- `docs/architecture/frontend.md` — React, TypeScript, Vite, React Router, TanStack Query, Context, React Hook Form + Zod, i18next, error boundaries, and Suspense conventions.
- `docs/architecture/design-system.md` — the adopted Project Armali design system, the ported component catalog, and the selected screen variants (Login: centered card; User management: cards; logo replacement).
- `docs/architecture/user-experience.md` — the launcher-as-entry-point model, the shared shell's "return to launcher" convention, and the attention/toast/calendar feedback model.
- `docs/planning/BACKEND_CORE_IMPLEMENTATION_PLAN.md` and its companion decision documents — the existing `/api/session` and `/api/admin/users` contracts, module conventions, and CI/Compose topology that this plan extends.

## Target Outcome

At the end of this plan, the repository should contain:

- A deployable `src/frontend` React + TypeScript + Vite single-page application, built and served as static assets by a real frontend container that replaces `deploy/frontend-placeholder`.
- A shared shell (top bar, "return to launcher" action, session/profile awareness) and a minimal launcher presenting "Users" (Admin only) and "My profile" as module cards, structured so future business-module cards can be added without reworking the shell.
- A working sign-in flow (centered-card login) against the existing `/api/session` endpoints, including antiforgery handling and session-expiry redirects.
- A self-service profile screen where any user can edit their display name, language preference, password, and avatar photo, with changes reflected immediately in the shared shell.
- An administrative user-management screen (cards variant) covering list, create, activate/deactivate, and password reset against `/api/admin/users`.
- A small, scoped extension to the backend Identity module adding the user-profile fields and avatar endpoints the frontend needs, with paired SQLite/PostgreSQL migrations and tests.
- The Project Armali design tokens and the listed shared components (Button, IconButton, Input, Select, Checkbox, Switch, Card, Badge, Avatar, Tooltip, Toast, Spinner, Tabs, Dialog) ported as typed React components.
- i18next bootstrapped with a shared `platform` namespace, complete in `en-GB`.
- TanStack Query, routing, and error-boundary conventions that categorize authentication, authorization, validation, not-found, transient, and unavailable failures per `docs/architecture/frontend.md`.
- A full Docker Compose stack (PostgreSQL, backend, real frontend, Caddy) that serves the application core end to end, with CI enforcing a new required "Segaris Frontend" check and an automated end-to-end check covering login, profile, and admin flows.

## Scope Boundaries

Included:

- Frontend repository scaffolding, tooling, scripts, and developer commands.
- The design-system token and component port described in `docs/architecture/design-system.md`.
- The shared shell, routing, session context, TanStack Query setup, error boundaries, and i18next bootstrap.
- Login, self-service profile (including avatar), and administrative user-management screens.
- A minimal launcher with only the module cards that exist at the end of this plan.
- The scoped backend Identity extension required for the profile and avatar contracts (new fields, migrations, endpoints), reusing the existing Attachments service.
- Frontend Dockerfile, Compose updates, CI jobs, image publication updates, and developer documentation.

Excluded:

- Spanish (`es-ES`) translations. The i18next architecture must support them, but only `en-GB` strings are written in this plan.
- Business modules (Capex, Opex, Inventory, Travel, and the rest of the Phase 2 backlog) and their launcher cards.
- The calendar view, a unified notification inbox, and the command palette.
- Alternate themes or dark mode; the design system is light-theme only.
- Changes to the session cookie's `Secure` flag, CORS policy, or any posture change required for deployment beyond the trusted local network.
- Any backend change beyond the scoped Identity-profile/avatar extension defined in this plan.

## Decisions Required Before Or During Implementation

The following open items must be resolved at the indicated point. They should not silently acquire implementation defaults.

| Decision                                                                                                                                    | Required by         |
| ------------------------------------------------------------------------------------------------------------------------------------------- | ------------------- |
| Frontend package manager, lint/format/test tooling, and directory conventions                                                               | Resolved in Wave 0  |
| Development integration between the Vite dev server and the backend (proxy vs. CORS)                                                        | Resolved in Wave 0  |
| Frontend environment/configuration contract (`src/frontend/.env.example`)                                                                   | Resolved in Wave 0  |
| Self-service profile and avatar API contract, including endpoint routes, DTO shapes, migration names, and the admin-list response extension | Resolved in Wave 1  |
| Avatar validation rules (allowed types, size, single-avatar replace semantics) on top of the existing attachment policy                     | Resolved in Wave 1  |
| Frontend container build and static-asset-serving strategy, replacing `deploy/frontend-placeholder`                                         | Resolved in Wave 10 |
| Required CI checks for the frontend and placement of end-to-end tests                                                                       | Resolved in Wave 10 |

## Delivery Strategy

Work is divided into dependency-ordered waves. A wave may be split into multiple pull requests, but its exit criteria should pass before work that depends on it is considered complete. Waves 5 through 7 (Login, Profile, Admin user management) share the same foundation from Wave 4 and may proceed in parallel once Wave 4 is complete; Wave 1 (backend extension) only blocks Wave 6 (Profile).

### Wave 0: Resolve Frontend Foundation Blockers

Status: **Completed**.

Tasks:

1. Select the package manager (recommended: pnpm), Node.js LTS version, and pin them for the repository.
2. Select and configure lint/format/test tooling: ESLint (flat config) with `typescript-eslint`, React, React Hooks, and `jsx-a11y` plugins; Prettier; Vitest with Testing Library for unit and component tests; Playwright for end-to-end tests, per `docs/architecture/development-and-operations.md`.
3. Decide how the Vite development server reaches the backend during local development. Recommended: a Vite dev-server proxy for `/api`, so cookies remain same-origin and the backend's `SameSite=Strict` session and antiforgery cookies work unchanged without introducing a CORS policy.
4. Define the `src/frontend/.env.example` contract: build-time `VITE_*` variables only, no secrets, mirroring the role of `src/backend/appsettings.example.json`.
5. Record the frontend source-tree conventions (for example `src/frontend/src/{app,components,modules,styles,...}`) consistent with the module-namespace guidance in `docs/architecture/frontend.md`.
6. Add any newly discovered open items to `ROADMAP.md`.

Deliverables:

- `docs/planning/FRONTEND_FOUNDATION_DECISIONS.md` recording the tooling, dev-proxy decision, environment contract, and directory conventions.

Exit criteria:

- Wave 2 can scaffold the frontend repository without inventing undocumented conventions.

Resolution: the runtime, package manager, tooling boundaries, test placement,
same-origin development proxy, public environment contract, and source-tree
conventions are recorded in `docs/planning/FRONTEND_FOUNDATION_DECISIONS.md`.

### Wave 1: Backend Identity-Profile Extension

Status: **Completed**.

Tasks:

1. Extend `SegarisUser` (`src/backend/Segaris.Api/Modules/Identity/SegarisUser.cs`) with a display name and a language-preference field, defaulting the language to `en-GB`.
2. Add paired SQLite and PostgreSQL migrations for the new fields, following the paired-migration convention established for Attachments and Jobs, including PostgreSQL upgrade coverage from the current schema.
3. Add `GET` and `PUT /api/session/profile` endpoints (display name and language) to the session endpoint group (`src/backend/Segaris.Api/Modules/Identity/Endpoints/SessionEndpoints.cs`), antiforgery-gated on the mutation, following `docs/planning/BACKEND_MODULE_CONVENTIONS.md`.
4. Add avatar endpoints under `/api/session/profile/avatar` (`PUT` to upload or replace, `GET` to stream, `DELETE` to remove), implemented through the existing `IAttachmentService` with an `AttachmentOwner` scoped to the Identity module and the current user. Replacing an avatar removes the previous attachment.
5. Restrict avatar uploads to common image types within the existing attachment size policy, in addition to the generic allow-list validation.
6. Validate the `language` field against an explicit allow-list (`en-GB` only for now); reject unsupported values with `400` rather than coercing them.
7. Extend the current-session and admin user-list responses with the display name and an avatar reference, so the frontend can present consistent identity information across the profile and admin screens.
8. Update OpenAPI summaries and confirm `/openapi/v1.json` and `/scalar` reflect the new endpoints.

Tests:

- Unit tests for profile-update and language-allow-list validation.
- API integration tests for profile get/update, avatar upload/replace/download/delete, ownership (a user cannot read or modify another user's profile or avatar), and antiforgery enforcement.
- SQLite and PostgreSQL migration tests covering upgrade from the current schema.

Deliverables:

- An addendum to `docs/planning/BACKEND_IDENTITY_DECISIONS.md` (or a new `docs/planning/IDENTITY_PROFILE_DECISIONS.md`) recording the endpoint contract, DTO shapes, migration names, language allow-list, and avatar ownership/replace semantics.

Exit criteria:

- The frontend can retrieve and update a user's display name, language, and avatar entirely through documented `/api/session/profile*` endpoints on both database providers.

Resolution: the profile DTOs, `IdentityProfile` migration pair, `en-GB`
allow-list, avatar validation and replacement behavior, and authenticated
household avatar-read policy are recorded in
`docs/planning/IDENTITY_PROFILE_DECISIONS.md`.

### Wave 2: Frontend Repository And Scaffold

Status: **Completed**.

Tasks:

1. Create `src/frontend` as a Vite + React + TypeScript application, following the conventions recorded in Wave 0.
2. Configure ESLint, Prettier, `tsconfig`, and path aliases.
3. Add `package.json` scripts for development, build, preview, lint, format, type-checking, unit tests, and end-to-end tests.
4. Add repeatable developer scripts under `scripts/`, following the `{layer}-{action}` convention used by the backend (`frontend-restore.ps1`, `frontend-build.ps1`, `frontend-lint.ps1`, `frontend-test.ps1`, `frontend-run.ps1`).
5. Configure the Vite development-server proxy for `/api` per the Wave 0 decision.
6. Add `src/frontend/.env.example` and ignore real local environment files.
7. Add baseline Vitest, Testing Library, and Playwright configuration with placeholder smoke tests.
8. Add a minimal root application that renders a placeholder page, so the build and run commands succeed end to end.

Tests:

- Clean install, lint, type-check, unit-test run, and production build all succeed from a clean checkout.
- A placeholder end-to-end test launches the application and confirms it renders.

Exit criteria:

- A contributor can clone the repository, install dependencies, build the frontend, and run it locally against the backend through the development proxy.

Resolution: `src/frontend` is scaffolded as a Vite + React + TypeScript SPA with
ESLint (flat), Prettier, project-referenced `tsconfig`, the `/api` development
proxy, the `.env.example` contract, and colocated Vitest plus
`tests/frontend/e2e` Playwright smoke coverage. The `{layer}-{action}`
PowerShell wrappers (`frontend-restore|build|lint|format|test|run`) and the
exactly pinned dependency set are committed with `pnpm-lock.yaml`. Selected
versions and the ESLint 9 pin are recorded in
`docs/planning/FRONTEND_FOUNDATION_DECISIONS.md`.

### Wave 3: Design-System Token And Component Port

Status: **Completed**.

Tasks:

1. Port the Project Armali design tokens (fonts, colors, typography, spacing, effects, base styles) from `docs/ui-design/_ds/.../tokens/*.css` into `src/frontend`'s global styles, self-hosting the existing woff2 fonts. The unused `--sidebar-w` token is dropped, per `docs/architecture/design-system.md`.
2. Re-implement the Project Armali components — Button, IconButton, Input, Select, Checkbox, Switch, Card, Badge, Avatar, Tooltip, Toast, Spinner, Tabs, and Dialog — as typed React components under a shared component area, preserving the variants, sizes, and states shown in `_ds_bundle.js`.
3. Replace the prototype's CDN-loaded Lucide icons and `window`-based `createIcons()` pattern with the `lucide-react` package.
4. Add the Armali logo (`docs/ui-design/uploads/New Armali Logo.png`) as a frontend asset, replacing the prototype's inline SVG brand mark.
5. Add component-level tests covering interactive states: focus, keyboard activation, disabled state, and controlled value changes.

Tests:

- Render and interaction tests for each ported component.
- A test confirming the design tokens are loaded and applied (for example, that the expected CSS custom properties resolve).

Exit criteria:

- All listed components are available as typed, framework-native components with consistent variants, sizes, and states, independent of the prototype's runtime dependencies.

Resolution: the Project Armali tokens were ported to
`src/frontend/src/styles/tokens/*.css` (self-hosting the four woff2 fonts under
`src/assets/fonts`, dropping `--sidebar-w`) and chained from `global.css`. The
fourteen components are reimplemented as typed React components under
`src/components/ui/` with co-located CSS imported statically by Vite, preserving
the `arm-*` class names and replacing the prototype's `ensureStyle()`/`window`
runtime pattern. Form controls forward their `ref` for React Hook Form, the icon
strategy uses the `lucide-react` package (pinned `1.17.0`), and the Armali logo
is added at `src/assets/armali-logo.png`. The `IconButton` label is mandatory,
and the `Dialog` adds Escape-to-close and panel focus management over the
prototype. Component render/interaction tests and a token test accompany the
port.

### Wave 4: Application Shell, Routing, Session, Query, And Error Handling

Status: **Completed**.

Tasks:

1. Configure React Router with a public login route, a protected route tree for the shared shell and launcher, and an explicit not-found route, per `docs/architecture/frontend.md`.
2. Implement a session context backed by `GET /api/session` and `GET /api/session/profile`, covering loading, authenticated, and unauthenticated states.
3. Configure the TanStack Query client and a shared API client that attaches the `X-CSRF-TOKEN` header (obtained from `/api/session/antiforgery`) to mutating requests, and categorizes failures into expired authentication, authorization denial, validation, not-found, transient, and backend-unavailable, per `docs/architecture/frontend.md`.
4. Implement root and module-level React error boundaries with translated recovery actions that do not auto-retry.
5. Bootstrap i18next and react-i18next with a shared `platform` namespace (`en-GB` only) and wire `Intl`-based date/number formatting helpers.
6. Implement the shared `ServiceUnavailable` and `NotFound` screens from the design-system port.
7. Implement the shared shell top bar (brand mark, "return to launcher" action, profile entry, sign-out) wrapping the protected route tree.

Tests:

- Routing tests: unauthenticated users are redirected to login; authenticated users reach the launcher; unknown routes render the not-found screen.
- Session-context tests: initial load, session expiry triggers a redirect to login, sign-out clears the session.
- API client tests: the antiforgery header is attached to mutations, a `401` triggers the expired-session path, and an unreachable backend triggers the service-unavailable screen.
- An i18next test detects missing `platform` keys and confirms `en-GB` fallback behavior.
- An error-boundary test confirms a simulated rendering failure shows the recovery screen without looping.

Exit criteria:

- An authenticated session loads the shared shell and launcher chrome; unauthenticated access redirects to login; the global failure and not-found states render correctly before any feature screen exists.

Resolution: React Router now separates the public login entry point, protected
shell routes, module boundaries, and the explicit not-found route. TanStack
Query owns the combined session/profile view through a focused session context;
the shared API client supplies antiforgery tokens, bounded request timeouts, and
typed failure categories. The `platform` i18next namespace, `Intl` formatters,
root/module error boundaries, shared loading/unavailable/not-found screens, and
the shell top bar are implemented with focused Vitest coverage.

### Wave 5: Login

Status: **Completed**.

Tasks:

1. Implement the centered-card login screen from the design-system port, including the aurora background and the replacement brand mark.
2. Build the login form with React Hook Form and a Zod schema for client-side presence checks; the backend remains authoritative for credential validation.
3. Submit the form through a TanStack Query mutation against `POST /api/session`; on success, populate the session context and navigate to the launcher.
4. Present invalid credentials as a generic, non-revealing form-level error, and handle login rate-limiting and unexpected failures through the shared error treatment.
5. Add `platform` i18n keys for the login screen.
6. Ensure the first invalid field is focusable after a failed submission and that the submit action is disabled while pending.

Tests:

- A successful login navigates to the launcher and populates the session context.
- Invalid credentials produce a generic error without revealing account existence or state.
- Accessibility checks cover labels, error association, and keyboard-only submission.
- An end-to-end test signs in with seeded credentials and reaches the launcher.

Exit criteria:

- A user can sign in through the real interface against the running backend and land on the launcher shell.

Resolution: the centered-card `LoginPage` (`src/frontend/src/modules/auth/`)
replaces the Wave 4 placeholder, rendering the aurora background and the Armali
logo brand mark over a glass card. The form uses React Hook Form with a Zod
presence schema (`react-hook-form`, `zod`, and `@hookform/resolvers` were added,
exactly pinned) and submits through a TanStack Query mutation against
`POST /api/session`; on success it refreshes the session context and navigates
to the launcher. A failed sign-in is reported as a single generic, non-revealing
form-level alert, with rate limiting (`429`) given a distinct message and other
failures a generic one. To keep a login `401` local to the form, `apiRequest`
gained a `suppressSessionExpired` option that the new `sessionApi.signIn` sets,
so an invalid credential no longer triggers the global session-expired redirect.
The submit action is disabled while pending and focus returns to the first field
after a failure. Login strings live under `platform.auth.login` (`en-GB`).
Component/accessibility Vitest coverage and an env-driven full-stack Playwright
sign-in journey (skipped without seeded credentials, replacing the placeholder
smoke spec) accompany the screen.

### Wave 6: Self-Service Profile, Including Avatar

Status: **Completed**. Depends on Waves 1 and 4.

Tasks:

1. Implement the profile screen from the design-system port (`screens-profile-states.jsx`), adapted to real routing and data.
2. Build a display-name and language form using React Hook Form and Zod, submitted through `PUT /api/session/profile`, invalidating the session context on success.
3. Populate the language selector from the allow-list defined in Wave 1 (`en-GB` only for now), with a contract that supports future additions without UI rework.
4. Build the password-change form against `POST /api/session/password`, with current/new/confirm fields, mapped backend validation errors, and a success toast confirming the session remains active.
5. Implement the avatar control: display the current avatar or an initials fallback using the `Avatar` component, with upload, replace, and remove actions against `/api/session/profile/avatar`, including client-side pre-validation that mirrors the backend's type and size policy.
6. Add an unsaved-changes guard using React Hook Form's dirty state, per `docs/architecture/frontend.md`.
7. Add `platform` i18n keys for all profile strings, including avatar error messages.

Tests:

- Updating display name and language is reflected in the shared shell without a reload.
- Password-change success and failure paths, including a rejected current password.
- Avatar upload, replace, and remove, including client-side and backend-side rejection of invalid files.
- The unsaved-changes guard blocks navigation away from a dirty form until confirmed.
- An end-to-end test signs in, edits the display name and avatar, and confirms the shared shell reflects both.

Exit criteria:

- Any authenticated user can edit their display name, language, password, and avatar, with changes reflected immediately across the shared shell.

Resolution: the profile placeholder is replaced by the responsive Project
Armali profile screen under `src/frontend/src/modules/profile/`. React Hook Form
and Zod own separate profile and password forms, with the Wave 1 language and
password policies mirrored for immediate feedback and translated API failures
kept at the form boundary. Profile and avatar mutations update the TanStack
Query session-profile cache directly, including avatar cache busting, so the
shared shell reflects changes without a reload. Avatar upload, replacement, and
removal enforce the JPEG/PNG/WebP and 25 MiB client policy before the backend's
authoritative validation. The application now uses React Router's data-router
provider, without loaders or actions, so dirty profile/password forms can block
internal navigation through the shared confirmation dialog while
`beforeunload` covers refresh and tab closure. Focused Vitest coverage and an
environment-gated full-stack Playwright journey cover profile details,
password success/rejection, avatar lifecycle and rejection, shell
synchronization, and unsaved navigation.

### Wave 7: Administrative User Management

Status: **Completed**. Depends on Wave 4.

Tasks:

1. Implement the cards-variant user-management screen from the design-system port, including per-user avatars, role and status badges, and edit/activate/deactivate actions.
2. Fetch the paginated user list through `GET /api/admin/users`, respecting the backend's pagination and deterministic sorting conventions.
3. Implement the create/edit user dialog with React Hook Form and Zod, mapping backend validation errors to fields, covering account creation and password reset (`POST /api/admin/users/{id}/password`).
4. Implement activate and deactivate actions (`POST /api/admin/users/{id}/activate|deactivate`), with a confirmation step for deactivation as a destructive action.
5. Guard the screen behind the `Admin` role on the client as a convenience; if a non-admin reaches the route directly, the backend's `403` is shown through the shared authorization-denied state without revealing data.
6. Add `platform` i18n keys for all user-management strings.

Tests:

- List rendering, pagination, and sorting.
- Account creation, including validation errors, and password reset.
- Activation and deactivation, including the deactivation confirmation.
- A non-admin user is shown the access-denied state without leaking data.
- An end-to-end test signs in as an administrator, creates a user, deactivates it, and resets its password.

Exit criteria:

- An administrator can fully manage household accounts through the interface, matching the documented `/api/admin/users` contract.

Resolution: the cards-variant user-management screen lives under
`src/frontend/src/modules/admin/UsersPage.tsx`, reachable from an Admin-only
launcher entry (the full module grid arrives in Wave 9) and guarded both on the
client (the `Admin` role) and by surfacing the backend's `403` through a new
shared `AccessDenied` system screen. The paginated list (`GET /api/admin/users`)
uses explicit previous/next pagination and a sort control mapped to the
backend's `userName`/`createdAt` allow-list. Account creation
(`POST /api/admin/users`), password reset (`POST /api/admin/users/{id}/password`),
and activate/deactivate (`POST /api/admin/users/{id}/activate|deactivate`, with a
confirmation step before deactivation) are implemented with React Hook Form, Zod,
field-mapped backend validation, and TanStack Query cache invalidation, plus
`platform.admin.users` i18n keys and a full-stack Playwright journey gated on
seeded administrator credentials.

Scope note: the existing `/api/admin/users` contract supports creating accounts,
resetting passwords, and activating/deactivating them, but has **no** endpoint to
change an existing member's display name or role (and `create` derives the
display name from the username). At the project owner's request, editing the
display name and role is treated as a deliberate gap and deferred to **Wave 8**
below, which adds the required backend endpoints and the frontend edit dialog.
The remaining launcher and shared-shell completion work shifts to **Wave 9**, and
the containers/Compose/CI completion work to **Wave 10**.

### Wave 8: Administrative User Editing (Display Name And Role)

Status: **Completed**. Depends on Wave 7.

This wave closes the gap identified in Wave 7: administrators can create,
reset, activate, and deactivate accounts, but cannot yet edit an existing
member's display name or role. It is a small backend Identity extension plus the
paired frontend edit dialog, structured like Wave 1 (frontend against a
documented contract) so it can ship as one reviewable change.

Tasks:

1. Add an admin update endpoint to `AdminUserEndpoints`
   (`PUT /api/admin/users/{id}`) that updates a member's display name and role,
   antiforgery-gated, with explicit DTOs (no EF entity serialization). Changing
   the role removes the previous role and assigns the new one through
   `UserManager`; reuse the `User`/`Admin` allow-list and the display-name
   validation already used by the self-service profile.
2. Decide and document whether an administrator may change their own role, and
   guard against removing the last remaining administrator so a household cannot
   lock itself out of administration.
3. Extend the create flow if desired so a separate display name can be set at
   creation time rather than derived from the username, or explicitly keep the
   derive-from-username behavior and document it.
4. In the frontend admin screen, add an "Edit" action per user card opening a
   dialog (React Hook Form + Zod) that edits display name and role against the
   new endpoint, mapping backend validation errors to fields and invalidating the
   user list on success. Keep password reset and activate/deactivate as the
   existing discrete actions.
5. Add the new `platform.admin.users.edit.*` i18n keys (`en-GB`).

Tests:

- Backend unit and integration tests for the update endpoint, including role
  reassignment, the last-administrator guard, display-name validation, ownership
  of the antiforgery requirement, and `403` for non-administrators.
- Paired SQLite/PostgreSQL coverage only if a schema change is introduced
  (none is expected, since `DisplayName` already exists).
- Frontend tests for the edit dialog: a successful display-name/role change is
  reflected in the list, and a backend validation error maps to the form.

Deliverables:

- An addendum to `docs/planning/IDENTITY_PROFILE_DECISIONS.md` (or a new admin
  decision document) recording the update endpoint contract, the role-change and
  last-administrator rules, and the create-time display-name decision.

Exit criteria:

- An administrator can edit a household member's display name and role through
  the interface, in addition to creating, resetting, activating, and
  deactivating accounts.

Resolution: a `PUT /api/admin/users/{id}` endpoint (`AdminUserEndpoints`,
antiforgery-gated, explicit `UpdateUserRequest`/`AdminUserResponse` DTOs) updates
a member's display name and role, reusing the self-service display-name rule
(`IdentityProfilePolicy.TryNormalizeDisplayName`) and the create-flow
`User`/`Admin` allow-list, reassigning the role through `UserManager` only when
it changes. Per the project owner's decisions, an administrator **cannot** change
their own role (a `400` `role` field error) but may edit their own display name,
and a defence-in-depth last-administrator guard is retained; the create flow
keeps deriving the display name from the username (no schema change). The
frontend admin screen (`UsersPage.tsx`) gains a per-card "Edit" action opening a
React Hook Form + Zod dialog against the new endpoint, with the role selector
locked when editing yourself, field-mapped backend validation, and user-list
invalidation on success, plus `platform.admin.users.edit.*` `en-GB` keys.
Backend integration tests cover role reassignment, the self-role prohibition,
own display-name editing, display-name validation, antiforgery enforcement, and
the `403` for non-administrators; frontend tests cover the edit success path,
backend validation mapping, and the self-edit role lock. The contract and rules
are recorded in `docs/planning/IDENTITY_PROFILE_DECISIONS.md`.

### Wave 9: Launcher And Shared-Shell Completion

Status: **Completed**. Depends on Waves 5, 6, and 7.

Tasks:

1. Implement the launcher module grid from the design-system port, showing only the "Users" card (visible to Admins) and the "My profile" card (visible to everyone), with a card data model that allows future business-module cards to be appended without reworking the shell.
2. Define the `ModuleCard` attention-indicator contract (an optional flag rendered as the breathing dot from `docs/architecture/user-experience.md`), left unused by both current cards but ready for future modules.
3. Wire the "return to launcher" action across the profile and admin routes, and confirm the launcher itself acts as the shell's home.
4. Implement sign-out (`DELETE /api/session`), clearing the session context and returning to login from any shell route.
5. Complete a final pass over the `platform` i18n namespace to confirm no missing `en-GB` keys remain across login, launcher, profile, and admin screens.
6. Complete an accessibility pass: keyboard navigation through the launcher, shell top bar, and all forms; `prefers-reduced-motion` handling for the aurora background and toasts.

Tests:

- The launcher shows the correct cards per role: administrators see "Users" and "My profile"; other users see only "My profile".
- A full navigation loop: launcher to profile, back to launcher, launcher to user management (as an administrator), back to launcher.
- An i18n test covers the complete `platform` namespace for missing keys.
- Automated accessibility checks cover the launcher, login, profile, and admin screens.

Exit criteria:

- The application core — login, launcher, profile including avatar, and administrative user management — is navigable end to end as one coherent shell, in English.

Resolution: the launcher placeholder is replaced by a responsive Project Armali
module grid backed by a typed `ModuleCardModel` catalog. It currently exposes
"My profile" to every authenticated user and "Household users" only to
administrators, while preserving optional role and attention-indicator fields
for future modules without adding domain data to the launcher. The shared top
bar provides the consistent return-to-launcher, profile, avatar, and sign-out
actions on every protected route. Sign-out now disables itself while the
antiforgery-protected `DELETE /api/session` request is pending, clears the
session and returns to login on success, and keeps the active shell with an
accessible persistent error toast if the request fails.

The `platform` resource now contains the final launcher and sign-out copy, and
its tests scan literal application translation calls in addition to validating
the registered fallback resource. Reduced-motion handling covers pseudo-elements,
transitions, the aurora, dialogs, toasts, spinners, and the optional breathing
attention dot. Focused component/application tests cover role-filtered cards,
the attention contract, the full launcher/profile/admin navigation loop, and
sign-out success/failure. `axe-core` audits cover login, launcher, profile, and
administration, and an environment-gated Playwright journey covers the complete
shared-shell navigation and sign-out flow against the running stack.

### Wave 10: Containers, Compose, And CI Completion

Status: **Not started**.

Tasks:

1. Add a multi-stage `src/frontend/Dockerfile`: a Node build stage that installs dependencies and builds the application, and a Caddy-alpine runtime stage that serves the built assets with single-page-application fallback routing, matching the existing frontend service's health check.
2. Update `deploy/compose/docker-compose.local.yml` to build the `frontend` service from `src/frontend` instead of `deploy/frontend-placeholder`, and update `deploy/compose/.env.example`'s default frontend image accordingly.
3. Retire `deploy/frontend-placeholder` once the real image is wired in, or clearly mark it as historical and unused.
4. Confirm the Caddy ingress configuration still routes `/api/*` to the backend and all other traffic to the frontend, with correct single-page-application fallback behavior.
5. Add a new required "Segaris Frontend" GitHub Actions check (install, lint, type-check, unit/component tests, build) alongside the existing "Segaris Backend", "Segaris PostgreSQL", and "Segaris Compose" checks.
6. Extend the Compose smoke test, or add an end-to-end job, that runs the login, profile, and administrative user-management flows against the full Compose stack.
7. Update the image-publication workflow to build and publish the real `segaris-frontend` image from `src/frontend/Dockerfile` instead of the placeholder.
8. Update `scripts/foundation-acceptance.ps1` to include the frontend build, tests, and the new end-to-end run.
9. Update the root developer documentation describing how to run the frontend and backend together locally, natively and through Compose.

Tests:

- The frontend Docker image builds and serves the application with correct single-page-application fallback behavior (a direct request to a client-side route returns the application shell, not a `404`).
- The full Compose stack starts from empty storage, routes correctly through Caddy, and serves a working login, profile, and administrative user-management flow.
- The new "Segaris Frontend" check passes on a pull request without requiring Docker or the backend; the extended Compose/end-to-end check passes with the full stack.

Exit criteria:

- `docker compose -f docker-compose.yml -f docker-compose.local.yml up --build` serves a working application with login, profile, and administrative user management functioning end to end, and CI enforces this on every pull request.

## Recommended Pull Request Breakdown

The following pull-request sequence keeps changes reviewable while preserving useful checkpoints:

1. Frontend foundation decisions — tooling, dev-proxy strategy, environment contract, and directory conventions.
2. Backend Identity-profile and avatar extension, with migrations, endpoints, and tests.
3. Frontend repository scaffold, scripts, and developer commands.
4. Design-system token and component port.
5. Application shell, routing, session context, TanStack Query, error boundaries, and i18n bootstrap.
6. Login.
7. Self-service profile, including avatar.
8. Administrative user management (list, create, password reset,
   activate/deactivate).
9. Administrative user editing — backend update endpoint plus the display-name
   and role edit dialog.
10. Launcher and shared-shell completion, with the final i18n and accessibility
    pass.
11. Frontend container, Compose updates, CI checks, and image publication.

Each pull request should update configuration examples and documentation in the same change whenever it introduces or changes a supported setting, endpoint, or command.

## Cross-Cutting Quality Gates

These conditions apply to every wave:

- No secret or backend-only configuration value enters the frontend bundle, `src/frontend/.env.example`, or source control.
- Every mutating request to `/api/session/*` and `/api/admin/users/*` includes the antiforgery header obtained from `/api/session/antiforgery`.
- Every interactive control has an accessible label, a visible focus state, and is operable by keyboard; form errors are associated with their fields.
- TanStack Query failures are categorized per `docs/architecture/frontend.md`; a single rejected request, validation error, or authorization response never triggers the global unavailable state.
- All new interface strings live in the `platform` i18n namespace with `en-GB` translations; no user-facing strings are hardcoded outside i18n resources.
- React error boundaries exist at the root and at module-route boundaries; fallbacks offer only meaningful recovery actions and never retry automatically.
- Forms with meaningful unsaved state use the React Hook Form dirty-state guard before navigation.
- No EF Core entity or internal backend type is serialized as an API contract for the new profile and avatar endpoints; explicit DTOs only.
- Avatar uploads are validated on the client for fast feedback and authoritatively validated on the backend; client-side validation is never the security boundary.
- New frontend settings and scripts are added to tracked examples and documentation in the same change.
- Architecture documents (`docs/architecture/frontend.md`, `docs/architecture/design-system.md`, `docs/architecture/user-experience.md`) are corrected when implementation proves an assumption invalid.

## Foundation Completion Criteria

The application core is complete when all of the following are true:

1. A clean checkout can install, lint, type-check, test, and build the frontend through documented native commands, and run it against a locally running backend through the development proxy.
2. A user can sign in, is redirected to the launcher, and can return to it from every shell screen.
3. Any authenticated user can view and edit their display name, language, password, and avatar, with changes reflected immediately in the shared shell.
4. An administrator can list, create, activate, deactivate, and reset passwords for household accounts through the interface, matching the documented `/api/admin/users` contract.
5. The design-system token set and the listed component catalog are ported, typed, and used consistently across login, launcher, profile, and administration screens.
6. i18next is bootstrapped with a complete `platform` namespace in `en-GB`; missing-key and fallback behavior are tested.
7. Root and module-level error boundaries, and the expected-failure categorization for authentication, authorization, validation, not-found, transient, and unavailable conditions, are implemented and tested.
8. The Compose stack builds and serves the real frontend image, routed through Caddy, with an automated end-to-end test covering login, profile, and administrative user management.
9. CI enforces a "Segaris Frontend" check and an end-to-end/Compose check on every pull request, and main-branch publication produces the real `segaris-frontend` image.
10. Documentation — `docs/architecture/frontend.md`, `docs/architecture/design-system.md`, the companion decision documents, and developer run instructions — matches the actual repository structure, commands, configuration, and behavior.

## Follow-Up

- Add `es-ES` translations once the `en-GB` `platform` namespace is stable and a translation workflow is chosen.
- Define the launcher attention-indicator API contract and the toast severity and placement conventions when the first business module is scoped, per the open items in `docs/architecture/user-experience.md`.
- Revisit the session cookie's `Secure` flag and the CORS posture if Segaris is ever exposed beyond the trusted local network.
- After Phase 2 defines the first business module, Phase 3 should create a small vertical version plan that adds a lazily loaded module route, a module-owned error boundary and i18n namespace, and a launcher card on top of this foundation.
