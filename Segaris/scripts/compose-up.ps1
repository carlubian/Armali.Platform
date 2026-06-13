[CmdletBinding()]
param(
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$composeDir = Join-Path $root "deploy/compose"
$baseFile = Join-Path $composeDir "docker-compose.yml"
$localFile = Join-Path $composeDir "docker-compose.local.yml"
$windowsFile = Join-Path $composeDir "docker-compose.windows.yml"
$envFile = Join-Path $composeDir ".env"
$exampleFile = Join-Path $composeDir ".env.example"

# Build (unless -NoBuild) and start the full container stack locally: PostgreSQL,
# backend, frontend placeholder, and Caddy ingress. Browse to
# http://localhost:5525/ (default).
if (-not (Test-Path $envFile)) {
    Copy-Item $exampleFile $envFile
    $devPassword = "segaris-local-dev"
    (Get-Content $envFile) `
        -replace '^SEGARIS_POSTGRES_PASSWORD=.*', "SEGARIS_POSTGRES_PASSWORD=$devPassword" `
        | Set-Content $envFile
    Write-Host "Created $envFile from the example with a local development password."
    Write-Host "Review it before any non-local use; never commit it."
}

$arguments = @("compose", "--env-file", $envFile, "-f", $baseFile, "-f", $localFile)
if ($IsWindows -or $env:OS -eq "Windows_NT") {
    $arguments += @("-f", $windowsFile)
}
$arguments += @("up", "-d")
if (-not $NoBuild) {
    $arguments += "--build"
}

& docker @arguments
if ($LASTEXITCODE -ne 0) {
    throw "docker compose up failed with exit code $LASTEXITCODE."
}

Write-Host "Stack is up. Open http://localhost:5525/ (or your configured SEGARIS_HTTP_PORT)."
