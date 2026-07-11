# Pruebas de carga: metodología y objetivos

La fase 7 fija la **metodología y los objetivos** de rendimiento; no incorpora
todavía un arnés de carga ejecutable ni un corpus sembrado. Se documenta aquí para
que la ejecución real sea reproducible cuando exista un corpus representativo (o
cuando el corpus de producción alcance la escala de diseño). Esta decisión de
alcance se acordó al planificar la fase.

## Escala de referencia

El diseño dimensiona Blackwing para **~25.000 imágenes por usuario** con un
crecimiento de ~50/semana. A esa escala la huella relacional es pequeña (decenas de
miles de filas de imagen, a lo sumo unos cientos de miles de filas `ImageTag`): **la
base de datos no es el cuello de botella**, sino el almacenamiento y la entrega de
ficheros. Las pruebas deben reflejarlo.

## Corpus representativo

Para medir las rutas calientes basta con sembrar **metadatos** a escala; no hace
falta generar 25.000 ficheros reales salvo para medir la entrega:

- **Galería y autocompletado** — sembrar ~25.000 filas `Image` por usuario (con
  `CapturedAt` distribuido en el tiempo para ejercitar la ordenación por fecha
  efectiva) y un vocabulario de tags realista (p. ej. cientos de `Person`/`Place`,
  algunos miles de `Topic`) con asociaciones `ImageTag` verosímiles. Se puede hacer
  con inserción directa en la base, sin pasar por la ingesta.
- **Ingesta y entrega** — un lote de ficheros reales (cientos), variados en formato
  y tamaño hasta el límite de 100 MB, para medir el rendimiento del worker y del
  streaming de derivados.
- Sembrar **al menos dos usuarios** con volúmenes comparables, para confirmar que el
  filtro por propietario mantiene el coste acotado y que un usuario no ve el trabajo
  del otro.

## Escenarios y objetivos

Umbrales orientativos para un despliegue interno de un nodo, en la percentil 95 bajo
carga concurrente moderada (varios usuarios activos a la vez). Ajustar al hardware
real; lo importante es que **el coste no crezca con el tamaño de la colección**.

| Escenario | Qué se mide | Objetivo p95 |
| --- | --- | --- |
| **Galería (primera página)** | `GET /api/images/` sin cursor, 60 elementos. | < 150 ms |
| **Galería (paginación keyset)** | Avance página a página con `?cursor=`. Debe ser **plano**: la página 100 cuesta como la página 1. | < 150 ms y sin degradación con la profundidad |
| **Filtro por tags (AND)** | `GET /api/images/?tag=…&tag=…` con varios tags combinados. | < 250 ms |
| **Autocompletado** | `GET /api/tags/?type=…&query=…` con prefijos de 1–3 caracteres. | < 100 ms |
| **Facetas** | `GET /api/tags/facets`. | < 300 ms |
| **Entrega de thumbnail** | `GET /api/images/{id}/thumb`, primer acceso (sin caché). | < 100 ms |
| **Entrega cacheada** | Segundo acceso con `If-None-Match`. | `304` inmediato |
| **Ingesta (lote grande)** | Subir cientos de ficheros; la respuesta del endpoint debe ser inmediata y el worker drenar sin fallos que se propaguen. | Respuesta de subida < 2 s; sin fallos en cascada |

## Comprobaciones estructurales (independientes del volumen)

Además de las latencias, verificar que el diseño se sostiene a escala:

- La paginación **keyset** recorre toda la colección sin huecos ni repeticiones y
  sin cargar el corpus completo (ya cubierto a nivel de base en `GalleryReadTests`;
  a escala debe seguir siendo O(página)).
- La ordenación por fecha efectiva se apoya en la **columna generada indexada**
  `EffectiveCapturedAt`, no en un cálculo por fila.
- El filtro AND por tags se apoya en el índice `(TagId, ImageId)`.
- El autocompletado se apoya en el índice de prefijo sobre `NormalizedValue`
  acotado por propietario y tipo.

Confirmar con `EXPLAIN` que estas consultas usan los índices esperados a escala, no
solo que responden rápido con pocos datos.

## Método de ejecución (cuando proceda)

1. Sembrar el corpus en un despliegue de Compose aislado (no producción).
2. Lanzar carga concurrente con una herramienta HTTP (p. ej. `k6`, `bombardier` o
   `NBomber`) contra los escenarios de la tabla, autenticando por cookie.
3. Registrar p50/p95/p99 y comparar contra los objetivos; usar `EXPLAIN (ANALYZE)`
   sobre las consultas calientes para confirmar el uso de índices.
4. Anotar los resultados y el hardware para tener una línea base repetible.
