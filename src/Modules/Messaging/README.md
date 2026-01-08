# Módulo de Messaging - FSH Platform

## Descripción

El módulo de Messaging proporciona una abstracción sobre RabbitMQ usando Rebus para gestionar comunicación entre procesos mediante patrones de mensajería modernos y confiables.

## Estructura del Módulo

```
Modules/Messaging/
??? Modules.Messaging/                      # Implementación del módulo
?   ??? Configuration/
?   ?   ??? RabbitMqOptions.cs             # Opciones de configuración
?   ??? Services/
?   ?   ??? RebusMessageBus.cs             # Implementación de IMessageBus
?   ??? HealthChecks/
?   ?   ??? RabbitMqHealthCheck.cs         # Health check para RabbitMQ
?   ??? Features/v1/Examples/
?   ?   ??? PingRequestHandler.cs          # Ejemplo de handler
?   ??? MessagingModule.cs                  # Punto de entrada del módulo
?   ??? MessagingModuleConstants.cs         # Constantes del módulo
?
??? Modules.Messaging.Contracts/            # Contratos públicos
    ??? Services/
    ?   ??? IMessageBus.cs                  # Interfaz pública del bus de mensajes
    ??? v1/
        ??? IMessage.cs                     # Interfaces base para mensajes
        ??? MessageBase.cs                  # Clases base para mensajes
        ??? Examples/
            ??? PingMessages.cs             # Ejemplos de mensajes
```

## Características Principales

### ? Patrones de Mensajería

1. **Request/Response**: Comunicación síncrona con respuesta
2. **Publish/Subscribe**: Broadcasting de eventos a múltiples suscriptores
3. **Fire-and-Forget**: Envío asíncrono sin esperar respuesta

### ? Características Técnicas

- **Retry Automático**: Política de reintentos configurable
- **Dead Letter Queue**: Manejo de mensajes fallidos
- **Serialización**: JSON por defecto
- **Correlation ID**: Tracking de mensajes relacionados
- **Health Checks**: Monitoreo de conectividad
- **Logging**: Trazabilidad completa
- **Type-Safe**: Fuertemente tipado

## Instalación

### 1. Paquetes NuGet

Los paquetes ya están configurados en `Directory.Packages.props`:

```xml
<ItemGroup Label="Messaging">
  <PackageVersion Include="Rebus" Version="8.7.2" />
  <PackageVersion Include="Rebus.RabbitMq" Version="10.0.1" />
  <PackageVersion Include="Rebus.ServiceProvider" Version="9.0.2" />
</ItemGroup>
```

### 2. Configuración

Añadir al `appsettings.json`:

```json
{
  "RabbitMqOptions": {
    "Enabled": true,
    "ConnectionString": "amqp://guest:guest@localhost:5672",
    "QueueName": "fsh.messaging.queue",
    "MaxConcurrentMessages": 10,
    "RetryAttempts": 3,
    "RequestTimeoutSeconds": 30
  }
}
```

### 3. Registro del Módulo

En `Program.cs`:

```csharp
using FSH.Modules.Messaging;

var moduleAssemblies = new Assembly[]
{
    typeof(IdentityModule).Assembly,
    typeof(MessagingModule).Assembly  // Añadir
};
```

## Uso

### Ejemplo Completo: Crear Usuario

#### 1. Definir Mensajes (Contracts)

```csharp
// Modules.Messaging.Contracts/v1/CreateUserMessages.cs
using FSH.Modules.Messaging.Contracts.v1;

public sealed record CreateUserRequest : RequestBase
{
    public string Email { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public sealed record CreateUserResponse : ResponseBase
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
}
```

#### 2. Crear Handler

```csharp
// Modules.Messaging/Features/v1/Users/CreateUserHandler.cs
using Rebus.Handlers;
using Rebus.Bus;

public class CreateUserRequestHandler : IHandleMessages<CreateUserRequest>
{
    private readonly IUserService _userService;
    private readonly IBus _bus;
    private readonly ILogger<CreateUserRequestHandler> _logger;

    public CreateUserRequestHandler(
        IUserService userService,
        IBus bus,
        ILogger<CreateUserRequestHandler> logger)
    {
        _userService = userService;
        _bus = bus;
        _logger = logger;
    }

    public async Task Handle(CreateUserRequest message)
    {
        _logger.LogInformation(
            "Processing CreateUser request for email: {Email}",
            message.Email);

        try
        {
            var userId = await _userService.CreateUserAsync(
                message.Email,
                message.Name,
                message.Password);

            var response = new CreateUserResponse
            {
                Success = true,
                UserId = userId,
                Email = message.Email,
                CorrelationId = message.CorrelationId
            };

            await _bus.Reply(response);

            _logger.LogInformation(
                "Successfully created user {UserId} for email {Email}",
                userId,
                message.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creating user for email {Email}",
                message.Email);

            var errorResponse = ResponseBase.CreateFailure<CreateUserResponse>(
                $"Failed to create user: {ex.Message}");
            
            errorResponse = errorResponse with
            {
                CorrelationId = message.CorrelationId
            };

            await _bus.Reply(errorResponse);
        }
    }
}
```

#### 3. Enviar Request desde API

