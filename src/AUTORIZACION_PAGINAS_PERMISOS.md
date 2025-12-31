# Autorización de Páginas con Permisos

## ?? Sistema Implementado

Sistema completo de autorización de páginas basado en permisos con soporte para:

1. ? **Permiso único**
2. ? **Múltiples permisos con lógica OR** (al menos uno)
3. ? **Múltiples permisos con lógica AND** (todos requeridos)
4. ? **Consulta via API** (sin token bloat)
5. ? **UI de Access Denied** elegante
6. ? **Uso súper simple** con `<AuthorizeView>`

## ?? Uso Rápido (Recomendado)

### Permiso Único

```razor
@page "/roles"

<AuthorizeView Permission="Permissions.Roles.View">
    <FshPageHeader Title="Roles" Description="Manage roles" />
    
    <!-- Todo el contenido protegido aquí -->
    <MudDataGrid T="RoleDto" Items="@_roles" />
</AuthorizeView>
```

### Múltiples Permisos (OR - Al Menos Uno)

```razor
@page "/users/manage"

<AuthorizeView Permissions="@(new[] {
    "Permissions.Users.View",
    "Permissions.Users.Update"
})">
    <!-- Visible si tiene VIEW O UPDATE -->
</AuthorizeView>
```

### Múltiples Permisos (AND - Todos Requeridos)

```razor
@page "/users/advanced"

<AuthorizeView 
    Permissions="@(new[] {
        "Permissions.Users.View",
        "Permissions.Users.Update",
        "Permissions.Users.Delete"
    })"
    RequireAll="true">
    <!-- Visible solo si tiene los 3 permisos -->
</AuthorizeView>
```

## ?? Componentes del Sistema

### 1. `AuthorizeView` - Wrapper Simple (Playground)

**Ubicación**: `Playground.Blazor/Components/Shared/AuthorizeView.razor`

```razor
<!-- Uso súper simple -->
<AuthorizeView Permission="Permissions.Roles.View">
    <!-- Contenido -->
</AuthorizeView>
```

**Características:**
- ? Inyecta automáticamente `IPermissionService`
- ? No necesitas inyectar nada manualmente
- ? Sintaxis más corta

### 2. `AuthorizePermission` - Componente Base (Building Blocks)

**Ubicación**: `BuildingBlocks/Blazor.UI/Authorization/AuthorizePermission.razor`

```razor
<!-- Uso avanzado con control total -->
<AuthorizePermission 
    Permission="..."
    HasPermissionAsync="@MyCustomPermissionCheck">
    <!-- Contenido -->
</AuthorizePermission>
```

**Características:**
- ? Reutilizable en otros proyectos
- ? No depende de `IPermissionService` específico
- ? Recibe función delegate para verificar permisos

## ?? Ejemplos Completos

### Ejemplo 1: RolesPage (Implementado)

```razor
@page "/roles"
@using FSH.Playground.Blazor.ApiClient
@using FSH.Framework.Blazor.UI.Components.Dialogs
@inherits ComponentBase
@inject IIdentityClient IdentityClient
@inject NavigationManager Navigation
@inject ISnackbar Snackbar
@inject IDialogService DialogService

<AuthorizeView Permission="Permissions.Roles.View">
    <FshPageHeader Title="Roles" Description="Manage roles and their permissions">
        <ActionContent>
            <MudButton Variant="Variant.Filled"
                       Color="Color.Primary"
                       StartIcon="@Icons.Material.Filled.Add"
                       OnClick="ShowCreate">
                New Role
            </MudButton>
        </ActionContent>
    </FshPageHeader>

    <MudGrid Class="mb-4">
        <!-- Stats cards -->
    </MudGrid>

    <MudDataGrid T="RoleDto" Items="@_roles">
        <!-- Grid -->
    </MudDataGrid>
</AuthorizeView>

@code {
    private List<RoleDto> _roles = new();
    // ...código...
}
```

### Ejemplo 2: UsersPage con Permisos Alternativos

```razor
@page "/users"

<!-- Usuario puede tener View O Search -->
<AuthorizeView Permissions="@(new[] {
    "Permissions.Users.View",
    "Permissions.Users.Search"
})">
    <FshPageHeader Title="Users" />
    
    <MudDataGrid T="UserDto" Items="@_users">
        <!-- Grid -->
    </MudDataGrid>
</AuthorizeView>
```

### Ejemplo 3: Secciones con Diferentes Permisos

