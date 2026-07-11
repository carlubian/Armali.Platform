# Despliegue interno

Blackwing se despliega de forma reproducible con Docker Compose, con la misma
imagen que valida el CI del monorepo. Este documento describe el despliegue interno
de extremo a extremo; para el estado persistente y su recuperación ver
[volumenes-y-backup.md](volumenes-y-backup.md), y para trazas, métricas y salud ver
[observabilidad.md](observabilidad.md).

## Componentes del stack

`deploy/compose/docker-compose.yml` (proyecto `blackwing`) levanta cuatro piezas:

| Servicio | Imagen / origen | Rol |
| --- | --- | --- |
| `postgres` | `postgres:17` | Catálogo relacional. Healthcheck con `pg_isready`. |
| `backend` | build de `src/backend/Blackwing.Api/Dockerfile` | API .NET. Aplica migraciones y siembra el admin inicial al arrancar. Healthcheck contra `/health/ready`. |
| `frontend` | build de `src/frontend` (Caddy) | Sirve el SPA y hace de proxy inverso de `/api` al backend. Único servicio con puerto publicado. |

Solo `frontend` publica puerto al host (`BLACKWING_HTTP_PORT`, por defecto 5055).
Backend y base de datos quedan en la red interna de Compose.

## Configuración

Copiar `deploy/compose/.env.example` a `deploy/compose/.env` y fijar valores
fuertes. Variables obligatorias (el Compose falla si faltan las marcadas con `?`):

| Variable | Uso | Obligatoria |
| --- | --- | --- |
| `BLACKWING_POSTGRES_PASSWORD` | Contraseña de PostgreSQL. | Sí |
| `BLACKWING_INITIAL_ADMIN_PASSWORD` | Contraseña del administrador sembrado en el primer arranque. | Sí |
| `BLACKWING_POSTGRES_DB` / `BLACKWING_POSTGRES_USER` | Nombre y usuario de la base. | No (por defecto `blackwing`) |
| `BLACKWING_INITIAL_ADMIN_USERNAME` | Usuario del administrador inicial. | No (por defecto `admin`) |
| `BLACKWING_HTTP_PORT` | Puerto publicado del frontend. | No (por defecto `5055`) |

Los secretos viven solo en `.env` (ignorado por git) y como variables de entorno
del contenedor; nunca se registran en logs ni se incluyen en el repositorio.

### Admin inicial

En el primer arranque, `IdentitySeeder` crea los roles `User` y `Admin` y, si
`InitialAdmin` está configurado, un administrador con ese usuario y contraseña. Es
idempotente: en arranques posteriores no duplica ni sobrescribe. El administrador
**solo gestiona cuentas** (alta de usuarios y restablecimiento de contraseña) y
nunca accede al contenido de otros usuarios.

## Puesta en marcha

Desde la raíz de `Blackwing/`:

```powershell
./scripts/compose-up.ps1
```

Equivale a `docker compose --env-file deploy/compose/.env -f deploy/compose/docker-compose.yml up -d --build`.
El orden de arranque lo garantizan los healthchecks: el backend espera a que
Postgres esté `healthy`, y el frontend a que el backend responda `/health/ready`.

Abrir `http://localhost:5055` e iniciar sesión con el administrador inicial.

Para detener el stack conservando los volúmenes:

```powershell
./scripts/compose-down.ps1
```

## Actualización de versión

1. Traer los cambios y reconstruir: `./scripts/compose-up.ps1` reconstruye las
   imágenes y recrea los contenedores.
2. El backend aplica automáticamente las migraciones EF pendientes antes de aceptar
   tráfico. No hay paso de migración manual.
3. Los volúmenes `postgres-data` e `image-data` persisten entre recreaciones, así
   que los datos sobreviven a la actualización.

Como las migraciones se aplican en el arranque, **desplegar hacia atrás a una
versión con un esquema anterior no está soportado**: hacer un backup antes de
actualizar (ver el runbook de volúmenes) si se necesita reversión.

## Integración continua

El workflow `.github/workflows/blackwing-validation.yml` valida en cada cambio bajo
`Blackwing/**`:

- **backend** — restore, build y test (los tests de integración corren con Docker
  disponible en el runner).
- **frontend** — restore, lint, build y test.
- **compose** — `docker compose config` sobre el stack con el `.env.example`, que
  verifica que el fichero de Compose es válido y reproducible.

La misma definición de Compose que valida el CI es la que se despliega, de modo que
un build verde es un despliegue reproducible.
