# Guía Práctica: Sistema de Navegación Dinámica

## ?? Casos de Uso Comunes

### Caso 1: Agregar un Nuevo Módulo con Navegación

**Escenario**: Estás creando un módulo de "Inventario" y quieres agregar elementos al menú.

**Paso 1**: Crear el módulo Blazor

```csharp
// Playground.Blazor/Modules/Inventory/InventoryBlazorModule.cs

using FSH.BuildingBlocks.Blazor.UI.Extensions;
using FSH.BuildingBlocks.Blazor.UI.Modules;
using FSH.BuildingBlocks.Blazor.UI.Navigation;
using MudBlazor;

namespace FSH.Playground.Blazor.Modules.Inventory;

public class InventoryBlazorModule : IBlazorModule
{
    public void ConfigureNavigation(INavigationRegistry registry)
    {
        // Crear una nueva sección
        registry.RegisterSection(new NavigationSection
        {
            Id = "Inventory",
            Title = "Inventario",
            Order = 150,
            IsVisible = true
        });

        // Agregar items a la sección
        registry.AddNavigationItem(item => item
            .WithId("inventory.products")
            .WithTitle("Productos")
            .WithHref("/inventory/products")
            .WithIcon(Icons.Material.Outlined.Inventory)
            .InSection("Inventory")
            .WithOrder(10));

        registry.AddNavigationItem(item => item
            .WithId("inventory.warehouses")
            .WithTitle("Almacenes")
            .WithHref("/inventory/warehouses")
            .WithIcon(Icons.Material.Outlined.Warehouse)
            .InSection("Inventory")
            .WithOrder(20));

        registry.AddNavigationItem(item => item
            .WithId("inventory.reports")
            .WithTitle("Reportes")
            .WithHref("/inventory/reports")
            .WithIcon(Icons.Material.Outlined.Assessment)
            .InSection("Inventory")
            .WithOrder(30)
            .RequirePermission(NavigationPermissions.Build("ViewReports", "Inventory")));
    }
}
```

**Paso 2**: ¡Eso es todo! El módulo se descubrirá automáticamente.

---

### Caso 2: Agregar un Item con Permisos Específicos (Type-Safe)

**Escenario**: Solo usuarios con permiso pueden ver reportes financieros.

```csharp
public class FinanceBlazorModule : IBlazorModule
{
    public void ConfigureNavigation(INavigationRegistry registry)
    {
        registry.AddNavigationItem(item => item
            .WithId("finance.reports")
            .WithTitle("Reportes Financieros")
            .WithHref("/finance/reports")
            .WithIcon(Icons.Material.Outlined.AttachMoney)
            .InSection(NavigationSections.Administration)
            .RequirePermission(NavigationPermissions.Build("View", "FinancialReports"))
            .WithOrder(100));
    }
}
```

**? MEJOR: Usar permisos predefinidos cuando sea posible**

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

**Resultado**: El item solo aparece si el usuario tiene el claim correspondiente.

---

### Caso 3: Permisos Type-Safe vs String

**? ANTES (Propenso a errores):**
```csharp
registry.AddNavigationItem(item => item
    .WithId("roles")
    .WithTitle("Roles")
    .WithHref("/roles")
    .WithIcon(Icons.Material.Outlined.AdminPanelSettings)
    .InSection(NavigationSections.Administration)
    .RequirePermission("Permissions.Roles.Veiw") // Typo: "Veiw" - compila pero falla!
    .WithOrder(20));
```

**? DESPUÉS (Type-safe):**
```csharp
registry.AddNavigationItem(item => item
    .WithId("roles")
    .WithTitle("Roles")
    .WithHref("/roles")
    .WithIcon(Icons.Material.Outlined.AdminPanelSettings)
    .InSection(NavigationSections.Administration)
    .RequirePermission(NavigationPermissions.Roles.View) // ? Error de compilación si no existe
    .WithOrder(20));
```

---

### Caso 4: Item Solo para Roles Específicos

**Escenario**: Solo Admins y Managers pueden acceder a configuración avanzada.

```csharp
registry.AddNavigationItem(item => item
    .WithId("settings.advanced")
    .WithTitle("Configuración Avanzada")
    .WithHref("/settings/advanced")
    .WithIcon(Icons.Material.Outlined.SettingsApplications)
    .InSection(NavigationSections.Settings)
    .RequireRoles("Admin", "Manager")
    .WithOrder(100));
```

**Resultado**: El item aparece si el usuario tiene rol "Admin" O "Manager".

---

### Caso 5: Item Solo para Root Tenant Admin

**Escenario**: Gestión de tenants debe estar disponible solo para admin del tenant root.

```csharp
registry.AddNavigationItem(item => item
    .WithId("admin.tenants")
    .WithTitle("Gestión de Tenants")
    .WithHref("/admin/tenants")
    .WithIcon(Icons.Material.Outlined.Business)
    .InSection(NavigationSections.Administration)
    .RequireRootTenantAdmin()
    .WithOrder(5));
```

