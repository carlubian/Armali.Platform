[CmdletBinding()]
param(
    # Test project path relative to the Segaris repository root, e.g.
    # "tests/backend/Segaris.Api.IntegrationTests/Segaris.Api.IntegrationTests.csproj".
    [Parameter(Mandatory = $true)][string]$Project,
    # 1-based index of this shard.
    [Parameter(Mandatory = $true)][int]$ShardIndex,
    # Total number of shards.
    [Parameter(Mandatory = $true)][int]$ShardCount,
    # Print the classes assigned to this shard without running them.
    [switch]$ListOnly
)

$ErrorActionPreference = "Stop"

if ($ShardCount -lt 1) { throw "ShardCount must be at least 1." }
if ($ShardIndex -lt 1 -or $ShardIndex -gt $ShardCount) {
    throw "ShardIndex must be between 1 and ShardCount ($ShardCount)."
}

$root = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $root $Project
if (-not (Test-Path $projectPath)) { throw "Test project not found: $projectPath" }

# Discover every test case, then collapse to distinct test classes. Partitioning by class
# (not by individual case) keeps a class's tests on a single runner and means a newly added
# module is sharded automatically the first time its classes appear in discovery, so no shard
# list ever has to be maintained by hand.
$discovered = dotnet test $projectPath --no-restore --no-build --list-tests
if ($LASTEXITCODE -ne 0) { throw "Test discovery failed for $Project." }

$classes = $discovered |
    ForEach-Object { $_.Trim() } |
    Where-Object { $_ -match '^Segaris\.' } |
    ForEach-Object { ($_ -replace '\(.*$', '') } |   # drop theory arguments, if present
    ForEach-Object { ($_ -replace '\.[^.]+$', '') } | # drop the method name -> class FQN
    Sort-Object -Unique

if (-not $classes) { throw "No test classes were discovered in $Project." }

$shardClasses = @()
for ($i = 0; $i -lt $classes.Count; $i++) {
    if (($i % $ShardCount) -eq ($ShardIndex - 1)) { $shardClasses += $classes[$i] }
}

Write-Host "Shard $ShardIndex/$ShardCount -> $($shardClasses.Count) of $($classes.Count) test classes."

if ($ListOnly) {
    $shardClasses | ForEach-Object { Write-Host "  $_" }
    return
}

if (-not $shardClasses) {
    Write-Host "Shard $ShardIndex/$ShardCount has no classes assigned; nothing to run."
    return
}

$filter = ($shardClasses | ForEach-Object { "FullyQualifiedName~$_" }) -join '|'
dotnet test $projectPath --no-restore --no-build --filter $filter
if ($LASTEXITCODE -ne 0) { throw "Shard $ShardIndex/$ShardCount reported test failures." }
