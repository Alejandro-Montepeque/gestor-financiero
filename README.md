# Gestor Financiero

Sistema web para gestión de finanzas personales: control de ingresos, gastos, presupuestos mensuales, seguimiento de deudas con amortización, y reportes anuales. Pensado para que un usuario lleve su contabilidad personal de forma ordenada.

## Stack tecnológico

| Capa | Tecnología |
|---|---|
| Backend | ASP.NET MVC 5 (.NET Framework 4.7.2) |
| ORM | Entity Framework 6.4 (Code First con BD existente) |
| Base de datos | SQL Server 2022 (en contenedor Docker) |
| Autenticación | Forms Authentication + PBKDF2 (`System.Web.Helpers.Crypto`) |
| Frontend | Razor + Tailwind CSS 3.4 (compilado local) |
| Iconos | Lucide (font local, sin CDN) |
| Tipografía | Inter (woff2 local, sin CDN) |
| Gráficos | Chart.js 4.4 (bundle local) |
| Build tooling | Node.js + npm (solo para compilar assets) |
| Containerización | Docker Compose |
| PDF (opcional) | Rotativa o impresión del navegador con `@media print` |

## Prerrequisitos

Antes de empezar necesitas tener instalado en tu máquina:

1. **Visual Studio 2019 o 2022** (Community es suficiente) con la carga de trabajo *ASP.NET y desarrollo web*.
2. **.NET Framework 4.7.2 Developer Pack**.
3. **Docker Desktop** corriendo en tu máquina (con WSL2 backend o Hyper-V).
4. **PowerShell 5.1+** (viene con Windows 10/11).
5. **Node.js 18+ con npm** — solo para compilar los assets del frontend (Tailwind, Chart.js, Lucide, fuentes). Una vez compilados, IIS sirve los archivos estáticos y Node no se vuelve a necesitar en runtime. Descarga desde https://nodejs.org/

Opcionalmente, para inspeccionar la BD:
- **SQL Server Management Studio (SSMS)** o **Azure Data Studio**.

## Estructura del proyecto

```
gestorFinanciero/
├── App_Start/
│   ├── FilterConfig.cs        # Filtro global [Authorize]
│   └── RouteConfig.cs         # Rutas MVC
├── Controllers/
│   ├── AccountController.cs   # Login / Register / Logout
│   ├── HomeController.cs      # Dashboard
│   ├── UsuarioController.cs
│   ├── CategoriaController.cs
│   ├── PresupuestoController.cs
│   ├── PresupuestoDetalleController.cs
│   ├── TransaccionController.cs
│   ├── DeudaController.cs
│   ├── PagoDeudaController.cs
│   ├── ResumenAnualController.cs
│   └── ReportesController.cs
├── Models/
│   ├── FinanzasContext.cs     # DbContext de EF
│   ├── Usuario.cs
│   ├── Categoria.cs
│   ├── Presupuesto.cs
│   ├── PresupuestoDetalle.cs
│   ├── Transaccion.cs
│   ├── Deuda.cs
│   ├── PagoDeuda.cs
│   └── ResumenAnual.cs
├── ViewModels/
│   ├── AccountViewModels.cs   # Login / Register VMs
│   └── DashboardViewModel.cs
├── Views/
│   ├── _ViewStart.cshtml
│   ├── Shared/
│   │   ├── _Layout.cshtml     # Layout principal con sidebar
│   │   └── _Form*.cshtml      # Partials de formularios
│   ├── Account/               # Login, Register
│   ├── Home/                  # Dashboard
│   └── [Módulo]/              # Index, Create, Edit, Details, Delete
├── database/
│   └── init.sql               # Script de inicialización (lo corre Docker)
├── docker-compose.yml         # Define contenedor SQL Server
├── start.ps1                  # Arranca BD + abre Visual Studio
├── stop.ps1                   # Detiene contenedores
├── .env.example               # Plantilla de variables (copialo a .env)
└── Web.config                 # Cadena de conexión + Forms Auth
```

## Instalación paso a paso

Sigue los pasos **en este orden** la primera vez:

### 1. Clonar el repositorio

```powershell
git clone <url-del-repo>
cd gestorFinanciero
```

### 2. Configurar variables de entorno (opcional)

Si quieres usar una contraseña distinta a la predeterminada para el usuario `sa` de SQL Server:

```powershell
Copy-Item .env.example .env
notepad .env    # ajusta MSSQL_SA_PASSWORD si quieres
```

