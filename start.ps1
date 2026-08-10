#requires -Version 5.1
<#
.SYNOPSIS
    Levanta la base de datos en Docker y abre la solucion en Visual Studio.

.DESCRIPTION
    Inicia el contenedor SQL Server con docker compose, espera a que el script
    de inicializacion termine, y opcionalmente abre la solucion en VS.

.PARAMETER NoVS
    No abrir Visual Studio al terminar.

.EXAMPLE
    .\start.ps1
    .\start.ps1 -NoVS
#>
param(
    [switch]$NoVS
)

$ErrorActionPreference = 'Stop'
Set-Location -Path $PSScriptRoot

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  gestorFinanciero - Iniciando base de datos en Docker" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

# Verificar que Docker esta corriendo
try {
    docker info *> $null
    if ($LASTEXITCODE -ne 0) { throw "Docker no responde" }
} catch {
    Write-Host "[ERROR] Docker Desktop no esta corriendo. Iniciarlo y reintentar." -ForegroundColor Red
    exit 1
}

# Levantar los contenedores
Write-Host "[1/3] Levantando contenedores..." -ForegroundColor Yellow
docker compose up -d db
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] Fallo al levantar el contenedor de SQL Server." -ForegroundColor Red
    exit 1
}

# Esperar a que SQL Server este healthy
Write-Host "[2/3] Esperando a que SQL Server este listo (puede tardar ~30s en el primer arranque)..." -ForegroundColor Yellow
$maxWait = 90
$elapsed = 0
while ($elapsed -lt $maxWait) {
    $status = docker inspect --format='{{.State.Health.Status}}' finanzas-db 2>$null
    if ($status -eq 'healthy') { break }
    Start-Sleep -Seconds 2
    $elapsed += 2
    Write-Host "    ... esperando ($elapsed s)" -ForegroundColor DarkGray
}
if ($status -ne 'healthy') {
    Write-Host "[ERROR] SQL Server no quedo listo en $maxWait segundos." -ForegroundColor Red
    docker compose logs db --tail 50
    exit 1
}

# Ejecutar inicializacion
Write-Host "[3/3] Ejecutando script de inicializacion..." -ForegroundColor Yellow
docker compose up db-init --exit-code-from db-init
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] Fallo la inicializacion de la base de datos." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "  Base de datos LISTA" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host "  Servidor : localhost,1433"
Write-Host "  Usuario  : sa"
Write-Host "  Password : Finanzas2026!  (configurable en .env)"
Write-Host "  Database : FinanzasPersonales"
Write-Host ""
Write-Host "  Conectarse con SSMS o Azure Data Studio usando estos datos."
Write-Host "  Para detener: .\stop.ps1"
Write-Host ""

# Abrir Visual Studio
if (-not $NoVS) {
    $sln = Join-Path $PSScriptRoot 'gestor_financiero.sln'
    if (Test-Path $sln) {
        Write-Host "Abriendo Visual Studio..." -ForegroundColor Cyan
        Start-Process -FilePath $sln
    }
}
