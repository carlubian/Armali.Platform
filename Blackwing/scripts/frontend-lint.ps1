corepack pnpm --dir "$PSScriptRoot/../src/frontend" lint
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
