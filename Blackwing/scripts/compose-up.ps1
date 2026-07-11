$composeDir = "$PSScriptRoot/../deploy/compose"
docker compose --env-file "$composeDir/.env" -f "$composeDir/docker-compose.yml" -f "$composeDir/docker-compose.local.yml" up --build --detach
