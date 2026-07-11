# Despliegue interno

Blackwing se despliega de forma reproducible con Docker Compose, con la misma
imagen que valida el CI del monorepo. Este documento describe el despliegue interno
de extremo a extremo; para el estado persistente y su recuperación ver
[volumenes-y-backup.md](volumenes-y-backup.md), y para trazas, métricas y salud ver
[observabilidad.md](observabilidad.md).

## Componentes del stack

`deploy/compose/docker-compose.yml` (proyecto `blackwing`) es la topología base y
levanta tres piezas:

| Servicio | Imagen / origen | Rol |
| --- | --- | --- |
| `postgres` | `postgres:17` | Catálogo relacional. Healthcheck con `pg_isready`. |
| `backend` | `BLACKWING_BACKEND_IMAGE` (por defecto `blackwing-backend:local`) | API .NET. Aplica migraciones y siembra el admin inicial al arrancar. Healthcheck contra `/health/ready`. |
| `frontend` | `BLACKWING_FRONTEND_IMAGE` (por defecto `blackwing-frontend:local`, Caddy) | Sirve el SPA y hace de proxy inverso de `/api` al backend. Único servicio con puerto publicado. |

Solo `frontend` publica puerto al host (`BLACKWING_HTTP_PORT`, por defecto 5055).
Backend y base de datos quedan en la red interna de Compose.

El compose base **referencia las imágenes por tag y nunca las construye**, de modo
que sirve tal cual para producción (donde se fijan `BLACKWING_BACKEND_IMAGE` y
`BLACKWING_FRONTEND_IMAGE` a los tags inmutables por commit del registro / ACR). El
override `deploy/compose/docker-compose.local.yml` añade el `build` para construir
las imágenes localmente en lugar de descargarlas.

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

Equivale a `docker compose --env-file deploy/compose/.env -f deploy/compose/docker-compose.yml -f deploy/compose/docker-compose.local.yml up -d --build`,
es decir, la topología base más el override que construye las imágenes localmente.
El orden de arranque lo garantizan los healthchecks: el backend espera a que
Postgres esté `healthy`, y el frontend a que el backend responda `/health/ready`.

Abrir `http://localhost:5055` e iniciar sesión con el administrador inicial.

### Producción (imágenes desde el registro / ACR)

En producción no se construye nada: se levanta **solo** el compose base y se fijan
`BLACKWING_BACKEND_IMAGE` y `BLACKWING_FRONTEND_IMAGE` a los tags inmutables por
commit publicados en el registro privado:

```
docker compose --env-file .env -f docker-compose.yml up -d
```

Las imágenes las genera el workflow `.github/workflows/blackwing-publish-images.yml`
(ver más abajo).

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
- **compose** — `docker compose config` sobre el stack base y sobre base + override
  local con el `.env.example`, que verifica que ambos ficheros de Compose son
  válidos y reproducibles.

La misma definición de Compose que valida el CI es la que se despliega, de modo que
un build verde es un despliegue reproducible.

## Publicación de imágenes al registro / ACR

El workflow `.github/workflows/blackwing-publish-images.yml` construye y sube las
imágenes `blackwing-backend` y `blackwing-frontend`, replicando el patrón de
Segaris:

- **Cuándo** — se dispara vía `workflow_run` cuando *Blackwing Foundation
  Validation* termina con éxito en `main` (es decir, tras el *merge* de un PR que
  toca `Blackwing/**`), o manualmente con `workflow_dispatch` sobre `main`. No se
  publican imágenes desde PR abiertos: solo desde `main` ya validado.
- **Qué** — una imagen por servicio en una matriz. El backend se construye con
  contexto `Blackwing/` y el frontend con contexto `Blackwing/src/frontend/`.
- **Tag** — el SHA del commit (`github.event.workflow_run.head_sha`), es decir, tags
  inmutables por commit. Se empujan a `${ACR_LOGIN_SERVER}/blackwing-<servicio>:<sha>`.
- **Autenticación** — inicio de sesión en Azure con OIDC (sin secretos de larga
  vida) y `az acr login`, usando las variables de repositorio `AZURE_CLIENT_ID`,
  `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `ACR_NAME` y `ACR_LOGIN_SERVER`. El job
  usa el *environment* `blackwing-production-images` para las reglas de protección.

Para desplegar una versión concreta, fijar `BLACKWING_BACKEND_IMAGE` y
`BLACKWING_FRONTEND_IMAGE` al tag de commit correspondiente y volver a levantar el
compose base.
