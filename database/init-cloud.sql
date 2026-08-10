-- =============================================================
-- Script de inicializacion para BD en HOSTING REMOTO (MonsterASP, Somee, etc.)
--
-- Diferencias con init.sql:
--   - NO crea la base de datos (el hosting ya te dio una vacia)
--   - NO usa "USE FinanzasPersonales" (estas conectado a la BD del hosting)
--   - Es idempotente: podes correrlo varias veces sin romper nada
--
-- COMO EJECUTARLO:
--   Opcion A: SSMS / Azure Data Studio
--     1. Conectate al servidor del hosting con tus credenciales
--     2. Abri este archivo
--     3. F5 (ejecutar)
--
--   Opcion B: myLittleAdmin (panel web del hosting)
--     1. Tools -> SQL Query
--     2. Pega el contenido y ejecuta
-- =============================================================

-- 1. USUARIOS
IF OBJECT_ID('dbo.Usuario', 'U') IS NULL
CREATE TABLE Usuario (
    id_usuario INT IDENTITY(1,1) PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    email VARCHAR(150) NOT NULL,
    password_hash VARCHAR(500) NOT NULL,
    fecha_registro DATETIME NOT NULL DEFAULT GETDATE(),
    activo BIT NOT NULL DEFAULT 1,
    CONSTRAINT UQ_Usuario_Email UNIQUE (email)
);
-- Agregar columna activo si la tabla ya existia sin ella
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = 'activo' AND Object_ID = Object_ID('Usuario'))
    ALTER TABLE Usuario ADD activo BIT NOT NULL DEFAULT 1;

-- 2. CATEGORIAS
IF OBJECT_ID('dbo.Categoria', 'U') IS NULL
CREATE TABLE Categoria (
    id_categoria INT IDENTITY(1,1) PRIMARY KEY,
    nombre VARCHAR(50) NOT NULL,
    tipo VARCHAR(20) NOT NULL
);

-- 3. PRESUPUESTO MENSUAL
IF OBJECT_ID('dbo.Presupuesto', 'U') IS NULL
CREATE TABLE Presupuesto (
    id_presupuesto INT IDENTITY(1,1) PRIMARY KEY,
    id_usuario INT NOT NULL,
    anio INT NOT NULL,
    mes INT NOT NULL,
    CONSTRAINT FK_Presupuesto_Usuario FOREIGN KEY (id_usuario) REFERENCES Usuario(id_usuario),
    CONSTRAINT UQ_Presupuesto_Usuario_Periodo UNIQUE (id_usuario, anio, mes)
);

-- 4. DETALLE DEL PRESUPUESTO
IF OBJECT_ID('dbo.PresupuestoDetalle', 'U') IS NULL
CREATE TABLE PresupuestoDetalle (
    id_detalle INT IDENTITY(1,1) PRIMARY KEY,
    id_presupuesto INT NOT NULL,
    id_categoria INT NOT NULL,
    estimado DECIMAL(12,2) DEFAULT 0,
    real DECIMAL(12,2) DEFAULT 0,
    CONSTRAINT FK_PresupuestoDetalle_Presupuesto FOREIGN KEY (id_presupuesto) REFERENCES Presupuesto(id_presupuesto),
    CONSTRAINT FK_PresupuestoDetalle_Categoria FOREIGN KEY (id_categoria) REFERENCES Categoria(id_categoria)
);

-- 5. TRANSACCIONES
IF OBJECT_ID('dbo.Transaccion', 'U') IS NULL
CREATE TABLE Transaccion (
    id_transaccion INT IDENTITY(1,1) PRIMARY KEY,
    id_usuario INT NOT NULL,
    id_categoria INT NOT NULL,
    monto DECIMAL(12,2) NOT NULL,
    fecha DATE NOT NULL,
    notas VARCHAR(255),
    CONSTRAINT FK_Transaccion_Usuario FOREIGN KEY (id_usuario) REFERENCES Usuario(id_usuario),
    CONSTRAINT FK_Transaccion_Categoria FOREIGN KEY (id_categoria) REFERENCES Categoria(id_categoria)
);

-- 6. DEUDAS
IF OBJECT_ID('dbo.Deuda', 'U') IS NULL
CREATE TABLE Deuda (
    id_deuda INT IDENTITY(1,1) PRIMARY KEY,
    id_usuario INT NOT NULL,
    nombre VARCHAR(100) NOT NULL,
    saldo_actual DECIMAL(12,2) NOT NULL,
    tasa_interes DECIMAL(5,2),
    pago_minimo DECIMAL(12,2),
    notas VARCHAR(255),
    CONSTRAINT FK_Deuda_Usuario FOREIGN KEY (id_usuario) REFERENCES Usuario(id_usuario)
);

