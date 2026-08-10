#requires -Version 5.1
<#
.SYNOPSIS
    Ejecuta database/init-cloud.sql contra la BD REMOTA del hosting (MonsterASP).
    Usa el contenedor de SQL Server 2022 que ya tenes descargado para correr sqlcmd.

.DESCRIPTION
    1. Lee credenciales de deploy.env (busca DB_HOST_REMOTE, no DB_HOST)
    2. Lanza un contenedor temporal con la imagen de SQL Server 2022
    3. Monta la carpeta database/ como /scripts
    4. Ejecuta sqlcmd con el script init-cloud.sql
    5. El contenedor se autodestruye al terminar (--rm)

    Esto evita tener que instalar SSMS o Azure Data Studio.
    Requiere Docker Desktop corriendo.

.EXAMPLE
    .\init-remote-db.ps1
#>

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  Inicializando BD REMOTA via Docker (sqlcmd)" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

# Verificar deploy.env
if (-not (Test-Path 'deploy.env')) {
    Write-Host "[ERROR] No existe deploy.env" -ForegroundColor Red
    Write-Host "  Copia deploy.env.example a deploy.env y completa con tus datos."
    exit 1
}

# Cargar variables de deploy.env
$env_vars = @{}
Get-Content 'deploy.env' | ForEach-Object {
    if ($_ -match '^\s*([^#=]+?)\s*=\s*(.+?)\s*$') {
        $env_vars[$matches[1]] = $matches[2]
    }
}

# Necesitamos el host PUBLICO (el que termina en .public.databaseasp.net)
$remoteHost = $env_vars['DB_HOST_REMOTE']
if ([string]::IsNullOrWhiteSpace($remoteHost)) {
    Write-Host "[ERROR] Falta DB_HOST_REMOTE en deploy.env" -ForegroundColor Red
    Write-Host "  Agregala con el host de 'Remote access for SSMS' del panel."
    Write-Host "  Ejemplo: DB_HOST_REMOTE=db52546.public.databaseasp.net,1433"
    exit 1
}

foreach ($key in @('DB_NAME', 'DB_USER', 'DB_PASSWORD')) {
    if ([string]::IsNullOrWhiteSpace($env_vars[$key])) {
        Write-Host "[ERROR] Falta variable $key en deploy.env" -ForegroundColor Red
        exit 1
    }
}

# Verificar Docker
try {
    docker info *> $null
    if ($LASTEXITCODE -ne 0) { throw "Docker no responde" }
} catch {
    Write-Host "[ERROR] Docker Desktop no esta corriendo. Iniciarlo y reintentar." -ForegroundColor Red
    exit 1
}

# Verificar que existe el script
$scriptPath = "database\init-cloud.sql"
if (-not (Test-Path $scriptPath)) {
    Write-Host "[ERROR] No existe $scriptPath" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Server : $remoteHost" -ForegroundColor Gray
Write-Host "DB     : $($env_vars['DB_NAME'])" -ForegroundColor Gray
Write-Host "User   : $($env_vars['DB_USER'])" -ForegroundColor Gray
Write-Host "Script : $scriptPath" -ForegroundColor Gray
Write-Host ""

# Ruta absoluta de la carpeta database para montar en Docker
$dbDir = (Resolve-Path 'database').Path

Write-Host "Lanzando contenedor temporal con sqlcmd..." -ForegroundColor Yellow
Write-Host ""

# Comando docker run:
#   --rm                          contenedor se borra al terminar
#   -v "$dbDir:/scripts:ro"       monta tu carpeta database como /scripts (read-only)
#   mssql/server:2022-latest      la imagen que ya tenes descargada
#   /opt/mssql-tools18/bin/sqlcmd la herramienta cliente
#   -S host                       servidor
#   -U user -P password           credenciales
#   -d database                   base por defecto
#   -C                            trust server certificate (necesario por SSL)
#   -i /scripts/init-cloud.sql    el script a ejecutar

docker run --rm `
    -v "${dbDir}:/scripts:ro" `
    mcr.microsoft.com/mssql/server:2022-latest `
    /opt/mssql-tools18/bin/sqlcmd `
        -S "$remoteHost" `
        -U "$($env_vars['DB_USER'])" `
        -P "$($env_vars['DB_PASSWORD'])" `
        -d "$($env_vars['DB_NAME'])" `
        -C `
        -N `
        -l 30 `
        -i /scripts/init-cloud.sql

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "============================================================" -ForegroundColor Green
    Write-Host "  BD REMOTA inicializada correctamente." -ForegroundColor Green
    Write-Host "============================================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Las 8 tablas (Usuario, Categoria, Presupuesto, etc.) ya existen en MonsterASP."
    Write-Host "Las categorias semilla tambien fueron insertadas."
    Write-Host ""
    Write-Host "Proximo paso: .\publish.ps1"
} else {
    Write-Host ""
    Write-Host "[ERROR] sqlcmd fallo. Revisa los mensajes de arriba." -ForegroundColor Red
    Write-Host ""
    Write-Host "Causas comunes:"
    Write-Host "  - Tu IP no esta autorizada en MonsterASP (whitelist en el panel)"
    Write-Host "  - DB_HOST_REMOTE incorrecto (debe ser '...public.databaseasp.net,1433')"
    Write-Host "  - Password tiene caracteres especiales sin escapar"
    exit 1
}
