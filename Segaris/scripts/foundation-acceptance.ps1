[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

function Invoke-Step {
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter(Mandatory)]
        [scriptblock]$Action
    )

    Write-Host ""
    Write-Host "=== $Name ==="
    & $Action
    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE."
    }
}

Push-Location $root
try {
    Invoke-Step "Restore" { & "$root/scripts/backend-restore.ps1" }
    Invoke-Step "Formatting" { & "$root/scripts/backend-format.ps1" -Verify }
    Invoke-Step "Build" { & "$root/scripts/backend-build.ps1" }

    Invoke-Step "Unit tests" {
        dotnet test tests/backend/Segaris.UnitTests/Segaris.UnitTests.csproj --no-restore --no-build
    }
    Invoke-Step "Architecture tests" {
        dotnet test tests/backend/Segaris.ArchitectureTests/Segaris.ArchitectureTests.csproj --no-restore --no-build
    }
    Invoke-Step "API integration tests" {
        dotnet test tests/backend/Segaris.Api.IntegrationTests/Segaris.Api.IntegrationTests.csproj --no-restore --no-build
    }
    Invoke-Step "PostgreSQL integration tests" {
        dotnet test tests/backend/Segaris.Postgres.IntegrationTests/Segaris.Postgres.IntegrationTests.csproj --no-restore --no-build
    }
    Invoke-Step "Migration tests" {
        dotnet test tests/backend/Segaris.Migrations.IntegrationTests/Segaris.Migrations.IntegrationTests.csproj --no-restore --no-build
    }

    Invoke-Step "Frontend restore" { & "$root/scripts/frontend-restore.ps1" }
    Invoke-Step "Frontend lint" { & "$root/scripts/frontend-lint.ps1" }
    Invoke-Step "Frontend formatting" { & "$root/scripts/frontend-format.ps1" -Verify }
    Invoke-Step "Frontend build" { & "$root/scripts/frontend-build.ps1" }
    Invoke-Step "Frontend unit tests" { & "$root/scripts/frontend-test.ps1" }

    $bash = Get-Command bash -ErrorAction SilentlyContinue
    if ($null -eq $bash) {
        throw "The Compose acceptance gate requires bash and Docker Compose."
    }

    Invoke-Step "Compose smoke test" { & $bash.Source ./scripts/compose-smoke-test.sh }

    Write-Host ""
    Write-Host "Foundation acceptance passed."
}
finally {
    Pop-Location
}