Si te saltas este paso, se usará `Finanzas2026!` por defecto.

### 3. Levantar la base de datos en Docker

```powershell
.\start.ps1
```

Esto hace todo lo siguiente automáticamente:

1. Verifica que Docker Desktop esté corriendo.
2. Descarga la imagen de SQL Server 2022 (~1.5 GB, **solo la primera vez**, tarda unos minutos).
3. Inicia el contenedor `finanzas-db` en `localhost:1433`.
4. Espera a que SQL Server reporte estado `healthy`.
5. Ejecuta `database/init.sql` que crea la BD, tablas, índices, constraints y siembra las categorías.
6. Abre la solución en Visual Studio.

Si todo sale bien verás:

```
============================================================
  Base de datos LISTA
============================================================
  Servidor : localhost,1433
  Usuario  : sa
  Password : Finanzas2026!
  Database : FinanzasPersonales
```

### 4. Instalar Entity Framework (solo la primera vez)

En Visual Studio, abre **Tools → NuGet Package Manager → Package Manager Console** y ejecuta:

```powershell
Install-Package EntityFramework -Version 6.4.4
```

### 4.5. Instalar y compilar los assets del frontend (solo la primera vez)

Desde una terminal de PowerShell o CMD parada en la raíz del proyecto:

```powershell
npm install
npm run build
```

Esto:
1. Descarga Tailwind CSS 3.4, Chart.js, Lucide y la fuente Inter a `node_modules/`.
2. Copia los archivos JS/woff2 necesarios a `Scripts/` y `Content/fonts/` con `build-assets.js`.
3. Compila el CSS de Tailwind con solo las clases que efectivamente usan las vistas (`tailwindcss -i Content/input.css -o Content/css/app.css --minify`).

El resultado: `Content/css/app.css` (~20-40 KB minificado, comparado con ~3 MB del CDN play). El sitio carga instantáneo.

**Durante el desarrollo**, si modificas las vistas y agregas clases nuevas de Tailwind, corre:

```powershell
npm run watch
```

Esto deja a Tailwind escuchando cambios y recompilando `app.css` automáticamente.

### 5. Compilar el proyecto

Presiona **Ctrl + Shift + B** o ve a **Build → Build Solution**. La primera compilación restaura los paquetes NuGet y puede tardar un minuto.

### 6. Correr la aplicación

Presiona **F5**. El navegador se abrirá en `https://localhost:44369` (puerto configurado en IIS Express) y serás redirigido a la pantalla de login.

### 7. Crear tu primera cuenta

1. Click en **"Regístrate"**.
2. Completa nombre, email y contraseña (mínimo 8 caracteres).
3. Al registrarte entras automáticamente al dashboard.

A partir de aquí ya puedes empezar a registrar transacciones, crear presupuestos, etc.

## Comandos útiles

| Acción | Comando |
|---|---|
| Levantar BD + abrir VS | `.\start.ps1` |
| Levantar BD sin abrir VS | `.\start.ps1 -NoVS` |
| Detener contenedores (conservar datos) | `.\stop.ps1` |
| Detener y **borrar todo** (empezar limpio) | `.\stop.ps1 -Volumes` |
| Ver logs del contenedor | `docker compose logs -f db` |
| Conectarse con sqlcmd | `docker exec -it finanzas-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Finanzas2026!' -C` |

## Módulos del sistema

### 1. Perfil de Usuario
Registro y login con email/contraseña. Las contraseñas se hashean con PBKDF2 (1000 iteraciones, salt aleatorio de 16 bytes) usando `System.Web.Helpers.Crypto`. El filtro global `[Authorize]` protege todas las rutas excepto `Account/Login` y `Account/Register`.

### 2. Categorías (Catálogos)
Gestión del catálogo de categorías. Cada categoría tiene un campo `Tipo` que determina su comportamiento en el resto del sistema:
- `INGRESO` (verde)
- `AHORRO` (azul)
- `GASTO_FIJO` (ámbar)
- `GASTO_VARIABLE` (rojo)
- `DEUDA` (violeta)

El listado se agrupa visualmente por tipo con iconos y colores consistentes.

### 3. Planificación Presupuestaria
Cabecera (`Presupuesto`) con año/mes + detalle (`PresupuestoDetalle`) por categoría. La vista de detalle muestra **barras de progreso comparando estimado vs real**, con colores reactivos:
- Verde si vas <80% del estimado
- Ámbar si vas 80-100%
- Rojo si te sobrepasaste

