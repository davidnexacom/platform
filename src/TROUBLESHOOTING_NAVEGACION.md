# Troubleshooting: Sistema de Navegación Dinámica

## Problema: No se muestran items en el menú

### Síntomas
- El menú aparece vacío
- No hay errores de compilación
- La aplicación funciona normalmente

### Causas y Soluciones

#### 1. Service Provider Incorrecto en Program.cs

**Problema**: Crear un `ServiceProvider` temporal antes de `app.Build()` crea una instancia diferente del singleton.

? **Incorrecto:**
```csharp
builder.Services.AddHeroUI();

// MALO: Este provider es diferente al de runtime
var serviceProvider = builder.Services.BuildServiceProvider();
var navigationRegistry = serviceProvider.GetRequiredService<INavigationRegistry>();
PlaygroundNavigationConfiguration.ConfigureNavigation(navigationRegistry);

var app = builder.Build();
```

? **Correcto:**
```csharp
builder.Services.AddHeroUI();

var app = builder.Build();

// BUENO: Usar el provider después de Build()
using (var scope = app.Services.CreateScope())
{
    var navigationRegistry = scope.ServiceProvider.GetRequiredService<INavigationRegistry>();
    PlaygroundNavigationConfiguration.ConfigureNavigation(navigationRegistry);
    BlazorModuleLoader.ConfigureModuleNavigation(navigationRegistry, Assembly.GetExecutingAssembly());
}
```

#### 2. Secciones No Registradas

**Problema**: Los items están en secciones que no existen.

? **Solución:**
```csharp
public static void ConfigureNavigation(INavigationRegistry registry)
{
    // Registrar TODAS las secciones, incluida Home
    registry.AddStandardSection(NavigationSections.Home, "Home", 0);
    registry.AddStandardSection(NavigationSections.Administration, "Administration", 100);
    registry.AddStandardSection(NavigationSections.Communication, "Communication", 200);
    registry.AddStandardSection(NavigationSections.System, "System", 300);
    registry.AddStandardSection(NavigationSections.Settings, "Settings", 400);

    // Luego agregar items
    registry.AddNavigationItem(item => item
        .WithId("home")
        .InSection(NavigationSections.Home) // La sección debe existir
        // ...
    );
}
```

#### 3. Restricciones de Autenticación Demasiado Estrictas

**Problema Original**: `ShouldShowItem` requería autenticación para TODOS los items.

? **Comportamiento Anterior:**
```csharp
private bool ShouldShowItem(NavigationItem item)
{
    if (!item.IsVisible)
        return false;

    // PROBLEMA: Siempre requiere autenticación
    if (_user?.Identity?.IsAuthenticated != true)
        return false;
    
    // ... resto del código
}
```

? **Comportamiento Actual:**
```csharp
private bool ShouldShowItem(NavigationItem item)
{
    if (!item.IsVisible)
        return false;

    // Si no tiene restricciones, mostrar sin requerir autenticación
    var hasRestrictions = item.RequiresRootTenantAdmin 
        || !string.IsNullOrWhiteSpace(item.RequiredPermission)
        || item.RequiredRoles?.Length > 0;

    if (!hasRestrictions)
        return true;

    // Solo verificar autenticación si hay restricciones
    if (_user?.Identity?.IsAuthenticated != true)
        return false;
    
    // ... verificar restricciones
}
```

### Herramientas de Diagnóstico

#### 1. Agregar Logging en NavMenu.razor

```razor
@inject ILogger<NavMenu> Logger

@code {
    private async Task LoadNavigationAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        _user = authState.User;
        _isRootTenantAdmin = CheckRootTenantAdmin(_user);

        _visibleSections = NavigationRegistry.GetSections();
        _itemsBySection = _visibleSections
            .ToDictionary(
                s => s.Id,
                s => NavigationRegistry.GetItemsForSection(s.Id));

        // AGREGAR LOGGING
        Logger.LogInformation("NavMenu: Total sections: {SectionCount}", _visibleSections.Count);
        Logger.LogInformation("NavMenu: User authenticated: {IsAuthenticated}", _user?.Identity?.IsAuthenticated);
        
        foreach (var section in _visibleSections)
        {
            var items = _itemsBySection.GetValueOrDefault(section.Id, []);
            Logger.LogInformation("NavMenu: Section '{SectionId}' ({SectionTitle}) has {ItemCount} items", 
                section.Id, section.Title, items.Count);
            
            foreach (var item in items)
            {
                var shouldShow = ShouldShowItem(item);
                Logger.LogInformation("NavMenu: Item '{ItemId}' ({ItemTitle}) visible: {IsVisible}, restrictions: Permission={HasPermission}, Roles={HasRoles}, RootAdmin={RequiresRoot}", 
                    item.Id, 
                    item.Title, 
                    shouldShow,
                    !string.IsNullOrWhiteSpace(item.RequiredPermission),
                    item.RequiredRoles?.Length > 0,
                    item.RequiresRootTenantAdmin);
            }
        }
    }
}
```

