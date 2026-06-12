[CmdletBinding()]
param(
    [switch]$Confirm,
    [switch]$NoSeed
)

$ErrorActionPreference = "Stop"

if (-not $Confirm) {
    throw "Database reset is destructive. Re-run with -Confirm."
}

$root = Split-Path -Parent $PSScriptRoot
$backendRoot = Join-Path $root "src/backend"
$configurationPath = Join-Path $backendRoot "appsettings.json"

if (-not (Test-Path $configurationPath)) {
    throw "Missing $configurationPath. Copy appsettings.example.json to appsettings.json and review its values."
}

$commandArguments = @(
    "run",
    "--project",
    "$backendRoot/Segaris.Api/Segaris.Api.csproj",
    "--",
    "database",
    "reset",
    "--confirm",
    "--contentRoot",
    $backendRoot
)

if ($NoSeed) {
    $commandArguments += "--no-seed"
}

dotnet @commandArguments
