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
