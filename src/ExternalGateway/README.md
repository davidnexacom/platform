# External Gateway - SQL Server Bridge

## ?? Descripción

El **External Gateway** es un proceso independiente que actúa como puente entre la API principal y bases de datos SQL Server remotas de clientes. Permite consultar datos de múltiples clientes con diferentes versiones de esquemas sin necesidad de despliegues en cada cliente.

## ??? Arquitectura con TypeBased Routing

```
???????????????????
?  Playground.Api ?
?                 ?
?  POST /invoices ?????
???????????????????   ?
                      ? Publish Request (ClientId: "default")
                      ?
              ?????????????????
              ?   RabbitMQ    ?
              ?               ?
              ?  Type-Based   ?
              ?   Routing     ?
              ?????????????????
                      ?
                      ?????????????????????????????????????
                      ?                 ?                 ?
              Queue: default    Queue: client1   Queue: client2
                      ?                 ?                 ?
        ??????????????????   ???????????????????   ??????????????????
        ? Gateway Default?   ? Gateway Client1  ?   ? Gateway Client2?
        ?                ?   ?                  ?   ?                ?
        ? Schema v1      ?   ? Schema v1        ?   ? Schema v2      ?
        ? ClientId:      ?   ? ClientId:        ?   ? ClientId:      ?
        ? "default"      ?   ? "client1"        ?   ? "client2"      ?
        ??????????????????   ????????????????????   ??????????????????
                 ?                    ?                    ?
                 ? Execute SQL        ? Execute SQL        ? Execute SQL
                 ?                    ?                    ?
        ??????????????????   ???????????????????   ??????????????????
        ? SQL Server     ?   ? SQL Server      ?   ? SQL Server     ?
        ? Aspire/Local   ?   ? Cliente 1       ?   ? Cliente 2      ?
        ? (Esquema v1)   ?   ? (Esquema v1)    ?   ? (Esquema v2)   ?
        ??????????????????   ???????????????????   ??????????????????
```

## ?? Características

### ? Ventajas

1. **Server-Side Queries**: Las queries SQL se definen en el servidor, no en el cliente
2. **Multi-Version Support**: Soporta múltiples versiones de esquemas de BD
3. **Zero-Deployment**: Los clientes solo ejecutan el Gateway, sin cambios de código
4. **Type-Safe**: Contratos fuertemente tipados compartidos
5. **Asynchronous**: Comunicación asíncrona via RabbitMQ
6. **Resilient**: Retry automático y manejo de errores
7. **TypeBased Routing**: Cada mensaje llega solo al gateway correcto según ClientId
8. **Isolated Queues**: Colas independientes por cliente para aislamiento total
9. **Aspire Integration**: Integración nativa con .NET Aspire para orquestación local

## ?? Integración con .NET Aspire

El External Gateway está completamente integrado con **.NET Aspire** para desarrollo local:

### Configuración en AppHost

```csharp
// En Playground/FSH.Playground.AppHost/AppHost.cs

// SQL Server container para External Gateway
var sqlserver = builder
    .AddSqlServer("sqlserver")
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("ClientDB");

// RabbitMQ para mensajería
var rabbitmq = builder
    .AddRabbitMQ("rabbitmq", userName: rabbitmqUsername, password: rabbitmqPassword)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithManagementPlugin();

// External Gateway con referencias a recursos
builder.AddProject<Projects.FSH_ExternalGateway_Host>("external-gateway")
    .WithReference(sqlserver)      // ? Conexión SQL Server automática
    .WithReference(rabbitmq)       // ? Conexión RabbitMQ automática
    .WithEnvironment("GatewayOptions__ClientId", "local-test")
    .WithEnvironment("GatewayOptions__SchemaVersion", "v1")
    .WaitFor(sqlserver)
    .WaitFor(rabbitmq);
```

### Cómo Funciona

1. **Aspire inyecta** las connection strings automáticamente:
   - `ConnectionStrings:ClientDB` ? SQL Server
   - `ConnectionStrings:rabbitmq` ? RabbitMQ

2. **Program.cs detecta** las connection strings de Aspire:
   ```csharp
   var sqlConnectionString = builder.Configuration.GetConnectionString("ClientDB");
   var rabbitMqConnectionString = builder.Configuration.GetConnectionString("rabbitmq");
   ```

3. **Si existen**, las usa automáticamente. Si no, usa `appsettings.json`

### Logs de Inicio

Cuando se ejecuta con Aspire, verás:
```
[Information] Starting External Gateway for Client: local-test
[Information] Using SQL Server connection from Aspire: ClientDB
[Information] RabbitMQ Connection: amqp://admin:***@localhost:5672
[Information] SQL Server: Configured (Source: Aspire)
```

## ?? Instalación y Configuración

### Opción A: Ejecutar con Aspire (Recomendado para desarrollo)

```bash
# 1. Ejecutar Aspire AppHost (levanta TODO: API, Blazor, Gateway, SQL Server, RabbitMQ, Redis)
cd Playground\FSH.Playground.AppHost
dotnet run

# 2. Acceder a Aspire Dashboard
# https://localhost:17000

# 3. Ver logs del External Gateway
# En el dashboard de Aspire ? "external-gateway"
```

