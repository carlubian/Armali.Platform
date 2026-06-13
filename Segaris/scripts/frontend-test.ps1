[CmdletBinding()]
param(
    [switch] $E2E,
    [switch] $Coverage
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

$script = if ($E2E) { "test:e2e" } elseif ($Coverage) { "test:coverage" } else { "test" }
corepack pnpm -C "$root/src/frontend" run $script