```razor
@page "/dashboard"

<!-- Sección 1: View -->
<AuthorizeView Permission="Permissions.Dashboard.View">
    <MudPaper Class="mb-4 pa-4">
        <h3>Dashboard Overview</h3>
        <p>Stats generales...</p>
    </MudPaper>
</AuthorizeView>

<!-- Sección 2: Admin + Reporting (ambos requeridos) -->
<AuthorizeView 
    Permissions="@(new[] {
        "Permissions.Dashboard.Admin",
        "Permissions.Reports.Generate"
    })"
    RequireAll="true">
    <MudPaper Class="mb-4 pa-4">
        <h3>Admin Panel</h3>
        <p>Solo para admins con reporting...</p>
    </MudPaper>
</AuthorizeView>

<!-- Sección 3: Cualquier permiso de análisis -->
<AuthorizeView Permissions="@(new[] {
    "Permissions.Analytics.View",
    "Permissions.Analytics.Export",
    "Permissions.Reports.View"
})">
    <MudPaper Class="pa-4">
        <h3>Analytics</h3>
        <p>Con cualquier permiso de análisis...</p>
    </MudPaper>
</AuthorizeView>
```

## ?? UI de Access Denied

Pantalla que se muestra cuando el usuario no tiene permisos:

```
????????????????????????????????????????????
?              ?? Block Icon               ?
?                                          ?
?           Access Denied                  ?
?                                          ?
?   You don't have the required            ?
?   permissions to access this page.       ?
?                                          ?
?   ??  Required Permissions (any):        ?
?      • View Roles                        ?
?      • Update Roles                      ?
?                                          ?
?          [? Go Back]                     ?
????????????????????????????????????????????
```

## ?? Flujo de Autorización

```
Usuario navega a /roles
  ?
AuthorizeView renderiza AuthorizePermission
  ?
AuthorizePermission.OnInitializedAsync()
  ?
1. Verificar autenticación
   ?? No autenticado ? Redirect /login
   ?? Autenticado ? Continuar
  ?
2. Obtener función HasPermissionAsync
   (inyectada por AuthorizeView)
  ?
3. Verificar permisos requeridos
   ?? RequireAll = true (AND)
   ?  ?? Todas las llamadas a HasPermissionAsync deben ser true
   ?
   ?? RequireAll = false (OR, default)
      ?? Al menos una llamada a HasPermissionAsync debe ser true
  ?
4. HasPermissionAsync consulta PermissionService
   ?? Cache hit? ? Retorna desde memoria
   ?? Cache miss? ? GET /api/v1/identity/permissions
  ?
5. Renderizar resultado
   ?? Autorizado ?  ? Render @ChildContent
   ?? Denegado ?   ? Render "Access Denied" UI
```

## ?? Parámetros

### AuthorizeView (Simple)

| Parámetro | Tipo | Descripción | Ejemplo |
|-----------|------|-------------|---------|
| `Permission` | `string` | Permiso único requerido | `"Permissions.Users.View"` |
| `Permissions` | `string[]` | Array de permisos | `new[] { "Perm1", "Perm2" }` |
| `RequireAll` | `bool` | Si `true`, requiere TODOS (AND). Si `false` (default), AL MENOS UNO (OR) | `true` o `false` |

### AuthorizePermission (Avanzado)

Mismos parámetros + `HasPermissionAsync`:

| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `HasPermissionAsync` | `Func<string, Task<bool>>` | Función para verificar permiso |

## ?? Mejores Prácticas

### ? DO: Usar AuthorizeView para Simplicidad

```razor
<!-- ? BUENO: Simple y limpio -->
<AuthorizeView Permission="Permissions.Users.View">
```

```razor
<!-- ?? VERBOSO: Usa AuthorizeView en su lugar -->
@inject IPermissionService PermissionService
<AuthorizePermission HasPermissionAsync="@PermissionService.HasPermissionAsync">
```

### ? DO: Envolver TODO el Contenido

```razor
<AuthorizeView Permission="...">
    <!-- TODO aquí -->
    <FshPageHeader ... />
    <MudGrid>...</MudGrid>
    <MudDataGrid>...</MudDataGrid>
</AuthorizeView>
```

### ? DO: Usar OR para Alternativas

```razor
<!-- User puede tener cualquiera -->
<AuthorizeView Permissions="@(new[] {
    "Permissions.Users.View",
    "Permissions.Users.Search"
})">
```

