# Blackwing — Roadmap de implementación

## Propósito y estado

Este documento convierte los requisitos funcionales de `REQUIREMENTS.md` y la
arquitectura de `ARCHITECTURE.md` en una secuencia de implementación. Se ha
cerrado la fase 0; cada fase posterior tiene una salida verificable y es
prerrequisito de la siguiente, salvo cuando se indique lo contrario.

Blackwing es una aplicación web independiente de Segaris y Belfalas dentro del
monorepo Armali.Platform. Reutiliza sus patrones, no sus instancias ni sus datos.

## Fase 0 — Decisiones fijadas

| Área | Decisión |
| --- | --- |
| Filtros por tags | Los tags seleccionados se combinan siempre con **AND**, sin importar su tipo. Una imagen debe contener todos los tags seleccionados. |
| Formatos v1 | Se aceptan JPEG, PNG y WebP. HEIC/HEIF se evaluará cuando haya una necesidad real; no modifica el modelo ni las APIs. |
| Tamaño y derivados | Límite de 100 MB por archivo. Se conserva el original sin modificar; preview y thumbnail se generan en WebP y con la orientación EXIF aplicada. |
| Cargas masivas | Se procesan mediante una cola persistente y un worker. La interfaz no espera a generar derivados y muestra progreso y errores por archivo. |
| Backups | No son responsabilidad de Blackwing v1. PostgreSQL y el almacenamiento de imágenes son volúmenes Docker persistentes que una aplicación futura respaldará por separado. |

Decisiones aplazadas que no bloquean el inicio: librería concreta de tratamiento
de imágenes (debe tener licencia compatible) y un posible proveedor SQLite
adicional. PostgreSQL es el único proveedor de v1.

## Fase 1 — Base del producto y desarrollo local

**Objetivo:** disponer de una aplicación vacía, ejecutable y validada de extremo
a extremo.

- Crear la solución ASP.NET por capas: API, Persistence y Shared.
- Crear el cliente React 19 + Vite con React Router, TanStack Query, React Hook
  Form, Zod e i18next.
- Integrar los tokens y fundamentos del sistema de diseño Armali para escritorio.
- Definir configuración tipada, gestión de secretos y endpoints de salud.
- Añadir Docker Compose: API, frontend servido por Caddy, PostgreSQL y volumen
  persistente de imágenes.
- Incorporar build, lint y pruebas al CI del monorepo.

**Salida verificable:** el stack arranca con un único comando, expone health
checks y CI construye y prueba frontend y backend.

## Fase 2 — Identidad, roles y perímetro de privacidad

**Objetivo:** establecer autenticación local y garantizar que el contenido nunca
sale del ámbito de su propietario.

- Implementar un módulo de identidad aislado basado en ASP.NET Identity.
- Añadir inicio/cierre de sesión por cookie, antiforgery y rate limiting de login.
- Modelar los roles `User` y `Admin`.
- Crear operaciones de administración: alta de cuentas y restablecimiento de
  contraseña. No incluir ninguna consulta de contenido de otros usuarios.
- Establecer la convención de propietario en entidades, repositorios y endpoints:
  toda consulta y mutación de contenido recibe el usuario autenticado como scope.
- Añadir pruebas de autorización y aislamiento entre dos usuarios.

**Salida verificable:** un usuario y un administrador no pueden leer, adivinar
ni manipular imágenes o tags de otra cuenta; el administrador sólo administra
cuentas.

## Fase 3 — Dominio, persistencia y almacenamiento privado

**Objetivo:** fijar el modelo de galería y un almacenamiento de archivos seguro,
escalable y desacoplado del dominio.

- Crear migraciones PostgreSQL para `Image`, `Tag` e `ImageTag`.
- Añadir las restricciones e índices definidos en arquitectura: unicidad por
  `(OwnerUserId, Sha256)`, unicidad de tag normalizado por usuario y tipo,
  índices para autocompletado, relaciones y ordenación.
- Representar la revisión mediante `ReviewedAt`; distinguir pendiente de
  revisado sin tags.
- Implementar el puerto de almacenamiento de imágenes y su adaptador local con
  rutas direccionadas por SHA-256 y separadas por usuario.
- Preparar operaciones transaccionales para asociaciones de tags, borrado de
  imágenes y fusión de tags.

**Salida verificable:** las migraciones crean el esquema completo y las pruebas
demuestran deduplicación por usuario, normalización de tags y aislamiento de
propietario.

## Fase 4 — Ingesta asíncrona y derivados

**Objetivo:** importar imágenes individuales o masivas sin bloquear la
aplicación ni cargar archivos completos en memoria.

- Definir y crear tablas/estados de trabajos de carga persistentes.
- Implementar endpoint y UI de carga múltiple con validación de JPEG, PNG y
  WebP, límite de 100 MB y resultados por archivo.
