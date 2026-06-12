[CmdletBinding()]
param(
    [switch]$Seq
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$composeDir = Join-Path $root "deploy/compose"
$composeFile = Join-Path $composeDir "docker-compose.infra.yml"

# Infrastructure-only stack for native backend development: PostgreSQL published
# on localhost (and optionally Seq with -Seq). Run the backend natively against
# it with ./scripts/backend-run.ps1.
$arguments = @("compose", "-f", $composeFile)
if ($Seq) {
    $arguments += @("--profile", "seq")
}
$arguments += @("up", "-d")

& docker @arguments
if ($LASTEXITCODE -ne 0) {
    throw "docker compose up failed with exit code $LASTEXITCODE."
}

Write-Host "Infrastructure is up. PostgreSQL is available on localhost:5432 (user/db: segaris)."
if ($Seq) {
    Write-Host "Seq UI is available on http://localhost:5341."
}
