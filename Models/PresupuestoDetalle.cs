using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gestor_financiero.Models
{
    [Table("PresupuestoDetalle")]
    public class PresupuestoDetalle
    {
        [Key]
        [Column("id_detalle")]
        public int IdDetalle { get; set; }

        [Required]
        [Column("id_presupuesto")]
        [Display(Name = "Presupuesto")]
        public int IdPresupuesto { get; set; }

        [Required]
        [Column("id_categoria")]
        [Display(Name = "Categoría")]
        public int IdCategoria { get; set; }

        [Column("estimado", TypeName = "decimal")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        [Display(Name = "Estimado")]
        public decimal Estimado { get; set; }

        // OJO: "real" es palabra reservada en T-SQL y choca con tipos en C#,
        // por eso la propiedad se llama 'Real' y se mapea explícitamente.
        [Column("real", TypeName = "decimal")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        [Display(Name = "Real")]
        public decimal Real { get; set; }

        // Navegación
        [ForeignKey("IdPresupuesto")]
        public virtual Presupuesto Presupuesto { get; set; }

        [ForeignKey("IdCategoria")]
        public virtual Categoria Categoria { get; set; }

        // Helper calculado (no se mapea a BD)
        [NotMapped]
        [Display(Name = "Diferencia")]
        public decimal Diferencia => Estimado - Real;
    }
}
