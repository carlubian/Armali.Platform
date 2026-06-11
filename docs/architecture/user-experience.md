# User Experience

This document records the Phase 1 decisions that define the overall user experience and navigation model of Armali Platform.

## Experience Model

Armali is a collection of immersive modules presented within one platform. Each module should feel like a small application dedicated to its domain rather than one section of a single continuous administrative workspace.

The platform provides shared infrastructure, identity, and baseline interaction conventions, but it must leave enough visual and navigational freedom for each module to develop an experience appropriate to its purpose.

## Launcher

After signing in, the user enters a central dashboard that acts as the Armali launcher. It presents the available modules as cards, buttons, or another visually prominent launch surface.

The launcher has one primary responsibility:

- Provide the primary entry point to every module available to the user.

It is intentionally not an operational dashboard. It does not present dynamic summaries, cross-module aggregations, recent activity, tasks, charts, or other domain data. The exact card design, ordering, grouping, and personalization remain open design decisions.

### Attention Indicator

A module card may display a simple icon indicating that the module has something pending or otherwise requires the current user's attention.

Each module owns the definition and evaluation of the conditions that activate its indicator. The launcher only obtains and renders the resulting state; it does not interpret domain data or reproduce the module's rules.

The indicator is intentionally lightweight:

- It communicates only that the user may need to enter the module.
- It does not display counts, summaries, record details, or aggregated domain data.
- The user enters the module to understand and act on the condition.
- Its state is evaluated for the current user and must respect entity visibility and privacy rules.

This indicator is one of the platform's three attention and feedback mechanisms. It does not imply a notification inbox, delivery history, email, or push notification.

## Attention, Feedback, And Calendar Events

Armali does not initially require a unified notification center or a general stream of notification records. Instead, it uses three mechanisms with different purposes and lifetimes.

### Launcher Attention Indicators

The indicator on a module card communicates that the module currently requires the user's attention. It is a state owned by the module, not an individual notification, and remains visible while the module's conditions continue to apply.

The launcher exposes no reason, count, record detail, or action beyond entering the module. Each module defines during functional planning which conditions activate and clear its current-user indicator.

### Toast Messages

Toast messages provide short-lived feedback about an action or background process while the user is using the application. They slide in from a screen edge, remain visible for a few seconds, and then disappear.

Typical uses include:

- Confirming that an operation completed successfully.
- Reporting that an operation failed or could not be completed.
- Informing the user that a background process completed or changed state.

Toasts are transient interface feedback rather than persistent notifications. They do not form an inbox or history, and important information must not be available only through an automatically disappearing toast. Their placement, duration, severity levels, interaction behavior, and accessibility treatment will be defined with the design system.

### Calendar Events And Due Dates

At least one module or application section may provide a calendar view containing events, due dates, and other date-bound domain information. Selecting or focusing a relevant day opens a popup or equivalent contextual surface with the associated details.

Calendar entries are persistent, queryable domain information. They are not toast messages and are not consumed or dismissed like notifications. The responsible module or section, included event types, visibility rules, and cross-module scope will be defined during functional planning.

These three mechanisms may refer to the same underlying condition without becoming one shared notification object. For example, an approaching due date may appear in the calendar and cause its owning module's launcher indicator to activate, while a toast may confirm an operation performed on that due date.

## Module Navigation

There is no persistent global sidebar or equivalent direct module-to-module navigation while the user is inside a module.

Each module owns its internal navigation and may use the structure best suited to its workflows. To move to another module, the user first returns to the central launcher and then opens the destination module.

Every module must therefore provide a clear, consistently located action for returning to the launcher. This action is part of the shared Armali shell even when the surrounding module interface has a distinct visual identity.

Direct URLs may still identify pages within a module for browser history, refresh, and future deep-linking needs. They do not change the intended interaction model: ordinary module switching happens through the launcher.

## Shared And Module-Specific Experience

The shared platform experience should define only the conventions needed for coherence and safety, including:

- Authentication and user profile access.
- Returning to the launcher.
- Localization and visible regional formatting.
- Loading, unavailable-service, expired-session, and operation-error states.
- Confirmation of irreversible destructive operations.
- A baseline for accessibility and common controls.

Module layout, navigation hierarchy, information density, visual emphasis, and domain-specific interactions may differ when that makes the module more effective or immersive.

## Design References

Screen designs are being prepared outside this repository workflow and will be added somewhere under the project documentation before frontend implementation begins.

Those designs will be the primary source of inspiration for Armali's visual language and module experiences. Phase 1 will not select a UI library, define a complete component catalog, or prescribe detailed styling before those references are available.

The designs are references rather than an automatic implementation specification. Before implementation, they must be reviewed together with the documented product constraints and translated into explicit decisions covering at least:

- Shared and module-specific components.
- Visual consistency and permitted variation between immersive modules.
- Information density and interaction behavior on large desktop displays.
- Accessibility, keyboard operation, localization, and reusable state patterns.
- The frontend libraries and implementation approach needed to reproduce the intended experience sustainably.

If a design conflicts with an established product, privacy, accessibility, or technical constraint, the conflict must be documented and resolved rather than silently copying the design.

## Rationale

This model reinforces the product concept that Armali contains several focused tools rather than exposing all household domains through one generic management interface. Returning through the launcher makes the transition between those tools explicit and preserves each module's sense of place.

The additional navigation step is intentional. The launcher should make that step fast and recognizable, and modules should not reproduce a second global navigation system that weakens their independence.

## Open Decisions

- Define how modules are ordered, grouped, hidden, or personalized.
- Define the visual treatment and accessibility semantics of the module attention indicator.
- Define the API contract through which each module exposes its current-user attention state to the launcher.
- Define shared toast behavior with the future design system, including duration, severity, accessibility, and treatment of background-process results.
- Define which module or application section owns the calendar and which domain events and due dates it includes.
- Define the shared shell elements that remain visible inside every module.
- Review the future screen designs and define whether modules may have distinct themes and how much visual variation the design system permits.
- Define the behavior of browser back navigation and direct deep links across authentication and authorization boundaries.
- Define keyboard navigation and whether a command palette is useful within the launcher or individual modules.
- Define the design system and accessibility baseline after the screen designs are available.
- Define the reporting model.
