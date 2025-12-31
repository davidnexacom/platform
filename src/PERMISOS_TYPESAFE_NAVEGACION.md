# Sistema de Permisos Type-Safe para Navegación

## Tu Pregunta

> "la opción requireroles funciona correctamente pero tengo dudas con requirepermission porque acepta un string pero los permisos deberían ser del tipo FshPermission?"

## Respuesta

Tienes razón en tu observación. El sistema **internamente** trabaja con strings porque:

1. **Los Claims son strings**: ASP.NET Identity almacena los permisos como claims con valores string
2. **Los permisos se almacenan como strings**: `FshPermission.Name` genera un string con formato `"Permissions.{Resource}.{Action}"`
3. **La comparación es con strings**: En `NavMenu.razor` comparamos claims (strings) con permisos (strings)

**Sin embargo**, aceptar strings directamente tiene desventajas:
- ? No hay type-safety
- ? Fácil cometer errores de tipeo
- ? No hay IntelliSense

## Solución Implementada

He creado **`NavigationPermissions`**, una clase helper que proporciona **permisos pre-definidos con IntelliSense**.

### Ubicación
```
BuildingBlocks/Blazor.UI/Navigation/NavigationPermissions.cs
```

### Estructura

```csharp
public static class NavigationPermissions
{
    // Método general para construir permisos custom
    public static string Build(string action, string resource)
    {
        return $"Permissions.{resource}.{action}";
    }

    // Permisos pre-definidos con IntelliSense
    public static class Users
    {
        public static string View => "Permissions.Users.View";
        public static string Create => "Permissions.Users.Create";
        public static string Update => "Permissions.Users.Update";
        public static string Delete => "Permissions.Users.Delete";
        // ... más permisos
    }

    public static class Roles { /* ... */ }
    public static class Tenants { /* ... */ }
    public static class AuditTrails { /* ... */ }
    // ... más recursos
}
```

## Cómo Usar

### ? Opción 1: NavigationPermissions (RECOMENDADO - Type-Safe)

```csharp
registry.AddNavigationItem(item => item
    .WithId("users")
    .WithTitle("Users")
    .WithHref("/users")
    .WithIcon(Icons.Material.Outlined.Person)
    .InSection(NavigationSections.Administration)
    .RequirePermission(NavigationPermissions.Users.View) // ? Type-safe con IntelliSense!
    .WithOrder(10));
```

**Ventajas:**
- ? IntelliSense: Autocompletado mientras escribes
- ? Type-safe: El compilador valida que exista
- ? Refactoring seguro: Si cambias el nombre, se actualiza automáticamente
- ? Menos errores: No puedes escribir mal el nombre

### ?? Opción 2: String Directo (Funciona pero no recomendado)

```csharp
registry.AddNavigationItem(item => item
    .WithId("roles")
    .WithTitle("Roles")
    .WithHref("/roles")
    .WithIcon(Icons.Material.Outlined.AdminPanelSettings)
    .InSection(NavigationSections.Administration)
    .RequirePermission("Permissions.Roles.View") // ?? String directo - propenso a errores
    .WithOrder(20));
```

**Desventajas:**
- ? Sin IntelliSense
- ? Fácil cometer typos: `"Permissions.Roles.Veiw"` compila pero falla en runtime
- ? Refactoring manual

### ?? Opción 3: FshPermission.NameFor (Para permisos dinámicos)

Si estás en un contexto donde ya tienes acceso a `FshPermission`:

```csharp
using FSH.Framework.Shared.Constants;

registry.AddNavigationItem(item => item
    .WithId("user-roles")
    .WithTitle("User Roles")
    .WithHref("/user-roles")
    .WithIcon(Icons.Material.Outlined.PersonAdd)
    .InSection(NavigationSections.Administration)
    .RequirePermission(FshPermission.NameFor(ActionConstants.View, ResourceConstants.UserRoles))
    .WithOrder(25));
```

### ?? Opción 4: Build Custom (Para permisos de módulos personalizados)

Para permisos que no están en la lista predefinida:

```csharp
registry.AddNavigationItem(item => item
    .WithId("my-custom-item")
    .WithTitle("Custom Feature")
    .WithHref("/custom")
    .WithIcon(Icons.Material.Outlined.Star)
    .InSection("MySection")
    .RequirePermission(NavigationPermissions.Build("View", "CustomResource"))
    .WithOrder(10));
```

## Comparación Visual

```csharp
// ? ANTES: Propenso a errores
.RequirePermission("Permissions.Users.Veiw") // Typo: "Veiw" en lugar de "View"
// Compila ? pero falla en runtime ?

// ? DESPUÉS: Type-safe
.RequirePermission(NavigationPermissions.Users.View)
// Si el permiso no existe, error de compilación ?
// IntelliSense muestra opciones disponibles ?
```

