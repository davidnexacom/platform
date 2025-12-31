# Sistema de Navegación Dinámica - Resumen de Implementación

## ?? Objetivo

Permitir que los módulos registren dinámicamente sus entradas de menú en la aplicación Blazor, eliminando la necesidad de modificar manualmente `NavMenu.razor`.

## ? Solución Implementada

### 1. **Infraestructura Base** (BuildingBlocks/Blazor.UI)

Se creó un sistema de registro de navegación con los siguientes componentes:

#### Clases de Modelo
- **`NavigationItem`**: Define un elemento individual del menú con soporte para:
  - Propiedades básicas: Id, Title, Href, Icon, Section, Order
  - Control de acceso: RequiredPermission, RequiredRoles, RequiresRootTenantAdmin
  - Visibilidad: IsVisible

- **`NavigationSection`**: Define secciones/grupos de menú
  - Id, Title, Order, IsVisible

#### Servicios
- **`INavigationRegistry`**: Interfaz del servicio de registro
  - `RegisterSection()`: Registra secciones
  - `RegisterItem()`: Registra elementos individuales
  - `GetSections()`: Obtiene todas las secciones
  - `GetItemsForSection()`: Obtiene elementos de una sección

- **`NavigationRegistry`**: Implementación thread-safe del registro

#### Extensiones y Builders
- **`NavigationRegistryExtensions`**: Helpers para registro fluido
  - `AddStandardSection()`: Registra secciones predefinidas
  - `AddNavigationItem()`: API fluida para agregar items

- **`NavigationItemBuilder`**: Builder con API fluida
  ```csharp
  .WithId("id")
  .WithTitle("Título")
  .WithHref("/ruta")
  .WithIcon(Icons.Material.Outlined.Icon)
  .InSection("Sección")
  .RequirePermission("permiso")
  ```

#### Constantes
- **`NavigationSections`**: Secciones estándar predefinidas
  - Home, Administration, Communication, System, Settings

### 2. **Sistema de Módulos** (BuildingBlocks/Blazor.UI/Modules)

#### Interfaz de Módulo
- **`IBlazorModule`**: Interfaz para módulos que desean contribuir al menú
  ```csharp
  public interface IBlazorModule
  {
      void ConfigureNavigation(INavigationRegistry registry);
  }
  ```

#### Cargador de Módulos
- **`BlazorModuleLoader`**: Descubre y carga módulos automáticamente
  - Escanea assemblies buscando implementaciones de `IBlazorModule`
  - Invoca `ConfigureNavigation()` en cada módulo encontrado
  - Manejo de errores robusto

### 3. **Implementación en Playground** (Playground.Blazor)

#### Configuración Base
- **`PlaygroundNavigationConfiguration`**: Configuración centralizada
  - Registra todas las secciones estándar
  - Configura los elementos de menú existentes:
    - Home
    - Users, Roles, Tenants, Tenant Settings, Audits (Administration)
    - Chat, Notifications (Communication)
    - Health, Logs (System)
    - Account, Theme, Security, Sessions, About (Settings)

#### Componente de Menú Actualizado
- **`NavMenu.razor`**: Ahora renderiza dinámicamente desde el registry
  - Obtiene secciones y elementos del `INavigationRegistry`
  - Evalúa permisos, roles y requisitos de tenant
  - Renderiza solo los elementos visibles para el usuario actual
  - Soporta sección "Home" sin encabezado

#### Integración en Program.cs
```csharp
// Configurar navegación base
PlaygroundNavigationConfiguration.ConfigureNavigation(navigationRegistry);

// Cargar módulos automáticamente
BlazorModuleLoader.ConfigureModuleNavigation(
    navigationRegistry,
    Assembly.GetExecutingAssembly());
```

### 4. **Ejemplo de Módulo**

Se creó un módulo de ejemplo (`IdentityBlazorModule`) que demuestra cómo registrar navegación:

```csharp
public class IdentityBlazorModule : IBlazorModule
{
    public void ConfigureNavigation(INavigationRegistry registry)
    {
        registry.AddNavigationItem(item => item
            .WithId("module.identity.users")
            .WithTitle("Users (Module)")
            .WithHref("/users")
            .WithIcon(Icons.Material.Outlined.Person)
            .InSection(NavigationSections.Administration)
            .WithOrder(10));
    }
}
```

## ?? Archivos Creados

