# ?? Ejecutar External Gateway con Aspire (Local)

Este documento explica cómo ejecutar el External Gateway localmente usando .NET Aspire para pruebas y desarrollo.

## ?? Requisitos Previos

- .NET 10 SDK
- Docker Desktop (para contenedores)
- Visual Studio 2025 o VS Code con extensión de C#

## ??? Arquitectura en Aspire

Cuando ejecutas el AppHost de Aspire, se levantan automáticamente:

```
???????????????????????????????????????????????????????????????
?                    Aspire Dashboard                          ?
?                 (https://localhost:15888)                    ?
???????????????????????????????????????????????????????????????
                              ?
        ?????????????????????????????????????????????
        ?                     ?                     ?
        ?                     ?                     ?
????????????????      ????????????????     ????????????????
? PostgreSQL   ?      ?  SQL Server  ?     ?  RabbitMQ    ?
? Container    ?      ?  Container   ?     ?  Container   ?
?              ?      ?              ?     ?              ?
? Port: 5432   ?      ? Port: 1433   ?     ? Port: 5672   ?
????????????????      ????????????????     ????????????????
        ?                     ?                     ?
        ?                     ?                     ?
        ?                     ?                     ?
????????????????      ????????????????     ????????????????
? Playground   ?      ?  External    ?     ?  Playground  ?
?    API       ????????   Gateway    ???????   Blazor     ?
????????????????      ????????????????     ????????????????
```

## ?? Inicio Rápido

### 1. Clonar y Restaurar

```bash
cd C:\Users\David\source\repos\platform\src
dotnet restore
```

### 2. Ejecutar con Aspire

```bash
# Desde el directorio del AppHost
cd Playground\FSH.Playground.AppHost
dotnet run
```

O desde Visual Studio:
1. Abrir la solución `FSH.Platform.sln`
2. Establecer `FSH.Playground.AppHost` como proyecto de inicio
3. Presionar F5 para ejecutar

### 3. Acceder al Dashboard de Aspire

Una vez iniciado, se abrirá automáticamente el dashboard en:
```
https://localhost:15888
```

Aquí podrás ver:
- ? Estado de todos los servicios
- ?? Logs en tiempo real
- ?? Métricas y trazas
- ?? Enlaces directos a cada servicio

## ??? Inicializar Base de Datos de Prueba

El contenedor de SQL Server se crea vacío. Para inicializarlo con datos de prueba:

### Opción A: Desde Visual Studio / Azure Data Studio

1. Conectarse a SQL Server:
   - Server: `localhost,1433`
   - User: `sa`
   - Password: (generada por Aspire, visible en el dashboard)
   - Database: `ClientDB`

2. Ejecutar el script:
   ```bash
   ExternalGateway\init-db-v1.sql
   ```

### Opción B: Desde línea de comandos

```bash
# Obtener el container ID
docker ps | grep sqlserver-fsh-playground

# Copiar el script al contenedor
docker cp ExternalGateway\init-db-v1.sql <container-id>:/tmp/

# Ejecutar el script
docker exec -it <container-id> /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P <password> \
  -i /tmp/init-db-v1.sql -C
```

### Opción C: Script PowerShell automatizado

```powershell
# Crear script init-gateway-db.ps1
$containerName = "sqlserver-fsh-playground"
$password = Read-Host "Ingrese la contraseña de SA" -AsSecureString
$passwordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($password))

docker cp ExternalGateway\init-db-v1.sql ${containerName}:/tmp/
docker exec -it $containerName /opt/mssql-tools18/bin/sqlcmd `
  -S localhost -U sa -P $passwordPlain `
  -i /tmp/init-db-v1.sql -C

Write-Host "Base de datos inicializada correctamente" -ForegroundColor Green
```

## ?? Probar el Gateway

### 1. Verificar que los servicios están corriendo

En el Aspire Dashboard, verifica que estén en estado "Running":
- ? playground-api
- ? external-gateway
- ? rabbitmq
- ? sqlserver

