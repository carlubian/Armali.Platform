corepack pnpm --dir "$PSScriptRoot/../src/frontend" test
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
