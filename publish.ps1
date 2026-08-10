#requires -Version 5.1
<#
.SYNOPSIS
    Prepara el proyecto para deploy a hosting (MonsterASP, Somee, etc.)

.DESCRIPTION
    1. Lee credenciales de deploy.env
    2. Compila los assets de frontend (npm run build)
    3. Inyecta la cadena de conexion en Web.Release.config
    4. Compila el proyecto en Release con MSBuild
    5. Empaqueta el sitio listo para subir en .\publish\

    Despues de correr este script: subi el contenido de .\publish\ via FTP a tu hosting.

.EXAMPLE
    .\publish.ps1
#>

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  Gestor Financiero - Pipeline de Deploy" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

# Paso 1: Verificar que existe deploy.env
if (-not (Test-Path 'deploy.env')) {
    Write-Host "[ERROR] No existe deploy.env" -ForegroundColor Red
    Write-Host "  Copia deploy.env.example a deploy.env y completa tus datos."
    exit 1
}

# Cargar variables de deploy.env
$env_vars = @{}
Get-Content 'deploy.env' | ForEach-Object {
    if ($_ -match '^\s*([^#=]+?)\s*=\s*(.+?)\s*$') {
        $env_vars[$matches[1]] = $matches[2]
    }
}

foreach ($key in @('DB_HOST', 'DB_NAME', 'DB_USER', 'DB_PASSWORD')) {
    if (-not $env_vars.ContainsKey($key) -or [string]::IsNullOrWhiteSpace($env_vars[$key])) {
        Write-Host "[ERROR] Falta variable $key en deploy.env" -ForegroundColor Red
        exit 1
    }
}

# Paso 2: Compilar assets de frontend
Write-Host ""
Write-Host "[1/4] Compilando assets de frontend (Tailwind, lucide, etc.)..." -ForegroundColor Yellow
npm run build
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] Fallo el build de assets. Verifica que tengas Node.js instalado." -ForegroundColor Red
    exit 1
}

# Paso 3: Inyectar credenciales en Web.Release.config (reemplaza placeholders)
Write-Host ""
Write-Host "[2/4] Inyectando cadena de conexion en Web.Release.config..." -ForegroundColor Yellow

$releaseConfig = Get-Content 'Web.Release.config' -Raw
$releaseConfig = $releaseConfig.Replace('__DB_HOST__',     $env_vars['DB_HOST'])
$releaseConfig = $releaseConfig.Replace('__DB_NAME__',     $env_vars['DB_NAME'])
$releaseConfig = $releaseConfig.Replace('__DB_USER__',     $env_vars['DB_USER'])
$releaseConfig = $releaseConfig.Replace('__DB_PASSWORD__', $env_vars['DB_PASSWORD'])

# Guarda en archivo temporal (NO sobreescribimos el template con placeholders)
$tmpReleaseConfig = "Web.Release.config.tmp"
Set-Content -Path $tmpReleaseConfig -Value $releaseConfig -Encoding UTF8

# Hacemos un swap temporal: backup del original, usar el inyectado, build, restaurar
Copy-Item 'Web.Release.config' 'Web.Release.config.bak' -Force
Copy-Item $tmpReleaseConfig 'Web.Release.config' -Force

try {
    # Paso 4: MSBuild Release publish a carpeta
    Write-Host ""
    Write-Host "[3/4] Compilando proyecto en Release..." -ForegroundColor Yellow

    $msbuild = Get-ChildItem "C:\Program Files\Microsoft Visual Studio\*\*\MSBuild\Current\Bin\MSBuild.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $msbuild) {
        $msbuild = Get-ChildItem "C:\Program Files (x86)\Microsoft Visual Studio\*\*\MSBuild\Current\Bin\MSBuild.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
    }
    if (-not $msbuild) {
        Write-Host "[ERROR] No se encontro MSBuild. Asegurate de tener Visual Studio instalado." -ForegroundColor Red
        exit 1
    }

    $publishDir = Join-Path $PSScriptRoot 'publish'
    if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

    & $msbuild.FullName gestor_financiero.csproj `
        /p:Configuration=Release `
        /p:DeployOnBuild=true `
        /p:WebPublishMethod=FileSystem `
        /p:publishUrl=$publishDir `
        /p:DeleteExistingFiles=true `
        /verbosity:minimal

    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] Fallo MSBuild" -ForegroundColor Red
        exit 1
    }

    # Copiar assets generados por npm (Tailwind, Lucide, Inter, Chart.js).
    # MSBuild no los incluye automaticamente porque no estan en el .csproj.
    Write-Host ""
    Write-Host "Copiando assets de npm al publish folder..." -ForegroundColor Yellow

    New-Item -ItemType Directory -Path "$publishDir\Content\css"   -Force | Out-Null
    New-Item -ItemType Directory -Path "$publishDir\Content\fonts" -Force | Out-Null
    New-Item -ItemType Directory -Path "$publishDir\Scripts"       -Force | Out-Null

    if (Test-Path 'Content\css') {
        Copy-Item 'Content\css\*' "$publishDir\Content\css\" -Recurse -Force
    }
    if (Test-Path 'Content\fonts') {
        Copy-Item 'Content\fonts\*' "$publishDir\Content\fonts\" -Recurse -Force
    }
    if (Test-Path 'Scripts\chart.umd.min.js') {
        Copy-Item 'Scripts\chart.umd.min.js' "$publishDir\Scripts\" -Force
    }
}
finally {
    # Restaurar Web.Release.config original (con placeholders)
    Move-Item 'Web.Release.config.bak' 'Web.Release.config' -Force
    Remove-Item $tmpReleaseConfig -ErrorAction SilentlyContinue
}

# Paso 5: Verificar y mostrar resumen
Write-Host ""
Write-Host "[4/4] Verificando archivos generados..." -ForegroundColor Yellow

$expectedFiles = @(
    'publish\bin\gestor_financiero.dll',
    'publish\Content\css\app.css',
    'publish\Content\css\lucide.css',
    'publish\Scripts\chart.umd.min.js',
    'publish\Web.config'
)

$missing = @()
foreach ($f in $expectedFiles) {
    if (-not (Test-Path $f)) { $missing += $f }
}

if ($missing.Count -gt 0) {
    Write-Host "[ADVERTENCIA] Faltan archivos esperados:" -ForegroundColor Yellow
    $missing | ForEach-Object { Write-Host "    - $_" }
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "  LISTO. Archivos en: .\publish\" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Proximos pasos:"
Write-Host "  1. Asegurate de haber ejecutado database\init-cloud.sql en tu BD del hosting"
Write-Host "  2. Sube TODO el contenido de .\publish\ via FTP a tu hosting:"
Write-Host "       Host:     $($env_vars['FTP_HOST'])"
Write-Host "       Usuario:  $($env_vars['FTP_USER'])"
Write-Host "       Path:     $($env_vars['FTP_REMOTE_PATH'])"
Write-Host "  3. Visita: $($env_vars['APP_URL'])"
Write-Host ""
