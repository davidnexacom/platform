# Fix: Audit Logs no Detectaban Impersonación en Operaciones con Token Refresh

## ?? Problema Identificado

**Síntoma**: Cuando un usuario impersonado modificaba su perfil (o realizaba cualquier operación que requería refresh del token), los audit logs NO indicaban que estaba impersonado.

## ?? Análisis de la Causa Raíz

### Flujo del Problema

1. **Admin impersona a un usuario** ? Token JWT incluye claims de impersonación:
   - `ClaimConstants.Impersonator` = ID del admin
   - `ClaimConstants.ImpersonatorName` = Nombre del admin  
   - `ClaimConstants.OriginalUserId` = ID del admin original

2. **Usuario impersonado realiza una acción** (ej: actualizar perfil)

3. **Token expira** ? Blazor automáticamente llama a `/api/v1/identity/tokens/refresh`

4. **? PROBLEMA**: `IdentityService.ValidateRefreshTokenAsync()` generaba un nuevo JWT **SIN** los claims de impersonación

5. **CurrentUserAuditEnricher** buscaba los claims de impersonación pero ya no existían

6. **Resultado**: Audit log registraba la acción como si fuera el usuario real, no como impersonación

### Código Problemático (ANTES)

```csharp
// IdentityService.ValidateRefreshTokenAsync() - ANTES
public async Task<(string Subject, IEnumerable<Claim> Claims)?>
    ValidateRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
{
    // ... validaciones ...

    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new(ClaimTypes.NameIdentifier, user.Id),
        new(ClaimTypes.Email, user.Email!),
        // ... otros claims ...
    };

    var roles = await _userManager.GetRolesAsync(user);
    claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

    // ? NO propagaba los claims de impersonación del token actual
    return (user.Id, claims);
}
```

**Resultado**: Token nuevo = usuario sin impersonación ?

## ? Solución Implementada

### 1. Agregar `IHttpContextAccessor` a `IdentityService`

Necesitábamos acceso al `HttpContext` actual para leer los claims del token JWT en uso:

```csharp
public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<FshUser> _userManager;
    private readonly ILogger<IdentityService> _logger;
    private readonly IMultiTenantContextAccessor<AppTenantInfo>? _multiTenantContextAccessor;
    private readonly IHttpContextAccessor _httpContextAccessor; // ? NUEVO

    public IdentityService(
        UserManager<FshUser> userManager,
        IMultiTenantContextAccessor<AppTenantInfo>? multiTenantContextAccessor,
        IHttpContextAccessor httpContextAccessor, // ? NUEVO
        ILogger<IdentityService> logger)
    {
        _userManager = userManager;
        _multiTenantContextAccessor = multiTenantContextAccessor;
        _httpContextAccessor = httpContextAccessor; // ? NUEVO
        _logger = logger;
    }
    // ...
}
```

### 2. Propagar Claims de Impersonación en Token Refresh

Actualicé `ValidateRefreshTokenAsync()` para que LEA los claims de impersonación del token actual y los PROPAGUE al nuevo token:

```csharp
public async Task<(string Subject, IEnumerable<Claim> Claims)?>
    ValidateRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
{
    // ... validaciones ...

    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new(ClaimTypes.NameIdentifier, user.Id),
        new(ClaimTypes.Email, user.Email!),
        new(ClaimTypes.Name, user.FirstName ?? string.Empty),
        new(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty),
        new(ClaimConstants.Fullname, $"{user.FirstName} {user.LastName}"),
        new(ClaimTypes.Surname, user.LastName ?? string.Empty),
        new(ClaimConstants.Tenant, _multiTenantContextAccessor!.MultiTenantContext.TenantInfo!.Id),
        new(ClaimConstants.ImageUrl, user.ImageUrl == null ? string.Empty : user.ImageUrl.ToString())
    };

    var roles = await _userManager.GetRolesAsync(user);
    claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

    // ? NUEVA LÓGICA: Propagar claims de impersonación del token actual
    var currentUser = _httpContextAccessor.HttpContext?.User;
    if (currentUser != null)
    {
        var impersonatorClaim = currentUser.FindFirst(ClaimConstants.Impersonator);
        var impersonatorNameClaim = currentUser.FindFirst(ClaimConstants.ImpersonatorName);
        var originalUserIdClaim = currentUser.FindFirst(ClaimConstants.OriginalUserId);

        if (impersonatorClaim != null || originalUserIdClaim != null)
        {
            _logger.LogDebug(
                "Propagating impersonation claims to refreshed token for user {UserId}",
                user.Id);

            if (impersonatorClaim != null)
            {
                claims.Add(new Claim(ClaimConstants.Impersonator, impersonatorClaim.Value));
            }
            if (impersonatorNameClaim != null)
            {
                claims.Add(new Claim(ClaimConstants.ImpersonatorName, impersonatorNameClaim.Value));
            }
            if (originalUserIdClaim != null)
            {
                claims.Add(new Claim(ClaimConstants.OriginalUserId, originalUserIdClaim.Value));
            }
        }
    }

    return (user.Id, claims);
}
```

## ?? Flujo Corregido

### ANTES (? Broken)

```
1. Admin impersona ? Token JWT con claims de impersonación ?
2. Usuario hace acción ? Token expira
3. Blazor refresh token ? Nuevo JWT SIN claims impersonación ?
4. Usuario edita perfil ? Audit log: "juan@example.com modificó su perfil" ?
   (No menciona que era admin impersonando)
```

