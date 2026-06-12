[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$backendRoot = Join-Path $root "src/backend"
$configurationPath = Join-Path $backendRoot "appsettings.json"

if (-not (Test-Path $configurationPath)) {
    throw "Missing $configurationPath. Copy appsettings.example.json to appsettings.json and review its values."
}

dotnet run --project "$backendRoot/Segaris.Api/Segaris.Api.csproj" -- database seed --contentRoot $backendRoot