-- 7. PAGOS DE DEUDA
IF OBJECT_ID('dbo.PagoDeuda', 'U') IS NULL
CREATE TABLE PagoDeuda (
    id_pago INT IDENTITY(1,1) PRIMARY KEY,
    id_deuda INT NOT NULL,
    fecha DATE NOT NULL,
    pago_minimo DECIMAL(12,2) DEFAULT 0,
    pago_extra DECIMAL(12,2) DEFAULT 0,
    interes DECIMAL(12,2) DEFAULT 0,
    capital DECIMAL(12,2) DEFAULT 0,
    saldo_restante DECIMAL(12,2),
    CONSTRAINT FK_PagoDeuda_Deuda FOREIGN KEY (id_deuda) REFERENCES Deuda(id_deuda)
);

-- 8. RESUMEN ANUAL
IF OBJECT_ID('dbo.ResumenAnual', 'U') IS NULL
CREATE TABLE ResumenAnual (
    id_resumen INT IDENTITY(1,1) PRIMARY KEY,
    id_usuario INT NOT NULL,
    anio INT NOT NULL,
    ingresos DECIMAL(12,2),
    ahorros DECIMAL(12,2),
    gastos_fijos DECIMAL(12,2),
    gastos_variables DECIMAL(12,2),
    deudas DECIMAL(12,2),
    CONSTRAINT FK_ResumenAnual_Usuario FOREIGN KEY (id_usuario) REFERENCES Usuario(id_usuario)
);

-- 9. CODIGOS OTP (recuperacion de contrasena + activacion de cuenta)
IF OBJECT_ID('dbo.PasswordResetToken', 'U') IS NULL
CREATE TABLE PasswordResetToken (
    id_token INT IDENTITY(1,1) PRIMARY KEY,
    id_usuario INT NOT NULL,
    token VARCHAR(64) NOT NULL,
    proposito VARCHAR(20) NOT NULL DEFAULT 'RESET_PASSWORD',
    expiracion DATETIME NOT NULL,
    usado BIT NOT NULL DEFAULT 0,
    creado_en DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_PwdResetToken_Usuario FOREIGN KEY (id_usuario) REFERENCES Usuario(id_usuario)
);
-- Si la tabla ya existia sin la columna proposito, agregarla
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = 'proposito' AND Object_ID = Object_ID('PasswordResetToken'))
    ALTER TABLE PasswordResetToken ADD proposito VARCHAR(20) NOT NULL DEFAULT 'RESET_PASSWORD';
-- Si existia el UNIQUE en token (cuando era GUID), lo eliminamos porque ahora
-- el "token" es un OTP de 6 digitos que se reusa entre distintos usuarios.
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_PwdResetToken')
    ALTER TABLE PasswordResetToken DROP CONSTRAINT UQ_PwdResetToken;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PwdResetToken_Usuario')
    CREATE INDEX IX_PwdResetToken_Usuario ON PasswordResetToken(id_usuario);
GO

-- Indices de rendimiento
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Presupuesto_Usuario')
    CREATE INDEX IX_Presupuesto_Usuario ON Presupuesto(id_usuario);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PresupuestoDetalle_Presupuesto')
    CREATE INDEX IX_PresupuestoDetalle_Presupuesto ON PresupuestoDetalle(id_presupuesto);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Transaccion_Usuario_Fecha')
    CREATE INDEX IX_Transaccion_Usuario_Fecha ON Transaccion(id_usuario, fecha);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Deuda_Usuario')
    CREATE INDEX IX_Deuda_Usuario ON Deuda(id_usuario);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PagoDeuda_Deuda')
    CREATE INDEX IX_PagoDeuda_Deuda ON PagoDeuda(id_deuda);
GO

-- Semilla de categorias (idempotente)
IF NOT EXISTS (SELECT 1 FROM Categoria)
BEGIN
    INSERT INTO Categoria (nombre, tipo) VALUES
    ('Salario Principal', 'INGRESO'),
    ('Freelance', 'INGRESO'),
    ('Fondo de Emergencia', 'AHORRO'),
    ('Inversiones', 'AHORRO'),
    ('Alquiler / Hipoteca', 'GASTO_FIJO'),
    ('Servicios (Agua, Luz, Internet)', 'GASTO_FIJO'),
    ('Supermercado', 'GASTO_VARIABLE'),
    ('Entretenimiento', 'GASTO_VARIABLE'),
    ('Tarjeta de Credito', 'DEUDA'),
    ('Prestamo Automotriz', 'DEUDA');
END
GO

PRINT 'BD inicializada. El primer usuario lo crea el DataSeeder al arrancar la app, o registralo via /Account/Register.';
