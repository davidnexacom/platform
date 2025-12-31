# Dynamic Navigation System

Este sistema permite que los módulos registren dinámicamente sus entradas de menú en la aplicación Blazor, eliminando la necesidad de modificar manualmente el `NavMenu.razor` para cada nueva funcionalidad.

## Características

- ? **Registro dinámico**: Los módulos pueden registrar sus entradas de menú automáticamente
- ? **Control de acceso**: Soporte para permisos, roles y requisitos de tenant
- ? **Secciones organizadas**: Agrupa elementos del menú en secciones lógicas
- ? **Ordenamiento flexible**: Control total sobre el orden de secciones e items
- ? **Type-safe**: API fluida con verificación de tipos en tiempo de compilación

## Arquitectura

### Componentes Principales

1. **NavigationItem**: Define un elemento de menú individual
2. **NavigationSection**: Define una sección/grupo de elementos
3. **INavigationRegistry**: Servicio para registrar y recuperar elementos de navegación
4. **IBlazorModule**: Interfaz para módulos que desean contribuir al menú
5. **BlazorModuleLoader**: Descubre y carga módulos automáticamente

## Uso Básico

### 1. Crear un Módulo Blazor

```csharp
using FSH.BuildingBlocks.Blazor.UI.Extensions;
using FSH.BuildingBlocks.Blazor.UI.Modules;
using FSH.BuildingBlocks.Blazor.UI.Navigation;
using MudBlazor;

namespace MiAplicacion.Modules.Catalog;

public class CatalogBlazorModule : IBlazorModule
{
    public void ConfigureNavigation(INavigationRegistry registry)
    {
        // Registrar una sección (opcional, si no existe)
        registry.AddStandardSection("Catalog", "Catálogo", 150);

        // Registrar items de navegación
        registry.AddNavigationItem(item => item
            .WithId("catalog.products")
            .WithTitle("Productos")
            .WithHref("/products")
            .WithIcon(Icons.Material.Outlined.Inventory)
            .InSection("Catalog")
            .WithOrder(10));

        registry.AddNavigationItem(item => item
            .WithId("catalog.categories")
            .WithTitle("Categorías")
            .WithHref("/categories")
            .WithIcon(Icons.Material.Outlined.Category)
            .InSection("Catalog")
            .WithOrder(20));
    }
}
```

### 2. El Módulo se Descubre Automáticamente

El `BlazorModuleLoader` en `Program.cs` descubre automáticamente todos los módulos que implementan `IBlazorModule`:

```csharp
// En Program.cs
BlazorModuleLoader.ConfigureModuleNavigation(
    navigationRegistry,
    Assembly.GetExecutingAssembly()); // Busca módulos en este assembly
```

### 3. Control de Acceso

#### Requerir Permisos

```csharp
registry.AddNavigationItem(item => item
    .WithId("admin.users")
    .WithTitle("Usuarios")
    .WithHref("/users")
    .WithIcon(Icons.Material.Outlined.Person)
    .InSection(NavigationSections.Administration)
    .RequirePermission("Permissions.Users.View"));
```

#### Requerir Roles

```csharp
registry.AddNavigationItem(item => item
    .WithId("admin.settings")
    .WithTitle("Configuración")
    .WithHref("/settings")
    .WithIcon(Icons.Material.Outlined.Settings)
    .InSection(NavigationSections.Administration)
    .RequireRoles("Admin", "SuperAdmin"));
```

#### Requerir Root Tenant Admin

```csharp
registry.AddNavigationItem(item => item
    .WithId("admin.tenants")
    .WithTitle("Tenants")
    .WithHref("/tenants")
    .WithIcon(Icons.Material.Outlined.CorporateFare)
    .InSection(NavigationSections.Administration)
    .RequireRootTenantAdmin());
```

## Secciones Predefinidas

Las siguientes secciones están disponibles en `NavigationSections`:

- `Home` - Items sin sección
- `Administration` - Administración del sistema
- `Communication` - Chat, notificaciones
- `System` - Salud, logs
- `Settings` - Configuración de usuario

### Crear Secciones Personalizadas

```csharp
registry.RegisterSection(new NavigationSection
{
    Id = "MiSeccion",
    Title = "Mi Sección Personalizada",
    Order = 250,
    IsVisible = true
});
```

## API Fluida

### NavigationItemBuilder

El builder proporciona una API fluida para construir items:

```csharp
registry.AddNavigationItem(item => item
    .WithId("unique-id")                    // Identificador único (requerido)
    .WithTitle("Título")                    // Texto visible (requerido)
    .WithHref("/ruta")                      // URL/ruta (requerido)
    .WithIcon(Icons.Material.Outlined.Home) // Icono de MudBlazor (requerido)
    .InSection("Sección")                   // Sección contenedora (requerido)
    .WithOrder(10)                          // Orden dentro de la sección
    .RequirePermission("permiso")           // Permiso requerido
    .RequireRoles("rol1", "rol2")          // Roles requeridos
    .RequireRootTenantAdmin()              // Solo root tenant admin
    .SetVisible(true));                     // Visibilidad
```

## Ventajas del Sistema

### Antes (Hardcoded)

```razor
<!-- NavMenu.razor -->
<MudNavLink Href="/products">Productos</MudNavLink>
<MudNavLink Href="/categories">Categorías</MudNavLink>
<!-- Cada módulo requiere editar este archivo -->
```

### Después (Dinámico)

```csharp
// CatalogBlazorModule.cs
public class CatalogBlazorModule : IBlazorModule
{
    public void ConfigureNavigation(INavigationRegistry registry)
    {
        registry.AddNavigationItem(item => item
            .WithId("catalog.products")
            .WithTitle("Productos")
            .WithHref("/products")
            .WithIcon(Icons.Material.Outlined.Inventory)
            .InSection("Catalog")
            .WithOrder(10));
    }
}
// El menú se actualiza automáticamente
```

## Mejores Prácticas

1. **IDs Únicos**: Use un prefijo de módulo para los IDs (ej: `catalog.products`)
2. **Orden Lógico**: Use múltiplos de 10 para el orden (10, 20, 30) para permitir inserción futura
3. **Iconos Consistentes**: Use iconos de MudBlazor del namespace `Icons.Material.Outlined`
4. **Permisos Específicos**: Asocie cada item con un permiso específico cuando sea posible
5. **Lazy Loading**: Los items se evalúan en cada renderizado, permitiendo visibilidad dinámica

## Integración con Módulos Existentes

Para agregar navegación a un módulo existente:

1. Cree una clase que implemente `IBlazorModule`
2. Implemente `ConfigureNavigation()`
3. Registre sus items de navegación
4. El módulo se descubrirá automáticamente

## Troubleshooting

### El item no aparece

- Verifique que el usuario tenga los permisos/roles requeridos
- Confirme que `IsVisible = true`
- Revise que la sección exista
- Asegúrese de que el módulo se está cargando

### Items duplicados

- Verifique que los IDs sean únicos
- El registro usa el ID para evitar duplicados

### Orden incorrecto

- Los items se ordenan por `Order` y luego por `Title`
- Use números más bajos para aparecer primero

## Extensiones Futuras

Posibles mejoras al sistema:

- [ ] Menús anidados / sub-items
- [ ] Badges y contadores en items
- [ ] Items con estado activo/inactivo dinámico
- [ ] Localización de títulos
- [ ] Configuración de navegación desde base de datos
- [ ] Hot-reload de configuración de navegación
