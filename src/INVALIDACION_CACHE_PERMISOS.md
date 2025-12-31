# Invalidación Automática de Cache de Permisos

## ?? Problema Identificado

> "con el paso del tiempo ha empezado a funcionar bien sin hacer nada. da la impresión que es problema de cache"

**Diagnóstico**: Correcto. El problema era que el cache de permisos en Redis no se invalidaba cuando se modificaban roles, permisos o asignaciones de usuarios.

## ? Solución Implementada

He implementado **invalidación automática de cache** en todos los puntos críticos donde cambian permisos.

### Componentes Creados

#### 1. `IPermissionCacheInvalidator` - Servicio Centralizado

```csharp
public interface IPermissionCacheInvalidator
{
    // Invalida cache de un usuario específico
    Task InvalidateUserPermissionsAsync(string userId, CancellationToken cancellationToken = default);
    
    // Invalida cache de TODOS los usuarios con un rol específico
    Task InvalidateRolePermissionsAsync(string roleId, CancellationToken cancellationToken = default);
    
    // Invalida cache de TODOS los usuarios (usar con cuidado)
    Task InvalidateAllPermissionsAsync(CancellationToken cancellationToken = default);
}
```

**Implementación**:
```csharp
public class PermissionCacheInvalidator : IPermissionCacheInvalidator
{
    private readonly ICacheService _cache;
    private readonly IdentityDbContext _db;
    
    public async Task InvalidateUserPermissionsAsync(string userId, ...)
    {
        var cacheKey = $"perm:{userId}";
        await _cache.RemoveItemAsync(cacheKey, cancellationToken);
    }
    
    public async Task InvalidateRolePermissionsAsync(string roleId, ...)
    {
        // Obtiene TODOS los usuarios con este rol
        var usersInRole = await _db.UserRoles
            .Where(ur => ur.RoleId == roleId)
            .Select(ur => ur.UserId)
            .ToListAsync(cancellationToken);
        
        // Invalida cache de cada usuario
        foreach (var userId in usersInRole)
        {
            await InvalidateUserPermissionsAsync(userId, cancellationToken);
        }
    }
}
```

## ?? Puntos de Invalidación Implementados

### 1. Al Actualizar Permisos de un Rol

**Archivo**: `RoleService.cs`  
**Método**: `UpdatePermissionsAsync`

```csharp
public async Task<string> UpdatePermissionsAsync(string roleId, List<string> permissions)
{
    // ... código existente para actualizar permisos ...
    
    // ? Invalida cache de TODOS los usuarios con este rol
    await cacheInvalidator.InvalidateRolePermissionsAsync(roleId);
    
    return "permissions updated";
}
```

**Trigger**: Cuando se modifican los permisos de un rol en `/api/v1/identity/roles/{id}/permissions`

**Efecto**: 
```
Usuario modifica permisos del rol "Manager"
  ?
Se actualizan los RoleClaims en BD
  ?
Se invalida cache de TODOS los usuarios con rol "Manager"
  ?
Próxima vez que un "Manager" acceda:
  - Su cache está vacío
  - Se consulta BD (con nuevos permisos)
  - Se actualiza cache
```

### 2. Al Eliminar un Rol

**Archivo**: `RoleService.cs`  
**Método**: `DeleteAsync`

```csharp
public async Task DeleteAsync(string id)
{
    FshRole? role = await roleManager.FindByIdAsync(id);
    _ = role ?? throw new NotFoundException("role not found");

    // ? Invalida cache ANTES de eliminar el rol
    await cacheInvalidator.InvalidateRolePermissionsAsync(id);

    await roleManager.DeleteAsync(role);
}
```

**Trigger**: Cuando se elimina un rol en `/api/v1/identity/roles/{id}`

**Efecto**: Cache de usuarios con ese rol se limpia antes de eliminarlo

### 3. Al Asignar/Remover Roles de un Usuario

**Archivo**: `UserService.cs`  
**Método**: `AssignRolesAsync`

```csharp
public async Task<string> AssignRolesAsync(string userId, List<UserRoleDto> userRoles, ...)
{
    // ... código para agregar/remover roles ...
    
    // ? Invalida cache del usuario específico
    await InvalidatePermissionCacheAsync(userId, cancellationToken);
    
    return "User Roles Updated Successfully.";
}
```

**Trigger**: Cuando se cambian los roles de un usuario en `/api/v1/identity/users/{id}/roles`

**Efecto**:
```
Admin asigna rol "Editor" al usuario "juan@example.com"
  ?
Se actualiza AspNetUserRoles en BD
  ?
Se invalida cache del usuario "juan"
  ?
Próxima vez que Juan acceda:
  - Su cache está vacío
  - Se consulta BD (con nuevo rol "Editor")
  - Se obtienen permisos del rol "Editor"
  - Se actualiza cache
```

### 4. Al Desactivar/Eliminar un Usuario

**Archivo**: `UserService.cs`  
**Método**: `DeleteAsync`

