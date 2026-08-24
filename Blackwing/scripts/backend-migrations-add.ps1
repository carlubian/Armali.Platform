[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string] $Name
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

# Migrations live in Blackwing.Persistence because that is the assembly of the DbContext, but
# they must be generated with Blackwing.Api as the startup project: only there is the identity
# model contributor registered, so only there does the model include the identity tables.
# There is deliberately no IDesignTimeDbContextFactory - one would force Blackwing.Persistence
# to know about the identity module and would break the isolation the perimeter depends on.
dotnet ef migrations add $Name `
    --project "$root/src/backend/Blackwing.Persistence" `
    --startup-project "$root/src/backend/Blackwing.Api" `
    --output-dir Migrations