**Ventajas**:
- ? No necesitas Docker compose ni configurar nada
- ? SQL Server, RabbitMQ y Redis se levantan automáticamente
- ? Connection strings se inyectan automáticamente
- ? Dashboard de Aspire para monitoreo en tiempo real
- ? Logs centralizados de todos los servicios

### Opción B: Ejecutar Standalone (Sin Aspire)

Si quieres ejecutar el gateway de forma independiente:

#### 1. Levantar infraestructura

```bash
# SQL Server
docker run -d --name sqlserver \
  -e "ACCEPT_EULA=Y" \
  -e "SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 \
  mcr.microsoft.com/mssql/server:2022-latest

# RabbitMQ
docker run -d --name rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  -e RABBITMQ_DEFAULT_USER=admin \
  -e RABBITMQ_DEFAULT_PASS=Admin123! \
  rabbitmq:3-management
```

#### 2. Configurar `appsettings.Development.json`

```json
{
  "GatewayOptions": {
    "ClientId": "local-test",
    "SchemaVersion": "v1",
    "ConnectionString": "Server=localhost,1433;Database=ClientDB;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;Encrypt=False",
    "DefaultTimeoutSeconds": 30,
    "EnableQueryLogging": true
  },
  "RabbitMqOptions": {
    "ConnectionString": "amqp://admin:Admin123!@localhost:5672",
    "QueueName": "fsh.gateway.local-test.queue",
    "MaxConcurrentMessages": 5,
    "RetryAttempts": 3
  }
}
```

#### 3. Ejecutar Gateway

```bash
cd ExternalGateway\FSH.ExternalGateway.Host
dotnet run
```

### Opción C: Docker Compose

```yaml
# docker-compose.yml
version: '3.8'

services:
  rabbitmq:
    image: "rabbitmq:3-management"
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: admin
      RABBITMQ_DEFAULT_PASS: Admin123!

  sqlserver:
    image: "mcr.microsoft.com/mssql/server:2022-latest"
    ports:
      - "1433:1433"
    environment:
      ACCEPT_EULA: "Y"
      SA_PASSWORD: "YourStrong@Passw0rd"

  gateway_default:
    image: "mcr.microsoft.com/dotnet/aspnet:6.0"
    depends_on:
      - rabbitmq
      - sqlserver
    volumes:
      - ./ExternalGateway:/app
    working_dir: /app/FSH.ExternalGateway.Host
    command: ["dotnet", "run"]
    environment:
      ASPNETCORE_ENVIRONMENT: "Default"

  gateway_client1:
    image: "mcr.microsoft.com/dotnet/aspnet:6.0"
    depends_on:
      - rabbitmq
      - sqlserver
    volumes:
      - ./ExternalGateway:/app
    working_dir: /app/FSH.ExternalGateway.Host
    command: ["dotnet", "run"]
    environment:
      ASPNETCORE_ENVIRONMENT: "Client1"

  gateway_client2:
    image: "mcr.microsoft.com/dotnet/aspnet:6.0"
    depends_on:
      - rabbitmq
      - sqlserver
    volumes:
      - ./ExternalGateway:/app
    working_dir: /app/FSH.ExternalGateway.Host
    command: ["dotnet", "run"]
    environment:
      ASPNETCORE_ENVIRONMENT: "Client2"
```

Para iniciar todo el stack:

```bash
docker-compose up -d
```

Para detener y eliminar contenedores:

```bash
docker-compose down
```

## ?? Ejemplos de Consultas SQL

### Obtener Factura por ID

```sql
-- Obtener factura por ID con detalles
EXEC GetInvoice @InvoiceId = 'INV-001', @ClientId = 'CUST-001';
```

### Obtener Factura por Cliente

```sql
-- Obtener facturas por cliente
SELECT * FROM Invoices WHERE CustomerId = 'CUST-001';
```

### Unir Tablas

```sql
-- Unir tablas de facturas y líneas de factura
SELECT 
    i.InvoiceId,
    i.InvoiceNumber,
    l.Description,
    l.Quantity,
    l.UnitPrice
FROM Invoices i
INNER JOIN InvoiceLines l ON i.InvoiceId = l.InvoiceId
WHERE i.CustomerId = 'CUST-001';
```

## ?? Consideraciones de Seguridad

1. **Cadenas de conexión**: Nunca hardcodear en código. Usar variables de entorno o servicios de secretos.
2. **Permisos mínimos**: Crear usuarios en BD con los mínimos permisos necesarios.
3. **Timeouts**: Configurar timeouts adecuados para evitar consultas colgadas.

## ?? Mantenimiento y Monitoreo

- **Logs**: Revisar logs de RabbitMQ y SQL Server para detectar problemas.
- **Métricas**: Monitorear métricas de rendimiento de consultas y uso de recursos.
- **Backups**: Realizar backups periódicos de bases de datos.

## ?? Roadmap

- [ ] Cache de resultados con Redis
- [ ] Soporte para stored procedures
- [ ] Queries dinámicas configurables
- [ ] Dashboard de métricas
- [ ] Alertas automáticas
- [ ] Soporte para PostgreSQL/MySQL
- [ ] GraphQL endpoint
