corepack pnpm --dir "$PSScriptRoot/../src/frontend" build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