## Cómo Funciona Internamente

### 1. Definición del Permiso (Backend)

```csharp
// En PermissionConstants.cs
public record FshPermission(string Description, string Action, string Resource, ...)
{
    public string Name => NameFor(Action, Resource);
    
    public static string NameFor(string action, string resource)
    {
        return $"Permissions.{resource}.{action}";
    }
}

// Ejemplo:
new FshPermission("View Users", "View", "Users")
// Name = "Permissions.Users.View"
```

### 2. Almacenamiento del Permiso (Claims)

```csharp
// En IdentityService.cs
var user = /* ... get user ... */;
var roles = await _userManager.GetRolesAsync(user);

// Los permisos se agregan como claims con el string generado
claims.Add(new Claim(CustomClaims.Permission, "Permissions.Users.View"));
```

### 3. Verificación en NavMenu

```csharp
// En NavMenu.razor
private bool ShouldShowItem(NavigationItem item)
{
    // ... código existente ...
    
    if (!string.IsNullOrWhiteSpace(item.RequiredPermission))
    {
        // Comparación de strings
        var hasPermission = _user.Claims.Any(c => 
            c.Type == CustomClaims.Permission && 
            c.Value == item.RequiredPermission); // Compara strings
        
        if (!hasPermission)
            return false;
    }
    
    return true;
}
```

## Agregar Nuevos Permisos

Si tu módulo tiene permisos personalizados:

### Opción A: Extender NavigationPermissions

```csharp
// En tu módulo: Modules.Inventory/Navigation/InventoryPermissions.cs
namespace Modules.Inventory.Navigation;

public static class InventoryPermissions
{
    public static class Products
    {
        public static string View => "Permissions.Products.View";
        public static string Create => "Permissions.Products.Create";
        public static string Update => "Permissions.Products.Update";
        public static string Delete => "Permissions.Products.Delete";
    }

    public static class Warehouses
    {
        public static string View => "Permissions.Warehouses.View";
        public static string Manage => "Permissions.Warehouses.Manage";
    }
}

// Uso:
registry.AddNavigationItem(item => item
    .WithId("products")
    .RequirePermission(InventoryPermissions.Products.View));
```

### Opción B: Usar Build()

```csharp
registry.AddNavigationItem(item => item
    .WithId("products")
    .RequirePermission(NavigationPermissions.Build("View", "Products")));
```

## Mejores Prácticas

### ? DO

1. **Usa NavigationPermissions siempre que sea posible**
   ```csharp
   .RequirePermission(NavigationPermissions.Users.View)
   ```

2. **Para permisos custom, crea tu propia clase de constantes**
   ```csharp
   public static class MyModulePermissions
   {
       public static string ViewSpecialFeature => "Permissions.SpecialFeature.View";
   }
   ```

3. **Documenta permisos personalizados**
   ```csharp
   /// <summary>
   /// Permissions for the Inventory module.
   /// </summary>
   public static class InventoryPermissions { }
   ```

### ? DON'T

1. **No uses strings mágicos directamente**
   ```csharp
   .RequirePermission("Permissions.Users.View") // ? Malo
   ```

2. **No copies y pegues strings**
   ```csharp
   // ? Malo - duplicación
   .RequirePermission("Permissions.Users.View")
   .RequirePermission("Permissions.Users.View") // En otro lugar
   ```

3. **No inventes formatos de permisos**
   ```csharp
   .RequirePermission("Users.View") // ? Formato incorrecto
   .RequirePermission("View-Users") // ? Formato incorrecto
   ```

## Resumen

| Aspecto | String Directo | NavigationPermissions | FshPermission.NameFor |
|---------|----------------|----------------------|----------------------|
| Type-Safe | ? | ? | ? |
| IntelliSense | ? | ? | ?? Parcial |
| Refactoring | ? | ? | ? |
| Fácil de usar | ?? | ? | ?? |
| Funciona | ? | ? | ? |
| **Recomendado** | ? | ? | Solo si ya tienes contexto |

## Conclusión

**Respuesta a tu pregunta:**

Sí, los permisos se definen como `FshPermission`, pero **internamente se almacenan y comparan como strings**. 

La solución `NavigationPermissions` te da lo mejor de ambos mundos:
- ? **Type-safety** en tiempo de desarrollo
- ? **Strings** en runtime (compatible con el sistema existente)
- ? **IntelliSense** para evitar errores

**Usa siempre `NavigationPermissions` en lugar de strings directos.**