#### 2. Verificar en Consola del Navegador

Abrir DevTools (F12) y buscar:
```
NavMenu: Total sections: 5
NavMenu: Section 'Home' (Home) has 1 items
NavMenu: Item 'home' (Home) visible: True
NavMenu: Section 'Administration' (Administration) has 5 items
...
```

#### 3. Agregar Logging en Program.cs

```csharp
using (var scope = app.Services.CreateScope())
{
    var navigationRegistry = scope.ServiceProvider.GetRequiredService<INavigationRegistry>();
    
    PlaygroundNavigationConfiguration.ConfigureNavigation(navigationRegistry);
    BlazorModuleLoader.ConfigureModuleNavigation(navigationRegistry, Assembly.GetExecutingAssembly());

    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var sections = navigationRegistry.GetSections();
    logger.LogInformation("Navigation configured: {SectionCount} sections", sections.Count);
    
    foreach (var section in sections)
    {
        var items = navigationRegistry.GetItemsForSection(section.Id);
        logger.LogInformation("Section '{SectionTitle}': {ItemCount} items", section.Title, items.Count);
    }
}
```

### Checklist de Verificación

Cuando el menú no se muestra, verifica:

- [ ] ¿`AddHeroUI()` se llama en Program.cs?
- [ ] ¿La configuración de navegación ocurre DESPUÉS de `app.Build()`?
- [ ] ¿Todas las secciones referenciadas están registradas?
- [ ] ¿Los items tienen `IsVisible = true` (por defecto)?
- [ ] ¿Las restricciones de autenticación son apropiadas?
- [ ] ¿El `INavigationRegistry` está inyectado correctamente en NavMenu?
- [ ] ¿Hay errores en la consola del navegador?
- [ ] ¿Hay errores en los logs del servidor?

### Comandos de Diagnóstico

**Ver logs del servidor:**
```bash
dotnet run --project Playground/Playground.Blazor
# Buscar líneas que digan "Navigation configured" y "NavMenu:"
```

**Inspeccionar en runtime:**

En NavMenu.razor, agregar temporalmente:
```razor
<div style="position: fixed; top: 0; right: 0; background: yellow; padding: 10px; z-index: 9999;">
    Sections: @_visibleSections.Count <br/>
    Items: @_itemsBySection.Values.Sum(v => v.Count) <br/>
    Authenticated: @(_user?.Identity?.IsAuthenticated.ToString() ?? "null")
</div>
```

### Solución Rápida para Testing

Si solo quieres probar y ver TODOS los items sin restricciones:

```csharp
// En NavMenu.razor, temporalmente:
private bool ShouldShowItem(NavigationItem item)
{
    return item.IsVisible; // Ignorar TODAS las restricciones
}
```

?? **Importante**: Esto es solo para testing. NO usar en producción.

### Restaurar Menú Anterior

Si necesitas volver al menú hardcoded temporalmente:

```razor
<!-- En NavMenu.razor, comentar el código dinámico y descomentar: -->
<nav class="fsh-nav">
    <MudNavMenu Class="pa-2" Rounded="true" Margin="Margin.Dense" Color="Color.Primary">
        <MudNavLink Href="/" Match="NavLinkMatch.All" Icon="@Icons.Material.Outlined.Home">
            Home
        </MudNavLink>
        <!-- ... resto del menú hardcoded ... -->
    </MudNavMenu>
</nav>
```

## Errores Comunes

### Error: "No se puede resolver el servicio INavigationRegistry"

**Solución**: Asegúrate de que `AddHeroUI()` se llama en Program.cs.

### Error: Items aparecen duplicados

**Causa**: El registro se llama múltiples veces (ej: hot reload).

**Solución**: El registry ya tiene protección contra duplicados por ID. Verificar logs.

### Error: Secciones aparecen en orden incorrecto

**Solución**: Verificar la propiedad `Order` de las secciones:
```csharp
registry.AddStandardSection(NavigationSections.Home, "Home", 0);      // Primera
registry.AddStandardSection(NavigationSections.Settings, "Settings", 400); // Última
```

## Soporte

Para más ayuda:
1. Revisar logs del servidor
2. Revisar consola del navegador
3. Agregar logging como se describe arriba
4. Verificar el checklist completo