- Guardar temporalmente de forma segura, calcular SHA-256 mediante streaming y
  detectar duplicados dentro del usuario.
- Implementar un worker recuperable que extraiga dimensiones, fecha y
  orientación EXIF, almacene el original y genere preview y thumbnail WebP.
- Registrar progreso, fallo y mensaje de diagnóstico por elemento; permitir
  reintentos seguros de fallos recuperables.
- Elegir en esta fase la librería de imagen después de verificar licencia,
  compatibilidad de formatos y uso de memoria.

**Salida verificable:** una importación grande devuelve control de inmediato,
los archivos procesados aparecen pendientes de revisión y los fallos no dañan
ni bloquean el resto del lote.

## Fase 5 — Revisión, tags y mantenimiento de la colección

**Objetivo:** permitir que cada usuario organice su propia galería.

- Construir la pantalla de revisión de imágenes pendientes, de una en una.
- Añadir tags de tipo `Person`, `Place` y `Topic`, con autocompletado por
  propietario y tipo; crear un tag cuando no exista.
- Permitir marcar explícitamente una imagen como revisada, con o sin tags.
- Crear edición de tags para imágenes existentes.
- Implementar borrado de imagen, asociaciones y derivados; eliminar sólo tags
  huérfanos del propio usuario.
- Implementar migración A→B de tags como transacción, evitando asociaciones
  duplicadas y eliminando A al finalizar.

**Salida verificable:** un usuario puede completar todo el ciclo de revisión,
editar y borrar sus imágenes, y fusionar tags sin afectar contenido ajeno.

## Fase 6 — Galería, filtros y entrega autorizada

**Objetivo:** navegar una colección grande de forma rápida y privada.

- Crear la galería paginada de thumbnails con orden de fecha de captura y
  fallback a fecha de carga.
- Implementar keyset pagination estable, sin cargar toda la colección.
- Exponer selector/autocompletado de tags y aplicar la semántica cerrada de AND
  para todos los tags seleccionados.
- Añadir vista específica de imágenes pendientes de revisión.
- Crear vista de detalle con preview y acción explícita para obtener el original.
- Implementar endpoints autenticados de thumbnail, preview y original, que
  comprueben propiedad antes de hacer streaming y soporten rangos HTTP.
- Añadir ETag y `Cache-Control: private, immutable` a derivados inmutables.

**Salida verificable:** una galería de decenas de miles de imágenes mantiene
respuesta ágil, y ninguna imagen se sirve por una ruta estática o sin comprobar
autorización.

## Fase 7 — Calidad, operación y lanzamiento interno

**Objetivo:** dejar la aplicación lista para un uso interno mantenible.

- Completar pruebas unitarias, de integración, autorización y flujos E2E.
- Ejecutar pruebas de carga de importación, galería y autocompletado sobre un
  corpus representativo.
- Revisar seguridad: validación de archivos, límites, antiforgery, cabeceras,
  trazabilidad y ausencia de información privada en logs.
- Añadir observabilidad, métricas de cola y documentación de despliegue,
  diagnóstico y operación de volúmenes.
- Documentar explícitamente los volúmenes que la solución externa de backup debe
  proteger, así como un procedimiento de restauración para cuando exista.
- Preparar despliegue interno reproducible mediante Compose y CI.

**Salida verificable:** la aplicación se despliega desde CI, los flujos críticos
están cubiertos por pruebas y los operadores conocen los datos persistentes que
debe incluir la solución externa de backup.

**Estado:** completada. Cobertura E2E de flujos completos vía integración HTTP
(`GalleryLifecycleTests`), observabilidad escalada (correlación `X-Trace-ID`,
`traceId` en `ProblemDetails`, métricas de la cola de ingesta y snapshot
admin-only `GET /api/ops/ingestion`), revisión de seguridad y runbooks de
operación bajo `docs/operations/`. El detalle y el mapa de aceptación están en
[`FASE7_ACEPTACION.md`](FASE7_ACEPTACION.md). Las decisiones de alcance: E2E HTTP
(sin navegador), pruebas de carga documentadas como metodología y objetivos, y
observabilidad sin Seq ni diagnóstico de frontend.

## Estado del roadmap

Las siete fases están completadas. Blackwing v1 queda listo para uso interno
mantenible: identidad y aislamiento por propietario, dominio y almacenamiento
privado, ingesta asíncrona con derivados, revisión y mantenimiento de la
colección, galería con filtros y entrega autorizada, y la capa de calidad y
operación de la fase 7. El trabajo futuro (HEIC/HEIF, un segundo proveedor
SQLite, o una solución de backup propia) queda fuera de v1 y no está planificado.
