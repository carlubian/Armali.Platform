docker compose --env-file "$PSScriptRoot/../deploy/compose/.env" -f "$PSScriptRoot/../deploy/compose/docker-compose.yml" up --build --detach
