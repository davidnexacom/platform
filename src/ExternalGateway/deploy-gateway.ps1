# Script de despliegue del External Gateway para clientes

param(
    [Parameter(Mandatory=$true)]
    [string]$ClientId,
    
    [Parameter(Mandatory=$true)]
    [string]$SqlServer,
    
    [Parameter(Mandatory=$true)]
    [string]$Database,
    
    [Parameter(Mandatory=$true)]
    [string]$SqlUser,
    
    [Parameter(Mandatory=$true)]
    [string]$SqlPassword,
    
    [Parameter(Mandatory=$true)]
    [ValidateSet("v1", "v2")]
    [string]$SchemaVersion,
    
    [Parameter(Mandatory=$false)]
    [string]$RabbitMqServer = "localhost",
    
    [Parameter(Mandatory=$false)]
    [string]$InstallPath = "C:\FSH\Gateway"
)

Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?  FSH External Gateway - Deployment Script               ?" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

# 1. Validar requisitos
Write-Host "?? Validando requisitos..." -ForegroundColor Yellow

if (-not (Test-Path "ExternalGateway/FSH.ExternalGateway.Host/FSH.ExternalGateway.Host.csproj")) {
    Write-Host "? No se encontró el proyecto del Gateway" -ForegroundColor Red
    exit 1
}

Write-Host "? Proyecto encontrado" -ForegroundColor Green

# 2. Publicar aplicación
Write-Host "`n?? Publicando aplicación..." -ForegroundColor Yellow

$publishPath = Join-Path $InstallPath $ClientId
dotnet publish ExternalGateway/FSH.ExternalGateway.Host/FSH.ExternalGateway.Host.csproj `
    -c Release `
    -o $publishPath `
    --self-contained false

if ($LASTEXITCODE -ne 0) {
    Write-Host "? Error al publicar la aplicación" -ForegroundColor Red
    exit 1
}

Write-Host "? Aplicación publicada en: $publishPath" -ForegroundColor Green

# 3. Crear configuración
Write-Host "`n??  Creando configuración..." -ForegroundColor Yellow

$connectionString = "Server=$SqlServer;Database=$Database;User Id=$SqlUser;Password=$SqlPassword;TrustServerCertificate=True"
$queueName = "fsh.gateway.$ClientId.queue"

$config = @{
    Serilog = @{
        MinimumLevel = @{
            Default = "Information"
            Override = @{
                Microsoft = "Warning"
                System = "Warning"
                Rebus = "Information"
            }
        }
    }
    GatewayOptions = @{
        ClientId = $ClientId
        SchemaVersion = $SchemaVersion
        ConnectionString = $connectionString
        DefaultTimeoutSeconds = 30
        EnableQueryLogging = $true
    }
    RabbitMqOptions = @{
        ConnectionString = "amqp://admin:Admin123!@${RabbitMqServer}:5672"
        QueueName = $queueName
        MaxConcurrentMessages = 10
        RetryAttempts = 3
    }
}

$configPath = Join-Path $publishPath "appsettings.Production.json"
$config | ConvertTo-Json -Depth 10 | Set-Content $configPath

Write-Host "? Configuración creada: $configPath" -ForegroundColor Green

# 4. Crear servicio de Windows
Write-Host "`n?? Instalando como servicio de Windows..." -ForegroundColor Yellow

$serviceName = "FSH.Gateway.$ClientId"
$serviceDisplayName = "FSH External Gateway - $ClientId"
$serviceDescription = "Gateway de acceso a SQL Server para cliente $ClientId (Schema $SchemaVersion)"
$exePath = Join-Path $publishPath "FSH.ExternalGateway.Host.exe"

# Detener y eliminar servicio existente si existe
$existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "??  Servicio existente encontrado, eliminando..." -ForegroundColor Yellow
    Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
    sc.exe delete $serviceName
    Start-Sleep -Seconds 2
}

# Crear nuevo servicio
$createResult = sc.exe create $serviceName `
    binPath="$exePath --environment=Production" `
    start=auto `
    DisplayName="$serviceDisplayName"

if ($LASTEXITCODE -ne 0) {
    Write-Host "? Error al crear el servicio" -ForegroundColor Red
    exit 1
}

# Configurar descripción
sc.exe description $serviceName "$serviceDescription"

# Configurar recovery options (reiniciar en caso de fallo)
sc.exe failure $serviceName reset=86400 actions=restart/60000/restart/60000/restart/60000

Write-Host "? Servicio instalado: $serviceName" -ForegroundColor Green

# 5. Iniciar servicio
Write-Host "`n?? Iniciando servicio..." -ForegroundColor Yellow

Start-Service -Name $serviceName

$service = Get-Service -Name $serviceName
if ($service.Status -eq 'Running') {
    Write-Host "? Servicio iniciado correctamente" -ForegroundColor Green
} else {
    Write-Host "??  El servicio fue instalado pero no está corriendo" -ForegroundColor Yellow
    Write-Host "   Intente iniciarlo manualmente: Start-Service $serviceName" -ForegroundColor Yellow
}

# 6. Resumen
Write-Host "`n?????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?  Despliegue Completado                                   ?" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""
Write-Host "?? Información del Despliegue:" -ForegroundColor White
Write-Host "   Cliente ID:      $ClientId" -ForegroundColor Gray
Write-Host "   Schema Version:  $SchemaVersion" -ForegroundColor Gray
Write-Host "   Ruta:           $publishPath" -ForegroundColor Gray
Write-Host "   Servicio:       $serviceName" -ForegroundColor Gray
Write-Host "   Queue:          $queueName" -ForegroundColor Gray
Write-Host ""
Write-Host "?? Comandos útiles:" -ForegroundColor White
Write-Host "   Detener:        Stop-Service $serviceName" -ForegroundColor Gray
Write-Host "   Iniciar:        Start-Service $serviceName" -ForegroundColor Gray
Write-Host "   Estado:         Get-Service $serviceName" -ForegroundColor Gray
Write-Host "   Logs:           Get-EventLog -LogName Application -Source $serviceName -Newest 50" -ForegroundColor Gray
Write-Host ""
Write-Host "? Gateway listo para recibir mensajes de RabbitMQ" -ForegroundColor Green
