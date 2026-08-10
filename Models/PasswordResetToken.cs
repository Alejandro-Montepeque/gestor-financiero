using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gestor_financiero.Models
{
    /// <summary>
    /// Codigo OTP (One-Time Password) usado para:
    ///   - REGISTRO        : activar una cuenta recien creada
    ///   - RESET_PASSWORD  : restablecer contrasena olvidada
    ///
    /// El nombre de la tabla quedo como "PasswordResetToken" por
    /// compatibilidad con la migracion anterior, pero el concepto
    /// es generico (un codigo de 6 digitos con vencimiento).
    /// </summary>
    [Table("PasswordResetToken")]
    public class PasswordResetToken
    {
        [Key]
        [Column("id_token")]
        public int IdToken { get; set; }

        [Required]
        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        // Codigo OTP: 6 digitos numericos como string (ej: "048521").
        // Usamos string para preservar ceros a la izquierda.
        [Required]
        [StringLength(64)]
        [Column("token")]
        public string Token { get; set; }

        // Discrimina entre los dos flujos: "REGISTRO" o "RESET_PASSWORD".
        [Required]
        [StringLength(20)]
        [Column("proposito")]
        public string Proposito { get; set; }

        [Required]
        [Column("expiracion")]
        public DateTime Expiracion { get; set; }

        [Required]
        [Column("usado")]
        public bool Usado { get; set; }

        [Required]
        [Column("creado_en")]
        public DateTime CreadoEn { get; set; }

        [ForeignKey("IdUsuario")]
        public virtual Usuario Usuario { get; set; }
    }

    // Constantes para evitar errores de tipeo
    public static class PropositoOtp
    {
        public const string Registro      = "REGISTRO";
        public const string ResetPassword = "RESET_PASSWORD";
    }
}