### 4. Registro de Transacciones
Formulario para registrar movimientos: usuario, categoría, monto, fecha y notas opcionales. El listado soporta filtros por usuario y rango de fechas.

### 5. Gestión y Amortización de Deudas
Tarjetas con saldo actual, tasa de interés, pago mínimo y notas. Cada deuda tiene su submódulo de pagos que separa contablemente:
- Pago mínimo + pago extra
- Cuánto se va a **interés** (no reduce deuda)
- Cuánto se va a **capital** (sí reduce deuda)
- Saldo restante después del pago

Al registrar un pago, el sistema actualiza automáticamente el `saldo_actual` de la deuda.

### 6. Dashboard y Analítica
Pantalla principal con:
- 4 KPIs del mes actual (ingresos, gastos, balance, deuda total)
- Gráfico de barras: ingresos vs gastos de los últimos 6 meses
- Gráfico doughnut: distribución de gastos por categoría del mes
- Listas de transacciones recientes y deudas activas

El módulo de Resumen Anual tiene su propio dashboard con gráficos comparativos multi-año.

### 7. Reportes
Cuatro reportes imprimibles:
- Transacciones (con filtro de fechas)
- Deudas (con totales pagados a capital/interés)
- Presupuesto (estimado vs real por categoría)
- Resumen Anual (multi-año)

Todos tienen estilos `@media print` que ocultan el sidebar al imprimir y se exportan a PDF directamente desde el botón **Imprimir → Guardar como PDF** del navegador.

Para PDFs generados server-side, instala Rotativa:
```powershell
Install-Package Rotativa -Version 1.7.3
```
Y descomenta las acciones marcadas como `// public ActionResult [...]Pdf(...)` en `ReportesController.cs`.

## Modelo de datos

```
Usuario (id_usuario PK)
  ├── Presupuesto (id_presupuesto PK) → PresupuestoDetalle (id_categoria FK)
  ├── Transaccion (id_categoria FK)
  ├── Deuda (id_deuda PK) → PagoDeuda
  └── ResumenAnual

Categoria (id_categoria PK, tipo: INGRESO|AHORRO|GASTO_FIJO|GASTO_VARIABLE|DEUDA)
```

Índices creados en todas las FK (`IX_Presupuesto_Usuario`, `IX_Transaccion_Usuario_Fecha`, etc.) y constraint único en `Presupuesto(id_usuario, anio, mes)` para evitar duplicados.

## Solución de problemas comunes

**El contenedor no arranca / "port 1433 already in use"**
Tienes otra instancia de SQL Server corriendo. Detenla o cambia el puerto en `docker-compose.yml`.

**Error "Cannot open database FinanzasPersonales"**
El script `init.sql` no se ejecutó. Verifica con `docker compose logs db-init`. Si falló, ejecuta `.\stop.ps1 -Volumes` y luego `.\start.ps1`.

**"The Entity Framework provider could not be found"**
Falta instalar el paquete `EntityFramework` vía NuGet (paso 4).

**Error al compilar: "The type or namespace name 'X' could not be found"**
Click derecho al proyecto en Visual Studio → **Restore NuGet Packages**.

**Login no funciona con usuarios viejos**
El script ahora **no** siembra usuarios demo (los hashes no se pueden hardcodear de forma segura). Crea tu cuenta desde `/Account/Register`.

**Quiero resetear todo y empezar limpio**
```powershell
.\stop.ps1 -Volumes
.\start.ps1
```

## Notas de seguridad

⚠️ Este proyecto está pensado para **desarrollo local**. Antes de desplegar a producción deberías:

- Cambiar la contraseña del usuario `sa` por algo robusto y guardarlo en un secret manager.
- Mover la cadena de conexión a Azure Key Vault o variables de entorno.
- Habilitar HTTPS estricto (HSTS, redirect HTTP→HTTPS).
- Activar `<httpCookies requireSSL="true" httpOnlyCookies="true" />` en Web.config.
- Configurar `Tailwind CSS` compilado en lugar del CDN play (que no es para producción).
- Implementar rate limiting en el endpoint de login para prevenir ataques de fuerza bruta.
- Considerar políticas de complejidad de contraseña más estrictas.

## Licencia

Proyecto educativo / personal. Úsalo libremente.