```csharp
public async Task DeleteAsync(string userId)
{
    // ... código para desactivar usuario ...
    
    // ? Invalida cache del usuario
    await InvalidatePermissionCacheAsync(userId, CancellationToken.None);
}
```

**Trigger**: Cuando se elimina un usuario en `/api/v1/identity/users/{id}`

**Efecto**: Cache del usuario eliminado se limpia

## ?? Flujo Completo: Ejemplo

### Escenario: Admin Actualiza Permisos del Rol "Editor"

```
???????????????????????????????????????????????????????????????
? 1. Estado Inicial                                           ?
???????????????????????????????????????????????????????????????
? BD: Rol "Editor" tiene permisos:                            ?
?   - Permissions.Articles.View                               ?
?   - Permissions.Articles.Create                             ?
?                                                              ?
? Redis Cache:                                                ?
?   perm:user123 ? ["Permissions.Articles.View", ...]        ?
?   perm:user456 ? ["Permissions.Articles.View", ...]        ?
?   (ambos usuarios tienen rol "Editor")                      ?
???????????????????????????????????????????????????????????????

???????????????????????????????????????????????????????????????
? 2. Admin Modifica Permisos                                  ?
???????????????????????????????????????????????????????????????
? PUT /api/v1/identity/roles/editor-id/permissions            ?
? Body: ["Permissions.Articles.View",                         ?
?        "Permissions.Articles.Create",                        ?
?        "Permissions.Articles.Update"]  ? NUEVO              ?
???????????????????????????????????????????????????????????????

???????????????????????????????????????????????????????????????
? 3. Backend Procesa                                          ?
???????????????????????????????????????????????????????????????
? RoleService.UpdatePermissionsAsync()                        ?
?   ?                                                          ?
? 3.1. Actualiza RoleClaims en BD                             ?
?      INSERT INTO AspNetRoleClaims                           ?
?      VALUES ('editor-id', 'permission',                     ?
?              'Permissions.Articles.Update')                 ?
?   ?                                                          ?
? 3.2. Llama a cacheInvalidator.InvalidateRolePermissions()  ?
?   ?                                                          ?
? 3.3. Query: Encuentra usuarios con rol "Editor"            ?
?      SELECT UserId FROM AspNetUserRoles                     ?
?      WHERE RoleId = 'editor-id'                             ?
?      ? Retorna: [user123, user456]                          ?
?   ?                                                          ?
? 3.4. Invalida cache de cada usuario                         ?
?      DEL perm:user123                                       ?
?      DEL perm:user456                                       ?
???????????????????????????????????????????????????????????????

???????????????????????????????????????????????????????????????
? 4. Usuario Accede Después del Cambio                        ?
???????????????????????????????????????????????????????????????
? user123 navega a página protegida                           ?
?   ?                                                          ?
? AuthorizeView verifica permiso                              ?
?   ?                                                          ?
? PermissionService.GetPermissionsAsync()                     ?
?   ?                                                          ?
? 4.1. Busca en cache: GET perm:user123                       ?
?      ? NULL (fue invalidado en paso 3.4)                    ?
?   ?                                                          ?
? 4.2. Consulta API: GET /api/v1/identity/permissions         ?
?   ?                                                          ?
? 4.3. UserService.GetPermissionsAsync()                      ?
?      ? Query BD con NUEVOS permisos                         ?
?      ? Retorna: ["Permissions.Articles.View",               ?
?                  "Permissions.Articles.Create",             ?
?                  "Permissions.Articles.Update"]  ? NUEVO   ?
?   ?                                                          ?
? 4.4. Guarda en cache:                                       ?
?      SET perm:user123 = [permisos...]                       ?
?   ?                                                          ?
? 4.5. Usuario VE el nuevo permiso inmediatamente            ?
???????????????????????????????????????????????????????????????
```

## ?? Comparación: Antes vs Después

### Antes (? Sin Invalidación)

```
Admin modifica permisos del rol "Editor"
  ?
BD se actualiza ?
  ?
Cache NO se invalida ?
  ?
Usuario "juan" (Editor) hace request
  ?
PermissionService consulta cache
  ?
Cache retorna permisos ANTIGUOS ?
  ?
Juan NO ve el nuevo permiso hasta que:
  - Cache expira (¿cuándo?)
  - Hace logout/login
  - Se reinicia Redis
  - Espera tiempo aleatorio...
```

**Problema**: Inconsistencia indefinida entre BD y cache

### Después (? Con Invalidación)

```
Admin modifica permisos del rol "Editor"
  ?
BD se actualiza ?
  ?
Cache se invalida automáticamente ?
  ?
Usuario "juan" (Editor) hace request
  ?
PermissionService consulta cache
  ?
Cache está vacío (fue invalidado)
  ?
Consulta API ? BD con permisos NUEVOS ?
  ?
Actualiza cache con permisos nuevos
  ?
Juan VE el nuevo permiso INMEDIATAMENTE ?
```

**Ventaja**: Consistencia inmediata después del cambio

## ?? Casos de Uso Cubiertos

### ? Caso 1: Modificar Permisos de un Rol

