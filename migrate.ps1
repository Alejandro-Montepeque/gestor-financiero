# =============================================================
# migrate.ps1 - Aplica las migraciones SQL pendientes a la BD local.
#
# Replica la logica que corre el workflow de GitHub Actions:
#   1. Crea la tabla _AppliedMigrations si no existe.
#   2. Lee todos los archivos database/migration_*.sql.
#   3. Para cada uno:
#        - Si ya esta registrado en _AppliedMigrations  -> lo salta.
#        - Si no  -> lo ejecuta y lo registra.
#
# Uso:
#   .\migrate.ps1                  -> usa los defaults de Docker
#   .\migrate.ps1 -Status          -> solo lista, NO aplica nada
#   .\migrate.ps1 -Container otra  -> apunta a otro contenedor
#
# Si tenes problemas con la ExecutionPolicy de PowerShell,
# usa el wrapper migrate.bat.
# =============================================================

param(
    [string]$Container  = "finanzas-db",
    [string]$Server     = "localhost",
    [string]$User       = "sa",
    [string]$Pass       = "Finanzas2026!",
    [string]$Database   = "FinanzasPersonales",
    [switch]$Status
)

$ErrorActionPreference = "Stop"

function Test-Container {
    $running = docker ps --format "{{.Names}}" 2>$null | Where-Object { $_ -eq $Container }
    if (-not $running) {
        Write-Host "ERROR: el contenedor '$Container' no esta corriendo." -ForegroundColor Red
        Write-Host "Levantalo con: .\start.ps1" -ForegroundColor Yellow
        exit 1
    }
}

function Invoke-Sql {
    param([string]$Sql)
    docker exec -i $Container /opt/mssql-tools18/bin/sqlcmd `
        -S $Server -U $User -P $Pass -d $Database -C -b -Q $Sql
    return $LASTEXITCODE
}

function Invoke-SqlFile {
    param([string]$LocalPath)
    $remoteName = "/tmp/migrate_$(Get-Random).sql"

    Write-Host "    [debug] copiando archivo al contenedor..." -ForegroundColor DarkGray
    $cpOut = docker cp $LocalPath "${Container}:${remoteName}" 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "    [debug] docker cp fallo: $cpOut" -ForegroundColor Red
        return 1
    }

    Write-Host "    [debug] chmod 644..." -ForegroundColor DarkGray
    docker exec --user root $Container chmod 644 $remoteName 2>&1 | Out-String | Write-Host -ForegroundColor DarkGray

    Write-Host "    [debug] ejecutando sqlcmd... (salida abajo)" -ForegroundColor DarkGray
    Write-Host "    ----------------------------------------------" -ForegroundColor DarkGray

    # Capturamos stdout + stderr y los volcamos a consola para diagnosticar
    $output = docker exec $Container /opt/mssql-tools18/bin/sqlcmd `
        -S $Server -U $User -P $Pass -d $Database -C -b -i $remoteName 2>&1
    $rc = $LASTEXITCODE
    $output | ForEach-Object { Write-Host "    $_" }

    Write-Host "    ----------------------------------------------" -ForegroundColor DarkGray
    Write-Host "    [debug] sqlcmd exit code = $rc" -ForegroundColor DarkGray

    docker exec --user root $Container rm $remoteName 2>&1 | Out-Null
    return $rc
}

function Invoke-SqlScalar {
    param([string]$Sql)
    $out = docker exec -i $Container /opt/mssql-tools18/bin/sqlcmd `
        -S $Server -U $User -P $Pass -d $Database -C -b -h -1 -W -Q $Sql
    $line = $out | Where-Object { $_ -and $_.Trim() -ne '' } | Select-Object -First 1
    if ($line) { return $line.Trim() } else { return "" }
}

# ---------- inicio ----------

Test-Container

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  Migraciones locales -> $Database en $Container" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

# 1. Bootstrap: tabla _AppliedMigrations
Write-Host "Verificando tabla _AppliedMigrations..." -NoNewline
$bootstrap = @"
IF OBJECT_ID('dbo._AppliedMigrations', 'U') IS NULL
CREATE TABLE _AppliedMigrations (
    nombre VARCHAR(200) NOT NULL PRIMARY KEY,
    aplicada_en DATETIME NOT NULL DEFAULT GETUTCDATE()
);
"@
$rc = Invoke-Sql $bootstrap
if ($rc -ne 0) {
    Write-Host " ERROR" -ForegroundColor Red
    exit 1
}
Write-Host " OK"

# 2. Listar migraciones
$migs = Get-ChildItem -Path "database" -Filter "migration_*.sql" -ErrorAction SilentlyContinue | Sort-Object Name
if (-not $migs) {
    Write-Host "No hay archivos migration_*.sql en database/." -ForegroundColor Yellow
    exit 0
}

Write-Host "Migraciones detectadas: $($migs.Count)"
Write-Host ""

$aplicadas = 0
$saltadas = 0

foreach ($m in $migs) {
    $nombre = $m.Name
    # SET NOCOUNT ON evita el "(1 rows affected)" que ensucia el output
    $countSql = "SET NOCOUNT ON; SELECT COUNT(*) FROM _AppliedMigrations WHERE nombre = '$nombre';"
    $existe = Invoke-SqlScalar $countSql

    if ($existe -eq '1') {
        Write-Host "  -- saltada : $nombre" -ForegroundColor DarkGray
        $saltadas++
        continue
    }

    if ($Status) {
        Write-Host "  -- pendiente: $nombre" -ForegroundColor Yellow
        continue
    }

    Write-Host "==> Aplicando $nombre" -ForegroundColor Cyan
    $rc = Invoke-SqlFile $m.FullName
    if ($rc -ne 0) {
        Write-Host "ERROR aplicando $nombre. Abortando." -ForegroundColor Red
        exit 1
    }

    $rc = Invoke-Sql "INSERT INTO _AppliedMigrations (nombre) VALUES ('$nombre');"
    if ($rc -ne 0) {
        Write-Host "ERROR registrando $nombre en _AppliedMigrations." -ForegroundColor Red
        exit 1
    }
    Write-Host "    registrada en _AppliedMigrations" -ForegroundColor Green
    $aplicadas++
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
if ($Status) {
    Write-Host "  Modo Status (no se aplico nada)" -ForegroundColor Yellow
} else {
    Write-Host "  Aplicadas: $aplicadas | Saltadas: $saltadas" -ForegroundColor Green
}
Write-Host "============================================================" -ForegroundColor Green
