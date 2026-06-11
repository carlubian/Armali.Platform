# Frontend Architecture

This document records the Phase 1 decisions for the Armali Platform frontend architecture.

## Application Stack

The frontend will be a single-page application built with:

- React as the user-interface library.
- TypeScript as the application language.
- Vite as the development server and production build tool.

The compiled application will be served as static assets by the frontend container. It communicates with the separate ASP.NET Core backend through the REST API and does not contain backend business logic.

## Rendering Model

Armali does not initially require server-side rendering or a React meta-framework such as Next.js.

The application is private, runs on the household network, and has no search-engine optimization requirement. A client-rendered SPA keeps the frontend deployment independent from the backend while avoiding an additional server runtime and framework layer.

This choice does not prevent direct URLs, browser history, route-based code splitting, or authenticated deep links. Those behaviors will be defined with the routing strategy.

## Routing

The SPA will use React Router for client-side routing.

React Router is a justified external dependency because Armali requires browser history, direct URLs, authenticated routes, nested module routes, and a consistent not-found experience. Implementing those behaviors locally would create unnecessary infrastructure code. More sophisticated routing systems are not needed unless future requirements demonstrate a concrete benefit.

The initial route model will remain simple:

- Public routes contain authentication entry points.
- Protected platform routes require an authenticated session.
- The launcher has a stable top-level route.
- Each immersive module owns a top-level route segment and its nested routes.
- Shared route guards handle expired sessions and authorization failures.
- Unknown routes render an explicit not-found state rather than silently redirecting.

Ordinary module switching continues to happen through the launcher, even though direct module URLs remain available for refresh, browser history, and future deep links.

Armali will use React Router as a library inside the Vite SPA. It will not initially adopt a React Router framework mode, server rendering, route loaders, or route actions as an additional application architecture. Data access remains a separate frontend concern and uses the ASP.NET Core REST API.

## State Management

Frontend state is separated by ownership and lifetime rather than placed in one global store.

### Server State

TanStack Query manages data obtained from the REST API, including loading and error states, caching, mutations, retries, and cache invalidation.

Domain records must not be copied into a separate client-side global store without a demonstrated requirement. TanStack Query's cache remains a synchronization mechanism for backend-owned data, not an alternative source of truth.

### Shared Application State

React Context holds the small amount of cross-cutting state that is owned by the frontend and needed across broad parts of the component tree. Expected examples include the current authenticated-session view, selected interface language, and stable application preferences.

Contexts should be focused by concern rather than combined into one general application context. Frequently changing domain data and broad collections do not belong in Context because they can create unclear ownership and unnecessary rerenders.

The authenticated user information exposed through Context is a frontend representation of the current session. The backend remains responsible for authentication and authorization decisions.

### URL State

React Router owns state that should survive refreshes or be represented by a direct link, such as the current module page, selected record, filters, search terms, sorting, and pagination where appropriate.

The detailed decision about which filters belong in the URL is made per module during functional definition. Temporary or sensitive values must not be placed in the URL merely to make them persistent.

### Local UI State

Components use React `useState` for simple local interaction state and `useReducer` when a local workflow has several related transitions. Examples include open dialogs, temporary selections, and unsaved interaction state.

Local state should remain close to the components that own it. It is promoted only when multiple areas genuinely need shared ownership.

### Additional Stores

Armali will not initially use Redux Toolkit, Zustand, or another general client-state library. A store may be introduced later only when a concrete shared-state problem cannot be handled clearly through TanStack Query, focused Context, URL state, or local React state. The requirement and rationale must be documented before adding it.

## Forms And Validation

Armali forms will use React Hook Form for form state and Zod for client-side schema validation.

React Hook Form owns field registration, touched and dirty state, submission state, and field-level errors. Zod schemas define the client-side shape and validation rules needed to provide immediate feedback before submission. The integration should use the standard React Hook Form resolver for Zod rather than a custom validation adapter.

TanStack Query mutations submit validated form data to the ASP.NET Core REST API and coordinate success, failure, and cache invalidation. Forms must prevent accidental duplicate submissions while a mutation is pending.

### Validation Boundary

Client-side validation improves usability but is not a security or data-integrity boundary. The backend validates every request independently and remains authoritative for domain rules, authorization, uniqueness, concurrency, and all other persisted-data constraints.

Frontend Zod schemas should replicate only the rules needed for useful immediate feedback. The TypeScript frontend and C# backend do not share validation implementation code. Any future contract generation may share transport types, but it must not make backend validation dependent on frontend schemas.

### API Errors

The frontend maps structured backend validation errors to the corresponding React Hook Form fields when a stable field mapping exists. Request-level, authorization, concurrency, and unexpected failures appear as a form-level error or through the appropriate shared error treatment.

Error messages must remain understandable without exposing internal exception details. A toast may supplement the form state, but an automatically disappearing toast must not be the only place where a submission failure is explained.

### Unsaved Changes

Forms use React Hook Form's dirty state to detect unsaved changes. Navigation away from a materially changed form requires an explicit confirmation when losing the changes would be surprising or costly.

This protection applies to in-application navigation and, where browser capabilities allow, page refresh or tab closure. It should not be enabled indiscriminately for trivial filters, searches, or interactions whose state is disposable.

### Form Conventions

- Accessible labels and error associations are required for every field.
- Validation should normally occur at a useful interaction boundary such as blur or submission, avoiding disruptive validation on every keystroke unless the field benefits from it.
- The first invalid field should be focusable after a failed submission.
- Destructive actions remain separate from normal form submission and require the shared confirmation treatment.
- Complex domain forms may be split into sections or steps, but the workflow details are defined per module during Phase 2.

## Internationalization

Armali will use i18next with react-i18next for interface translations. Browser-native `Intl` APIs provide locale-aware formatting for dates, times, numbers, currencies, lists, and relative values where appropriate.

