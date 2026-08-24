[CmdletBinding()]
param(
    [switch]$Verify
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$arguments = @("format", "$root/src/backend/Blackwing.slnx", "--no-restore")

if ($Verify) {
    $arguments += "--verify-no-changes"
}

dotnet @arguments
