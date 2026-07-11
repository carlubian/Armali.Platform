# Observabilidad y diagnóstico

Blackwing es una aplicación interna pequeña; su observabilidad está dimensionada en
consecuencia: correlación de peticiones, salud comprobable, métricas de la cola de
ingesta y una política estricta de no registrar contenido privado. No incorpora un
agregador de logs propio (tipo Seq) ni un endpoint de diagnóstico de frontend; si en
el futuro hicieran falta, encajan sin cambiar este contrato.

## Registro (logging)

- El backend usa el `ILogger` estándar de ASP.NET Core. En Compose los eventos van a
  `stdout`/`stderr` del contenedor y se consultan con `docker compose logs backend`.
- El nivel se controla con `Logging:LogLevel` en la configuración, como es
  convencional.
- **Los logs nunca contienen información privada**: ni contraseñas, tokens o
  cadenas de conexión, ni nombres de fichero subidos, ni bytes o contenido de
  imagen. Los eventos de ingesta se identifican por `JobId`, `ImageId` y
  `OwnerUserId` (un GUID opaco, no un dato personal). Los mensajes de fallo
  registran el código estable del fallo y el mensaje diagnóstico del procesador de
  imagen, nunca el contenido del archivo.

## Correlación de peticiones

- Cada respuesta lleva la cabecera **`X-Trace-ID`** con el identificador de traza
  activo (`Activity.Current?.Id`, con reserva a `HttpContext.TraceIdentifier`).
- Ese mismo identificador se incluye como campo **`traceId`** en todos los cuerpos
  `ProblemDetails` de error (`application/problem+json`).
- Así, un fallo reportado por un usuario (que puede leer el `traceId` de la
  respuesta) se ata directamente al evento correspondiente en los logs del servidor.

La cabecera y el cuerpo se generan una sola vez, temprano en el pipeline
(`UseBlackwingResponseContext`), de modo que tanto las respuestas correctas como las
de error las incluyen.

## Salud

Dos sondas, pensadas para orquestación:

| Endpoint | Qué prueba | Uso |
| --- | --- | --- |
| `GET /health/live` | Que el proceso responde HTTP. | Liveness. |
| `GET /health/ready` | Lo anterior más conectividad con PostgreSQL (`AddDbContextCheck`). | Readiness; el frontend espera esta sonda antes de arrancar. |

El backend aplica las migraciones EF antes de aceptar tráfico, por lo que un
`/health/ready` en verde implica un esquema al día.

## Métricas de la cola de ingesta

La ingesta asíncrona es la parte con más partes móviles, así que es la que se
instrumenta.

### Contadores (`System.Diagnostics.Metrics`)

El `Meter` **`Blackwing.Ingestion`** (`IngestionMetrics`) expone contadores de los
desenlaces terminales de cada trabajo, listos para que un colector externo
(OpenTelemetry, Prometheus) los recoja sin que Blackwing cargue con un exportador
propio:

| Instrumento | Significado |
| --- | --- |
| `blackwing.ingestion.completed` | Trabajos que produjeron una imagen. |
| `blackwing.ingestion.failed` | Trabajos que terminaron en fallo (etiquetados por `failure.code`). |
| `blackwing.ingestion.duplicate` | Trabajos omitidos por ser duplicados del propio usuario. |

Solo se registran números agregados: nunca nombres de fichero, identidades ni bytes.

### Snapshot en vivo (endpoint de operación)

`GET /api/ops/ingestion` — **solo rol `Admin`** — devuelve la foto instantánea de la
cola en todo el despliegue, que es lo que un operador consulta para saber si el
worker va al día:

```json
{
  "pending": 0,
  "processing": 0,
  "completed": 128,
  "failed": 1,
  "duplicate": 3,
  "oldestPendingAgeSeconds": null
}
```

`oldestPendingAgeSeconds` es la antigüedad del trabajo pendiente más viejo (o `null`
si no hay backlog); un valor que crece indica que la ingesta se está quedando atrás.
El endpoint expone **solo recuentos agregados**, nunca imágenes, tags ni nombres de
fichero de ningún usuario, coherente con que el administrador opera el despliegue
pero no ve contenido.

## Recuperación del worker

El worker de ingesta es recuperable por diseño: al arrancar devuelve a la cola los
trabajos que quedaron en `Processing` por una caída previa, y cada fallo se aísla a
su propio trabajo sin bloquear el lote. Un backlog visible en el snapshot que no baja
tras un reinicio es la señal para revisar los logs del backend.
