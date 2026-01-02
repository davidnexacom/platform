# User-Agent y IP Forwarding desde Blazor al API

## Problema

Cuando una petición de login viene desde **Blazor Server**, el API de Identity no puede obtener directamente el User-Agent y la IP del navegador del cliente porque:

1. La petición pasa primero por el servidor Blazor (BFF - Backend For Frontend)
2. El servidor Blazor hace una llamada HTTP al API de Identity
3. El API solo ve el User-Agent y la IP del servidor Blazor, no del cliente final

## Solución Implementada

### 1. ForwardedHeadersHandler

Se creó un `DelegatingHandler` en `Playground.Blazor/Services/Api/ForwardedHeadersHandler.cs` que:

- Intercepta todas las peticiones salientes del cliente HTTP
- Lee el User-Agent y la IP del `HttpContext` actual (la petición del navegador)
- Agrega estos valores como headers personalizados:
  - `X-Forwarded-User-Agent`: User-Agent del navegador
  - `X-Forwarded-For`: IP del cliente

### 2. Registro del Handler

En `ApiClientRegistration.cs`, el handler se registra específicamente para el `TokenClient`:

```csharp
services.AddHttpClient("TokenClient", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
.AddHttpMessageHandler<ForwardedHeadersHandler>();
```

### 3. Lectura en el API

En `GenerateTokenCommandHandler.cs`, se modificó para leer los headers personalizados primero:

```csharp
// Check for forwarded headers first (from Blazor BFF)
var ip = http?.Request.Headers["X-Forwarded-For"].ToString();
if (string.IsNullOrWhiteSpace(ip))
{
    ip = http?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

var ua = http?.Request.Headers["X-Forwarded-User-Agent"].ToString();
if (string.IsNullOrWhiteSpace(ua))
{
    ua = http?.Request.Headers.UserAgent.ToString() ?? "unknown";
}
```

## Flujo Completo

```
????????????        ???????????????        ????????????
? Browser  ????????>? Blazor BFF  ????????>? API      ?
?          ? POST   ?             ? POST   ?          ?
? UA: XXX  ? /login ? + Handler   ? /token ? Reads:   ?
? IP: YYY  ?        ? Adds:       ?        ? X-Fwd-UA ?
????????????        ? X-Fwd-UA    ?        ? X-Fwd-For?
                    ? X-Fwd-For   ?        ????????????
                    ???????????????
```

## Ventajas

1. ? El API registra correctamente el User-Agent y la IP del cliente final
2. ? Los datos de sesión (`UserSession`) contienen información precisa del dispositivo
3. ? Las auditorías reflejan el origen real de las peticiones
4. ? Compatible con peticiones directas al API (no desde Blazor) - fallback a headers estándar
5. ? No requiere cambios en el contrato de la API

## Notas de Seguridad

- Los headers `X-Forwarded-*` se añaden solo en el servidor Blazor (controlado)
- No son enviados directamente desde el navegador del usuario
- Si el API estuviera detrás de un proxy/load balancer, se debe configurar para confiar en estos headers

## Testing

Para verificar que funciona:

1. Inicia sesión desde Blazor
2. Verifica en la tabla `UserSessions` que:
   - `UserAgent` contiene el User-Agent del navegador (no del servidor)
   - `IpAddress` contiene la IP del cliente (no del servidor)
   - `DeviceType`, `Browser`, `OperatingSystem` se detectan correctamente

```sql
SELECT Id, UserAgent, IpAddress, DeviceType, Browser, OperatingSystem, CreatedAt
FROM Identity.UserSessions
ORDER BY CreatedAt DESC;
```
