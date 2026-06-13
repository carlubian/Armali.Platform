[CmdletBinding()]
param(
    [switch] $Verify
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

$script = if ($Verify) { "format:check" } else { "format" }
corepack pnpm -C "$root/src/frontend" run $script
