[CmdletBinding()]
param(
    [switch] $Fix
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

$script = if ($Fix) { "lint:fix" } else { "lint" }
corepack pnpm -C "$root/src/frontend" run $script
