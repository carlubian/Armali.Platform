[CmdletBinding()]
param(
    [switch]$Verify
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$arguments = @("format", "$root/src/backend/Segaris.slnx", "--no-restore")

if ($Verify) {
    $arguments += "--verify-no-changes"
}

dotnet @arguments

