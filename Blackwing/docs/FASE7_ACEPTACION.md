# Fase 7 — Aceptación (calidad, operación y lanzamiento interno)

Este documento mapea cada objetivo de la fase 7 del [ROADMAP](ROADMAP.md) a su
realización concreta en el código y la documentación, y registra las decisiones de
alcance acordadas al planificar la fase.

## Decisiones de alcance de la fase

| Frente | Decisión | Motivo |
| --- | --- | --- |
| E2E | Cobertura de flujos completos vía **integración HTTP** (sin navegador). | Ligera, corre en CI con Docker; es el patrón del hermano Segaris. Playwright sería infraestructura desproporcionada para una app interna. |
| Pruebas de carga | **Metodología y objetivos documentados**, sin arnés ejecutable todavía. | La ejecución real requiere un corpus representativo que aún no existe; se deja reproducible para cuando lo haya. |
| Observabilidad | **Escalada a Blackwing**: correlación, métricas de cola, redacción. Sin Seq ni diagnóstico de frontend. | Dimensionada al tamaño real de la app; los frentes omitidos encajan luego sin romper el contrato. |
| Backups | **Solo documentación** de volúmenes y restauración. | Fijado en la fase 0: los respalda una solución externa. |

## Mapa de objetivos

### 1. Pruebas unitarias, de integración, autorización y flujos E2E

- Suite de backend con Testcontainers (PostgreSQL), que se auto-omite sin Docker y
  corre completa en CI.
- Nuevo `GalleryLifecycleTests` cubre el ciclo completo sobre HTTP: subida → pendiente
  → revisión con tags → filtrado por tag y estado → autocompletado → fusión de tags →
  borrado, con un **segundo usuario presente en cada paso** para demostrar el
  aislamiento (lectura, etiquetado, fusión y borrado ajenos rechazados).
- Autorización cubierta además en `GalleryDeliveryTests` (entrega de derivados,
  404 entre usuarios, 401 anónimo) y en las pruebas del endpoint de operación
  (403 para no-admin, 401 anónimo).

### 2. Pruebas de carga sobre corpus representativo

- Metodología, corpus, escenarios y objetivos p95 en
  [operations/pruebas-de-carga.md](operations/pruebas-de-carga.md), incluidas las
  comprobaciones estructurales (keyset plano, columna generada indexada, índices de
  tags y de prefijo) a confirmar con `EXPLAIN`.

### 3. Revisión de seguridad

- [operations/seguridad.md](operations/seguridad.md): autenticación y rate limiting,
  antiforgery, aislamiento de propietario como frontera de privacidad, validación de
  subidas en capas (tamaño en streaming, magic bytes, decodificación autoritativa,
  límite de memoria, strip de metadatos, tokens validados), cabeceras (`nosniff`,
  caché privada inmutable), trazabilidad y ausencia de datos privados en logs.

### 4. Observabilidad, métricas de cola y documentación de operación

- Correlación `X-Trace-ID` en toda respuesta + `traceId` en `ProblemDetails`
  (`ObservabilityExtensions`, `AddProblemDetails`).
- `Meter` `Blackwing.Ingestion` con contadores de desenlaces + snapshot en vivo
  admin-only `GET /api/ops/ingestion` (`IngestionMetrics`, `OpsEndpoints`).
- Sondas `/health/live` y `/health/ready` (con chequeo de base).
- Documentación en [operations/observabilidad.md](operations/observabilidad.md) y
  [operations/despliegue.md](operations/despliegue.md).

### 5. Documentar volúmenes de backup y procedimiento de restauración

- [operations/volumenes-y-backup.md](operations/volumenes-y-backup.md): los dos
  volúmenes con estado (`postgres-data`, `image-data`), qué contiene cada uno, qué
  debe capturar la solución externa (y por qué como unidad coherente), y el
  procedimiento de restauración destructivo con verificación y ensayo periódico.

### 6. Despliegue interno reproducible por Compose y CI

- [operations/despliegue.md](operations/despliegue.md): componentes, configuración,
  admin inicial, puesta en marcha, actualización con migraciones automáticas y la
  relación con el workflow de CI, que valida el mismo Compose que se despliega.

## Salida verificable de la fase

> «La aplicación se despliega desde CI, los flujos críticos están cubiertos por
> pruebas y los operadores conocen los datos persistentes que debe incluir la
> solución externa de backup.»

- **Despliegue desde CI:** workflow que construye y prueba backend y frontend y
  valida el Compose; el mismo Compose es el de despliegue.
- **Flujos críticos cubiertos:** ciclo completo de revisión/tags/galería/fusión/
  borrado y aislamiento entre usuarios en pruebas de integración HTTP.
- **Datos persistentes conocidos:** runbook de volúmenes y restauración explícito.
