# Agent Operating Guide

This repository is the `Armali.Platform` monorepo. **Blackwing lives in the
`Blackwing/` subfolder**; everything that belongs to the application goes there.
Only repository-wide configuration and the GitHub workflows live at the
repository root.

Blackwing is a private, multi-user photo library: every user uploads their own
images, tags them with three tag types (person, place, topic) and browses their
gallery filtering by those tags. Images and tags are private and unique per
account.

## Required Startup Routine

At the start of every new conversation or task:

1. Read this file first.
2. Read `README.md` for the current repository shape and development commands.
3. Inspect the code, tests, and configuration directly related to the task.
4. Read only the relevant documents under `docs/architecture/` when
   implementation context or decision rationale is needed.
5. Before substantial edits, briefly summarize the relevant current state.

Do not read every document by default.

## Collaboration Language

The project owner communicates primarily in Spanish. Use Spanish for planning
conversations, questions, requirements, and summaries unless the user asks
otherwise.

Technical identifiers, filenames, code, code comments, and commit messages are
written in **English**.

## User Interface Language

The user interface is in **Spanish**, but it is translated from day one with
i18next.

**No component may contain a visible literal string.** Every user-facing text
goes through `useTranslation` and is declared in `src/app/i18n/resources.ts`.
This is enforced by tests and reviewed on every change; do not add a literal as
a temporary shortcut.

## Current Development Model

- Blackwing is an independent application inside the monorepo, a sibling of
  Segaris and Belfalas. It reuses their *patterns*, never their instances,
  their database, or their data.
- The backend is a .NET 10 ASP.NET Core application under `src/backend`, split
  into `Blackwing.Api`, `Blackwing.Persistence` and `Blackwing.Shared`. The
  dependency rule is `Api -> Persistence -> Shared`, never the other way round,
  and `Shared` references nothing. Architecture tests enforce it.
- The frontend is a React 19 + Vite + TypeScript application under
  `src/frontend`.
- PostgreSQL is the only database provider, in every environment including local
  development.
- Tests live under `tests/backend`, deployment assets under `deploy`, and
  repeatable development commands under `scripts`.
- Architecture decisions live under `docs/architecture/`.

## Implementation Expectations

- Follow existing project structure, naming, analyzers, formatting, and test
  conventions. When in doubt about style, architecture, or naming, the answer is
  in `Segaris/`: copy the pattern and rename, do not invent a variant.
- `TreatWarningsAsErrors` is intentional. Do not relax it to make something
  compile.
- Keep changes scoped to the requested behavior and preserve unrelated work in a
  dirty worktree.
- Add or update tests in proportion to the behavioral risk.
- **Do not run the full `Blackwing.Api.IntegrationTests` suite during local agent
  work** unless the user explicitly asks for it. That suite is validated as a
  sharded GitHub Actions check; an unsharded local run is slow because every
  fixture starts a real PostgreSQL container. Prefer focused project, class, or
  method tests for local backend feedback.
- Use the scripts documented in `README.md` for repeatable restore, build, test,
  format, and local execution workflows. The four backend and four frontend
  script names used by CI are contractual: do not rename them.

## Design System

The visual language comes from the Armali design system, already ported to
production by Segaris. Only the foundations are reused: fonts, color tokens,
buttons, headings, and the aurora/acrylic background.

Non-negotiable rules: light theme only; no pure black or white; elevation by
warm glow, never gray shadow; everything rounded (controls 12px, cards 20px,
dialogs 24px, pills 999px); aurora background of drifting blobs under acrylic
surfaces; focus always in aqua; sentence case throughout; Lucide icons via
`lucide-react`; no emoji. Every infinite animation respects
`prefers-reduced-motion`.