**Resultado**: Solo visible si:
- Usuario tiene claim `Tenant: "root"`
- Y tiene rol "Admin"

---

### Caso 6: Combinar Múltiples Requisitos

**Escenario**: Auditoría solo para Admins del tenant root con permiso específico.

```csharp
registry.AddNavigationItem(item => item
    .WithId("admin.audit")
    .WithTitle("Auditoría del Sistema")
    .WithHref("/admin/audit")
    .WithIcon(Icons.Material.Outlined.Security)
    .InSection(NavigationSections.Administration)
    .RequireRootTenantAdmin()
    .RequirePermission("Permissions.System.Audit")
    .WithOrder(90));
```

**Resultado**: Todas las condiciones deben cumplirse:
- Tenant root
- Rol Admin
- Permiso específico

---

### Caso 7: Agregar Items a una Sección Existente

**Escenario**: Quieres agregar un item a la sección "System" que ya existe.

```csharp
public class MonitoringBlazorModule : IBlazorModule
{
    public void ConfigureNavigation(INavigationRegistry registry)
    {
        // No necesitas crear la sección, ya existe
        registry.AddNavigationItem(item => item
            .WithId("monitoring.metrics")
            .WithTitle("Métricas")
            .WithHref("/monitoring/metrics")
            .WithIcon(Icons.Material.Outlined.ShowChart)
            .InSection(NavigationSections.System)  // Usa sección existente
            .WithOrder(25));  // Entre Health (10) y Logs (20)
    }
}
```

---

### Caso 8: Modificar Configuración Base

**Escenario**: Quieres cambiar el orden o visibilidad de items base.

**Opción A: Modificar PlaygroundNavigationConfiguration.cs**

```csharp
// En PlaygroundNavigationConfiguration.cs
registry.AddNavigationItem(item => item
    .WithId("users")
    .WithTitle("Usuarios")
    .WithHref("/users")
    .WithIcon(Icons.Material.Outlined.Person)
    .InSection(NavigationSections.Administration)
    .WithOrder(5)  // Cambiar orden
    .RequirePermission("Permissions.Users.View"));  // Agregar permiso
```

**Opción B: Crear un módulo de configuración**

```csharp
public class CustomNavigationModule : IBlazorModule
{
    public void ConfigureNavigation(INavigationRegistry registry)
    {
        // Este módulo se carga DESPUÉS de la config base
        // y puede modificar items (si tienen el mismo ID)
        
        // Agregar nuevos items
        registry.AddNavigationItem(item => item
            .WithId("custom.dashboard")
            .WithTitle("Mi Dashboard")
            .WithHref("/dashboard")
            .WithIcon(Icons.Material.Outlined.Dashboard)
            .InSection(NavigationSections.Home)
            .WithOrder(1));
    }
}
```

---

### Caso 9: Navegación Condicional Dinámica

**Escenario**: Mostrar un item solo si cierta configuración está habilitada.

**Solución 1: En el módulo**

```csharp
public class FeatureModule : IBlazorModule
{
    public void ConfigureNavigation(INavigationRegistry registry)
    {
        // Obtener configuración (inyectar IConfiguration si es necesario)
        var featureEnabled = true; // Leer de config
        
        if (featureEnabled)
        {
            registry.AddNavigationItem(item => item
                .WithId("feature.item")
                .WithTitle("Nueva Característica")
                .WithHref("/feature")
                .WithIcon(Icons.Material.Outlined.NewReleases)
                .InSection(NavigationSections.Settings)
                .WithOrder(100));
        }
    }
}
```

**Solución 2: Usando IsVisible**

```csharp
registry.AddNavigationItem(item => item
    .WithId("beta.feature")
    .WithTitle("Beta Feature")
    .WithHref("/beta")
    .WithIcon(Icons.Material.Outlined.Science)
    .InSection(NavigationSections.Settings)
    .SetVisible(false)  // Ocultar por ahora
    .WithOrder(100));
```

---

## ?? Patrones de Uso Avanzados

### Patrón 1: Factory de Items

Para módulos complejos, crear un factory:

```csharp
public static class NavigationItemFactory
{
    public static NavigationItem CreateAdminItem(
        string id, 
        string title, 
        string href, 
        string icon,
        int order = 10)
    {
        return new NavigationItem
        {
            Id = id,
            Title = title,
            Href = href,
            Icon = icon,
            Section = NavigationSections.Administration,
            Order = order,
            RequireRoles = ["Admin"]
        };
    }
}

// Uso:
registry.RegisterItem(NavigationItemFactory.CreateAdminItem(
    "admin.item",
    "Admin Item",
    "/admin/item",
    Icons.Material.Outlined.Star));
```

### Patrón 2: Registro en Lote

