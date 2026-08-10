using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gestor_financiero.Models
{
    [Table("Presupuesto")]
    public class Presupuesto
    {
        [Key]
        [Column("id_presupuesto")]
        public int IdPresupuesto { get; set; }

        [Required]
        [Column("id_usuario")]
        [Display(Name = "Usuario")]
        public int IdUsuario { get; set; }

        [Required]
        [Range(2000, 2100)]
        [Column("anio")]
        [Display(Name = "Año")]
        public int Anio { get; set; }

        [Required]
        [Range(1, 12)]
        [Column("mes")]
        [Display(Name = "Mes")]
        public int Mes { get; set; }

        // Navegación
        [ForeignKey("IdUsuario")]
        public virtual Usuario Usuario { get; set; }

        public virtual ICollection<PresupuestoDetalle> Detalles { get; set; }
    }
}
