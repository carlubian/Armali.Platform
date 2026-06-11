# Product Shape

This document records the Phase 1 decisions that define the intended shape of Armali Platform. These constraints should guide later architecture and functional decisions.

## Application Form

Armali Platform will be a standard web application hosted on an internal household server.

The product is intended primarily for desktop computers with large displays. Future use on touchscreens of approximately 21 inches or larger is desirable, but touch interaction is not a primary design constraint. When two approaches are otherwise equivalent, prefer the touch-friendly option; do not sacrifice visual quality, information density, dependency stability, or maintainability for preferential touch support.

Mobile use is outside the initial product scope. If it becomes necessary, it may be delivered through a separate client or integration rather than by forcing the main interface into a mobile-first or fully responsive design.

Browser access allows the application to be used from other capable household devices, including smart televisions, but those devices are not initially certified targets and must not constrain the primary desktop experience.

## Client And Server Boundary

The application will have two independently developed parts:

- A backend server written in C# on ASP.NET Core.
- A web frontend written in TypeScript with a mainstream frontend framework.
- A REST API as the contract between frontend and backend.

This separation keeps business logic and data access independent from presentation. The exact frontend framework and detailed API architecture remain Phase 1 architecture decisions.

Electron, native desktop clients, PWA-specific capabilities, and native mobile clients are outside the initial scope.

## Browser Support

Chromium-based browsers are the primary supported platform.

Firefox compatibility is desirable when it does not require substantial changes to the design, implementation, or dependencies. Safari, Opera, and less common browsers are outside guaranteed support.

## Household And Users

The initial system supports one household with approximately two or three distinct users. Multi-household tenancy is outside the current scope.

There are two roles:

- `User`: uses the household modules and manages their own profile.
- `Admin`: additionally manages user accounts and global configuration.

Only administrators may create users or activate and deactivate accounts. Users may change their own password, display name, profile image, and other personal profile details.

Administrators also manage shared classification data such as categories, statuses, and other properties used by domain modules.

## Visibility And Permissions

Domain entities can be public or private:

- Public entities are visible, editable, and removable by every user in the household.
- Private entities are visible and manageable only by their creator.

The `Admin` role grants system administration capabilities, not access to another user's private entities. Administrators may see non-identifying aggregate values, such as entity counts by module or category, when useful for configuration. Aggregates must not expose enough detail to reconstruct private information.

These visibility rules must be enforced by the backend rather than relying only on frontend filtering.

## Authentication

The application runs in a trusted internal environment and initially needs only straightforward username-and-password authentication.

The architecture should leave room for future headless authentication, such as API keys associated with a user, so trusted external applications can call the REST API without interactive browser login. API key behavior is not part of the initial implementation scope.

## Connectivity And Failure States

Armali Platform is online-only relative to its internal server. The frontend does not need offline data access, local change queues, or synchronization.

When the backend cannot be reached, the frontend must show an explicit global unavailable state. It must not display empty tables or incomplete views that could be mistaken for valid data.

Common failures should be distinguished:

- A missing or expired session redirects automatically to the login screen.
- An unreachable backend produces a global service-unavailable screen.
- Internal and operation-specific failures use distinct error handling appropriate to their context.

## Region And Localization

The initial regional context is Spain:

- Time zone: `Europe/Madrid`.
- Currency: EUR.
- First day of the week: Monday.
- Preferred visible date shape: `dd MMM yyyy`.

The frontend must support internationalization structurally from the beginning. English is the required complete interface language. Spanish may be added without architectural changes, and the selected interface language will be stored per user.

Visible localized values, including abbreviated month names, follow the selected interface language. For example, the same date may appear as `11 Jun 2026` in English and `11 jun 2026` in Spanish.

User-entered text such as names and descriptions may use any language. Selecting an interface language does not change the application's Spain-specific regional assumptions.

## Explicit Non-Goals

The initial product does not require:

- Mobile-first or fully responsive behavior.
- Offline operation or later synchronization.
- Multiple independent households.
- Strict support for browsers outside the Chromium family.
- Administrator access to private user data.
- Native desktop or mobile clients.
- Complete Spanish translations at first release.
