# Arquitectura Modular de Permisos

## ?? Problema Original

Anteriormente, todos los permisos estaban centralizados en `PermissionConstants.cs`:

```csharp
// ? ANTES: Todo centralizado
public static class PermissionConstants
{
    private static readonly List<FshPermission> _all = new()
    {
        // Identity permissions
        new("View Users", ActionConstants.View, ResourceConstants.Users),
        new("View Roles", ActionConstants.View, ResourceConstants.Roles),
        
        // Multitenancy permissions
        new("View Tenants", ActionConstants.View, ResourceConstants.Tenants),
        
        // Auditing permissions
        new("View Audit Trails", ActionConstants.View, ResourceConstants.AuditTrails),
        
        // ... más permisos
    };
}
```

**Problemas:**
- ? Violación del principio de responsabilidad única
- ? Acoplamiento fuerte entre módulos
- ? Difícil de mantener y extender
- ? Módulos no son verdaderamente independientes

## ? Solución: Permisos por Módulo

Cada módulo define sus propios permisos siguiendo la misma estructura.

### Estructura de Archivos

```
Modules/
??? Identity/
?   ??? Modules.Identity/
?       ??? Authorization/
?           ??? IdentityPermissionConstants.cs     ? Permisos del módulo Identity
??? Multitenancy/
?   ??? Modules.Multitenancy/
?       ??? Authorization/
?           ??? MultitenancyPermissionConstants.cs ? Permisos del módulo Multitenancy
??? Auditing/
    ??? Modules.Auditing/
        ??? Authorization/
            ??? AuditingPermissionConstants.cs     ? Permisos del módulo Auditing
```

## ?? Patrón Estándar

Cada módulo sigue este patrón:

### 1. Definir Permisos del Módulo

```csharp
// Modules.Identity/Authorization/IdentityPermissionConstants.cs

using FSH.Framework.Shared.Constants;

namespace FSH.Modules.Identity.Authorization;

public static class IdentityPermissionConstants
{
    // Lista de permisos del módulo
    private static readonly List<FshPermission> _permissions = new()
    {
        new("View Users", ActionConstants.View, ResourceConstants.Users, IsBasic: true),
        new("Create Users", ActionConstants.Create, ResourceConstants.Users),
        new("Update Users", ActionConstants.Update, ResourceConstants.Users),
        new("Delete Users", ActionConstants.Delete, ResourceConstants.Users),
        // ... más permisos
    };

    /// <summary>
    /// All permissions defined by this module.
    /// </summary>
    public static IReadOnlyList<FshPermission> All => _permissions.AsReadOnly();

    /// <summary>
    /// Type-safe permission names for use in navigation, authorization, etc.
    /// </summary>
    public static class Names
    {
        public static class Users
        {
            public static string View => FshPermission.NameFor(ActionConstants.View, ResourceConstants.Users);
            public static string Create => FshPermission.NameFor(ActionConstants.Create, ResourceConstants.Users);
            // ... más permisos
        }
    }
}
```

### 2. Registrar Permisos en el Módulo

```csharp
// Modules.Identity/IdentityModule.cs

using FSH.Framework.Shared.Constants;
using FSH.Modules.Identity.Authorization;

public class IdentityModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        // ? Registrar permisos del módulo
        PermissionConstants.Register(IdentityPermissionConstants.All);
        
        // ... resto de la configuración del módulo
    }
}
```

### 3. Usar Permisos Type-Safe

```csharp
// En navegación
registry.AddNavigationItem(item => item
    .WithId("users")
    .WithTitle("Users")
    .RequirePermission(IdentityPermissionConstants.Names.Users.View) // ? Type-safe!
);

// En autorización de endpoints
group.MapGet("/users", GetUsersHandler)
    .RequireAuthorization(IdentityPermissionConstants.Names.Users.View);

// En código
if (user.HasPermission(IdentityPermissionConstants.Names.Users.Delete))
{
    // ...
}
```

## ??? Arquitectura

```
???????????????????????????????????????????????????????????????????
?                    PermissionConstants                          ?
?                  (Central Registry)                             ?
?                                                                 ?
?  private static List<FshPermission> _all = new() { ... };     ?
?                                                                 ?
?  ? Register(IEnumerable<FshPermission>)                       ?
?  ? All ? IReadOnlyList<FshPermission>                        ?
?  ? Root ? Root permissions                                    ?
?  ? Admin ? Admin permissions                                  ?
?  ? Basic ? Basic permissions                                  ?
???????????????????????????????????????????????????????????????????
                              ?
                              ? Register()
                 ???????????????????????????
                 ?            ?            ?
        ????????????????  ??????????????  ?????????????????
        ?  Identity    ?  ? Multitenancy?  ?   Auditing   ?
        ?   Module     ?  ?   Module    ?  ?    Module    ?
        ?              ?  ?             ?  ?              ?
        ?  Permisos:   ?  ?  Permisos:  ?  ?  Permisos:   ?
        ?  - Users     ?  ?  - Tenants  ?  ?  - Audits    ?
        ?  - Roles     ?  ?             ?  ?              ?
        ?  - UserRoles ?  ?             ?  ?              ?
        ?  - RoleClaims?  ?             ?  ?              ?
        ????????????????  ???????????????  ????????????????
```

## ?? Comparación

| Aspecto | Centralizado (Antes) | Modular (Ahora) |
|---------|---------------------|-----------------|
| **Responsabilidad** | ? Un archivo para todo | ? Cada módulo sus permisos |
| **Acoplamiento** | ? Alto | ? Bajo |
| **Mantenibilidad** | ? Difícil | ? Fácil |
| **Extensibilidad** | ? Modificar archivo central | ? Agregar archivo en módulo |
| **Independencia** | ? Módulos dependientes | ? Módulos independientes |
| **Type-Safety** | ?? Parcial | ? Completo |
| **Descubrimiento** | ? Buscar en archivo grande | ? IntelliSense por módulo |

