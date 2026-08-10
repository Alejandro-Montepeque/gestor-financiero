-- =============================================================
-- MIGRACION: Crear tabla PasswordResetToken si no existe.
-- Fecha: 2026-05-28 (antes de la migracion del OTP)
--
-- Contexto:
--   - En entornos creados ANTES de tener soporte de OTP, esta
--     tabla podia no existir. La migracion del OTP del 2026-05-29
--     intenta agregarle una columna 'proposito' y falla porque
--     la tabla no estaba.
--   - Esta migracion crea la tabla con TODAS sus columnas finales
--     (incluyendo 'proposito') para que la del 2026-05-29 quede
--     como no-op cuando se aplique despues.
--
-- Es IDEMPOTENTE.
-- =============================================================

PRINT '== Crear PasswordResetToken (si no existe) - inicio ==';

IF OBJECT_ID('dbo.PasswordResetToken', 'U') IS NULL
BEGIN
    CREATE TABLE PasswordResetToken (
        id_token INT IDENTITY(1,1) PRIMARY KEY,
        id_usuario INT NOT NULL,
        token VARCHAR(64) NOT NULL,
        proposito VARCHAR(20) NOT NULL DEFAULT 'RESET_PASSWORD',
        expiracion DATETIME NOT NULL,
        usado BIT NOT NULL DEFAULT 0,
        creado_en DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_PwdResetToken_Usuario FOREIGN KEY (id_usuario)
            REFERENCES Usuario(id_usuario)
    );
    PRINT '  + tabla PasswordResetToken creada';
END
ELSE
    PRINT '  = tabla PasswordResetToken ya existia';
GO

-- Indice por id_usuario (para los WHERE en login/reset)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PwdResetToken_Usuario')
BEGIN
    CREATE INDEX IX_PwdResetToken_Usuario ON PasswordResetToken(id_usuario);
    PRINT '  + indice IX_PwdResetToken_Usuario creado';
END
ELSE
    PRINT '  = indice IX_PwdResetToken_Usuario ya existia';
GO

PRINT '== Crear PasswordResetToken - fin ==';
