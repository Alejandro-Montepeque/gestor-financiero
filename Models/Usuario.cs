using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gestor_financiero.Models
{
    [Table("Usuario")]
    public class Usuario
    {
        [Key]
        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100)]
        [Column("nombre")]
        [Display(Name = "Nombre completo")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "Email no válido")]
        [StringLength(150)]
        [Column("email")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; }

        [Required]
        [StringLength(500)]
        [Column("password_hash")]
        public string PasswordHash { get; set; }

        [Column("fecha_registro")]
        [Display(Name = "Registrado el")]
        public DateTime FechaRegistro { get; set; }

        // Activo = true cuando el usuario completo el flujo de verificacion
        // por OTP. Si Activo = false, NO puede loguearse hasta validar su
        // codigo en /Account/VerifyOtp.
        [Column("activo")]
        public bool Activo { get; set; }

        // Navegación
        public virtual ICollection<Presupuesto> Presupuestos { get; set; }
        public virtual ICollection<Transaccion> Transacciones { get; set; }
        public virtual ICollection<Deuda> Deudas { get; set; }
        public virtual ICollection<ResumenAnual> ResumenesAnuales { get; set; }
    }
}