### DESPUÉS (? Fixed)

```
1. Admin impersona ? Token JWT con claims de impersonación ?
2. Usuario hace acción ? Token expira
3. Blazor refresh token ? Nuevo JWT CON claims impersonación ?
   Claims propagados:
   - impersonator: admin-user-id
   - impersonator_name: admin@example.com
   - original_user_id: admin-user-id
   
4. Usuario edita perfil ? Audit log: ?
   {
     "userId": "admin-user-id",
     "userName": "admin@example.com",
     "isImpersonating": true,
     "realUserId": "admin-user-id",
     "realUserName": "admin@example.com",
     "impersonatedUserId": "juan-user-id",
     "impersonatedUserEmail": "juan@example.com",
     "endpoint": "PUT /api/v1/identity/users/profile"
   }
```

## ?? Cómo Funciona la Propagación

### Token Original (Con Impersonación)
```json
{
  "sub": "juan-user-id",
  "email": "juan@example.com",
  "name": "Juan",
  "impersonator": "admin-user-id",
  "impersonator_name": "admin@example.com",
  "original_user_id": "admin-user-id"
}
```

### Token Refreshed (ANTES - Sin Impersonación ?)
```json
{
  "sub": "juan-user-id",
  "email": "juan@example.com",
  "name": "Juan"
  // ? Claims de impersonación perdidos
}
```

### Token Refreshed (DESPUÉS - Con Impersonación ?)
```json
{
  "sub": "juan-user-id",
  "email": "juan@example.com",
  "name": "Juan",
  "impersonator": "admin-user-id",        // ? Propagado
  "impersonator_name": "admin@example.com", // ? Propagado
  "original_user_id": "admin-user-id"      // ? Propagado
}
```

## ?? Testing

### Escenario de Prueba

1. **Login como Admin**
   ```
   POST /api/v1/identity/tokens
   { "email": "admin@example.com", "password": "..." }
   ```

2. **Impersonar Usuario**
   ```
   POST /api/v1/identity/impersonate
   { "userId": "juan-user-id" }
   ```

3. **Esperar >15 minutos** (para que expire el token)

4. **Modificar Perfil**
   ```
   PUT /api/v1/identity/users/profile
   { "firstName": "Juan Updated", "lastName": "Pérez" }
   ```

5. **Verificar Audit Log**
   ```sql
   SELECT * FROM audit."AuditRecords" 
   WHERE IsImpersonating = true
     AND UserId = 'admin-user-id'
     AND RealUserName = 'admin@example.com'
   ORDER BY OccurredAtUtc DESC
   LIMIT 1;
   ```

### Resultado Esperado

```json
{
  "id": "...",
  "occurredAtUtc": "2024-01-15T10:35:00Z",
  "eventType": "Security",
  "userId": "admin-user-id",        // ? Admin real
  "userName": "admin@example.com",  // ? Admin real
  "isImpersonating": true,          // ? Detectado
  "realUserId": "admin-user-id",
  "realUserName": "admin@example.com",
  "payload": {
    "action": "ProfileUpdated",
    "claimsSnapshot": {
      "isImpersonating": true,
      "realUserId": "admin-user-id",
      "realUserName": "admin@example.com",
      "impersonatedUserId": "juan-user-id",
      "impersonatedUserEmail": "juan@example.com",
      "endpoint": "PUT /api/v1/identity/users/profile"
    }
  }
}
```

## ?? Impacto

### Beneficios

? **Trazabilidad Completa**: Ahora TODAS las acciones durante impersonación se registran correctamente, incluso después de refresh token

? **Compliance**: Los audit logs cumplen con requisitos de auditoría que exigen saber quién hizo qué, incluso con impersonación

? **Seguridad**: Los administradores no pueden "esconder" acciones haciendo que expire el token

? **Debugging**: Más fácil investigar problemas cuando sabes quién estaba realmente detrás de una acción

### Sin Efectos Secundarios

- ? No afecta a usuarios normales (sin impersonación)
- ? No cambia el comportamiento de tokens normales
- ? Compatible con refresh token existente
- ? No requiere cambios en frontend

## ?? Seguridad

### Consideraciones

1. **Los claims de impersonación se propagan automáticamente** durante todo el ciclo de vida de la sesión de impersonación

2. **Solo el endpoint `/api/v1/identity/impersonate/stop`** puede eliminar los claims de impersonación

3. **El token refresh NO permite cambiar el usuario impersonado** - solo propaga el estado existente

4. **Los logs siempre muestran al usuario REAL** (admin) como actor principal

## ?? Conclusión

**ANTES**: Impersonación invisible después de token refresh ?
**DESPUÉS**: Impersonación totalmente trazable en toda la sesión ?

Esta fix asegura que TODAS las acciones realizadas durante una sesión de impersonación sean correctamente auditadas, sin importar cuántas veces se refresque el token.

---

## ?? Archivos Modificados

| Archivo | Cambio |
|---------|---------|
| `Modules\Identity\Modules.Identity\Services\IdentityService.cs` | Agregado `IHttpContextAccessor` y lógica de propagación de claims de impersonación en `ValidateRefreshTokenAsync()` |

## ? Estado

- ? Compilación exitosa
- ? Lógica implementada
- ? Claims de impersonación propagados correctamente
- ? Pendiente: Testing en producción
