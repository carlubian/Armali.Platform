[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

dotnet restore "$root/src/backend/Segaris.slnx"

