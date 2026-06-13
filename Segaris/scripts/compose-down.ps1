[CmdletBinding()]
param(
    [switch]$Volumes
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$composeDir = Join-Path $root "deploy/compose"
$baseFile = Join-Path $composeDir "docker-compose.yml"
$localFile = Join-Path $composeDir "docker-compose.local.yml"
$windowsFile = Join-Path $composeDir "docker-compose.windows.yml"
$envFile = Join-Path $composeDir ".env"

# Stop the full local container stack. Use -Volumes to also delete the PostgreSQL
# and local data volumes. Linux host bind mounts under SEGARIS_DATA_PATH are not
# removed.
if (-not (Test-Path $envFile)) {
    Copy-Item (Join-Path $composeDir ".env.example") $envFile
}

$arguments = @("compose", "--env-file", $envFile, "-f", $baseFile, "-f", $localFile)
if ($IsWindows -or $env:OS -eq "Windows_NT") {
    $arguments += @("-f", $windowsFile)
}
$arguments += "down"
if ($Volumes) {
    $arguments += "--volumes"
}

& docker @arguments
if ($LASTEXITCODE -ne 0) {
    throw "docker compose down failed with exit code $LASTEXITCODE."
}