### BuildingBlocks/Blazor.UI/
```
Navigation/
??? NavigationItem.cs
??? NavigationSection.cs
??? INavigationRegistry.cs
??? NavigationRegistry.cs
??? NavigationSections.cs
??? README.md

Extensions/
??? NavigationRegistryExtensions.cs

Modules/
??? IBlazorModule.cs
??? BlazorModuleLoader.cs
```

### Playground/Playground.Blazor/
```
Navigation/
??? PlaygroundNavigationConfiguration.cs

Modules/Identity/
??? IdentityBlazorModule.cs (ejemplo)
```

### Archivos Modificados
```
BuildingBlocks/Blazor.UI/ServiceCollectionExtensions.cs
Playground/Playground.Blazor/Program.cs
Playground/Playground.Blazor/Components/Layout/NavMenu.razor
```

## ?? Características Principales

### 1. **Descubrimiento Automático**
Los módulos que implementan `IBlazorModule` son descubiertos automáticamente en tiempo de inicio.

### 2. **Control de Acceso Granular**
- **Permisos**: Verifica claims de permisos específicos
- **Roles**: Requiere uno o más roles
- **Root Tenant Admin**: Elementos solo para admin del tenant raíz

### 3. **Ordenamiento Flexible**
- Secciones ordenadas por `Order` y `Title`
- Items ordenados por `Order` y `Title` dentro de cada sección
- Uso de múltiplos de 10 permite inserción futura

### 4. **Thread-Safe**
El registro usa locks para garantizar seguridad en ambientes concurrentes.

### 5. **Evaluación Dinámica**
Los permisos y visibilidad se evalúan en cada renderizado, permitiendo cambios dinámicos.

## ?? Cómo Usar

### Para Agregar un Nuevo Elemento desde un Módulo

1. **Crear una clase que implemente `IBlazorModule`**
```csharp
public class MiModuloBlazor : IBlazorModule
{
    public void ConfigureNavigation(INavigationRegistry registry)
    {
        registry.AddNavigationItem(item => item
            .WithId("mimodulo.item")
            .WithTitle("Mi Ítem")
            .WithHref("/mi-ruta")
            .WithIcon(Icons.Material.Outlined.Star)
            .InSection(NavigationSections.Administration)
            .WithOrder(25));
    }
}
```

2. **El módulo se descubrirá automáticamente** (si está en el assembly escaneado)

### Para Agregar un Elemento de Forma Directa

En `PlaygroundNavigationConfiguration.cs`:
```csharp
registry.AddNavigationItem(item => item
    .WithId("nuevo-item")
    .WithTitle("Nuevo Item")
    .WithHref("/nuevo")
    .WithIcon(Icons.Material.Outlined.NewReleases)
    .InSection(NavigationSections.System)
    .WithOrder(30));
```

## ?? Ventajas

| Antes | Después |
|-------|---------|
| ? Menú hardcoded en Razor | ? Menú dinámico desde registry |
| ? Editar NavMenu.razor para cada cambio | ? Módulos auto-registran sus items |
| ? Control de acceso disperso | ? Control de acceso centralizado |
| ? Difícil de mantener | ? Fácil de extender |
| ? Acoplamiento fuerte | ? Bajo acoplamiento |

## ?? Posibles Extensiones Futuras

1. **Menús Anidados**: Soporte para sub-items y jerarquías
2. **Badges**: Contadores y notificaciones en items
3. **Iconos Dinámicos**: Cambio de icono basado en estado
4. **Localización**: Títulos multiidioma
5. **Configuración Persistente**: Guardar en BD
6. **Hot-Reload**: Recargar configuración sin reiniciar
7. **Drag & Drop**: Reordenar visualmente
8. **Favoritos**: Usuario puede marcar favoritos

## ? Testing

Para verificar:
1. ? Compilación exitosa
2. ? NavMenu renderiza correctamente
3. ? Control de acceso funciona (permisos, roles, tenant)
4. ? Módulos pueden registrar items
5. ? Ordenamiento correcto de secciones e items

## ?? Documentación

La documentación completa se encuentra en:
- `BuildingBlocks/Blazor.UI/Navigation/README.md`

Este archivo incluye:
- Guía de uso detallada
- Ejemplos de código
- Mejores prácticas
- Troubleshooting
- API completa

## ?? Conclusión

El sistema de navegación dinámica proporciona una solución flexible, mantenible y extensible para gestionar el menú de la aplicación. Los módulos ahora pueden contribuir sus propias entradas sin modificar código existente, siguiendo el principio Open/Closed de SOLID.