```csharp
// Endpoint que envía el request
[ApiController]
[Route("api/v1/users")]
public class UsersController : ControllerBase
{
    private readonly IMessageBus _messageBus;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IMessageBus messageBus,
        ILogger<UsersController> logger)
    {
        _messageBus = messageBus;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<CreateUserResult>> CreateUser(
        [FromBody] CreateUserDto dto)
    {
        var request = new CreateUserRequest
        {
            Email = dto.Email,
            Name = dto.Name,
            Password = dto.Password,
            CorrelationId = HttpContext.TraceIdentifier
        };

        _logger.LogInformation(
            "Sending CreateUser request for email: {Email}",
            dto.Email);

        var response = await _messageBus.SendRequestAsync<
            CreateUserRequest,
            CreateUserResponse>(
                request,
                timeout: TimeSpan.FromSeconds(30));

        if (!response.Success)
        {
            _logger.LogWarning(
                "CreateUser request failed: {Error}",
                response.ErrorMessage);
            return BadRequest(new { error = response.ErrorMessage });
        }

        _logger.LogInformation(
            "CreateUser request successful, UserId: {UserId}",
            response.UserId);

        return Ok(new CreateUserResult
        {
            UserId = response.UserId,
            Email = response.Email
        });
    }
}
```

## Patrones Avanzados

### Publish/Subscribe

```csharp
// Publisher
public class OrderService
{
    private readonly IMessageBus _messageBus;

    public async Task CompleteOrder(Guid orderId)
    {
        // Procesar orden...

        var orderCompletedEvent = new OrderCompletedEvent
        {
            OrderId = orderId,
            CompletedAt = DateTimeOffset.UtcNow,
            TotalAmount = 100.00m
        };

        await _messageBus.PublishAsync(orderCompletedEvent);
    }
}

// Subscriber 1: Email Service
public class OrderCompletedEmailHandler : IHandleMessages<OrderCompletedEvent>
{
    private readonly IEmailService _emailService;

    public async Task Handle(OrderCompletedEvent message)
    {
        await _emailService.SendOrderCompletionEmail(message.OrderId);
    }
}

// Subscriber 2: Inventory Service
public class OrderCompletedInventoryHandler : IHandleMessages<OrderCompletedEvent>
{
    private readonly IInventoryService _inventoryService;

    public async Task Handle(OrderCompletedEvent message)
    {
        await _inventoryService.UpdateStock(message.OrderId);
    }
}
```

### Fire-and-Forget

```csharp
public class NotificationService
{
    private readonly IMessageBus _messageBus;

    public async Task SendNotification(string userId, string message)
    {
        var notification = new SendNotificationCommand
        {
            UserId = userId,
            Message = message,
            Timestamp = DateTimeOffset.UtcNow
        };

        // No espera respuesta
        await _messageBus.SendAsync(notification);
    }
}
```

## Testing

### Unit Test de Handler

```csharp
public class CreateUserHandlerTests
{
    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var userService = Substitute.For<IUserService>();
        var bus = Substitute.For<IBus>();
        var logger = Substitute.For<ILogger<CreateUserRequestHandler>>();
        
        var expectedUserId = Guid.NewGuid();
        userService.CreateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(expectedUserId);

        var handler = new CreateUserRequestHandler(userService, bus, logger);
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            Name = "Test User",
            Password = "Password123"
        };

        // Act
        await handler.Handle(request);

        // Assert
        await bus.Received(1).Reply(Arg.Is<CreateUserResponse>(r => 
            r.Success && r.UserId == expectedUserId));
    }
}
```

### Integration Test

```csharp
public class MessagingIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MessagingIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateUser_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateUserDto
        {
            Email = "integration@test.com",
            Name = "Integration Test",
            Password = "Test123!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/users", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateUserResult>();
        Assert.NotEqual(Guid.Empty, result.UserId);
    }
}
```

## Monitoreo y Troubleshooting

### Health Check

```bash
GET /api/v1/messaging/health
```

Response:
```json
{
  "status": "Healthy",
  "module": "Messaging",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### Logs

El módulo genera logs detallados:

```
[Information] Sending request of type CreateUserRequest, expecting response of type CreateUserResponse
[Information] Successfully received response of type CreateUserResponse for request CreateUserRequest
[Error] Timeout waiting for response of type CreateUserResponse for request CreateUserRequest
```

### RabbitMQ Management UI

Acceder a `http://localhost:15672` (guest/guest) para:
- Ver colas y mensajes
- Monitorear throughput
- Analizar dead letter queues
- Ver conexiones activas

## Best Practices

1. **Siempre usar CorrelationId** para tracking
2. **Manejar timeouts** apropiadamente
3. **Loggear requests y responses**
4. **Usar tipos específicos** para requests/responses
5. **Implementar idempotencia** en handlers
6. **Validar mensajes** antes de procesarlos
7. **Usar dead letter queues** para mensajes fallidos
8. **Monitorear métricas** de RabbitMQ

## Troubleshooting

| Problema | Solución |
|----------|----------|
| Timeout en requests | Aumentar `RequestTimeoutSeconds` |
| Handler no se ejecuta | Verificar que implemente `IHandleMessages<T>` |
| Mensajes perdidos | Revisar dead letter queue |
| Alta latencia | Aumentar `MaxConcurrentMessages` |
| Conexión fallida | Verificar `ConnectionString` y RabbitMQ |

## Referencias

- [Rebus Documentation](https://github.com/rebus-org/Rebus)
- [RabbitMQ Tutorials](https://www.rabbitmq.com/tutorials.html)
- [Messaging Patterns](https://www.enterpriseintegrationpatterns.com/)
