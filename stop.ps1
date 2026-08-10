#requires -Version 5.1
<#
.SYNOPSIS
    Detiene los contenedores de la base de datos.

.PARAMETER Volumes
    Adicionalmente borra el volumen de datos (se pierde toda la informacion).

.EXAMPLE
    .\stop.ps1
    .\stop.ps1 -Volumes
#>
param(
    [switch]$Volumes
)

$ErrorActionPreference = 'Stop'
Set-Location -Path $PSScriptRoot

if ($Volumes) {
    Write-Host "Deteniendo contenedores y BORRANDO datos..." -ForegroundColor Yellow
    docker compose down -v
} else {
    Write-Host "Deteniendo contenedores (los datos se conservan)..." -ForegroundColor Yellow
    docker compose down
}

Write-Host "Listo." -ForegroundColor Green
