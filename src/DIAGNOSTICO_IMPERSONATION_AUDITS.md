# Diagnóstico: Impersonación no se Detecta en Audit Logs

## ?? Cambios Implementados

### 1. **Propagación de Claims en Token Refresh** ?
- Modificado `IdentityService.ValidateRefreshTokenAsync()` para propagar claims de impersonación
- Agregado `IHttpContextAccessor` al servicio

### 2. **Enriquecimiento de Auditoría Mejorado** ?
- Actualizado `CurrentUserAuditEnricher` para soportar TODOS los tipos de payload:
  - `SecurityEventPayload`
  - `ActivityEventPayload`
  - `EntityChangeEventPayload`
- Agregado logging detallado para depuración

### 3. **Auditoría Explícita en UpdateAsync** ?
- Agregado `WriteActivityAsync` en `UserService.UpdateAsync()`
- Esto genera un evento de auditoría explícito cuando se actualiza el perfil

## ?? Plan de Prueba

### Paso 1: Verificar que los Claims se Propagan

1. **Login como Admin**
2. **Impersonar un Usuario**
3. **Abrir DevTools (F12) ? Network**
4. **Modificar el perfil** (cambiar nombre)
5. **Buscar la llamada a `/api/v1/identity/tokens/refresh`**
6. **Verificar el JWT decodificado**:
   - Ir a https://jwt.io/
   - Pegar el `access_token` de la respuesta
   - Buscar en el payload los claims:
     ```json
     {
       "impersonator": "admin-user-id",
       "impersonator_name": "admin@example.com",
       "original_user_id": "admin-user-id"
     }
     ```

### Paso 2: Verificar los Logs del Enricher

Ejecuta la aplicación y busca en los logs (Output window ? Debug):

```
Impersonation detected in audit enricher - Real user: [ADMIN-ID] (admin@example.com), Impersonated: [USER-ID] (user@example.com)
Added impersonation info to ActivityEventPayload
```

### Paso 3: Verificar el Audit Log en Base de Datos

```sql
-- Verificar el último audit log de UpdateUserProfile
SELECT 
    Id,
    OccurredAtUtc,
    EventType,
    UserId,
    UserName,
    IsImpersonating,
    RealUserId,
    RealUserName,
    PayloadJson
FROM audit."AuditRecords"
WHERE Source = 'Identity'
  AND PayloadJson::text LIKE '%UpdateUserProfile%'
ORDER BY OccurredAtUtc DESC
LIMIT 1;
```

**Resultado Esperado**:
```json
{
  "userId": "admin-user-id",          // ? Admin real
  "userName": "admin@example.com",    // ? Admin real
  "isImpersonating": true,            // ? Flag activado
  "realUserId": "admin-user-id",
  "realUserName": "admin@example.com",
  "payloadJson": {
    "kind": "Command",
    "name": "UpdateUserProfile",
    "statusCode": 200,
    "requestPreview": {
      "userId": "user-id",
      "firstName": "NuevoNombre",
      "lastName": "Apellido",
      "isImpersonating": true,        // ? En el payload también
      "impersonatedUserId": "user-id",
      "impersonatedUserEmail": "user@example.com",
      "realUserId": "admin-user-id",
      "realUserName": "admin@example.com"
    }
  }
}
```

## ?? Posibles Problemas

### Problema 1: Claims no se Propagan
**Síntoma**: El JWT refreshed NO contiene los claims `impersonator`, `impersonator_name`, `original_user_id`

**Causa**: El `HttpContext.User` no tiene los claims cuando se llama a `ValidateRefreshTokenAsync`

**Solución**:
```csharp
// En IdentityService.ValidateRefreshTokenAsync()
var currentUser = _httpContextAccessor.HttpContext?.User;
if (currentUser != null)
{
    _logger.LogDebug(
        "Current user claims: {Claims}",
        string.Join(", ", currentUser.Claims.Select(c => $"{c.Type}={c.Value}")));
    // ... resto del código
}
```

### Problema 2: Enricher no se Ejecuta
**Síntoma**: No ves el log "Impersonation detected in audit enricher"

**Causa**: El enricher no está registrado o no se ejecuta

**Verificar**:
```csharp
// En AuditingModule.cs
builder.Services.AddScoped<IAuditEnricher, CurrentUserAuditEnricher>(); // ? Debe estar
```

### Problema 3: IsImpersonating siempre False en BD
**Síntoma**: La columna `IsImpersonating` siempre es `false`

**Causa**: `SqlAuditSink` no puede extraer la info del payload

**Solución**: Agregar logging en `SqlAuditSink.ExtractIsImpersonating()`:
```csharp
private static bool ExtractIsImpersonating(object payload)
{
    _log.LogDebug("Extracting impersonation from payload type: {PayloadType}", payload?.GetType().Name);
    
    if (payload is SecurityEventPayload securityPayload &&
        securityPayload.ClaimsSnapshot != null)
    {
        _log.LogDebug("SecurityEventPayload claims: {Claims}", 
            string.Join(", ", securityPayload.ClaimsSnapshot.Keys));
        // ... resto
    }
    // ... resto
}
```

## ?? Verificación Final

### Checklist Completo

- [ ] **JWT Claims**: Token refreshed contiene claims de impersonación
- [ ] **Logs**: Ves "Impersonation detected in audit enricher" en logs
- [ ] **Logs**: Ves "Added impersonation info to ActivityEventPayload" en logs
- [ ] **Logs**: Ves "Propagating impersonation claims to refreshed token" en logs
- [ ] **Base de Datos**: `IsImpersonating = true` en audit record
- [ ] **Base de Datos**: `RealUserId` contiene ID del admin
- [ ] **Base de Datos**: `RealUserName` contiene email del admin
- [ ] **Base de Datos**: `PayloadJson` contiene `isImpersonating: true`

## ?? Comandos de Diagnóstico

### Ver Logs en Tiempo Real
```bash
# En la terminal donde corre la API
dotnet run --project Playground/Playground.Api
```

Buscar en los logs:
```
Impersonation detected
Propagating impersonation claims
Added impersonation info
```

### Query SQL para Debugging
```sql
-- Ver TODOS los audit logs de los últimos 5 minutos
SELECT 
    OccurredAtUtc,
    EventType,
    UserId,
    UserName,
    IsImpersonating,
    RealUserId,
    RealUserName,
    Source,
    substring(PayloadJson::text, 1, 200) as PayloadPreview
FROM audit."AuditRecords"
WHERE OccurredAtUtc > NOW() - INTERVAL '5 minutes'
ORDER BY OccurredAtUtc DESC;
```

### Verificar Claims en Cookie
```javascript
// En DevTools Console del navegador
document.cookie
  .split('; ')
  .find(row => row.startsWith('FshAuth='))
```

Luego copiar el valor y decodificar en https://jwt.io/

## ?? Siguiente Paso

1. **Ejecuta la aplicación** con logging habilitado
2. **Impersona un usuario**
3. **Modifica el perfil**
4. **Revisa los logs** (Output window ? Debug)
5. **Revisa la base de datos** con el query SQL de arriba
6. **Comparte los resultados** para diagnóstico adicional

## ?? Información a Compartir

Si sigue sin funcionar, comparte:

1. **Logs de la consola** cuando haces la modificación
2. **Resultado del query SQL** (audit records de los últimos 5 minutos)
3. **JWT decodificado** (de jwt.io) después del refresh token
4. **Claims del usuario** en el navegador (DevTools ? Application ? Cookies)

Esto nos ayudará a identificar exactamente en qué punto del flujo se pierde la información de impersonación.