```csharp
public class CatalogBlazorModule : IBlazorModule
{
    public void ConfigureNavigation(INavigationRegistry registry)
    {
        var catalogItems = new[]
        {
            ("products", "Productos", "/catalog/products", Icons.Material.Outlined.Inventory),
            ("categories", "Categorías", "/catalog/categories", Icons.Material.Outlined.Category),
            ("brands", "Marcas", "/catalog/brands", Icons.Material.Outlined.LocalOffer)
        };

        var items = catalogItems.Select((item, index) => new NavigationItem
        {
            Id = $"catalog.{item.Item1}",
            Title = item.Item2,
            Href = item.Item3,
            Icon = item.Item4,
            Section = "Catalog",
            Order = (index + 1) * 10
        });

        registry.RegisterItems(items);
    }
}
```

### Patrón 3: Herencia de Configuración

```csharp
public abstract class BaseModuleNavigation : IBlazorModule
{
    protected abstract string SectionId { get; }
    protected abstract string SectionTitle { get; }
    protected abstract int SectionOrder { get; }

    public virtual void ConfigureNavigation(INavigationRegistry registry)
    {
        registry.AddStandardSection(SectionId, SectionTitle, SectionOrder);
        ConfigureItems(registry);
    }

    protected abstract void ConfigureItems(INavigationRegistry registry);
}

public class InventoryNavigation : BaseModuleNavigation
{
    protected override string SectionId => "Inventory";
    protected override string SectionTitle => "Inventario";
    protected override int SectionOrder => 150;

    protected override void ConfigureItems(INavigationRegistry registry)
    {
        registry.AddNavigationItem(item => item
            .WithId("inventory.products")
            .WithTitle("Productos")
            .WithHref("/inventory/products")
            .WithIcon(Icons.Material.Outlined.Inventory)
            .InSection(SectionId)
            .WithOrder(10));
    }
}
```

---

## ?? Troubleshooting

### Problema: Mi item no aparece

**Checklist:**
1. ? ¿El módulo implementa `IBlazorModule`?
2. ? ¿El módulo está en el assembly escaneado?
3. ? ¿El item tiene `IsVisible = true`?
4. ? ¿El usuario está autenticado?
5. ? ¿El usuario tiene los permisos/roles requeridos?
6. ? ¿La sección existe?

**Debug:**

```csharp
// En NavMenu.razor, agregar logging temporal
protected override async Task OnInitializedAsync()
{
    await LoadNavigationAsync();
    
    Console.WriteLine($"Total sections: {_visibleSections.Count}");
    foreach (var section in _visibleSections)
    {
        var items = _itemsBySection.GetValueOrDefault(section.Id, []);
        Console.WriteLine($"Section {section.Title}: {items.Count} items");
        foreach (var item in items)
        {
            Console.WriteLine($"  - {item.Title} (visible: {ShouldShowItem(item)})");
        }
    }
}
```

### Problema: Items en orden incorrecto

**Solución:** Usa múltiplos de 10 para permitir inserción:

```csharp
// ? Malo
.WithOrder(1)
.WithOrder(2)
.WithOrder(3)

// ? Bueno
.WithOrder(10)
.WithOrder(20)
.WithOrder(30)

// Ahora puedes insertar en el medio:
.WithOrder(25)  // Entre 20 y 30
```

### Problema: Sección no aparece aunque tiene items

**Causa:** Ningún item de la sección es visible para el usuario actual.

**Solución:** Verifica que al menos un item cumpla los requisitos de acceso.

---

## ?? Mejores Prácticas

### ? DO

1. **Usa prefijos en IDs**
   ```csharp
   .WithId("inventory.products")  // Bueno
   .WithId("catalog.categories")  // Bueno
   ```

2. **Órdenes en múltiplos de 10**
   ```csharp
   .WithOrder(10)
   .WithOrder(20)
   .WithOrder(30)
   ```

3. **Permisos específicos**
   ```csharp
   .RequirePermission("Permissions.Inventory.ViewProducts")
   ```

4. **Iconos consistentes**
   ```csharp
   // Usa Icons.Material.Outlined para consistencia
   .WithIcon(Icons.Material.Outlined.Inventory)
   ```

### ? DON'T

1. **No uses IDs genéricos**
   ```csharp
   .WithId("item1")  // Malo - puede colisionar
   ```

2. **No uses órdenes consecutivos**
   ```csharp
   .WithOrder(1)
   .WithOrder(2)  // Malo - difícil insertar después
   ```

3. **No mezcles estilos de iconos**
   ```csharp
   Icons.Material.Filled.Home      // Evitar mezclar
   Icons.Material.Outlined.Person  // Usa consistentemente
   ```

---

## ?? Recursos

- **Documentación completa**: `BuildingBlocks/Blazor.UI/Navigation/README.md`
- **Arquitectura**: `ARQUITECTURA_NAVEGACION.md`
- **Resumen**: `SOLUCION_NAVEGACION_DINAMICA.md`

## ?? Próximos Pasos

1. Implementa tu primer módulo
2. Prueba los controles de acceso
3. Experimenta con órdenes y secciones
4. Contribuye mejoras al sistema
