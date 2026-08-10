-- =============================================================
-- MIGRACION: Soporte de OTP para registro y reset de contrasena
-- Fecha: 2026-05-29
--
-- Cambios:
--   1. Usuario.activo (BIT)              -> default 1 (las cuentas
--      previas quedan activas).
--   2. PasswordResetToken.proposito      -> default 'RESET_PASSWORD'
--      (los tokens viejos siguen siendo de reset).
--   3. Quitar UNIQUE de PasswordResetToken.token, porque ahora son
--      OTP de 6 digitos y dos usuarios pueden tener el mismo codigo
--      por casualidad.
--
-- Es IDEMPOTENTE: si lo ejecutas 2 veces no rompe nada.
-- NO borra datos.
-- =============================================================

PRINT '== Migracion OTP - inicio ==';

-- 1. Columna activo en Usuario
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE Name = 'activo' AND Object_ID = Object_ID('Usuario'))
BEGIN
    ALTER TABLE Usuario ADD activo BIT NOT NULL DEFAULT 1;
    PRINT '  + columna Usuario.activo agregada';
END
ELSE
    PRINT '  = columna Usuario.activo ya existia';
GO

-- 2. Columna proposito en PasswordResetToken
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE Name = 'proposito' AND Object_ID = Object_ID('PasswordResetToken'))
BEGIN
    ALTER TABLE PasswordResetToken ADD proposito VARCHAR(20) NOT NULL DEFAULT 'RESET_PASSWORD';
    PRINT '  + columna PasswordResetToken.proposito agregada';
END
ELSE
    PRINT '  = columna PasswordResetToken.proposito ya existia';
GO

-- 3. Quitar UNIQUE viejo sobre token
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_PwdResetToken')
BEGIN
    ALTER TABLE PasswordResetToken DROP CONSTRAINT UQ_PwdResetToken;
    PRINT '  - UNIQUE UQ_PwdResetToken eliminada';
END
ELSE
    PRINT '  = UNIQUE UQ_PwdResetToken ya no existia';
GO

PRINT '== Migracion OTP - fin ==';
