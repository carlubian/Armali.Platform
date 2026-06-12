[CmdletBinding()]
param(
    [switch]$Volumes
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$composeDir = Join-Path $root "deploy/compose"
$composeFile = Join-Path $composeDir "docker-compose.infra.yml"

# Stop the infrastructure-only dev stack. Use -Volumes to also delete the
# PostgreSQL and Seq data volumes.
$arguments = @("compose", "-f", $composeFile, "--profile", "seq", "down")
if ($Volumes) {
    $arguments += "--volumes"
}

& docker @arguments
if ($LASTEXITCODE -ne 0) {
    throw "docker compose down failed with exit code $LASTEXITCODE."
}
