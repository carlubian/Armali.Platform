# Volúmenes persistentes, backup y restauración

## Alcance de Blackwing frente al backup

La fase 0 fijó que **los backups no son responsabilidad de Blackwing v1**. La
aplicación no programa copias, no las cifra ni las transfiere fuera del servidor.
Su única obligación es dejar el estado persistente en volúmenes Docker bien
delimitados y **documentar explícitamente qué debe proteger una solución externa**
y cómo restaurarlo. Este documento es ese contrato.

## Los dos volúmenes con estado

El stack de Compose (`deploy/compose/docker-compose.yml`, proyecto `blackwing`)
declara exactamente dos volúmenes con estado. Todo lo demás es reconstruible desde
la imagen y la configuración.

| Volumen Docker | Montado en | Contenido | Consecuencia de perderlo |
| --- | --- | --- | --- |
| `postgres-data` | `postgres:/var/lib/postgresql/data` | Base de datos completa: cuentas e identidad, imágenes (metadatos y `sha256`), tags, asociaciones `ImageTag`, cola `upload_jobs` y el historial de migraciones EF. | Pérdida total del catálogo. Los ficheros de imagen quedarían huérfanos: existen en disco pero ninguna fila los referencia. |
| `image-data` | `backend:/data` | Árbol de imágenes direccionado por contenido en `/data/images/{userId}/{ab}/{cd}/{sha256}.orig\|.preview.webp\|.thumb.webp` y el área de staging transitoria en `/data/staging`. | Pérdida de todos los bytes de imagen (originales y derivados). El catálogo quedaría íntegro pero la entrega devolvería 404. |

**Ambos volúmenes deben respaldarse como una unidad coherente.** La base de datos
y el árbol de imágenes se referencian mutuamente por `sha256`; una copia con solo
uno de los dos deja el sistema inconsistente.

Nombres reales de los volúmenes en Docker: con el nombre de proyecto `blackwing`
(fijado en el Compose) son `blackwing_postgres-data` y `blackwing_image-data`.

### Sobre el subdirectorio `staging`

`/data/staging` guarda bytes de subidas aún sin procesar. Es transitorio y **la
solución de backup puede excluirlo**: el worker de ingesta recupera o descarta
cualquier trabajo pendiente en el arranque. Respaldarlo no es dañino, pero no
aporta valor.

## Qué debe hacer la solución externa de backup

1. **Base de datos** — preferir un volcado lógico consistente en lugar de copiar
   los ficheros del volumen en caliente:
   ```bash
   docker exec blackwing-postgres-1 \
     pg_dump -U "$BLACKWING_POSTGRES_USER" -Fc "$BLACKWING_POSTGRES_DB" \
     > blackwing-database.dump
   ```
   El formato custom (`-Fc`) se restaura con `pg_restore`.
2. **Árbol de imágenes** — copiar `image-data` completo excepto `staging`. Como el
   árbol es inmutable y direccionado por contenido, admite copia incremental
   segura (un `sha256` nunca cambia de bytes).
3. **Coordinación** — capturar ambas fuentes con la menor separación temporal
   posible. Un original recién ingerido aparece primero en disco y luego como fila;
   por eso, si hay desajuste, es preferible que la copia de imágenes sea **igual o
   más reciente** que la de base de datos (bytes huérfanos son inertes; una fila
   sin bytes provoca 404).
4. **Fuera del servidor** — cifrado, retención y transferencia externa son
   responsabilidad de esa solución, no de Blackwing.

## Procedimiento de restauración

La restauración es **destructiva**: reemplaza el catálogo y las imágenes vivas por
los del backup. Ejecutar con el stack detenido salvo la base de datos.

### Requisitos previos

- El volcado `blackwing-database.dump` y la copia del árbol `image-data` disponibles
  en el host.
- `deploy/compose/.env` presente con las credenciales correctas de PostgreSQL.

### Pasos

1. **Detener** backend y frontend para impedir escrituras concurrentes, dejando
   solo Postgres en marcha:
   ```bash
   docker compose -p blackwing stop backend frontend
   ```
2. **Restaurar la base de datos** dentro del contenedor de Postgres:
   ```bash
   cat blackwing-database.dump | docker exec -i blackwing-postgres-1 \
     pg_restore -U "$BLACKWING_POSTGRES_USER" -d "$BLACKWING_POSTGRES_DB" \
     --clean --if-exists --no-owner --no-privileges
   ```
3. **Restaurar el árbol de imágenes** reponiendo el contenido de `image-data`
   (`/data/images`). Si se restaura montando el volumen en un contenedor auxiliar,
   sincronizar la copia sobre `/data/images` y dejar `/data/staging` vacío.
4. **Arrancar** el resto del stack:
   ```bash
   docker compose -p blackwing start backend frontend
   ```
   En el arranque, el backend aplica automáticamente cualquier migración pendiente
   (ver `DatabaseMigrationExtensions`), por lo que un volcado de un esquema anterior
   se pone al día sin pasos manuales.

### Verificación

```bash
curl -fsS http://localhost:${BLACKWING_HTTP_PORT:-5055}/health/ready
```

Después, iniciar sesión y comprobar que la galería muestra imágenes y que una
miniatura y un original se abren correctamente (valida que catálogo y bytes están
alineados).

## Ensayo de restauración

Ensayar la restauración contra un destino desechable (otro proyecto de Compose o un
host de pruebas) al menos **trimestralmente**, y además tras cualquier cambio en el
layout de persistencia o en la versión mayor de PostgreSQL. Registrar el resultado
para que la recuperación sea un procedimiento probado, no solo escrito.