English using the `en-GB` locale is the required complete interface language and the fallback when a translation is unavailable. Spanish using `es-ES` may be added incrementally, but the application architecture and new interface text must support translation from the beginning.

### Language And Regional Context

Interface language and household regional rules are separate concepts.

The selected interface language controls translated text and language-sensitive presentation such as month names. The household remains initially configured for Spain, including `Europe/Madrid`, EUR, Monday-first weeks, and the documented civil-date conventions. Selecting English must not silently replace those household assumptions with United Kingdom or United States business rules.

The user's selected interface language is stored in their backend profile. Before an authenticated profile is available, the login and startup experience uses a documented fallback, initially English. Browser language detection may be considered later but must not override an explicit user preference.

### Translation Organization

Translation resources are stored in the frontend repository and divided into focused namespaces:

- A shared platform namespace for authentication, launcher, profile, common actions, errors, and shared controls.
- One namespace for each immersive domain module.
- Additional focused namespaces only when a module becomes too large to maintain clearly as one resource.

Translation keys are semantic and stable, for example `inventory.item.createTitle`. Source-language sentences should not be used as keys. Modules own their translation resources alongside their frontend feature code or through an equivalent module-oriented structure selected during implementation.

Translation files remain local to the repository initially. Armali does not require an external translation-management service.

### Backend Messages

The backend does not send user-facing English text for the frontend to display as normal application copy. API errors and domain outcomes use stable machine-readable codes plus structured parameters where necessary. The frontend maps those codes to translated messages.

Unexpected backend failures may include a safe generic fallback message and correlation identifier, but internal exception details must never be exposed for translation or display.

### Formatting

Formatting helpers wrap the relevant `Intl` APIs so modules apply consistent date, time, amount, and number conventions. Persisted values remain locale-neutral: technical timestamps use UTC, civil dates remain date-only values, and money retains its explicit ISO currency code.

Formatting must not be implemented through manual string concatenation. Pluralization, interpolation, and grammatical variants use i18next capabilities rather than application conditionals where the distinction is linguistic.

### Verification

Automated frontend validation should detect missing fallback-language keys, invalid translation resources, and inconsistent keys between supported complete locales. Tests may render with keys or controlled test resources, but critical flows require coverage that catches untranslated or malformed interface output.

## Failure Boundaries And Loading

Armali separates unexpected rendering failures, expected API failures, routing outcomes, and service availability so each problem receives an appropriate recovery experience.

### Rendering Error Boundaries

A shared application error-boundary component uses React's native error-boundary lifecycle. Armali does not initially add a separate error-boundary dependency.

The application has two primary boundary levels:

- A root boundary protects the complete React application and displays a translated recovery screen when the shared shell or startup rendering fails.
- A boundary around each immersive module prevents a rendering failure inside one module from taking down the launcher or other modules.

The fallback provides only recovery actions that are meaningful for its level, such as retrying the render, returning to the launcher, or reloading the application. It must not repeatedly retry without user action or hide a persistent failure behind an automatic redirect.

Unexpected captured errors are submitted through the protected frontend diagnostics endpoint described in the observability architecture. Diagnostic reporting is best-effort and includes safe context such as the route, module, application version, and correlation identifier when available. Sensitive form values, private record contents, credentials, and raw API payloads must not be included.

React error boundaries do not replace explicit handling for event-handler failures, asynchronous operations, or expected control flow. Those errors remain handled by the operation that owns them.

### API And Operation Failures

TanStack Query exposes expected query and mutation failures to the relevant screen or component. The interface distinguishes at least:

- Missing or expired authentication, which redirects to the login flow.
- Authorization denial, which displays an explicit access-denied state without revealing private data.
- Validation and domain failures, which remain attached to the form or operation.
- Missing records, which display a localized not-found state appropriate to the module.
- Transient operation failures, which may offer a user-controlled retry.
- Backend unavailability, which activates the global unavailable-service experience.

The global unavailable state is reserved for a genuine inability to reach the required backend or establish application readiness. A single rejected request, validation error, authorization response, or missing record must not mark the whole backend as unavailable.

Queries may use limited automatic retries for failures classified as transient. Mutations do not retry automatically unless the operation is known to be idempotent and the behavior is explicitly documented. Retrying must not create duplicate records or repeat destructive actions.

### Routing Outcomes

React Router provides explicit route-level states for unknown URLs and authorization boundaries. A not-found route is ordinary application behavior rather than an exception and must not be reported as a rendering failure.

Direct links into modules pass through the same authentication and authorization checks as launcher navigation. Returning to the launcher remains the normal module-switching path.

### Code Splitting And Suspense

Each immersive module is loaded through `React.lazy` and a dynamic import at its top-level route boundary. React `Suspense` displays a shared, accessible loading experience while the module bundle is fetched.

The launcher, authentication flow, shared shell, and critical global failure screens remain in the initial application bundle. Code splitting should happen at meaningful module or large-feature boundaries rather than for every small component.

Loading indicators avoid unnecessary flicker for operations that complete quickly and communicate meaningful progress for longer startup or navigation waits. A loading state must not be confused with an empty-data or unavailable-service state.

Failure to download a lazy module bundle is handled by that module's boundary with a retry or application-refresh action. It is not presented as an empty module.

## Rationale

React has mature TypeScript support, a broad ecosystem, and established testing and accessibility tooling. Vite provides a focused development and build setup suitable for a frontend that is deployed separately from the ASP.NET Core API.

The stack also aligns with the existing decisions to use Vitest, Testing Library, Playwright, separate frontend and backend images, and Chromium-based browsers as the primary platform.

## Open Decisions

- Select the UI component and styling strategy after the screen designs are available.