### 2. Probar desde la API

```bash
# Obtener el puerto de la API desde el dashboard (ej: https://localhost:7030)
curl -X POST https://localhost:7030/api/v1/gateway/invoices/INV-001 \
  -H "Content-Type: application/json"
```

### 3. Probar desde Playground Blazor

1. Navegar a la app Blazor (ej: https://localhost:5001)
2. Ir a la sección "External Gateway"
3. Ingresar ID de factura: `INV-001`
4. Hacer clic en "Consultar"

### 4. Ver los logs en tiempo real

En el Aspire Dashboard:
1. Seleccionar `external-gateway` en la lista de recursos
2. Click en "View Logs"
3. Deberías ver logs como:

```
[Information] External Gateway starting for Client: local-test
[Information] Processing GetInvoice request for InvoiceId: INV-001
[Information] Query GetInvoice completed: 1 rows in 45ms
[Information] Successfully processed GetInvoice for INV-001, 3 lines
```

## ?? Configuración

### Variables de Entorno

El AppHost configura automáticamente:

```csharp
.WithEnvironment("GatewayOptions__ClientId", "local-test")
.WithEnvironment("GatewayOptions__SchemaVersion", "v1")
.WithEnvironment("GatewayOptions__DefaultTimeoutSeconds", "30")
.WithEnvironment("GatewayOptions__EnableQueryLogging", "true")
```

### Conexiones Automáticas

Aspire inyecta automáticamente las connection strings:
- **SQL Server**: via `.WithReference(sqlserver)`
- **RabbitMQ**: via `.WithReference(rabbitmq)`

No necesitas configurar las connection strings manualmente.

## ?? Monitoreo

### RabbitMQ Management UI

```
http://localhost:15672
User: admin (del parámetro rabbitmq-username)
Password: (del parámetro rabbitmq-password)
```

Aquí puedes ver:
- Colas creadas: `fsh.gateway.local-test.queue`
- Mensajes en tránsito
- Tasa de mensajes

### Aspire Dashboard

El dashboard muestra:
- **Traces**: Seguimiento de cada request end-to-end
- **Metrics**: CPU, memoria, request rate
- **Logs**: Logs agregados de todos los servicios
- **Dependencies**: Grafo de dependencias entre servicios

## ?? Troubleshooting

### El Gateway no se conecta a SQL Server

1. Verificar que el contenedor esté corriendo:
   ```bash
   docker ps | grep sqlserver
   ```

2. Ver logs del contenedor:
   ```bash
   docker logs sqlserver-fsh-playground
   ```

3. Verificar la connection string en el dashboard de Aspire

### RabbitMQ no acepta conexiones

1. Esperar a que RabbitMQ esté completamente iniciado (puede tardar 30-60s)
2. Verificar en el dashboard que el estado sea "Running"
3. Verificar las credenciales en `appsettings.Development.json`

### No se procesan los mensajes

1. Verificar que la cola existe en RabbitMQ Management UI
2. Ver logs del Gateway para errores
3. Verificar que la API esté publicando a la cola correcta

## ?? Datos de Prueba

El script `init-db-v1.sql` crea:

| Cliente | Factura | Total | Líneas |
|---------|---------|-------|--------|
| CUST-001 | INV-001 | $1,500 | 3 |
| CUST-002 | INV-002 | $2,500 | 2 |

Puedes probar con:
- `INV-001`: Factura con 3 líneas
- `INV-002`: Factura con 2 líneas
- `INV-999`: No existe (error esperado)

## ?? Siguientes Pasos

1. **Agregar más queries**: Edita `QueryRepository.cs`
2. **Probar esquema v2**: Crear nuevo script `init-db-v2.sql`
3. **Múltiples clientes**: Ejecutar varias instancias del Gateway
4. **Performance testing**: Usar herramientas de carga

## ?? Recursos

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Rebus Documentation](https://github.com/rebus-org/Rebus)
- [External Gateway README](./README.md)
