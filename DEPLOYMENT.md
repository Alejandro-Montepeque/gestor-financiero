# Guía de Deployment — Gestor Financiero

Cómo desplegar la aplicación y la base de datos **gratis** a hosting público usando **MonsterASP.NET**.

## Resumen del flujo

```
┌────────────────────────────────┐
│  Tu PC (Visual Studio)         │
│  - npm run build (Tailwind)    │
│  - .\publish.ps1               │
│  - Genera carpeta \publish\    │
└─────────────┬──────────────────┘
              │ FTP
              ▼
┌────────────────────────────────┐         ┌────────────────────────────┐
│  MonsterASP — App Hosting      │ ──────▶ │  MonsterASP — SQL Server   │
│  https://tuapp.monsterasp.net  │  TCP    │  mssql.monsterasp.net      │
│  IIS + .NET Framework 4.7.2    │  1433   │  150 MB MS SQL gratis      │
└────────────────────────────────┘         └────────────────────────────┘
```

Todo gratis, sin tarjeta de crédito.

## Paso 1: Crear cuenta y recursos en MonsterASP

1. Ir a https://www.monsterasp.net/Account/Register
2. Registrarse (verificar email)
3. En el panel:
   - Click **Add New Site**:
     - Site Name: `gestorfinanciero` (tu subdominio: `https://gestorfinanciero.monsterasp.net`)
     - ASP.NET version: 4.x
   - Click **Add New Database**:
     - Type: MSSQL
     - Name: `FinanzasPersonales`
     - Password: una fuerte (anotala)

Al terminar el panel te muestra:

| Dato | Ubicación en el panel |
|---|---|
| FTP host, user, password | Sitio → **FTP Account** |
| SQL host, user, password | Database → **Manage** |

## Paso 2: Inicializar la base de datos remota

Conectarte a MonsterASP SQL desde tu PC con SSMS o Azure Data Studio:

- **Server**: el host que aparece en el panel (ej: `mssql.monsterasp.net,1433`)
- **Authentication**: SQL Server
- **Login**: el usuario que te dio MonsterASP
- **Password**: la que pusiste

Una vez conectado, abrí el archivo `database/init-cloud.sql` (que ya no tiene `CREATE DATABASE`) y ejecutalo (F5). Debería decir "BD inicializada".

**Alternativa sin SSMS:** En el panel de MonsterASP click en **Manage Database** → se abre myLittleAdmin web → Tools → SQL Query → pegá `init-cloud.sql` y ejecutá.

## Paso 3: Configurar credenciales de deploy

1. En tu PC, en la raíz del proyecto:
   ```powershell
   Copy-Item deploy.env.example deploy.env
   notepad deploy.env
   ```
2. Llenar con tus datos reales de MonsterASP:
   ```
   DB_HOST=mssql.monsterasp.net
   DB_NAME=DB_xxxxxx_FinanzasPersonales
   DB_USER=DB_xxxxxx_FinanzasPersonales
   DB_PASSWORD=TuPasswordReal!

   FTP_HOST=ftp.monsterasp.net
   FTP_USER=tu_usuario_ftp
   FTP_PASSWORD=tu_password_ftp
   FTP_REMOTE_PATH=/

   APP_URL=https://gestorfinanciero.monsterasp.net
   ```

El archivo `deploy.env` está en `.gitignore`, no se sube al repo.

## Paso 4: Generar el paquete de deploy

```powershell
.\publish.ps1
```

Esto:
1. Compila Tailwind, copia assets de npm
2. Inyecta tu cadena de conexión en `Web.Release.config`
3. Llama a MSBuild para compilar el proyecto en Release
4. Genera la carpeta `.\publish\` con TODO listo para subir

Verás al final algo como:
```
============================================================
  LISTO. Archivos en: .\publish\
============================================================
```

## Paso 5: Subir vía FTP

Usá **FileZilla** (gratis) o el cliente FTP que prefieras:

1. Conectate con los datos FTP de MonsterASP
2. En el lado local navegá a la carpeta `publish\`
3. En el lado remoto navegá a la carpeta raíz (`/` o `/wwwroot/` según MonsterASP)
4. Seleccioná todo el contenido de `publish\` y arrastrá al servidor
5. Esperá a que termine (5-10 min la primera vez)

**Alternativa con Visual Studio:** Click derecho al proyecto → Publish → FTP → llenar datos → Publish. Sube directo sin pasar por `\publish\`.

## Paso 6: Verificar

Abrí `https://gestorfinanciero.monsterasp.net` (con tu subdominio real).

Deberías ver:
- Pantalla de login (lo que indica que la app cargó)
- Estilos correctos (Tailwind funcionando)
- Iconos visibles (lucide funcionando)

**Login de prueba:**
- El primer arranque de la app ejecuta `DataSeeder.Seed()` que crea el usuario `demo@gestor.com` / `Demo2026!`
- Si no funciona, registrá uno nuevo en `/Account/Register`

## Problemas comunes

**"500 Internal Server Error" sin más info**

Subí temporalmente este `Web.config` que muestra el error real:
```xml
<system.web>
  <customErrors mode="Off" />
</system.web>
```
Recargá, vas a ver el stack trace. Cuando lo arregles, quitalo.

**"Cannot connect to database"**

Verificá:
- Connection string en `Web.config` apunta al host correcto de MonsterASP
- El usuario/password son correctos
- En MonsterASP, la BD esté en estado "Online"

**Iconos o CSS no cargan**

Por FTP confirmá que existan estos archivos en el servidor:
- `/Content/css/app.css`
- `/Content/css/lucide.css`
- `/Content/css/lucide.woff2`
- `/Scripts/chart.umd.min.js`

Si faltan, es porque `publish.ps1` no los empaquetó. Verificá que corriste `npm run build` antes.

**"The Entity Framework provider could not be found"**

Falta `EntityFramework.SqlServer.dll` en `/bin/`. Asegurate que el paquete NuGet `EntityFramework` esté instalado en Visual Studio antes de publicar.

**La cuenta se suspendió**

MonsterASP suspende cuentas sin actividad por 30 días. Login al panel y la reactivás con un click.

## Limitaciones del plan gratuito de MonsterASP

| Recurso | Límite |
|---|---|
| Storage | 1 GB |
| Base de datos SQL | ~150 MB |
| Ancho de banda mensual | 50 GB |
| Apps concurrentes | 1 |
| Custom domain | No (solo subdominio) |
| Siempre online | Sí (no se duerme) |
| Soporte | Solo foro de comunidad |

Si necesitás más, los planes pagos arrancan en ~$3/mes.

## Cuando termine la demo

No hace falta hacer nada — el plan gratuito sigue activo. Si querés borrar todo, login al panel y eliminá el sitio y la BD.
