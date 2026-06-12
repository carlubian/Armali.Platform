# Backend CI And Publication Decisions

## Scope

This document records the Wave 9 decisions for pull-request validation, branch
protection, immutable image publication, and final backend-foundation acceptance.

## Pull-Request Validation

Segaris lives beneath `Segaris/` in the shared `Armali.Platform` Git repository.
Every pull request targeting `main` runs the repository-root
`.github/workflows/segaris-validation.yml`.
The workflow has read-only repository permission, does not use environments, and
does not receive production or registry credentials. Fork pull requests therefore
exercise the same validation path without secret access.

The same validation runs again for the resulting trusted commit on `main`. Image
publication is triggered only after that main-branch run completes successfully.

The following job names are the required branch checks:

- `Segaris Backend`: restore, formatting verification, build, unit tests, architecture
  tests, and API integration tests.
- `Segaris PostgreSQL`: production-provider integration tests and SQLite/PostgreSQL
  migration tests through Testcontainers.
- `Segaris Compose`: clean full-stack build and routing/readiness smoke test.

Jobs are deliberately separated so a failure identifies its ownership boundary
without weakening the complete merge gate.

The workflow currently has no path filter. GitHub branch protection can otherwise
leave a required check permanently pending when an entire workflow is skipped.
If more independently built projects are added to `Armali.Platform`, replace this
with always-present path-aware gate jobs rather than filtering out the workflow.

## Branch Protection

The `main` branch should be protected by a GitHub ruleset with these settings:

- Require the `Segaris Backend`, `Segaris PostgreSQL`, and `Segaris Compose` status checks.
- Require branches to be up to date before merging.
- Require all review conversations to be resolved.
- Block force pushes and branch deletion.
- Do not require an approving reviewer initially because the repository is
  maintained by one project owner; review requirements can be raised when a
  second regular maintainer joins.
- Do not permit administrators to bypass failed required checks during normal
  development. Emergency bypass remains an explicit repository-owner action.

## Image Publication

The repository-root `.github/workflows/segaris-publish-images.yml` runs only after
a successful Segaris validation of a trusted commit pushed to `main`, or through
an explicit manual dispatch from `main`. It publishes three images:

- `segaris-backend`
- `segaris-frontend` (the temporary placeholder until the real frontend exists)
- `segaris-caddy`

Each image is tagged with the exact Git commit SHA. No mutable `latest` tag is
published. Portainer deployments must reference those immutable tags explicitly.

The workflow authenticates to Azure through GitHub OIDC and uses an Azure identity
whose only required data-plane role is `AcrPush` on the target registry. Configure
the `segaris-production-images` GitHub environment with these non-secret variables:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `ACR_NAME`
- `ACR_LOGIN_SERVER`

The federated credential must trust only this repository and the
`segaris-production-images` environment subject. No Azure client secret or registry
password is stored in GitHub.

The configured production values are:

- Tenant: `27ba5272-5971-4f26-bfc7-49a2d1a2275e`
- Subscription: `e8502426-10c1-4b7d-be4d-8a8a9563b351`
- Client ID: `16d82d6e-0e47-4a42-9fbf-0b2b717c5bc0`
- Registry: `olyssia.azurecr.io` (`olyssia`)
- Federated credential: `github-armali-platform-segaris-production-images`
- Registry role: `AcrPush`, scoped only to `olyssia`

The GitHub environment and all five variables were configured and verified on
2026-06-12. The Entra federated credential and ACR role assignment were also
created and verified without generating a client secret.

## Local Acceptance

Run `./scripts/foundation-acceptance.ps1` from the repository root. It executes
the same three validation boundaries locally: backend quality and API tests,
PostgreSQL and migration tests, then the disposable Compose smoke test.

Requirements are the pinned .NET SDK, PowerShell, Docker with the Compose plugin,
and `bash` for the existing smoke-test script. PostgreSQL test databases and the
full stack are disposable; no production credentials or storage are used.

## Activation Checklist

Wave 9 is marked complete only after:

- The three required checks and branch rules are enabled for `main`.
- The `segaris-production-images` environment variables and Azure federated
  credential are configured.
- A pull request completes all three checks successfully.
- The resulting trusted `main` SHA publishes all three images and their tags are
  verified in ACR.

As of 2026-06-12, environment/OIDC configuration and classic `main` branch
protection are active. The repository was made public by its owner, enabling the
protection feature without a paid GitHub plan. Administrators are included in the
rules, required branches must be current, conversations must be resolved, and
force pushes and deletion are disabled. Only the first successful validation and
publication rehearsal remains.
