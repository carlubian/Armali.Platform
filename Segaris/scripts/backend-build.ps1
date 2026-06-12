[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

dotnet build "$root/src/backend/Segaris.slnx" --no-restore