### ? DO: Usar AND para Combinaciones

```razor
<!-- User DEBE tener ambos -->
<AuthorizeView 
    Permissions="@(new[] {
        "Permissions.Users.Update",
        "Permissions.Users.Delete"
    })"
    RequireAll="true">
```

### ?? CONSIDERA: Constants vs Strings

```razor
<!-- Si tienes acceso al módulo Identity -->
@using FSH.Modules.Identity.Authorization
<AuthorizeView Permission="@IdentityPermissionConstants.Names.Users.View">

<!-- Si NO tienes acceso (más común en Blazor) -->
<AuthorizeView Permission="Permissions.Users.View">
```

## ?? Migración de Páginas

### Antes (Sin Protección)

```razor
@page "/users"

<FshPageHeader Title="Users" />
<MudDataGrid T="UserDto" Items="@_users" />
```

### Después (Con Protección) - 2 Pasos

```razor
@page "/users"

<AuthorizeView Permission="Permissions.Users.View">
    <FshPageHeader Title="Users" />
    <MudDataGrid T="UserDto" Items="@_users" />
</AuthorizeView>
```

**Cambios:**
1. Envolver en `<AuthorizeView>`
2. Especificar `Permission`

¡Eso es todo! No necesitas `@inject`, `@using`, ni nada más.

## ?? Archivos del Sistema

### Building Blocks (Reutilizable)

1. **`BuildingBlocks/Blazor.UI/Authorization/AuthorizePermission.razor`**
   - Componente base genérico
   - Recibe delegate `HasPermissionAsync`
   - Reutilizable en cualquier proyecto

2. **`BuildingBlocks/Blazor.UI/Authorization/RequirePermissionsAttribute.cs`**
   - Atributo para metadata (opcional)

3. **`BuildingBlocks/Blazor.UI/Authorization/PageAuthorizationHelper.cs`**
   - Helpers para leer atributos

### Playground (App-Specific)

4. **`Playground.Blazor/Components/Shared/AuthorizeView.razor`**
   - Wrapper simple
   - Inyecta `IPermissionService` automáticamente
   - Uso recomendado en Playground

5. **`Playground.Blazor/Services/PermissionService.cs`**
   - Implementación de consulta API
   - Cache en memoria

## ? Ventajas

### 1. **Súper Simple**
```razor
<!-- Una línea para proteger -->
<AuthorizeView Permission="Permissions.Roles.View">
```

### 2. **Flexible**
```razor
<!-- Un permiso -->
<AuthorizeView Permission="..." />

<!-- Varios con OR -->
<AuthorizeView Permissions="@(new[] { ... })" />

<!-- Varios con AND -->
<AuthorizeView Permissions="@(new[] { ... })" RequireAll="true" />
```

### 3. **Eficiente**
```
GET /api/v1/identity/permissions (solo una vez)
?
Cache en memoria
?
Todas las verificaciones usan cache
? Solo 1 request por sesión
```

### 4. **UX Profesional**
- ? Pantalla "Access Denied" clara
- ? Lista de permisos requeridos
- ? Botón "Go Back"

### 5. **Reutilizable**
- ? `AuthorizePermission` en BuildingBlocks ? Cualquier proyecto
- ? `AuthorizeView` en Playground ? App-specific wrapper

## ?? Resumen

**Pediste:**
1. ? Múltiples permisos ? Soporte completo
2. ? Lógica OR (al menos uno) ? `RequireAll="false"` (default)
3. ? Lógica AND (todos) ? `RequireAll="true"`

**Implementado:**

### Uso Simple (Recomendado)
```razor
<AuthorizeView Permission="Permissions.Roles.View">
    <!-- Contenido protegido -->
</AuthorizeView>
```

### Uso con Múltiples Permisos
```razor
<!-- OR: Al menos uno -->
<AuthorizeView Permissions="@(new[] { "Perm1", "Perm2" })">

<!-- AND: Todos requeridos -->
<AuthorizeView 
    Permissions="@(new[] { "Perm1", "Perm2" })" 
    RequireAll="true">
```

**Sistema:**
- ? Consulta permisos via API
- ? Cache eficiente
- ? UI elegante
- ? 2 componentes (base + wrapper)
- ? Compilación exitosa

**Próximos pasos:**
1. Agregar `<AuthorizeView>` a tus páginas
2. Especificar `Permission` o `Permissions`
3. Opcionalmente usar `RequireAll="true"` para AND
