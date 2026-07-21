# Megnir

Servicio de backup one-shot en .NET 10. Este README documenta la configuración del
ejecutable `Megnir`.

## Configuración

La configuración se define en `appsettings.json` (sección `Megnir`) y se bindea a los
POCOs de `Configuration/MegnirOptions.cs`:

| Clave | Descripción |
| --- | --- |
| `Apps[]` | Lista de aplicaciones a respaldar. Cada una con `Name` y `Paths` (rutas de origen). |
| `Azure.Container` | Contenedor de destino en Azure Storage / Data Lake. |
| `Azure.HostPrefix` | Prefijo por host dentro del contenedor. `auto` = hostname del nodo. |
| `Retention.KeepLast` | Número de copias más recientes a conservar por host. |
| `SizeWarningMb` | Tamaño (MB) a partir del cual se avisa por el log. |

## Secreto de conexión de Azure (RNF3)

El secreto de conexión (connection string / SAS) **no se versiona ni se define en
`appsettings.json`**. Se lee en tiempo de ejecución de la variable de entorno:

```
MEGNIR_AZURE_CONNECTIONSTRING
```

Se asigna a `AzureOptions.ConnectionString` durante el binding. Si la variable no está
definida, el campo queda vacío.

H2 acepta exclusivamente un **connection string de Storage Account**. Puede ser uno con
`AccountKey` o uno que incluya `SharedAccessSignature`; no se acepta una SAS URL aislada.
La variable se valida antes de usarla y nunca se muestra en logs ni excepciones.

Ejemplo (Linux / systemd, no versionar):

```bash
export MEGNIR_AZURE_CONNECTIONSTRING="DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net"
```

Ejemplo (PowerShell, desarrollo local):

```powershell
$env:MEGNIR_AZURE_CONNECTIONSTRING = "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net"
```

## Ejecución

```
dotnet run --project src/Megnir
```

## Azure Blob Storage operation (H2)

### Required permissions

The identity represented by `MEGNIR_AZURE_CONNECTIONSTRING` must be allowed to
create the container when it does not exist, and to create and query blobs within
the host prefix. For an account configured with SAS, the equivalent permissions
are `c`, `l`, and `w`. H3 will additionally require delete permission; H2 never
deletes blobs.

Each execution uploads the archive to:

```
<container>/<hostPrefix>/megnir-backup-<yyyyMMdd-HHmmss>.zip
```

`hostPrefix=auto` is derived from the machine name. If a blob with the same name
already exists, the upload fails without replacing it and the local `.zip` remains
in `OutputDirectory`.

### Local development (Windows)

Set the variable in the same console that starts Megnir; persistent Windows
environment variables are not inherited by processes that were already open:

```powershell
$env:MEGNIR_AZURE_CONNECTIONSTRING = "<connection-string>"
dotnet run --project src/Megnir
```

For a real test, use a test container and an application that points to a small
folder. Afterwards, confirm that the blob is under the expected prefix and its
length matches the `.zip` in `OutputDirectory`. Do not copy the connection string
or SAS URLs into commands, configuration, logs, or incident reports.

### Planned production setup (H4)

H4 will configure systemd to read the secret from a file outside the repository:

```ini
# /etc/megnir/megnir.env (owned by root; mode 0600)
MEGNIR_AZURE_CONNECTIONSTRING="<connection-string>"
```

The unit will include `EnvironmentFile=/etc/megnir/megnir.env`. Do not add the
secret to `appsettings.json`, command-line arguments, or versioned files.

### Troubleshooting

| Symptom | Check / action |
| --- | --- |
| Azure configuration is missing | Check that the variable exists in the Megnir process, without printing it. |
| Authorization or network failure | Verify connectivity and create/list/write permissions for the container. The local `.zip` remains available. |
| The blob already exists | This is an intentional non-overwrite collision; use a new execution or investigate the existing artifact. |
| Partial result | The remote upload was confirmed but a local path could not be copied; Megnir returns code `2`. |

An Azure failure returns code `1`; a successful backup returns `0`. Megnir never
emits the connection string, SAS, or signed URLs in its logs.