```
PUT /api/v1/identity/roles/{id}/permissions
```
**Invalida**: Todos los usuarios con ese rol

### ? Caso 2: Eliminar un Rol

```
DELETE /api/v1/identity/roles/{id}
```
**Invalida**: Todos los usuarios con ese rol (antes de eliminarlo)

### ? Caso 3: Asignar Rol a Usuario

```
POST /api/v1/identity/users/{id}/roles
Body: { userRoles: [{ roleName: "Manager", enabled: true }] }
```
**Invalida**: Solo ese usuario específico

### ? Caso 4: Remover Rol de Usuario

```
POST /api/v1/identity/users/{id}/roles
Body: { userRoles: [{ roleName: "Manager", enabled: false }] }
```
**Invalida**: Solo ese usuario específico

### ? Caso 5: Desactivar/Eliminar Usuario

```
DELETE /api/v1/identity/users/{id}
```
**Invalida**: Solo ese usuario específico

## ?? Performance

### Invalidación por Usuario (Rápida)

```
1 usuario ? 1 DELETE en Redis
Tiempo: ~1-5ms
```

### Invalidación por Rol (Moderada)

```
Rol con 10 usuarios ? 10 DELETEs en Redis
Tiempo: ~10-50ms
```

### Invalidación por Rol (Intensiva)

```
Rol con 1000 usuarios ? 1000 DELETEs en Redis
Tiempo: ~1-5 segundos
```

**Optimización Futura** (si necesario):
```csharp
// En lugar de DEL uno por uno:
foreach (var userId in usersInRole)
{
    await _cache.RemoveItemAsync($"perm:{userId}");
}

// Usar batch delete (si soportado por implementación):
var keys = usersInRole.Select(u => $"perm:{u}").ToArray();
await _cache.RemoveManyAsync(keys);
```

## ?? Seguridad y Auditoría

### Logging Implementado

```csharp
_logger.LogInformation("Invalidating permission cache for user: {UserId}", userId);
_logger.LogInformation("Permission cache invalidated for user: {UserId}", userId);

_logger.LogInformation("Found {Count} users with role {RoleId}, invalidating their caches", 
    usersInRole.Count, roleId);
_logger.LogInformation("Permission cache invalidated for {Count} users", usersInRole.Count);
```

**Ejemplo de Logs**:
```
[2024-01-15 10:30:00] [Information] Invalidating permission cache for all users with role: editor-role-id
[2024-01-15 10:30:00] [Information] Found 15 users with role editor-role-id, invalidating their caches
[2024-01-15 10:30:00] [Information] Invalidating permission cache for user: user123
[2024-01-15 10:30:00] [Information] Permission cache invalidated for user: user123
... (15 veces)
[2024-01-15 10:30:01] [Information] Permission cache invalidated for 15 users
```

## ?? Mejores Prácticas

### ? DO: Invalidar Después de Cambios Exitosos

```csharp
// ? CORRECTO
await userManager.AddToRoleAsync(user, roleName);
await InvalidatePermissionCacheAsync(userId);  // Después del cambio

// ? INCORRECTO
await InvalidatePermissionCacheAsync(userId);  // Antes del cambio
await userManager.AddToRoleAsync(user, roleName);
// Si falla AddToRoleAsync, cache ya fue invalidado innecesariamente
```

### ? DO: Invalidar en Operaciones Transaccionales

```csharp
using var transaction = await _db.Database.BeginTransactionAsync();
try
{
    // Modificar BD
    _db.RoleClaims.Add(...);
    await _db.SaveChangesAsync();
    
    // Invalidar cache
    await cacheInvalidator.InvalidateRolePermissionsAsync(roleId);
    
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

### ?? DON'T: Invalidar Todo Innecesariamente

```csharp
// ? MAL: Invalida TODO cuando solo cambia 1 usuario
await cacheInvalidator.InvalidateAllPermissionsAsync();

// ? BIEN: Solo invalida el usuario afectado
await cacheInvalidator.InvalidateUserPermissionsAsync(userId);
```

## ? Resultado Final

### Problema Original
```
? Permisos no se actualizaban inmediatamente
? Usuario tenía que esperar tiempo aleatorio
? Logout/login necesario para ver cambios
? Inconsistencia entre BD y cache
```

### Solución Implementada
```
? Permisos se actualizan inmediatamente
? Cache se invalida automáticamente
? Próximo request obtiene permisos actualizados
? Consistencia garantizada BD ? Cache
? Logging completo para auditoría
? Performance optimizada (invalidación selectiva)
```

## ?? Resumen

**Tu observación fue correcta**: El problema era cache.

**Solución**: Invalidación automática en todos los puntos críticos:
1. ? Modificar permisos de rol ? Invalida usuarios con ese rol
2. ? Eliminar rol ? Invalida usuarios con ese rol
3. ? Asignar/remover roles ? Invalida ese usuario
4. ? Eliminar usuario ? Invalida ese usuario

**Beneficio**: Cambios de permisos se reflejan **inmediatamente** sin esperar a que expire el cache.
