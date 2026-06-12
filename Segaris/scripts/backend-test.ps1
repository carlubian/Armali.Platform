[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

dotnet test "$root/src/backend/Segaris.slnx" --no-restore

