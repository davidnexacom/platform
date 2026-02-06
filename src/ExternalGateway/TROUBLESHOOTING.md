# ?? Troubleshooting - External Gateway con Aspire

## ? Error: PLAIN login refused: user 'admin' - invalid credentials

### Causa
Este error ocurre cuando:
1. RabbitMQ aún no ha terminado de inicializarse completamente
2. Las credenciales no coinciden entre la configuración y el contenedor
3. El Gateway intenta conectarse antes de que RabbitMQ esté listo

### Soluciones

#### Solución 1: Esperar a que RabbitMQ esté completamente iniciado

```bash
# 1. Verificar el estado de RabbitMQ en el dashboard de Aspire
# https://localhost:15888

# 2. Buscar en los logs de RabbitMQ el mensaje:
"Server startup complete"

# 3. Una vez que veas ese mensaje, reiniciar el External Gateway
```

#### Solución 2: Verificar las credenciales

Asegúrate de que el archivo `appsettings.Development.json` del AppHost tenga:

```json
{
  "Parameters": {
    "rabbitmq-username": "admin",
    "rabbitmq-password": "Admin123!"
  }
}
```

#### Solución 3: Reiniciar solo el contenedor problemático

En el dashboard de Aspire:
1. Click en el recurso `rabbitmq`
2. Click en "Stop"
3. Esperar a que se detenga completamente
4. Click en "Start"
5. Esperar al mensaje "Server startup complete" en los logs
6. El Gateway debería reconectarse automáticamente

#### Solución 4: Limpiar volúmenes y reiniciar

Si el problema persiste, limpiar los volúmenes:

```powershell
# Detener todo
docker stop rabbitmq-fsh-playground

# Eliminar el volumen
docker volume rm fsh-rabbitmq-data

# Reiniciar Aspire
# Los contenedores se recrearán automáticamente
```

#### Solución 5: Delay manual al inicio

Si RabbitMQ siempre tarda mucho, edita `GatewayWorker.cs`:

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    _logger.LogInformation("Waiting for RabbitMQ to be fully ready...");
    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
    
    _logger.LogInformation("External Gateway Worker starting...");
    // ... resto del código
}
```

## ?? Verificar el Estado de RabbitMQ

### Desde el Dashboard de Aspire
1. Abrir: https://localhost:15888
2. Buscar el recurso `rabbitmq`
3. Ver el estado: debe estar en "Running"
4. Click en "View Logs" para ver los logs del contenedor

### Desde RabbitMQ Management UI
```
URL: http://localhost:15672
User: admin
Password: Admin123!
```

Verifica:
- **Connections**: Debe haber al menos 1 conexión del Gateway
- **Queues**: Debe existir `fsh.gateway.local-test.queue`
- **Exchanges**: Debe existir el exchange `fsh.gateway.requests`

### Desde Docker CLI
```powershell
# Ver logs de RabbitMQ
docker logs rabbitmq-fsh-playground --tail 50

# Verificar que el contenedor esté corriendo
docker ps | grep rabbitmq

# Ver el estado del servicio RabbitMQ dentro del contenedor
docker exec rabbitmq-fsh-playground rabbitmq-diagnostics status
```

## ?? Otros Errores Comunes

### Error: Connection refused (ECONNREFUSED)

**Causa**: RabbitMQ no está corriendo o no es accesible.

**Solución**:
```powershell
# Verificar que el contenedor esté corriendo
docker ps | grep rabbitmq

# Si no está corriendo, iniciarlo manualmente
docker start rabbitmq-fsh-playground
```

### Error: Cannot connect to SQL Server

**Causa**: SQL Server no está inicializado.

**Solución**:
1. Verificar en Aspire Dashboard que `sqlserver` esté en "Running"
2. Ejecutar el script de inicialización: `ExternalGateway\init-db-v1.sql`

### Error: Queue not found

**Causa**: El Gateway no ha creado la cola automáticamente.

**Solución**:
- Rebus crea las colas automáticamente al suscribirse
- Verificar en RabbitMQ Management UI que la cola exista
- Si no existe, reiniciar el Gateway

## ?? Orden Recomendado de Inicio

Para evitar problemas, sigue este orden:

1. **Iniciar Aspire AppHost**
   ```bash
   cd Playground\FSH.Playground.AppHost
   dotnet run
   ```

2. **Esperar a que RabbitMQ esté listo** (30-60 segundos)
   - Verificar en dashboard: https://localhost:15888
   - Buscar log: "Server startup complete"

3. **Esperar a que SQL Server esté listo** (20-30 segundos)
   - Verificar en dashboard

4. **Inicializar la base de datos** (solo la primera vez)
   - Ejecutar: `ExternalGateway\init-db-v1.sql`

5. **Los servicios deberían estar listos**
   - playground-api: ?
   - playground-blazor: ?
   - external-gateway: ?

## ?? Logs Importantes

### Logs del Gateway que indican éxito:

```
[Information] Configuring RabbitMQ Connection: amqp://admin@localhost:5672
[Information] Queue Name: fsh.gateway.local-test.queue
[Information] Subscribing to GetInvoiceRequest messages...
[Information] Successfully subscribed to messages
[Information] External Gateway Worker starting...
```

### Logs de RabbitMQ que indican que está listo:

```
 completed with 3 plugins.
2026-01-08 09:30:45.123456+00:00 [info] <0.222.0> Server startup complete
2026-01-08 09:30:45.234567+00:00 [info] <0.333.0> Management plugin started
```

## ??? Comandos Útiles

```powershell
# Ver todos los contenedores de FSH
docker ps | grep fsh

# Ver logs en tiempo real del Gateway
docker logs -f external-gateway

# Ver logs en tiempo real de RabbitMQ
docker logs -f rabbitmq-fsh-playground

# Reiniciar solo RabbitMQ
docker restart rabbitmq-fsh-playground

# Limpiar todo y empezar de cero
docker stop $(docker ps -q --filter "name=fsh")
docker volume prune -f
```

## ?? Si Nada Funciona

1. Detener todos los contenedores de Docker
2. Eliminar todos los volúmenes de FSH
3. Cerrar Visual Studio / VS Code
4. Reiniciar Docker Desktop
5. Volver a ejecutar el AppHost

```powershell
# Script de limpieza completa
docker stop $(docker ps -q --filter "name=fsh")
docker rm $(docker ps -aq --filter "name=fsh")
docker volume rm fsh-rabbitmq-data fsh-postgres-data fsh-redis-data fsh-sqlserver-data
```

## ? Verificación Final

Una vez que todo esté corriendo, verifica:

```bash
# 1. Aspire Dashboard
https://localhost:15888
# Todos los recursos deben estar en "Running"

# 2. RabbitMQ Management
http://localhost:15672
# Debe haber 1+ conexiones activas

# 3. Test del endpoint
curl -X GET "https://localhost:7030/api/v1/external/invoices/local-test/INV-001?includeDetails=true"
# Debe retornar HTTP 202 Accepted
```
