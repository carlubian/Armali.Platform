# Revisión de seguridad

Revisión de la postura de seguridad de Blackwing v1 al cierre de la fase 7. El
principio rector es que **todo el contenido es estrictamente privado por usuario**:
ninguna imagen ni tag sale del ámbito de su propietario, y el rol de administrador
gestiona cuentas pero nunca ve contenido ajeno.

## Autenticación y sesión

- Identidad basada en ASP.NET Identity con cookie de sesión (`IdentityConstants.ApplicationScheme`).
- El login limita la contraseña a un mínimo de 12 caracteres y aplica bloqueo de
  cuenta por intentos fallidos (`AccessFailedAsync` / `ResetAccessFailedCountAsync`).
- **Rate limiting** de login: ventana fija de 5 intentos por minuto (`login`),
  además del bloqueo de Identity, para frenar fuerza bruta.
- El usuario y contraseña iniciales del administrador se inyectan por configuración
  y se siembran de forma idempotente; no hay credenciales en el repositorio.

## Antiforgery

- Toda petición mutante a `/api` (cualquier método que no sea GET/HEAD/OPTIONS)
  valida el token antiforgery `X-CSRF-TOKEN`; la única excepción es el propio
  endpoint que lo emite (`/api/auth/antiforgery`). Ver el middleware en `Program.cs`.
- El token se emite por sesión y el frontend lo adjunta a cada escritura.

## Aislamiento de propietario (la frontera de privacidad)

- El identificador de propietario no es un parámetro de la petición: se deriva del
  usuario autenticado (`IUserScope` / `CurrentUserScope`) y toda consulta y mutación
  se acota por él.
- La entrega de imágenes **nunca** usa ficheros estáticos. `GET /api/images/{id}/thumb|preview|original`
  resuelve primero la fila por `(id, OwnerUserId)`; si no es del solicitante devuelve
  `404` (no `403`), de modo que un id ajeno «no existe» y no se puede sondear.
- La separación física por `userId` en el árbol de ficheros es defensa en
  profundidad, no la frontera principal; la frontera es la comprobación de propiedad
  en cada endpoint.
- Cobertura de pruebas: `GalleryDeliveryTests` y `GalleryLifecycleTests` verifican
  que un segundo usuario no puede leer, listar, etiquetar, fusionar ni borrar el
  contenido del primero.

## Validación de subidas

Defensa en capas antes de que un byte llegue a convertirse en imagen:

1. **Límite de tamaño en streaming** — el área de staging corta la subida en cuanto
   supera `Ingestion:MaxFileBytes` (100 MB por defecto) sin bufferizar el fichero
   completo; un fichero vacío también se rechaza.
2. **Sniffing de formato por magic bytes** — `ImageFormatDetector` decide el formato
   real a partir de la cabecera (JPEG/PNG/WebP), no del `Content-Type` declarado, que
   es solo una pista y puede mentir.
3. **Decodificación autoritativa** — el procesador (Magick.NET) decodifica de verdad;
   un fichero mal etiquetado u hostil que pase el sniffing falla aquí y el trabajo se
   marca como fallo permanente descartando los bytes.
4. **Límite de memoria de decodificación** — `ResourceLimits.Memory` acota la memoria
   por decodificación (`Ingestion:ProcessingMemoryLimitBytes`, 1 GiB por defecto), de
   forma que una «bomba de descompresión» no agota el proceso (ImageMagick vuelca a
   disco al superar el límite).
5. **Derivados sin metadatos** — preview y thumbnail se generan con `Strip()`, así que
   no propagan EXIF ni datos incrustados del original.
6. **Tokens de staging validados** — el token debe ser hexadecimal, evitando
   travesía de rutas en el árbol de staging.

## Cabeceras y transporte

- `X-Content-Type-Options: nosniff` en todas las respuestas de la API, incluidas las
  de entrega de imágenes, para impedir que el navegador infiera un tipo distinto del
  declarado.
- Los derivados se sirven con `Cache-Control: private, immutable` + `ETag` fuerte;
  al ser inmutables (direccionados por hash) no hay problema de invalidación y no se
  cachean en intermediarios compartidos.
- El original se entrega como `Content-Disposition: attachment` (descarga), no
  incrustable.
- TLS es responsabilidad del reverse proxy que termina el tráfico delante del
  frontend Caddy; el stack interno de Compose no expone puertos salvo el del
  frontend.

## Trazabilidad y logs

- Cada respuesta lleva `X-Trace-ID`, correlacionado con el `traceId` de los cuerpos
  `ProblemDetails`. Ver [observabilidad.md](observabilidad.md).
- **Los logs no contienen información privada**: ni secretos, ni nombres de fichero
  subidos, ni contenido de imagen. Los eventos de ingesta se identifican por GUIDs
  opacos y códigos de fallo estables.
- El snapshot de operación (`/api/ops/ingestion`) expone solo recuentos agregados y
  exige rol `Admin`.

## Riesgos aceptados y límites de v1

- **Sin backups propios** (decisión de fase 0): una solución externa respalda los
  volúmenes; ver [volumenes-y-backup.md](volumenes-y-backup.md).
- **TLS delegado** al proxy de borde; Blackwing no gestiona certificados.
- **Un solo proveedor** (PostgreSQL) y despliegue de un solo nodo; no hay alta
  disponibilidad, acorde al uso interno previsto.