## ?? Ejemplo Completo: Crear Nuevo Módulo

### Paso 1: Definir Permisos

```csharp
// Modules.Inventory/Authorization/InventoryPermissionConstants.cs

using FSH.Framework.Shared.Constants;

namespace FSH.Modules.Inventory.Authorization;

public static class InventoryPermissionConstants
{
    private static readonly List<FshPermission> _permissions = new()
    {
        // Products
        new("View Products", ActionConstants.View, "Products"),
        new("Create Products", ActionConstants.Create, "Products"),
        new("Update Products", ActionConstants.Update, "Products"),
        new("Delete Products", ActionConstants.Delete, "Products"),
        
        // Warehouses
        new("View Warehouses", ActionConstants.View, "Warehouses"),
        new("Manage Warehouses", "Manage", "Warehouses"),
    };

    public static IReadOnlyList<FshPermission> All => _permissions.AsReadOnly();

    public static class Names
    {
        public static class Products
        {
            public static string View => FshPermission.NameFor(ActionConstants.View, "Products");
            public static string Create => FshPermission.NameFor(ActionConstants.Create, "Products");
            public static string Update => FshPermission.NameFor(ActionConstants.Update, "Products");
            public static string Delete => FshPermission.NameFor(ActionConstants.Delete, "Products");
        }

        public static class Warehouses
        {
            public static string View => FshPermission.NameFor(ActionConstants.View, "Warehouses");
            public static string Manage => FshPermission.NameFor("Manage", "Warehouses");
        }
    }
}
```

### Paso 2: Registrar en el Módulo

```csharp
// Modules.Inventory/InventoryModule.cs

using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.Inventory.Authorization;

public class InventoryModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        // Registrar permisos del módulo
        PermissionConstants.Register(InventoryPermissionConstants.All);
        
        // Resto de la configuración
        // ...
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/inventory");
        
        // Usar permisos en endpoints
        group.MapGet("/products", GetProducts)
            .RequireAuthorization(InventoryPermissionConstants.Names.Products.View);
        
        group.MapPost("/products", CreateProduct)
            .RequireAuthorization(InventoryPermissionConstants.Names.Products.Create);
    }
}
```

### Paso 3: Usar en Navegación

```csharp
// Playground.Blazor/Modules/Inventory/InventoryBlazorModule.cs

using FSH.Modules.Inventory.Authorization;

public class InventoryBlazorModule : IBlazorModule
{
    public void ConfigureNavigation(INavigationRegistry registry)
    {
        registry.AddNavigationItem(item => item
            .WithId("inventory.products")
            .WithTitle("Products")
            .WithHref("/inventory/products")
            .RequirePermission(InventoryPermissionConstants.Names.Products.View) // ? Type-safe!
        );
    }
}
```

## ? Ventajas de Esta Arquitectura

### 1. **Separación de Responsabilidades**
Cada módulo es responsable de definir sus propios permisos.

### 2. **Bajo Acoplamiento**
Los módulos no dependen entre sí para definir permisos.

### 3. **Alta Cohesión**
Los permisos están junto al código que los usa.

### 4. **Type-Safety**
IntelliSense muestra solo los permisos relevantes del módulo.

### 5. **Fácil Mantenimiento**
Agregar/modificar permisos solo afecta al módulo específico.

### 6. **Plug & Play**
Los módulos se auto-registran sin modificar código central.

## ?? Migración de Código Existente

Si tienes código que usa la versión centralizada:

### Antes

```csharp
// ? Referencia genérica
.RequirePermission("Permissions.Users.View")
```

### Después (Opción 1: Usar constantes del módulo)

```csharp
// ? MEJOR: Usar constantes del módulo
using FSH.Modules.Identity.Authorization;

.RequirePermission(IdentityPermissionConstants.Names.Users.View)
```

### Después (Opción 2: Usar NavigationPermissions)

```csharp
// ? BUENO: Usar NavigationPermissions (si no tienes acceso al módulo)
using FSH.BuildingBlocks.Blazor.UI.Navigation;

.RequirePermission(NavigationPermissions.Users.View)
```

## ?? Referencias

### Archivos Creados/Modificados

1. **`Modules.Identity/Authorization/IdentityPermissionConstants.cs`** ? Nuevo
2. **`Modules.Multitenancy/Authorization/MultitenancyPermissionConstants.cs`** ? Nuevo
3. **`Modules.Auditing/Authorization/AuditingPermissionConstants.cs`** ? Nuevo
4. **`BuildingBlocks/Shared/Identity/PermissionConstants.cs`** ?? Refactorizado
5. **`Modules.Identity/IdentityModule.cs`** ?? Actualizado
6. **`Modules.Multitenancy/MultitenancyModule.cs`** ?? Actualizado
7. **`Modules.Auditing/AuditingModule.cs`** ?? Actualizado

## ?? Resumen

? **Los permisos ahora están modulares**
- Cada módulo define sus propios permisos
- Los permisos se registran automáticamente al cargar el módulo
- Type-safety completo con IntelliSense
- Fácil de extender con nuevos módulos

? **PermissionConstants es un registry central**
- Solo contiene permisos de plataforma core (Hangfire, Dashboard)
- Los módulos registran sus permisos usando `Register()`
- Provee vistas consolidadas: All, Root, Admin, Basic

? **Mejor arquitectura y mantenibilidad**
- Módulos verdaderamente independientes
- Fácil agregar nuevos módulos
- Código más organizado y localizado
